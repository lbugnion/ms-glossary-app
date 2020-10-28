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
        public static async Task<string> UpdateDocsMakeDisambiguation(
            [ActivityTrigger]
            IList<KeywordInformation> keywords,
            ILogger log)
        {
            var exception = await TopicMaker.SaveDisambiguation(keywords, log);
            return exception;
        }

        [FunctionName(nameof(UpdateDocsMakeMarkdown))]
        public static async Task<string> UpdateDocsMakeMarkdown(
            [ActivityTrigger]
            KeywordInformation keyword,
            ILogger log)
        {
            var exception = await TopicMaker.SaveKeyword(keyword, log);
            return exception;
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
                //var topic = allTopics.First();

                var keywordsToReplace = allKeywords
                    .Where(k => k.Topic == null || k.Topic.Title != topic.Title)
                    .Where(k => !k.MustDisambiguate)
                    .ToList();

                if (keywordsToReplace.Count > 0)
                {
                    replaceKeywordsTasks.Add(context.CallActivityAsync<TopicInformation>(
                        nameof(UpdateDocsReplaceKeywords),
                        (keywordsToReplace, topic)));
                }
            }

            allTopics = await Task.WhenAll(replaceKeywordsTasks);

            var filesCreationTasks = new List<Task<string>>();

            foreach (var keyword in allKeywords.Where(k => !k.IsDisambiguation))
            {
                //var keyword = allKeywords.First(k => k.Keyword.MakeSafeFileName() == "aad"
                //    && k.TopicName == "aad");

                var currentTopic = allTopics
                    .Single(testc => testc.TopicName == keyword.TopicName);

                keyword.Topic = currentTopic;

                filesCreationTasks.Add(context.CallActivityAsync<string>(
                    nameof(UpdateDocsMakeMarkdown),
                    keyword));
            }

            var errors = (await Task.WhenAll(filesCreationTasks))
                .Where(e => !string.IsNullOrEmpty(e))
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

            var disambiguationTasks = new List<Task<string>>();

            foreach (var group in keywordsGroups)
            {
                //var group = keywordsGroups.First();

                foreach (var keyword in group)
                {
                    var currentTopic = allTopics
                        .Single(testc => testc.TopicName == keyword.TopicName);

                    keyword.Topic = currentTopic;
                }

                disambiguationTasks.Add(context.CallActivityAsync<string>(
                    nameof(UpdateDocsMakeDisambiguation),
                    group.ToList()));
            }

            errors = (await Task.WhenAll(disambiguationTasks))
                .Where(s => !string.IsNullOrEmpty(s))
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

            // TODO Create and save TOC.yml
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