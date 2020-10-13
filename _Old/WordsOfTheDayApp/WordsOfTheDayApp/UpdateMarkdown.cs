using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using WordsOfTheDayApp.Model;

namespace WordsOfTheDayApp
{
    public static partial class UpdateMarkdown
    {
        [FunctionName("UpdateMarkdown_CreateDisambiguation")]
        public static async Task CreateDisambiguation(
            [ActivityTrigger]
            Dictionary<string, List<KeywordPair>> dic,
            ILogger log)
        {
            await TopicMaker.CreateDisambiguation(dic, log);

            log?.LogInformation($"Created disambiguation for {dic.Keys.First()}.");

            //await NotificationService.Notify(
            //    "Updated disambiguation",
            //    $"Created disambiguation for {dic.Keys.First()}.",
            //    log);
        }

        [FunctionName("UpdateMarkdown_CreateSubtopics")]
        public static async Task CreateSubtopics(
            [ActivityTrigger]
            TopicInformation topic,
            ILogger log)
        {
            await TopicMaker.CreateSubtopics(topic, log);
            log?.LogInformation($"Created subtopics for {topic.TopicName}.");

            //await NotificationService.Notify(
            //    "Updated subtopic",
            //    $"Created subtopics for {topic.TopicName}.",
            //    log);
        }

        [FunctionName("UpdateMarkdown_CreateTopics")]
        public static async Task<TopicInformation> CreateTopics(
            [ActivityTrigger]
            Uri blobUri,
            ILogger log)
        {
            var topic = await TopicMaker.CreateTopic(blobUri, log);
            log?.LogInformation($"Updated {topic.TopicName}.");

            //await NotificationService.Notify(
            //    "Updated topic",
            //    $"The topic {topic.TopicName} has been updated in markdown",
            //    log);

            return topic;
        }

        [FunctionName("UpdateMarkdown_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "get",
                Route = "update")]
            HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            await NotificationService.Notify(
                "Trigger received",
                "Starting orchestration",
                log);

            log?.LogInformation($"Getting the blobs");

            var account = CloudStorageAccount.Parse(
                Environment.GetEnvironmentVariable(
                    Constants.AzureWebJobsStorageVariableName));

            var blobClient = account.CreateCloudBlobClient();
            var blobHelper = new BlobHelper(blobClient, log);
            var topicsContainer = blobHelper.GetContainer(
                Constants.TopicsUploadContainerVariableName);

            BlobContinuationToken continuationToken = null;
            var topics = new List<string>();

            do
            {
                var response = await topicsContainer.ListBlobsSegmentedAsync(continuationToken);
                continuationToken = response.ContinuationToken;

                foreach (CloudBlockBlob blob in response.Results)
                {
                    log?.LogInformation($"Found: {blob.Name}");
                    topics.Add(blob.Uri.ToString());
                }
            }
            while (continuationToken != null);

            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("UpdateMarkdown", null, topics);

            log?.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName("UpdateMarkdown_ReplaceKeywords")]
        public static async Task<Dictionary<char, List<KeywordPair>>> ReplaceKeywords(
            [ActivityTrigger]
            TopicInformation topic,
            ILogger log)
        {
            var dic = await MarkdownReplacer.ReplaceKeywords(topic, log);

            //await NotificationService.Notify(
            //    "Replaced keywords in topic",
            //    $"Keywords have been linked in the topic {topic.TopicName}",
            //    log);

            return dic;
        }

        [FunctionName("UpdateMarkdown_ResetSettings")]
        public static async Task ResetSettings(
            [ActivityTrigger]
            ILogger log)
        {
            await TopicMaker.DeleteAllSettings(log);
        }

        [FunctionName("UpdateMarkdown_ResetTopics")]
        public static async Task ResetTopics(
            [ActivityTrigger]
            ILogger log)
        {
            await TopicMaker.DeleteAllTopics(log);
        }

        [FunctionName("UpdateMarkdown")]
        public static async Task<List<TopicInformation>> RunOrchestrator(
            [OrchestrationTrigger]
            IDurableOrchestrationContext context)
        {
            await context.CallActivityAsync(
                "UpdateMarkdown_ResetSettings",
                null);

            await context.CallActivityAsync(
                "UpdateMarkdown_ResetTopics",
                null);

            var list = context.GetInput<List<string>>();
            var topics = new List<TopicInformation>();

            foreach (var topicUrl in list)
            {
                Uri topicUri = new Uri(topicUrl);

                topics.Add(await context.CallActivityAsync<TopicInformation>(
                    "UpdateMarkdown_CreateTopics",
                    topicUri));
            }

            Dictionary<char, List<KeywordPair>> keywordsDictionary = null;

            foreach (var topic in topics)
            {
                keywordsDictionary = await context.CallActivityAsync<Dictionary<char, List<KeywordPair>>>(
                    "UpdateMarkdown_ReplaceKeywords",
                    topic);
            }

            var topicsByLanguages = topics
                .GroupBy(t => t.TopicName)
                .ToList();

            foreach (var topicGroup in topicsByLanguages)
            {
                var topicList = topicGroup.ToList();

                if (topicList.Count > 1)
                {
                    await context.CallActivityAsync<string>(
                        "UpdateMarkdown_UpdateOtherLanguages",
                        topicList);
                }
            }

            foreach (var topic in topics)
            {
                await context.CallActivityAsync<string>(
                    "UpdateMarkdown_CreateSubtopics",
                    topic);
            }

            var disambiguations = keywordsDictionary.Values
                .SelectMany(k => k)
                .Where(k => k.MustDisambiguate)
                .GroupBy(k => k.Keyword)
                .ToList();

            foreach (var d in disambiguations)
            {
                var dictionary = new Dictionary<string, List<KeywordPair>>
                {
                    {
                        d.Key,
                        d.ToList()
                    }
                };

                await context.CallActivityAsync(
                    "UpdateMarkdown_CreateDisambiguation",
                    dictionary);
            }

            var languages = topics
                .Select(t => t.Language)
                .GroupBy(l => l.Code)
                .Select(g => g.First())
                .ToList();

            foreach (var language in languages)
            {
                await context.CallActivityAsync(
                    "UpdateMarkdown_SaveSideBar",
                    language.Code);
            }

            foreach (var language in languages)
            {
                var topicsForLanguage = topics
                    .GroupBy(t => t.Language.Code)
                    .First(g => g.Key == language.Code)
                    .Select(g => g)
                    .ToList();

                var info = new TopicLanguageInfo
                {
                    LanguageCode = language.Code,
                    Topics = topicsForLanguage
                };

                await context.CallActivityAsync(
                    "UpdateMarkdown_SaveTopics",
                    info);
            }

            await NotificationService.Notify(
                "Done",
                "Done handling changes",
                null);

            return topics;
        }

        [FunctionName("UpdateMarkdown_SaveSideBar")]
        public static async Task SaveSideBar(
            [ActivityTrigger]
            string languageCode,
            ILogger log)
        {
            await SettingsFilesSaver.SaveSideBar(languageCode, log);
        }

        [FunctionName("UpdateMarkdown_SaveTopics")]
        public static async Task SaveTopics(
            [ActivityTrigger]
            TopicLanguageInfo info,
            ILogger log)
        {
            await SettingsFilesSaver.SaveTopics(info.LanguageCode, info.Topics, log);
        }

        [FunctionName("UpdateMarkdown_UpdateOtherLanguages")]
        public static async Task UpdateOtherLanguages(
            [ActivityTrigger]
            IList<TopicInformation> topicsByLanguage,
            ILogger log)
        {
            await TopicMaker.UpdateOtherLanguages(topicsByLanguage, log);
        }
    }
}