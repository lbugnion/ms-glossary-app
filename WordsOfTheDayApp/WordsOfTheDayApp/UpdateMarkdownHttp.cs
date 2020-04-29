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
    public static class UpdateMarkdownHttp
    {
        private const string UriMask = "https://wordsoftheday.blob.core.windows.net/{0}/{1}.md";

        [FunctionName("UpdateMarkdownHttp")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(
                AuthorizationLevel.Function, 
                "get", 
                Route = null)] HttpRequest req,
            ILogger log)
        {
            string blobName = req.Query["name"];

            var uri = new Uri(string.Format(UriMask, Environment.GetEnvironmentVariable("MarkdownFolder"), blobName));
            var topic = await MarkdownUpdater.Update(uri, log);

            return new OkObjectResult($"OK {topic}");
        }
    }
}