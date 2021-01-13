using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using MsGlossaryApp.DataModel;
using MsGlossaryApp.Model;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace MsGlossaryApp
{
    public static class GetSynopsis
    {
        private const string UserEmailHeaderKey = "x-glossary-user-email";
        private const string FileNameHeaderKey = "x-glossary-file-name";

        [FunctionName(nameof(GetSynopsis))]
        public static async Task<IActionResult> RunGet(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "get",
                Route = "synopsis")]
            HttpRequest req,
            ILogger log)
        {
            log?.LogInformationEx("GetSynopsis", LogVerbosity.Verbose);

            StringValues userEmailValues;
            var success = req.Headers.TryGetValue(UserEmailHeaderKey, out userEmailValues);

            if (!success
                || userEmailValues.Count == 0)
            {
                log?.LogError("No user email found in header");
                return new BadRequestObjectResult("No user email found in header");
            }

            StringValues fileNameValues;
            success = req.Headers.TryGetValue(FileNameHeaderKey, out fileNameValues);

            if (!success
                || fileNameValues.Count == 0)
            {
                log?.LogError("No file name found in header");
                return new BadRequestObjectResult("No file name found in header");
            }

            var userEmail = userEmailValues[0];
            var fileName = fileNameValues[0];

            if (string.IsNullOrEmpty(userEmail)
                || string.IsNullOrEmpty(fileName))
            {
                log?.LogError("No user email or file name found in header");
                return new BadRequestObjectResult("Incomplete request");
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
            catch (Exception ex)
            {
                log?.LogError(ex, "Error when getting synopsis markdown");
                error = ex.Message;
            }

            if (!string.IsNullOrEmpty(error))
            {
                await NotificationService.Notify(
                    "Invalid synopsis edit request",
                    $"We got the following request: {userEmail} / {fileName}",
                    log);

                return new BadRequestObjectResult("Invalid request");
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

            var json = JsonConvert.SerializeObject(synopsis);
            log?.LogInformationEx("GetSynopsis success, returning Synopsis", LogVerbosity.Verbose);
            return new OkObjectResult(json);
        }
    }
}