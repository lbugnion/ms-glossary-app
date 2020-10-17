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
using MsGlossaryApp.Model;

namespace MsGlossaryApp
{
    public static class UpdateDocs
    {
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
                allTopicsTasks.Add(context.CallActivityAsync<TopicInformation>(
                    nameof(UpdateDocsParseTopic),
                    new Uri(topicUrl)));
            }

            var allTopics = await Task.WhenAll(allTopicsTasks);

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
                    log?.LogInformation($"Found: {blob.Name}");
                    topics.Add(blob.Uri.ToString());
                }
            }
            while (continuationToken != null);

            return topics;
        }

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

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}