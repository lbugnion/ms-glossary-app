using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using MsGlossaryApp.Model;
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
        [FunctionName("UpdateDocs_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "get")]
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
            string instanceId = await starter.StartNewAsync("UpdateDocs", null);

            log.LogInformationEx($"Started orchestration in UpdateDocs with ID = '{instanceId}'.", LogVerbosity.Normal);

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName(nameof(UpdateDocsMakeDisambiguation))]
        public static async Task<GlossaryFileInfo> UpdateDocsMakeDisambiguation(
            [ActivityTrigger]
            IList<KeywordInformation> keywords,
            ILogger log)
        {
            var file = await TopicMaker.CreateDisambiguationFile(keywords, log);
            return file;
        }

        [FunctionName(nameof(UpdateDocsMakeMarkdown))]
        public static async Task<GlossaryFileInfo> UpdateDocsMakeMarkdown(
            [ActivityTrigger]
            KeywordInformation keyword,
            ILogger log)
        {
            var file = await TopicMaker.CreateKeywordFile(keyword, log);
            return file;
        }

        [FunctionName(nameof(UpdateDocsReplaceKeywords))]
        public static async Task<TopicInformation> UpdateDocsReplaceKeywords(
            [ActivityTrigger]
            (List<KeywordInformation> keywordsToReplace, TopicInformation currentTopic) input,
            ILogger log)
        {
            var newTranscript = await KeywordReplacer.Replace(
                input.currentTopic.Transcript,
                input.keywordsToReplace,
                log);

            if (newTranscript != input.currentTopic.Transcript)
            {
                input.currentTopic.Transcript = newTranscript;
                input.currentTopic.MustSave = true;
            }

            return input.currentTopic;
        }

        [FunctionName("UpdateDocs")]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger]
            IDurableOrchestrationContext context)
        {
            var allTopicsUrls = await context.CallActivityAsync<List<string>>(
                nameof(UpdateDocsGetAllTopics),
                null);

            var allTopicsTasks = new List<Task<TopicInformation>>();

            foreach (var topicUrl in allTopicsUrls)
            {
                //var topicUrl = allTopicsUrls.First();

                allTopicsTasks.Add(context.CallActivityAsync<TopicInformation>(
                    nameof(UpdateDocsParseTopic),
                    new Uri(topicUrl)));
            }

            var allTopics = await Task.WhenAll(allTopicsTasks);

            await context.CallActivityAsync<TopicInformation>(
                nameof(UpdateDocsSaveTopicsToSettings),
                allTopics);

            var allKeywordsTasks = new List<Task<IList<KeywordInformation>>>();

            foreach (var topic in allTopics)
            {
                //var topic = allTopics.First();

                allKeywordsTasks.Add(context.CallActivityAsync<IList<KeywordInformation>>(
                    nameof(UpdateDocsSortKeywords),
                    (allTopics, topic)));
            }

            var allKeywords = (await Task.WhenAll(allKeywordsTasks))
                .SelectMany(i => i);

            allKeywords = await context.CallActivityAsync<IList<KeywordInformation>>(
                nameof(UpdateDocsSortDisambiguations),
                allKeywords);

            var replaceKeywordsTasks = new List<Task<TopicInformation>>();

            foreach (var topic in allTopics)
            {
                // var topic = allTopics.First(t => t.TopicName == "aad");

                var keywordsToReplace = allKeywords
                    .Where(k => 
                        k.TopicName != topic.TopicName
                        && !k.MustDisambiguate)
                    .ToList();

                if (keywordsToReplace.Count > 0)
                {
                    replaceKeywordsTasks.Add(context.CallActivityAsync<TopicInformation>(
                        nameof(UpdateDocsReplaceKeywords),
                        (keywordsToReplace, topic)));
                }
            }

            allTopics = await Task.WhenAll(replaceKeywordsTasks);

            var filesCreationTasks = new List<Task<GlossaryFileInfo>>();

            foreach (var keyword in allKeywords.Where(k => !k.IsDisambiguation))
            {
                //var keyword = allKeywords.First(k => k.Keyword.MakeSafeFileName() == "aad"
                //    && k.TopicName == "aad");

                var currentTopic = allTopics
                    .Single(testc => testc.TopicName == keyword.TopicName);

                keyword.Topic = currentTopic;

                filesCreationTasks.Add(context.CallActivityAsync<GlossaryFileInfo>(
                    nameof(UpdateDocsMakeMarkdown),
                    keyword));
            }

            var filesToSave = (await Task.WhenAll(filesCreationTasks))
                .ToList();

            var errors = filesToSave
                .Where(f => !string.IsNullOrEmpty(f.ErrorMessage))
                .Select(f => f.ErrorMessage)
                .ToList();

            if (errors.Count > 0)
            {
                foreach (var e in errors)
                {
                    await NotificationService.Notify(
                        "ERROR when updating topics",
                        e,
                        null);
                }

                return;
            }

            // Save the disambiguation

            var keywordsGroups = allKeywords
                .Where(k => k.MustDisambiguate)
                .GroupBy(k => k.Keyword.ToLower());

            var disambiguationTasks = new List<Task<GlossaryFileInfo>>();

            foreach (var group in keywordsGroups)
            {
                //var group = keywordsGroups.First();

                foreach (var keyword in group)
                {
                    var currentTopic = allTopics
                        .Single(testc => testc.TopicName == keyword.TopicName);

                    keyword.Topic = currentTopic;
                }

                disambiguationTasks.Add(context.CallActivityAsync<GlossaryFileInfo>(
                    nameof(UpdateDocsMakeDisambiguation),
                    group.ToList()));
            }

            filesToSave.AddRange(await Task.WhenAll(disambiguationTasks));

            errors = filesToSave
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

            var toc = await context.CallActivityAsync<GlossaryFileInfo>(
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

            filesToSave.Add(toc);

            var verifyTasks = new List<Task<GlossaryFileInfo>>();

            foreach (var file in filesToSave)
            {
                //var file = filesToSave.First();

                verifyTasks.Add(context.CallActivityAsync<GlossaryFileInfo>(
                    nameof(UpdateDocsVerifyFiles),
                    file));
            }

            var verifiedFiles = await Task.WhenAll(verifyTasks);
        }

        [FunctionName(nameof(UpdateDocsVerifyFiles))]
        public static async Task<GlossaryFileInfo> UpdateDocsVerifyFiles(
            [ActivityTrigger]
            GlossaryFileInfo file,
            ILogger log)
        {
            return await TopicMaker.VerifyFile(file);
        }

        [FunctionName(nameof(UpdateDocsUpdateTableOfContents))]
        public static async Task<GlossaryFileInfo> UpdateDocsUpdateTableOfContents(
            [ActivityTrigger]
            IList<KeywordInformation> keywords,
            ILogger log)
        {
            return await TopicMaker.CreateTableOfContentsFile(keywords, log);
        }

        [FunctionName(nameof(UpdateDocsSortDisambiguations))]
        public static async Task<IList<KeywordInformation>> UpdateDocsSortDisambiguations(
            [ActivityTrigger]
            IList<KeywordInformation> keywords,
            ILogger log)
        {
            return await TopicMaker.SortDisambiguations(keywords, log);
        }

        [FunctionName(nameof(UpdateDocsSaveTopicsToSettings))]
        public static async Task UpdateDocsSaveTopicsToSettings(
            [ActivityTrigger]
            IList<TopicInformation> topics,
            ILogger log)
        {
            log?.LogInformationEx("In SaveTopicsToSettings", LogVerbosity.Verbose);

            var account = CloudStorageAccount.Parse(
                Environment.GetEnvironmentVariable(
                    Constants.AzureWebJobsStorageVariableName));

            var blobClient = account.CreateCloudBlobClient();
            var blobHelper = new BlobHelper(blobClient, log);
            var settingsContainer = blobHelper.GetContainer(
                Constants.SettingsContainerVariableName);

            var blob = settingsContainer.GetBlockBlobReference(Constants.TopicsSettingsFileName);

            var topicsNames = topics.Select(t => t.TopicName).ToList();

            var json = JsonConvert.SerializeObject(topicsNames);
            log?.LogInformationEx($"json: {json}", LogVerbosity.Debug);

            await blob.UploadTextAsync(json);
            log?.LogInformationEx("Out SaveTopicsToSettings", LogVerbosity.Verbose);
        }

        [FunctionName(nameof(UpdateDocsSortKeywords))]
        public static async Task<IList<KeywordInformation>> UpdateDocsSortKeywords(
            [ActivityTrigger]
            (IList<TopicInformation> allTopics, TopicInformation currentTopic) input,
            ILogger log)
        {
            return await TopicMaker.SortKeywords(input.allTopics, input.currentTopic, log);
        }

        [FunctionName(nameof(UpdateDocsGetAllTopics))]
        public static async Task<List<string>> UpdateDocsGetAllTopics(
            [ActivityTrigger]
            ILogger log)
        {
            var account = CloudStorageAccount.Parse(
                Environment.GetEnvironmentVariable(
                    Constants.AzureWebJobsStorageVariableName));

            var blobClient = account.CreateCloudBlobClient();
            var blobHelper = new BlobHelper(blobClient, log);
            var topicsContainer = blobHelper.GetContainer(
                Constants.TopicsContainerVariableName);

            BlobContinuationToken continuationToken = null;
            var topics = new List<string>();

            do
            {
                var response = await topicsContainer.ListBlobsSegmentedAsync(continuationToken);
                continuationToken = response.ContinuationToken;

                foreach (CloudBlockBlob blob in response.Results)
                {
                    log?.LogInformationEx($"Found: {blob.Name}", LogVerbosity.Debug);
                    topics.Add(blob.Uri.ToString());
                }
            }
            while (continuationToken != null);

            return topics;
        }

        [FunctionName(nameof(UpdateDocsParseTopic))]
        public static async Task<TopicInformation> UpdateDocsParseTopic(
            [ActivityTrigger]
            Uri topicUri,
            ILogger log)
        {
            TopicInformation topic = null;

            try
            {
                topic = await TopicMaker.CreateTopic(topicUri, log);
            }
            catch (Exception ex)
            {
                log?.LogError($"Error with topic {topicUri}: {ex.Message}");
            }

            return topic;
        }
    }
}