using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using MsGlossaryApp.DataModel;
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
        private const string GitHubMarker = "<!-- ENTER YOUR GITHUB NAME HERE -->";
        private const string NameMarker = "<!-- ENTER YOUR NAME HERE -->";
        private const string NewFileName = "synopsis/{0}.md";
        private const string NewSynopsisUrl = "https://github.com/{0}/{1}/blob/{2}/synopsis/{2}.md";
        private const string RawTemplateUrl = "https://raw.githubusercontent.com/{0}/{1}/{2}/templates/synopsis-template.md";
        private const string ShortDescriptionMarker = "<!-- ENTER A SHORT DESCRIPTION HERE -->";
        private const string TermMarker = "<!-- TERM -->";
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
            log?.LogInformationEx("In AddSynopsis", LogVerbosity.Normal);

            // Initialize

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "MsGlossaryApp");

            var accountName = Environment.GetEnvironmentVariable(Constants.MsGlossaryGitHubAccountVariableName);
            var repoName = Environment.GetEnvironmentVariable(Constants.MsGlossaryGitHubRepoVariableName);
            var mainBranchName = Environment.GetEnvironmentVariable(Constants.MsGlossaryGitHubMainBranchName);
            var token = Environment.GetEnvironmentVariable(Constants.GitHubTokenVariableName);

            log?.LogInformationEx($"accountName: {accountName}", LogVerbosity.Debug);
            log?.LogInformationEx($"repoName: {repoName}", LogVerbosity.Debug);
            log?.LogInformationEx($"mainBranchName: {mainBranchName}", LogVerbosity.Debug);
            log?.LogInformationEx($"token: {token}", LogVerbosity.Debug);

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var newTerm = JsonConvert.DeserializeObject<NewSynopsis>(requestBody);

            log?.LogInformation("Received new term");
            log?.LogInformationEx($"newTerm.SubmitterName: {newTerm.SubmitterName}", LogVerbosity.Debug);
            log?.LogInformationEx($"newTerm.SubmitterEmail: {newTerm.SubmitterEmail}", LogVerbosity.Debug);
            log?.LogInformationEx($"newTerm.SubmitterTwitter: {newTerm.SubmitterTwitter}", LogVerbosity.Debug);
            log?.LogInformationEx($"newTerm.SubmitterGithub: {newTerm.SubmitterGithub}", LogVerbosity.Debug);
            log?.LogInformationEx($"newTerm.Term: {newTerm.Term}", LogVerbosity.Debug);
            log?.LogInformationEx($"newTerm.ShortDescription: {newTerm.ShortDescription}", LogVerbosity.Debug);

            if (string.IsNullOrEmpty(newTerm.SubmitterName)
                || string.IsNullOrEmpty(newTerm.SubmitterEmail)
                || string.IsNullOrEmpty(newTerm.SubmitterTwitter)
                || string.IsNullOrEmpty(newTerm.SubmitterGithub)
                || string.IsNullOrEmpty(newTerm.Term)
                || string.IsNullOrEmpty(newTerm.ShortDescription))
            {
                log?.LogError("Incomplete submission");
                return new BadRequestObjectResult("Incomplete submission");
            }

            // Get the main head

            newTerm.FileName = newTerm.Term.MakeSafeFileName();
            log?.LogInformation($"Safe term: {newTerm.FileName}");

            var helper = new GitHubHelper(client);

            var mainHead = await helper.GetHead(
                accountName,
                repoName,
                mainBranchName,
                token,
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
                newTerm.FileName,
                log);

            if (!string.IsNullOrEmpty(newBranch.ErrorMessage))
            {
                return new BadRequestObjectResult(newBranch.ErrorMessage);
            }

            // Get and update file template from GitHub

            log?.LogInformation("Getting file template from GitHub");
            var templateUrl = string.Format(RawTemplateUrl, accountName, repoName, mainBranchName);
            log?.LogInformationEx($"templateUrl: {templateUrl}", LogVerbosity.Debug);

            var markdownTemplate = await client.GetStringAsync(templateUrl);

            markdownTemplate = markdownTemplate
                .Replace(TermMarker, newTerm.Term)
                .Replace(NameMarker, newTerm.SubmitterName)
                .Replace(EmailMarker, newTerm.SubmitterEmail)
                .Replace(ShortDescriptionMarker, newTerm.ShortDescription);

            log?.LogInformationEx("Template replaced", LogVerbosity.Debug);

            if (!newTerm.SubmitterTwitter.StartsWith('@'))
            {
                newTerm.SubmitterTwitter = $"@{newTerm.SubmitterTwitter}";
            }

            markdownTemplate = markdownTemplate.Replace(TwitterMarker, newTerm.SubmitterTwitter);
            markdownTemplate = markdownTemplate.Replace(GitHubMarker, newTerm.SubmitterGithub);

            log?.LogInformation("Done getting file template from GitHub and updating it");

            // Commit new file to GitHub

            var newHeadResult = await helper.CommitFiles(
                accountName,
                repoName,
                newTerm.FileName,
                token,
                string.Format(CommitMessage, newTerm.FileName),
                new List<(string, string)>
                {
                    (string.Format(NewFileName, newTerm.FileName), markdownTemplate)
                },
                newBranch,
                log);

            newTerm.Ref = newHeadResult.Ref;
            newTerm.Url = string.Format(NewSynopsisUrl, accountName, repoName, newTerm.FileName);
            var jsonResult = JsonConvert.SerializeObject(newTerm);

            log?.LogInformationEx($"newTerm.Ref: {newTerm.Ref}", LogVerbosity.Debug);
            log?.LogInformation($"newTerm.Url: {newTerm.Url}");
            log?.LogInformationEx("Out AddSynopsis", LogVerbosity.Normal);

            return new OkObjectResult(jsonResult);
        }
    }
}