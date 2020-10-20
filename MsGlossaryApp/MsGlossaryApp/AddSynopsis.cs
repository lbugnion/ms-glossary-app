using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using MsGlossaryApp.Model;
using MsGlossaryApp.Model.GitHub;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace MsGlossaryApp
{
    public static class AddSynopsis
    {
        private const string CommitMessage = "Creating new synopsis for {0}";
        private const string EmailMarker = "<!-- ENTER YOUR EMAIL HERE -->";
        private const string GitHubTokenVariableName = "GitHubToken";
        private const string MsGlossaryGitHubAccountVariableName = "MsGlossaryGitHubAccount";
        private const string MsGlossaryGitHubMainBranchName = "MsGlossaryGitHubMainBranchName";
        private const string MsGlossaryGitHubRepoVariableName = "MsGlossaryGitHubRepo";
        private const string NameMarker = "<!-- ENTER YOUR NAME HERE -->";
        private const string NewFileName = "synopsis/{0}.md";
        private const string NewSynopsisUrl = "https://github.com/{0}/{1}/blob/{2}/synopsis/{2}.md";
        private const string RawTemplateUrl = "https://raw.githubusercontent.com/{0}/{1}/{2}/templates/synopsis-template.md";
        private const string ShortDescriptionMarker = "<!-- ENTER A SHORT DESCRIPTION HERE -->";
        private const string TopicMarker = "<!-- TOPIC -->";
        private const string TwitterMarker = "<!-- ENTER YOUR TWITTER NAME HERE -->";

        [FunctionName("AddSynopsis")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "post",
                Route = "add-new")]
            HttpRequest req,
            ILogger log)
        {
            log?.LogInformation("In AddSynopsis");

            // Initialize

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "MsGlossaryApp");

            var accountName = Environment.GetEnvironmentVariable(MsGlossaryGitHubAccountVariableName);
            var repoName = Environment.GetEnvironmentVariable(MsGlossaryGitHubRepoVariableName);
            var mainBranchName = Environment.GetEnvironmentVariable(MsGlossaryGitHubMainBranchName);
            var token = Environment.GetEnvironmentVariable(GitHubTokenVariableName);

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var newTopic = JsonConvert.DeserializeObject<NewTopicInfo>(requestBody);

            if (string.IsNullOrEmpty(newTopic.SubmitterName)
                || string.IsNullOrEmpty(newTopic.SubmitterEmail)
                || string.IsNullOrEmpty(newTopic.Topic)
                || string.IsNullOrEmpty(newTopic.ShortDescription))
            {
                log?.LogInformation("Incomplete submission");
                return new BadRequestObjectResult("Incomplete submission");
            }

            // Get the main head

            newTopic.SafeTopic = newTopic.Topic.MakeSafeFileName();
            log?.LogInformation($"Safe topic: {newTopic.SafeTopic}");

            var helper = new GitHubHelper(client);

            var mainHead = await helper.GetHead(
                accountName,
                repoName,
                mainBranchName,
                log);

            if (!string.IsNullOrEmpty(mainHead.ErrorMessage))
            {
                return new BadRequestObjectResult(mainHead.ErrorMessage);
            }

            // Create a new branch

            var newBranch = await helper.CreateNewBranch(
                accountName,
                repoName,
                token,
                mainHead,
                newTopic.SafeTopic,
                log);

            if (!string.IsNullOrEmpty(newBranch.ErrorMessage))
            {
                return new BadRequestObjectResult(newBranch.ErrorMessage);
            }

            //// Grab main commit

            //var mainCommit = await helper.GetMainCommit(mainHead, log);

            //if (!string.IsNullOrEmpty(mainCommit.ErrorMessage))
            //{
            //    return new BadRequestObjectResult(mainCommit.ErrorMessage);
            //}

            // Get and update file template from GitHub

            log?.LogInformation("Getting file template from GitHub");
            var templateUrl = string.Format(RawTemplateUrl, accountName, repoName, mainBranchName);
            var markdownTemplate = await client.GetStringAsync(templateUrl);

            markdownTemplate = markdownTemplate
                .Replace(TopicMarker, newTopic.Topic)
                .Replace(NameMarker, newTopic.SubmitterName)
                .Replace(EmailMarker, newTopic.SubmitterEmail)
                .Replace(ShortDescriptionMarker, newTopic.ShortDescription);

            if (!string.IsNullOrEmpty(newTopic.SubmitterTwitter))
            {
                if (!newTopic.SubmitterTwitter.StartsWith('@'))
                {
                    newTopic.SubmitterTwitter = $"@{newTopic.SubmitterTwitter}";
                }

                markdownTemplate = markdownTemplate.Replace(TwitterMarker, newTopic.SubmitterTwitter);
            }
            else
            {
                markdownTemplate = markdownTemplate.Replace(TwitterMarker, string.Empty);
            }

            log?.LogInformation("Done getting file template from GitHub and updating it");

            // Commit new file to GitHub

            var newHeadResult = await helper.CommitFiles(
                accountName,
                repoName,
                newTopic.SafeTopic,
                token,
                string.Format(CommitMessage, newTopic.SafeTopic),
                new List<(string, string)>
                {
                    (string.Format(NewFileName, newTopic.SafeTopic), markdownTemplate)
                },
                newBranch,
                log);

            newTopic.Ref = newHeadResult.Ref;
            newTopic.Url = string.Format(NewSynopsisUrl, accountName, repoName, newTopic.SafeTopic);
            var jsonResult = JsonConvert.SerializeObject(newTopic);

            log?.LogInformation($"newTopic.Ref: {newTopic.Ref}");
            log?.LogInformation($"newTopic.Url: {newTopic.Url}");

            return new OkObjectResult(jsonResult);
        }
    }
}