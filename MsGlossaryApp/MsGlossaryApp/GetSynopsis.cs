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
        private const string SynopsisPathMask = "synopsis/{0}.md";

        [FunctionName(nameof(GetSynopsis))]
        public static async Task<IActionResult> RunGet(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "get",
                Route = "synopsis")]
            HttpRequest req,
            ILogger log)
        {
            log?.LogInformation("GetSynopsis");

            var (userEmail, fileName) = req.GetUserInfoFromHeaders();

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
                fileName,
                string.Format(SynopsisPathMask, fileName),
                token,
                log);

            //var synopsisUrl = string.Format(
            //    Constants.GitHubSynopsisUrlTemplate,
            //    accountName,
            //    repoName,
            //    fileName.ToLower());

            //log?.LogDebug($"synopsisUrl {synopsisUrl}");

            //string markdown = null;
            //string error = null;

            //try
            //{
            //    var client = new HttpClient();
            //    client.DefaultRequestHeaders.Add("User-Agent", "MsGlossaryApp");
            //    markdown = await client.GetStringAsync(synopsisUrl);
            //}
            //catch (HttpRequestException ex)
            //{
            //    log?.LogError(ex, "HttpRequestException when getting synopsis markdown");
            //    error = "Double check the file name";
            //}
            //catch (Exception ex)
            //{
            //    log?.LogError(ex, "Error when getting synopsis markdown");
            //    log?.LogDebug(ex.GetType().FullName);
            //    error = ex.Message;
            //}

            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                await NotificationService.Notify(
                    "Invalid synopsis edit request",
                    $"We got the following request: {userEmail} / {fileName}",
                    log);

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
                    "Invalid author for synopsis edit request",
                    $"We got the following request: {userEmail} / {fileName} but author is invalid",
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