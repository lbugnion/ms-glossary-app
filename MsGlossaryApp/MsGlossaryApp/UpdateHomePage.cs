using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using MsGlossaryApp.Model;
using MsGlossaryApp.Model.GitHub;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MsGlossaryApp
{
    public static class UpdateHomePage
    {
        private const string BlobStoreNameVariableName = "BlobStoreName";
        private const string CommitMessage = "Updated the home page";
        private const string GitHubAccountVariableName = "GitHubAccount";
        private const string GitHubRepoVariableName = "GitHubRepo";
        private const string GitHubTokenVariableName = "GitHubToken";
        private const string HomePageFilePath = "https://raw.githubusercontent.com/{0}/{1}/master/glossary/index.md";
        private const string IncludLineMask = "[!INCLUDE [Random topic for today:](./topic/{0}/index.md)]";
        private const string ListOfTopicsUrlMask = "https://{0}.blob.core.windows.net/settings/topics.en.json";

        [FunctionName("UpdateHomePage")]
        public static async Task Run(
            [TimerTrigger("0 0 6 * * *")]
            TimerInfo myTimer,
            //[HttpTrigger(Microsoft.Azure.WebJobs.Extensions.Http.AuthorizationLevel.Function, "get", Route = null)]
            //Microsoft.AspNetCore.Http.HttpRequest req,
            ILogger log)
        {
            log.LogInformation($"UpdateHomePage function executed at: {DateTime.Now}");
            Exception error = null;

            try
            {
                var accountName = Environment.GetEnvironmentVariable(GitHubAccountVariableName);
                var repoName = Environment.GetEnvironmentVariable(GitHubRepoVariableName);

                log.LogInformation($"accountName: {accountName}");
                log.LogInformation($"repoName: {repoName}");

                // Read the current state of the file

                var filePath = string.Format(
                    HomePageFilePath,
                    accountName,
                    repoName);

                var client = new HttpClient();
                var content = await client.GetStringAsync(filePath);
                var reader = new StringReader(content);
                var newContentBuilder = new StringBuilder();
                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    if (!line.StartsWith("[!INCLUDE"))
                    {
                        newContentBuilder.AppendLine(line);
                    }
                }

                var newContent = newContentBuilder.ToString()
                    .Trim();

                // Get the list of topics

                var blobStoreName = Environment.GetEnvironmentVariable(BlobStoreNameVariableName);
                var topicsUrl = string.Format(ListOfTopicsUrlMask, blobStoreName);
                var topicsJson = await client.GetStringAsync(topicsUrl);

                // TODO Remove checking for test and another-test when these are removed from the repos.
                var topics = JsonConvert.DeserializeObject<List<string>>(topicsJson)
                    .Where(s => s != "test"
                        && s != "another-test")
                    .ToList();

                var random = new Random();
                var index = random.Next(0, topics.Count - 1);
                var randomTopic = topics[index];

                log?.LogInformation($"New random topic: {randomTopic}");

                var include = string.Format(IncludLineMask, randomTopic);

                // Line before last is the include directive

                newContent += Environment.NewLine
                    + Environment.NewLine
                    + include
                    + Environment.NewLine;

                var token = Environment.GetEnvironmentVariable(GitHubTokenVariableName);
                var helper = new GitHubHelper();

                var list = new List<(string, string)>
                {
                    ("glossary/index.md", newContent)
                };

                await helper.CommitFiles(
                    accountName,
                    repoName,
                    token,
                    CommitMessage,
                    list);

                await NotificationService.Notify(
                    "Updated homepage",
                    $"The glossary homepage was updated with topic {randomTopic}",
                    log);

                log.LogInformation("Done updating homepage");
            }
            catch (Exception ex)
            {
                error = ex;
                log.LogError(ex, "Error updating homepage");
            }

            if (error != null)
            {
                await NotificationService.Notify(
                    "Error when updating the homepage",
                    error.Message,
                    log);
            }
        }
    }
}