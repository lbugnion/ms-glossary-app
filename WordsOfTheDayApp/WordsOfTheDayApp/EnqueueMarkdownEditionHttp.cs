using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WordsOfTheDayApp.Model;

namespace WordsOfTheDayApp
{
    public static class EnqueueMarkdownEditionHttp
    {
        private const string UriMask = "https://wordsoftheday.blob.core.windows.net/{0}/{1}.md";

        [FunctionName("EnqueueMarkdownEditionHttp")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(
                AuthorizationLevel.Function, 
                "get", 
                Route = null)] 
            HttpRequest req,
            ILogger log)
        {
            string blobName = req.Query["name"];

            var topicsContainerName = Environment.GetEnvironmentVariable(
                Constants.TopicsContainerVariableName);
            log?.LogInformation($"topicsContainerName: {topicsContainerName}");

            var uri = new Uri(
                string.Format(
                    UriMask,
                    topicsContainerName, 
                    blobName));
            var topic = await MarkdownEditionEnqueuer.Enqueue(uri, log);

            return new OkObjectResult($"OK: {topic}");
        }
    }
}
