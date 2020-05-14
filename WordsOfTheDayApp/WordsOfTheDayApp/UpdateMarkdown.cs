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
        [FunctionName("UpdateMarkdown_CreateTopics")]
        public static async Task<TopicInformation> CreateTopics(
            [ActivityTrigger]
            Uri blobUri,
            ILogger log)
        {
            if (blobUri.AbsolutePath.Contains("staging"))
            {
                throw new Exception();
            }

            var topic = await TopicMaker.CreateTopic(blobUri, log);
            log?.LogInformation($"Updated {topic.TopicName}.");

            await NotificationService.Notify(
                "Updated topic",
                $"The topic {topic.TopicName} has been updated in markdown",
                log);

            return topic;
        }

        //[FunctionName("UpdateMarkdown_CreateSubtopics")]
        //public static async Task<string> CreateSubtopics(
        //    [ActivityTrigger]
        //    Uri blobUri,
        //    ILogger log)
        //{
        //    var fullTopic = await MarkdownUpdater.CreateSubtopics(blobUri, log);
        //    log?.LogInformation($"Updated {fullTopic}.");

        //    await NotificationService.Notify(
        //        "Updated subtopic",
        //        $"The topic/subtopic {fullTopic} has been updated in markdown",
        //        log);

        //    return fullTopic;
        //}

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

            if (topicsContainer.Name.Contains("staging"))
            {
                return null;
            }

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
        public static async Task<string> ReplaceKeywords(
            [ActivityTrigger]
            TopicInformation topic,
            ILogger log)
        {
            await MarkdownReplacer.ReplaceKeywords(topic, log);

            await NotificationService.Notify(
                "Replaced keywords in topic",
                $"Keywords have been linked in the topic {topic.TopicName}",
                log);

            return null;
        }

        [FunctionName("UpdateMarkdown")]
        public static async Task<List<TopicInformation>> RunOrchestrator(
            [OrchestrationTrigger]
            IDurableOrchestrationContext context)
        {
            var list = context.GetInput<List<string>>();
            var topics = new List<TopicInformation>();

            foreach (var topicUrl in list)
            {
                if (topicUrl.Contains("staging"))
                {
                    throw new Exception();
                }

                Uri topicUri = new Uri(topicUrl);

                topics.Add(await context.CallActivityAsync<TopicInformation>(
                    "UpdateMarkdown_CreateTopics",
                    topicUri));
            }

            foreach (var topic in topics)
            {
                await context.CallActivityAsync<string>(
                    "UpdateMarkdown_ReplaceKeywords",
                    topic);
            }

            var topicsByLanguages = topics
                .GroupBy(t => t.TopicName)
                .ToList();

            foreach (var topicGroup in topicsByLanguages)
            {
                var topicList = topicGroup.ToList();

                await context.CallActivityAsync<string>(
                    "UpdateMarkdown_UpdateOtherLanguages",
                    topicList);
            }

            //foreach (var topicUrl in list)
            //{
            //    Uri topicUri = new Uri(topicUrl);

            //    topics.Add(await context.CallActivityAsync<string>(
            //        "UpdateMarkdown_CreateSubtopics",
            //        topicUri));
            //}

            var languages = topics
                .Select(t => t.Language)
                .GroupBy(l => l.Code)
                .Select(g => g.First())
                .ToList();

            await context.CallActivityAsync(
                "UpdateMarkdown_SaveLanguages",
                languages);

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

        [FunctionName("UpdateMarkdown_SaveLanguages")]
        public static async Task SaveLanguages(
            [ActivityTrigger]
            IList<LanguageInfo> languages,
            ILogger log)
        {
            await SettingsFilesSaver.SaveLanguages(languages, log);
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