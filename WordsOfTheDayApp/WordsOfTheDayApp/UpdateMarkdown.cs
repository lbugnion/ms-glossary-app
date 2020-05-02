using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using WordsOfTheDayApp.Model;

namespace WordsOfTheDayApp
{
    public static class UpdateMarkdown
    {
        [FunctionName("UpdateMarkdown")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] 
            IDurableOrchestrationContext context)
        {
            var list = context.GetInput<List<string>>();
            var topics = new List<string>();

            foreach (var topicUrl in list)
            {
                Uri topicUri = new Uri(topicUrl);

                topics.Add(await context.CallActivityAsync<string>(
                    "UpdateMarkdown_Execute",
                    topicUri));
            }

            foreach (var topic in topics)
            {
                await context.CallActivityAsync<string>(
                    "UpdateMarkdown_ReplaceKeywords",
                    topic);
            }

            await context.CallActivityAsync(
                "UpdateMarkdown_SaveTopics",
                topics);

            return topics;
        }

        [FunctionName("UpdateMarkdown_SaveTopics")]
        public static async Task Save(
            [ActivityTrigger]
            IList<string> topics,
            ILogger log)
        {
            await TopicsListSaver.Save(topics, log);
        }

        [FunctionName("UpdateMarkdown_ReplaceKeywords")]
        public static async Task<string> Replace(
            [ActivityTrigger]
            string topic,
            ILogger log)
        {
            await MarkdownReplacer.Replace(topic, log);

            await NotificationService.Notify(
                "Replaced keywords in topic",
                $"The topic {topic} has been updated",
                log);

            return null;
        }

        [FunctionName("UpdateMarkdown_Execute")]
        public static async Task<string> Execute(
            [ActivityTrigger] 
            Uri blobUri, 
            ILogger log)
        {
            var topic = await MarkdownUpdater.Update(blobUri, log);
            log.LogInformation($"Updated {topic}.");

            await NotificationService.Notify(
                "Updated topic",
                $"The topic {topic} has been updated",
                log);

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

            log.LogInformation($"Getting the blobs");

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
                    log.LogInformation($"Found: {blob.Name}");
                    topics.Add(blob.Uri.ToString());
                }
            }
            while (continuationToken != null);

            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("UpdateMarkdown", null, topics);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}