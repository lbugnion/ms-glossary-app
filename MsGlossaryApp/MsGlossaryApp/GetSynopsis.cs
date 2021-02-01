using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using MsGlossaryApp.DataModel;
using MsGlossaryApp.Model;
using MsGlossaryApp.Model.GitHub;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace MsGlossaryApp
{
    public static class GetSynopsis
    {
        [FunctionName(nameof(GetSynopsis))]
        public static async Task<IActionResult> RunGet(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "get",
                Route = "synopsis")]
            HttpRequest req,
            ILogger log)
        {
            log?.LogInformation("-> GetSynopsis");

            var (userEmail, fileName, _) = req.GetUserInfoFromHeaders();

            log?.LogDebug($"Original fileName {fileName}");

            fileName = fileName.ToLower();

            if (string.IsNullOrEmpty(userEmail))
            {
                log?.LogError("No user email found in header");
                return new BadRequestObjectResult("No user email found in header");
            }

            if (string.IsNullOrEmpty(fileName))
            {
                log?.LogError("No file name found in header");
                return new BadRequestObjectResult("No file name found in header");
            }

            log?.LogDebug($"userEmail {userEmail}");
            log?.LogDebug($"fileName {fileName}");

            // Get the markdown file

            var accountName = Environment.GetEnvironmentVariable(
                Constants.MsGlossaryGitHubAccountVariableName);
            var repoName = Environment.GetEnvironmentVariable(
                Constants.MsGlossaryGitHubRepoVariableName);
            var token = Environment.GetEnvironmentVariable(
                Constants.GitHubTokenVariableName);

            log?.LogDebug($"accountName {accountName}");
            log?.LogDebug($"repoName {repoName}");
            log?.LogDebug($"token {token}");

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "MsGlossaryApp");

            var helper = new GitHubHelper(client);

            var result = await helper.GetTextFile(
                accountName,
                repoName,
                fileName.ToLower(),
                string.Format(Constants.SynopsisPathMask, fileName),
                token,
                log);

            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                await NotificationService.Notify(
                    "Invalid synopsis edit request",
                    $"We got the following request: {userEmail} / {fileName}",
                    log);

                if (result.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return new BadRequestObjectResult($"Not found: {fileName}. Make sure to use the file name and NOT the Synopsis title!");
                }

                return new BadRequestObjectResult(result.ErrorMessage);
            }

            var synopsis = SynopsisMaker.ParseSynopsis(
                new Uri(result.HtmlUrl),
                result.TextContent,
                log);

            // Check if the author is trying to edit the synopsis

            var isAuthorValid = false;

            if (synopsis.Authors != null)
            {
                foreach (var author in synopsis.Authors)
                {
                    if (author.Email.ToLower() == userEmail.ToLower())
                    {
                        isAuthorValid = true;
                        break;
                    }
                }
            }

            if (!isAuthorValid)
            {
                await NotificationService.Notify(
                    "Invalid author for synopsis get request",
                    $"We got the following GET request: {userEmail} / {fileName} but author is invalid",
                    log);

                log?.LogError($"Invalid author: {userEmail} / {fileName}");

                return new BadRequestObjectResult(
                    $"Sorry but the author {userEmail} is not listed as one of the original author");
            }

            log?.LogInformation("GetSynopsis success, returning Synopsis");
            return new OkObjectResult(synopsis);
        }
    }
}