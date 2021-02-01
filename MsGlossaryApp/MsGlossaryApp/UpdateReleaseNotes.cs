using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MsGlossaryApp.Model.GitHub;
using System.Net.Http;
using MsGlossaryApp.DataModel;
using System.Linq;
using System.Text;
using Dynamitey.DynamicObjects;
using System.Collections;
using System.Collections.Generic;
using MsGlossaryApp.Model;

namespace MsGlossaryApp
{
    public static class UpdateReleaseNotes
    {
        [FunctionName("UpdateReleaseNotes")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(
                AuthorizationLevel.Function, 
                "get", 
                Route = "release-notes/milestones/{milestones}")] 
            HttpRequest req,
            string milestones,
            ILogger log)
        {
            log.LogInformation("-> UpdateReleaseNotes");

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "MsGlossaryApp");

            var helper = new GitHubHelper(client);

            var accountName = Environment.GetEnvironmentVariable(
                Constants.MsGlossaryAppGitHubAccountVariableName);
            var repoName = Environment.GetEnvironmentVariable(
                Constants.MsGlossaryAppGitHubRepoVariableName);
            var mainBranchName = Environment.GetEnvironmentVariable(
                Constants.MsGlossaryAppGitHubMainBranchName);
            var token = Environment.GetEnvironmentVariable(
                Constants.GitHubTokenVariableName);

            log?.LogDebug($"accountName {accountName}");
            log?.LogDebug($"repoName {repoName}");
            log?.LogDebug($"mainBranchName {mainBranchName}");
            log?.LogDebug($"token {token}");

            var projects = Environment.GetEnvironmentVariable(
                Constants.CreateReleaseNotesForVariableName)
                .Split(new char[]
                {
                    ','
                })
                .Select(p => p.Split(new char[]
                {
                    '|'
                }))
                .Select(f => new ReleaseNotesPageInfo
                {
                    Project = f[0].Trim(),
                    ProjectId = f[1].Trim()
                })
                .ToList();

            var forMilestones = (milestones == "all")
                ? null
                : milestones.Split(new char[]
                {
                    ','
                }).ToList();

            var synopsisReleaseNote = projects
                .FirstOrDefault(r => r.Project == "Synopsis Client");

            if (synopsisReleaseNote != null)
            {
                synopsisReleaseNote.Header = new List<string>
                {
                    "Use this application to edit a synopsis for the Microsoft Glossary.",
                    "[Production app](https://www.ms-glossary-synopsis.cloud)"
                };
            }

            var result = await helper.CreateReleaseNotesMarkdown(
                accountName,
                repoName,
                projects,
                forMilestones,
                token,
                log);

            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                return new BadRequestObjectResult(result.ErrorMessage);
            }

            var glossaryFiles = new List<GlossaryFile>();

            foreach (var page in result.CreatedPages)
            {
                var existingContent = await helper.GetTextFile(
                    accountName,
                    repoName,
                    mainBranchName,
                    page.FilePath,
                    token,
                    log);

                if (!string.IsNullOrEmpty(existingContent.ErrorMessage)
                    || existingContent.TextContent != page.Markdown)
                {
                    glossaryFiles.Add(new GlossaryFile
                    {
                        Content = page.Markdown,
                        MustSave = true,
                        Path = page.FilePath
                    });
                }
            }

            if (glossaryFiles.Count == 0)
            {
                var noSaveMessage = $"No modifed release notes found for {accountName}/{repoName}";
                log.LogWarning(noSaveMessage);

                await NotificationService.Notify(
                    "Release notes NOT saved to GitHub",
                    noSaveMessage,
                    log);

                log.LogInformation("UpdateReleaseNotes ->");
                return new OkObjectResult(noSaveMessage);
            }

            var errorMessage = await FileSaver.SaveFiles(
                accountName,
                repoName,
                mainBranchName,
                token,
                glossaryFiles, 
                "Updated release notes");

            if (!string.IsNullOrEmpty(errorMessage))
            {
                log.LogError($"Cannot save file: {errorMessage}");

                await NotificationService.Notify(
                    "Release notes NOT saved to GitHub",
                    $"There was an error: {errorMessage}",
                    log);

                log.LogInformation("UpdateReleaseNotes ->");
                return new UnprocessableEntityObjectResult($"There was an issue: {errorMessage}");
            }

            await NotificationService.Notify(
                "Release notes saved to GitHub",
                $"The release notes were saved to {accountName}/{repoName}",
                log);

            log.LogInformation("UpdateReleaseNotes ->");
            return new OkObjectResult(result);
        }
    }
}
