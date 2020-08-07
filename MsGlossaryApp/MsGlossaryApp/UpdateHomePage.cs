using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using MsGlossaryApp.Model.GitHub;

namespace MsGlossaryApp
{
    public static class UpdateHomePage
    {
        private const string CommitMessage = "Updated the home page";

        [FunctionName("UpdateHomePage")]
        public static async Task Run(
            //[TimerTrigger("0 0 12 * * *")]
            // TimerInfo myTimer,
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]
            Microsoft.AspNetCore.Http.HttpRequest req,
            ILogger log)
        {
            log.LogInformation($"UpdateHomePage function executed at: {DateTime.Now}");

            var helper = new GitHubHelper();

            var list = new List<(string, string)>
            {
                ("test/index.md", "Hello content"),
                ("test/index2.md", "Hello again"),
                ("test2/index.md", "Hello test2")
            };

            await helper.CommitFiles(
                $"Hello world {DateTime.Now.Ticks}",
                list);
        }
    }
}
