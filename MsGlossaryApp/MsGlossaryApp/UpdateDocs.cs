using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using MsGlossaryApp.DataModel;
using MsGlossaryApp.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace MsGlossaryApp
{
    public static class UpdateDocs
    {
        private const string CommitMessage = "New files committed by the pipeline";

        [FunctionName("UpdateDocs_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "get",
                Route = "update-docs")]
            HttpRequestMessage req,
            [DurableClient]
            IDurableOrchestrationClient starter,
            ILogger log)
        {
            await NotificationService.Notify(
                "Trigger received",
                "Starting orchestration",
                log);

            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync(nameof(UpdateDocsRunOrchestrator), null);

            log.LogInformation($"Started orchestration in UpdateDocs with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName(nameof(UpdateDocsCommitFiles))]
        public static async Task<string> UpdateDocsCommitFiles(
            [ActivityTrigger]
            IList<GlossaryFile> files,
            ILogger log)
        {
            log?.LogInformation("In UpdateDocsCommitFiles");
            //return null;

            var accountName = Environment.GetEnvironmentVariable(
                Constants.DocsGlossaryGitHubAccountVariableName);
            var repoName = Environment.GetEnvironmentVariable(
                Constants.DocsGlossaryGitHubRepoVariableName);
            var branchName = Environment.GetEnvironmentVariable(
                Constants.DocsGlossaryGitHubMainBranchNameVariableName);
            var token = Environment.GetEnvironmentVariable(
                Constants.GitHubTokenVariableName);

            log.LogDebug($"accountName: {accountName}");
            log.LogDebug($"repoName: {repoName}");
            log.LogDebug($"branchName: {branchName}");
            log.LogDebug($"token: {token}");

            return await FileSaver.SaveFiles(
                accountName,
                repoName,
                branchName,
                token,
                files,
                CommitMessage,
                log: log);
        }

        [FunctionName(nameof(UpdateDocsGetAllTerms))]
        public static async Task<List<string>> UpdateDocsGetAllTerms(
            [ActivityTrigger]
            ILogger log)
        {
            var account = CloudStorageAccount.Parse(
                Environment.GetEnvironmentVariable(
                    Constants.AzureWebJobsStorageVariableName));

            var blobClient = account.CreateCloudBlobClient();
            var blobHelper = new BlobHelper(blobClient, log);
            var termsContainer = blobHelper.GetContainerFromVariable(
                Constants.TermsContainerVariableName);

            BlobContinuationToken continuationToken = null;
            var terms = new List<string>();

            do
            {
                var response = await termsContainer.ListBlobsSegmentedAsync(continuationToken);
                continuationToken = response.ContinuationToken;

                foreach (CloudBlockBlob blob in response.Results)
                {
                    log?.LogDebug($"Found: {blob.Name}");
                    terms.Add(blob.Uri.ToString());
                }
            }
            while (continuationToken != null);

            return terms;
        }

        [FunctionName(nameof(UpdateDocsMakeDisambiguation))]
        public static async Task<GlossaryFile> UpdateDocsMakeDisambiguation(
            [ActivityTrigger]
            IList<Keyword> keywords,
            ILogger log)
        {
            var file = await TermMaker.CreateDisambiguationFile(keywords, log);
            return file;
        }

        [FunctionName(nameof(UpdateDocsMakeMarkdown))]
        public static async Task<GlossaryFile> UpdateDocsMakeMarkdown(
            [ActivityTrigger]
            Keyword keyword,
            ILogger log)
        {
            var file = await TermMaker.CreateKeywordFile(keyword, log);
            return file;
        }

        [FunctionName(nameof(UpdateDocsParseTerm))]
        public static async Task<Term> UpdateDocsParseTerm(
            [ActivityTrigger]
            Uri termUri,
            ILogger log)
        {
            Term term;

            try
            {
                var termBlob = new CloudBlockBlob(termUri);
                string markdown = await termBlob.DownloadTextAsync();
                term = TermMaker.ParseTerm(termUri, markdown, log);

                var results = new List<ValidationResult>();
                var isValid = term.TryValidate(results);

                if (!isValid)
                {
                    await NotificationService.Notify(
                        "Incomplete term",
                        $"The term {term.FileName} was queued for parsing but is incomplete",
                        log);

                    log?.LogError($"Incomplete term {term.FileName}");

                    foreach (var result in results)
                    {
                        log?.LogError(result.ErrorMessage);
                    }

                    return null;
                }
            }
            catch (Exception ex)
            {
                await NotificationService.Notify(
                    "Faulty term",
                    $"The term {termUri} was queued for parsing but is incomplete",
                    log);

                log?.LogError($"Error with term {termUri}: {ex.Message}");
                return null;
            }

            return term;
        }

        [FunctionName(nameof(UpdateDocsReplaceKeywords))]
        public static async Task<Term> UpdateDocsReplaceKeywords(
            [ActivityTrigger]
            (List<Keyword> keywordsToReplace, Term currentTerm) input,
            ILogger log)
        {
            var oldTranscript = input.currentTerm.GetTranscriptMarkdown();

            var newTranscript = await KeywordReplacer.Replace(
                oldTranscript,
                input.keywordsToReplace,
                log);

            if (newTranscript != oldTranscript)
            {
                input.currentTerm.SetTranscriptMarkdown(newTranscript);
                input.currentTerm.MustSave = true;
            }

            return input.currentTerm;
        }

        [FunctionName(nameof(UpdateDocsRunOrchestrator))]
        public static async Task UpdateDocsRunOrchestrator(
            [OrchestrationTrigger]
            IDurableOrchestrationContext context)
        {
            var allTermsUrls = await context.CallActivityAsync<List<string>>(
                nameof(UpdateDocsGetAllTerms),
                null);

            var allTermsTasks = new List<Task<Term>>();

            foreach (var termUrl in allTermsUrls)
            {
                //var termUrl = allTermsUrls.First(u => u.Contains("/test.en.md"));

                allTermsTasks.Add(context.CallActivityAsync<Term>(
                    nameof(UpdateDocsParseTerm),
                    new Uri(termUrl)));
            }

            var allTerms = (await Task.WhenAll(allTermsTasks))
                .Where(t => t != null);

            await context.CallActivityAsync<Term>(
                nameof(UpdateDocsSaveTermsToSettings),
                allTerms);

            var allKeywordsTasks = new List<Task<IList<Keyword>>>();

            foreach (var term in allTerms)
            {
                //var term = allTerms.First();

                allKeywordsTasks.Add(context.CallActivityAsync<IList<Keyword>>(
                    nameof(UpdateDocsSortKeywords),
                    (allTerms, term)));
            }

            var allKeywords = (await Task.WhenAll(allKeywordsTasks))
                .SelectMany(i => i);

            allKeywords = await context.CallActivityAsync<IList<Keyword>>(
                nameof(UpdateDocsSortDisambiguations),
                allKeywords);

            var replaceKeywordsTasks = new List<Task<Term>>();

            foreach (var term in allTerms)
            {
                //var term = allTerms.First(t => t.FileName == "aad");

                var keywordsToReplace = allKeywords
                    .Where(k =>
                        k.TermSafeFileName != term.FileName
                        && !k.MustDisambiguate)
                    .ToList();

                if (keywordsToReplace.Count > 0)
                {
                    replaceKeywordsTasks.Add(context.CallActivityAsync<Term>(
                        nameof(UpdateDocsReplaceKeywords),
                        (keywordsToReplace, term)));
                }
            }

            allTerms = await Task.WhenAll(replaceKeywordsTasks);

            var filesCreationTasks = new List<Task<GlossaryFile>>();

            foreach (var keyword in allKeywords.Where(k => !k.IsDisambiguation))
            {
                //var keyword = allKeywords.First(k => k.KeywordName.MakeSafeFileName() == "node-js"
                //    && k.TermSafeFileName == "app-service");

                var currentTerm = allTerms
                    .Single(t => t.FileName == keyword.TermSafeFileName);

                keyword.Term = currentTerm;

                filesCreationTasks.Add(context.CallActivityAsync<GlossaryFile>(
                    nameof(UpdateDocsMakeMarkdown),
                    keyword));
            }

            var filesToVerify = (await Task.WhenAll(filesCreationTasks))
                .ToList();

            var errors = filesToVerify
                .Where(f => !string.IsNullOrEmpty(f.ErrorMessage))
                .Select(f => f.ErrorMessage)
                .ToList();

            if (errors.Count > 0)
            {
                foreach (var e in errors)
                {
                    await NotificationService.Notify(
                        "ERROR when updating terms",
                        e,
                        null);
                }

                return;
            }

            // Save the disambiguation

            var keywordsGroups = allKeywords
                .Where(k => k.MustDisambiguate)
                .GroupBy(k => k.KeywordName.ToLower());

            var disambiguationTasks = new List<Task<GlossaryFile>>();

            foreach (var group in keywordsGroups)
            {
                //var group = keywordsGroups.First();

                foreach (var keyword in group)
                {
                    var currentTerm = allTerms
                        .Single(testc => testc.FileName == keyword.TermSafeFileName);

                    keyword.Term = currentTerm;
                }

                disambiguationTasks.Add(context.CallActivityAsync<GlossaryFile>(
                    nameof(UpdateDocsMakeDisambiguation),
                    group.ToList()));
            }

            filesToVerify.AddRange(await Task.WhenAll(disambiguationTasks));

            errors = filesToVerify
                .Where(f => !string.IsNullOrEmpty(f.ErrorMessage))
                .Select(f => f.ErrorMessage)
                .ToList();

            if (errors.Count > 0)
            {
                foreach (var e in errors)
                {
                    await NotificationService.Notify(
                        "ERROR when updating disambiguations",
                        e,
                        null);
                }

                return;
            }

            // Create the TOC

            var toc = await context.CallActivityAsync<GlossaryFile>(
                nameof(UpdateDocsUpdateTableOfContents),
                allKeywords);

            if (!string.IsNullOrEmpty(toc.ErrorMessage))
            {
                await NotificationService.Notify(
                    "ERROR when updating TOC",
                    toc.ErrorMessage,
                    null);
                return;
            }

            filesToVerify.Add(toc);

            var verifyTasks = new List<Task<GlossaryFile>>();

            foreach (var file in filesToVerify)
            {
                //var file = filesToSave.First();

                verifyTasks.Add(context.CallActivityAsync<GlossaryFile>(
                    nameof(UpdateDocsVerifyFiles),
                    file));
            }

            var filesToSave = (await Task.WhenAll(verifyTasks))
                .ToList();

            var error = await context.CallActivityAsync<string>(
                nameof(UpdateDocsCommitFiles),
                filesToSave);

            if (!string.IsNullOrEmpty(error))
            {
                await NotificationService.Notify(
                    "ATTENTION Files not committed",
                    error,
                    null);

                return;
            }

            var message = "New files committed";
            var savingLocation = FileSaver.GetSavingLocation();

            switch (savingLocation)
            {
                case SavingLocations.Both:
                    message += " and saved to storage";
                    break;

                case SavingLocations.Storage:
                    message = "New files saved to storage";
                    break;
            }

            message += " without errors to the Docs repository (UpdateDocs)";

            await NotificationService.Notify(
                "New files saved to Docs repo",
                message,
                null);
        }

        [FunctionName(nameof(UpdateDocsSaveTermsToSettings))]
        public static async Task UpdateDocsSaveTermsToSettings(
            [ActivityTrigger]
            IList<Term> terms,
            ILogger log)
        {
            log?.LogInformation("In UpdateDocsSaveTermsToSettings");

            var account = CloudStorageAccount.Parse(
                Environment.GetEnvironmentVariable(
                    Constants.AzureWebJobsStorageVariableName));

            var blobClient = account.CreateCloudBlobClient();
            var blobHelper = new BlobHelper(blobClient, log);
            var settingsContainer = blobHelper.GetContainerFromVariable(
                Constants.SettingsContainerVariableName);

            var blob = settingsContainer.GetBlockBlobReference(Constants.TermsSettingsFileName);

            var termsNames = terms.Select(t => t.FileName).ToList();

            var json = JsonConvert.SerializeObject(termsNames);
            log?.LogDebug($"json: {json}");

            await blob.UploadTextAsync(json);
            log?.LogInformation("Out UpdateDocsSaveTermsToSettings");
        }

        [FunctionName(nameof(UpdateDocsSortDisambiguations))]
        public static async Task<IList<Keyword>> UpdateDocsSortDisambiguations(
            [ActivityTrigger]
            IList<Keyword> keywords,
            ILogger log)
        {
            return await TermMaker.SortDisambiguations(keywords, log);
        }

        [FunctionName(nameof(UpdateDocsSortKeywords))]
        public static async Task<IList<Keyword>> UpdateDocsSortKeywords(
            [ActivityTrigger]
            (IList<Term> allTerms, Term currentTerm) input,
            ILogger log)
        {
            return await TermMaker.SortKeywords(input.allTerms, input.currentTerm, log);
        }

        [FunctionName(nameof(UpdateDocsUpdateTableOfContents))]
        public static async Task<GlossaryFile> UpdateDocsUpdateTableOfContents(
            [ActivityTrigger]
            IList<Keyword> keywords,
            ILogger log)
        {
            return await TermMaker.CreateTableOfContentsFile(keywords, log);
        }

        [FunctionName(nameof(UpdateDocsVerifyFiles))]
        public static async Task<GlossaryFile> UpdateDocsVerifyFiles(
            [ActivityTrigger]
            GlossaryFile file,
            ILogger log)
        {
            return await TermMaker.VerifyFile(file, log);
        }
    }
}