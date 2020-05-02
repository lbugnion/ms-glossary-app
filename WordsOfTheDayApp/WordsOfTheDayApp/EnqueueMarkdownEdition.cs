// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName=EnqueueMarkdownEdition

// /blobServices/default/containers/topics/blobs
// /blobServices/default/containers/staging-topics/blobs
// /blobServices/default/containers/test-topics/blobs

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using System;
using WordsOfTheDayApp.Model;
using System.Threading.Tasks;
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
            if (Constants.UseSemaphores)
            {
                if (File.Exists(SemaphorePath))
                {
                    log.LogError($"Semaphore found at {SemaphorePath}");
                    return;
                }

                File.CreateText(SemaphorePath);
            }
#endif

            log.LogInformation("Executing EnqueueMarkdownEdition");
            log.LogInformation(eventGridEvent.Data.ToString());

            if (eventGridEvent.Data is JObject blobEvent)
            {
                var uri = new Uri(blobEvent["url"].Value<string>());
                await MarkdownEditionEnqueuer.Enqueue(uri, log);
            }
        }
    }
}
