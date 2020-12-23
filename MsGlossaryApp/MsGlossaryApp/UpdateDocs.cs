using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using MsGlossaryApp.DataModel;
using MsGlossaryApp.Model;
using MsGlossaryApp.Model.GitHub;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace MsGlossaryApp
{
    public static class UpdateDocs
    {
        private const string CommitMessage = "New files committed by the pipeline";

        private static SavingLocations GetSavingLocation()
        {
            SavingLocations savingLocation = SavingLocations.GitHub;

            var savingLocationString = Environment.GetEnvironmentVariable(
                Constants.SavingLocationVariableName);

            if (!string.IsNullOrEmpty(savingLocationString))
            {
                var success = Enum.TryParse(
                    savingLocationString,
                    out savingLocation);

                if (!success)
                {
                    savingLocation = SavingLocations.GitHub;
                }
            }

            return savingLocation;
        }

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

            log.LogInformationEx($"Started orchestration in UpdateDocs with ID = '{instanceId}'.", LogVerbosity.Normal);

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName(nameof(UpdateDocsCommitFiles))]
        public static async Task<string> UpdateDocsCommitFiles(
            [ActivityTrigger]
            IList<GlossaryFile> files,
            ILogger log)
        {
            log?.LogInformationEx("In UpdateDocsCommitFiles", LogVerbosity.Normal);
            //return null;

            var savingLocation = GetSavingLocation();

            var filesToCommit = files
                .Where(f => f.MustSave)
                .ToList();

            if (savingLocation == SavingLocations.GitHub
                && filesToCommit.Count == 0)
            {
                return "No changes detected in the files to commit";
            }

            string errorMessage = null;

            if ((savingLocation == SavingLocations.GitHub
                    || savingLocation == SavingLocations.Both)
                && filesToCommit.Count > 0)
            {
                log?.LogInformationEx("Committing to GitHub", LogVerbosity.Verbose);

                var accountName = Environment.GetEnvironmentVariable(
                    Constants.DocsGlossaryGitHubAccountVariableName);
                var repoName = Environment.GetEnvironmentVariable(
                    Constants.DocsGlossaryGitHubRepoVariableName);
                var branchName = Environment.GetEnvironmentVariable(
                    Constants.DocsGlossaryGitHubMainBranchNameVariableName);
                var token = Environment.GetEnvironmentVariable(
                    Constants.GitHubTokenVariableName);

                log?.LogInformationEx($"accountName: {accountName}", LogVerbosity.Debug);
                log?.LogInformationEx($"repoName: {repoName}", LogVerbosity.Debug);
                log?.LogInformationEx($"branchName: {branchName}", LogVerbosity.Debug);
                log?.LogInformationEx($"token: {token}", LogVerbosity.Debug);

                // Commit only files who have changed
                var commitContent = filesToCommit
                    .Select(f => (f.Path, f.Content))
                    .ToList();

                var helper = new GitHubHelper();

                var result = await helper.CommitFiles(
                    accountName,
                    repoName,
                    branchName,
                    token,
                    CommitMessage,
                    commitContent,
                    log: log);

                errorMessage = result.ErrorMessage;
                log?.LogInformationEx("Done committing to GitHub", LogVerbosity.Verbose);
            }

            if (!string.IsNullOrEmpty(errorMessage))
            {
                log?.LogError($"Error when committing files: {errorMessage}");
                return errorMessage;
            }

            if ((savingLocation == SavingLocations.Storage
                    || savingLocation == SavingLocations.Both)
                && files.Count > 0)
            {
                log?.LogInformationEx("Saving to storage", LogVerbosity.Verbose);

                try
                {
                    var account = CloudStorageAccount.Parse(
                        Environment.GetEnvironmentVariable(Constants.AzureWebJobsStorageVariableName));

                    var client = account.CreateCloudBlobClient();
                    var helper = new BlobHelper(client, log);
                    var targetContainer = helper.GetContainerFromVariable(Constants.OutputContainerVariableName);

                    // Always save all files
                    foreach (var file in files)
                    {
                        var name = file.Path.Replace("/", "_");
                        var targetBlob = targetContainer.GetBlockBlobReference(name);
                        await targetBlob.UploadTextAsync(file.Content);
                        log?.LogInformationEx($"Uploaded {name} to storage", LogVerbosity.Debug);
                    }
                }
                catch (Exception ex)
                {
                    log?.LogError($"Error when committing files: {ex.Message}");
                    return ex.Message;
                }

                log?.LogInformationEx("Done saving to storage", LogVerbosity.Verbose);
            }

            log?.LogInformationEx("Out UpdateDocsCommitFiles", LogVerbosity.Normal);
            return null;
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
                    log?.LogInformationEx($"Found: {blob.Name}", LogVerbosity.Debug);
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
            Term term = null;

            try
            {
                term = await TermMaker.CreateTerm(termUri, log);
            }
            catch (Exception ex)
            {
                log?.LogError($"Error with term {termUri}: {ex.Message}");
            }

            return term;
        }

        [FunctionName(nameof(UpdateDocsReplaceKeywords))]
        public static async Task<Term> UpdateDocsReplaceKeywords(
            [ActivityTrigger]
            (List<Keyword> keywordsToReplace, Term currentTerm) input,
            ILogger log)
        {
            var newTranscript = await KeywordReplacer.Replace(
                input.currentTerm.Transcript,
                input.keywordsToReplace,
                log);

            if (newTranscript != input.currentTerm.Transcript)
            {
                input.currentTerm.Transcript = newTranscript;
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
                //var termUrl = allTermsUrls.First();

                allTermsTasks.Add(context.CallActivityAsync<Term>(
                    nameof(UpdateDocsParseTerm),
                    new Uri(termUrl)));
            }

            var allTerms = await Task.WhenAll(allTermsTasks);

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
                // var term = allTerms.First(t => t.TermName == "aad");

                var keywordsToReplace = allKeywords
                    .Where(k =>
                        k.TermName != term.TermName
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
                //var keyword = allKeywords.First(k => k.Keyword.MakeSafeFileName() == "node-js"
                //    && k.TermName == "app-service");

                var currentTerm = allTerms
                    .Single(testc => testc.TermName == keyword.TermName);

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
                        .Single(testc => testc.TermName == keyword.TermName);

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

            //string error = null;
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
            var savingLocation = GetSavingLocation();

            switch (savingLocation)
            {
                case SavingLocations.Both:
                    message += " and saved to storage";
                    break;

                case SavingLocations.Storage:
                    message = "New files saved to storage";
                    break;
            }

            message += " without errors";

            await NotificationService.Notify(
                "New files saved",
                message,
                null);
        }

        [FunctionName(nameof(UpdateDocsSaveTermsToSettings))]
        public static async Task UpdateDocsSaveTermsToSettings(
            [ActivityTrigger]
            IList<Term> terms,
            ILogger log)
        {
            log?.LogInformationEx("In UpdateDocsSaveTermsToSettings", LogVerbosity.Normal);

            var account = CloudStorageAccount.Parse(
                Environment.GetEnvironmentVariable(
                    Constants.AzureWebJobsStorageVariableName));

            var blobClient = account.CreateCloudBlobClient();
            var blobHelper = new BlobHelper(blobClient, log);
            var settingsContainer = blobHelper.GetContainerFromVariable(
                Constants.SettingsContainerVariableName);

            var blob = settingsContainer.GetBlockBlobReference(Constants.TermsSettingsFileName);

            var termsNames = terms.Select(t => t.TermName).ToList();

            var json = JsonConvert.SerializeObject(termsNames);
            log?.LogInformationEx($"json: {json}", LogVerbosity.Debug);

            await blob.UploadTextAsync(json);
            log?.LogInformationEx("Out UpdateDocsSaveTermsToSettings", LogVerbosity.Normal);
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
            return await TermMaker.VerifyFile(file);
        }
    }
}