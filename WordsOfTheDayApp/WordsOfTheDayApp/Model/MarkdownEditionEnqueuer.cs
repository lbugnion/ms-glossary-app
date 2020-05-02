using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WordsOfTheDayApp.Model
{
    public static class MarkdownEditionEnqueuer
    {
        public static async Task<string> Enqueue(Uri uri, ILogger log)
        {
            var newBlob = new CloudBlockBlob(uri);
            log.LogInformation($"newBlobName: {newBlob.Name}");

            var account = CloudStorageAccount.Parse(
                Environment.GetEnvironmentVariable(Constants.AzureWebJobsStorageVariableName));

            var queueClient = account.CreateCloudQueueClient();
            var queueName = Environment.GetEnvironmentVariable(Constants.QueueNameVariableName);
            log?.LogInformation($"queueName: {queueName}");
            var queue = queueClient.GetQueueReference(queueName);
            await queue.CreateIfNotExistsAsync();

            var blobClient = account.CreateCloudBlobClient();
            var blobHelper = new BlobHelper(blobClient, log);

            var topicsContainer = blobHelper.GetContainer(Constants.TopicsContainerVariableName);
            BlobContinuationToken continuationToken = null;
            var topics = new List<string>();

            do
            {
                var response = await topicsContainer.ListBlobsSegmentedAsync(continuationToken);
                continuationToken = response.ContinuationToken;

                foreach (var blob in response.Results)
                {
                    var name = Path.GetFileNameWithoutExtension(blob.Uri.Segments.Last());
                    log.LogInformation($"Enqueueing: {name}");

                    // Enqueue the blob's name for processing
                    var message = new CloudQueueMessage(name);
                    await queue.AddMessageAsync(message);

                    topics.Add(name);
                }
            }
            while (continuationToken != null);

            var settingsContainer = blobHelper.GetContainer(Constants.SettingsContainerVariableName);
            var topicsJsonBlob = settingsContainer.GetBlockBlobReference(Constants.TopicsBlob);
            var json = JsonConvert.SerializeObject(topics);
            await topicsJsonBlob.UploadTextAsync(json);

            log.LogInformation($"Sending notification in MarkdownEditionEnqueuer.Enqueue");

            await NotificationService.Notify(
                "Enqueued",
                $"{newBlob.Name}: Enqueued all markdown files for processing",
                log);

            log.LogInformation($"Done");
            return newBlob.Name;
        }
    }
}