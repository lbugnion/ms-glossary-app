// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName=UpdateMarkdown

// /blobServices/default/containers/markdown/blobs
// /blobServices/default/containers/test-markdown/blobs

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using WordsOfTheDayApp.Model;
using System.Threading.Tasks;
using System.IO;

namespace WordsOfTheDayApp
{
    public static class UpdateMarkdown
    {
#if DEBUG
        public const string SemaphorePath = "c:\\temp\\semaphore.txt";
#endif

        [FunctionName("UpdateMarkdown")]
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

            log.LogInformation("Executing UpdateMarkdown");
            log.LogInformation(eventGridEvent.Data.ToString());

            if (eventGridEvent.Data is JObject blobEvent)
            {
                var uri = new Uri(blobEvent["url"].Value<string>());

                var topic = await MarkdownUpdater.Update(uri, log);

                log.LogInformation($"Sending notification");

                await NotificationService.Notify(
                    "Uploaded", 
                    $"{topic}.md: Markdown file updated and uploaded", 
                    log);

                log.LogInformation($"Done");
            }
        }
    }
}
