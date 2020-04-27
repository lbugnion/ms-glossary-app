// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName=EnqueueMarkdownEdition

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using System;
using WordsOfTheDayApp.Model;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using System.Linq;
using System.IO;
using Newtonsoft.Json.Linq;

namespace WordsOfTheDayApp
{
    public static class EnqueueMarkdownEdition
    {
#if DEBUG
        public const string SemaphorePath = "c:\\temp\\semaphore2.txt";
#endif

        [FunctionName("EnqueueMarkdownEdition")]
        public static async Task Run(
            [EventGridTrigger]
            EventGridEvent eventGridEvent, 
            ILogger log)
        {
#if DEBUG
            if (File.Exists(SemaphorePath))
            {
                log.LogError($"Semaphore found at {SemaphorePath}");
                return;
            }

            File.CreateText(SemaphorePath);
#endif

            log.LogInformation(eventGridEvent.Data.ToString());

            if (eventGridEvent.Data is JObject blobEvent)
            {
                var uri = new Uri(blobEvent["url"].Value<string>());
                var newBlob = new CloudBlockBlob(uri);

                var account = CloudStorageAccount.Parse(
                Environment.GetEnvironmentVariable("AzureWebJobsStorage"));

                var queueClient = account.CreateCloudQueueClient();
                var blobClient = account.CreateCloudBlobClient();

                var queue = queueClient.GetQueueReference(Constants.QueueName);
                await queue.CreateIfNotExistsAsync();

                var container = blobClient.GetContainerReference(Constants.TargetMarkdownContainer);
                BlobContinuationToken continuationToken = null;

                do
                {
                    var response = await container.ListBlobsSegmentedAsync(continuationToken);
                    continuationToken = response.ContinuationToken;

                    foreach (var blob in response.Results)
                    {
                        var name = blob.Uri.Segments.Last();

                        // Enqueue the blob's name for processing
                        var message = new CloudQueueMessage(Path.GetFileNameWithoutExtension(name));
                        await queue.AddMessageAsync(message);
                    }
                }
                while (continuationToken != null);

                await NotificationService.Notify(
                    "Enqueued", 
                    $"{newBlob.Name}: Enqueued all markdown files for processing", 
                    log);
            }
        }
    }
}
