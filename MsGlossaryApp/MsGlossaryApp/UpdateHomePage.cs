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
        private const string CommitMessage = "Updated the home page";
        private const string HomePageFilePath = "https://raw.githubusercontent.com/{0}/{1}/{2}/glossary/index.md";
        private const string IncludLineMask = "[!INCLUDE [Random topic for today:](./topic/{0}/index.md)]";

        [FunctionName("UpdateHomePage")]
        public static async Task Run(
            [TimerTrigger("0 0 6 * * *")]
            TimerInfo myTimer,
            //[HttpTrigger(Microsoft.Azure.WebJobs.Extensions.Http.AuthorizationLevel.Function, "get", Route = null)]
            //Microsoft.AspNetCore.Http.HttpRequest req,
            ILogger log)
        {
            return;

            log.LogInformationEx($"In UpdateHomePage", LogVerbosity.Normal);
            Exception error = null;

            try
            {
                var accountName = Environment.GetEnvironmentVariable(Constants.DocsGlossaryGitHubAccountVariableName);
                var repoName = Environment.GetEnvironmentVariable(Constants.DocsGlossaryGitHubRepoVariableName);
                var branchName = Environment.GetEnvironmentVariable(Constants.DocsGlossaryGitHubMainBranchNameVariableName);

                log.LogInformationEx($"accountName: {accountName}", LogVerbosity.Debug);
                log.LogInformationEx($"repoName: {repoName}", LogVerbosity.Debug);
                log.LogInformationEx($"branchName: {branchName}", LogVerbosity.Debug);

                // Read the current state of the file

                var filePath = string.Format(
                    HomePageFilePath,
                    accountName,
                    repoName,
                    branchName);

                log.LogInformationEx($"filePath: {filePath}", LogVerbosity.Debug);

                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "MsGlossaryApp");

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

                    log.LogInformationEx($"line: {line}", LogVerbosity.Debug);
                }

                log.LogInformationEx("Obtained and read the content", LogVerbosity.Verbose);

                var newContent = newContentBuilder.ToString()
                    .Trim();

                // Get the list of topics

                var blobStoreName = Environment.GetEnvironmentVariable(Constants.BlobStoreNameVariableName);
                var settingsContainerName = Environment.GetEnvironmentVariable(Constants.SettingsContainerVariableName);
                var topicsUrl = string.Format(Constants.ListOfTopicsUrlMask, blobStoreName, settingsContainerName, Constants.TopicsSettingsFileName);

                log.LogInformationEx($"blobStoreName: {blobStoreName}", LogVerbosity.Debug);
                log.LogInformationEx($"settingsContainerName: {settingsContainerName}", LogVerbosity.Debug);
                log.LogInformationEx($"topicsUrl: {topicsUrl}", LogVerbosity.Debug);

                var topicsJson = await client.GetStringAsync(topicsUrl);

                log.LogInformationEx("Topics JSON loaded", LogVerbosity.Verbose);

                // TODO Remove checking for test and another-test when these are removed from the repos.
                var topics = JsonConvert.DeserializeObject<List<string>>(topicsJson)
                    .Where(s => s != "test"
                        && s != "another-test")
                    .ToList();

                log.LogInformationEx($"{topics.Count} topics found", LogVerbosity.Debug);

                var random = new Random();
                var index = random.Next(0, topics.Count - 1);
                var randomTopic = topics[index];

                log?.LogInformationEx($"New random topic: {randomTopic}", LogVerbosity.Verbose);

                var include = string.Format(IncludLineMask, randomTopic);

                log?.LogInformationEx($"include: {include}", LogVerbosity.Debug);

                // Line before last is the include directive

                newContent += Environment.NewLine
                    + Environment.NewLine
                    + include
                    + Environment.NewLine;

                var token = Environment.GetEnvironmentVariable(Constants.GitHubTokenVariableName);
                log?.LogInformationEx($"GitHub topic: {token}", LogVerbosity.Debug);

                var helper = new GitHubHelper(client);

                var list = new List<(string, string)>
                {
                    ("glossary/index.md", newContent)
                };

                await helper.CommitFiles(
                    accountName,
                    repoName,
                    branchName,
                    token,
                    CommitMessage,
                    list);

                log?.LogInformationEx("Topic commited to Github", LogVerbosity.Verbose);

                await NotificationService.Notify(
                    "Updated homepage",
                    $"The glossary homepage was updated with topic {randomTopic}",
                    log);

                log?.LogInformationEx("Done updating homepage", LogVerbosity.Verbose);
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

            log.LogInformationEx($"Out UpdateHomePage", LogVerbosity.Normal);
        }
    }
}