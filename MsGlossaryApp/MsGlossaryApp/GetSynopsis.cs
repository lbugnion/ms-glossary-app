using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using MsGlossaryApp.DataModel;
using MsGlossaryApp.Model;
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

            log?.LogInformationEx($"userEmail {userEmail}", LogVerbosity.Debug);
            log?.LogInformationEx($"fileName {fileName}", LogVerbosity.Debug);

            // Get the markdown file

            var accountName = Environment.GetEnvironmentVariable(
                Constants.DocsGlossaryGitHubAccountVariableName);
            var repoName = Environment.GetEnvironmentVariable(
                Constants.DocsGlossaryGitHubRepoVariableName);

            log?.LogInformationEx($"accountName {accountName}", LogVerbosity.Debug);
            log?.LogInformationEx($"repoName {repoName}", LogVerbosity.Debug);

            var synopsisUrl = string.Format(
                Constants.GitHubSynopsisUrlTemplate,
                accountName,
                repoName,
                fileName);

            log?.LogInformationEx($"synopsisUrl {synopsisUrl}", LogVerbosity.Debug);

            string markdown = null;
            string error = null;

            try
            {
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "MsGlossaryApp");
                markdown = await client.GetStringAsync(synopsisUrl);
            }
            catch (HttpRequestException ex)
            {
                log?.LogError(ex, "HttpRequestException when getting synopsis markdown");
                error = "Double check the file name";
            }
            catch (Exception ex)
            {
                log?.LogError(ex, "Error when getting synopsis markdown");
                log?.LogInformationEx(ex.GetType().FullName, LogVerbosity.Debug);
                error = ex.Message;
            }

            if (!string.IsNullOrEmpty(error))
            {
                await NotificationService.Notify(
                    "Invalid synopsis edit request",
                    $"We got the following request: {userEmail} / {fileName}",
                    log);

                return new BadRequestObjectResult(error);
            }

            var synopsis = SynopsisMaker.ParseSynopsis(
                new Uri(synopsisUrl),
                markdown,
                log);

            // Check if the author is trying to edit the synopsis

            var isAuthorValid = false;

            if (synopsis.Authors != null)
            {
                foreach (var author in synopsis.Authors)
                {
                    if (author.Email.ToLower() == userEmail)
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