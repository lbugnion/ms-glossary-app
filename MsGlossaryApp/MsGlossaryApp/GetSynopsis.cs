using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using MsGlossaryApp.DataModel;
using MsGlossaryApp.Model;
using Newtonsoft.Json;
using System;
using System.IO;
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
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            if (string.IsNullOrEmpty(requestBody))
            {
                return new BadRequestObjectResult("Incomplete request");
            }

            var synopsisRequest = JsonConvert.DeserializeObject<NewSynopsis>(requestBody);

            if (string.IsNullOrEmpty(synopsisRequest.Term)
                || string.IsNullOrEmpty(synopsisRequest.SubmitterEmail))
            {
                return new BadRequestObjectResult("Incomplete request");
            }

            synopsisRequest.SafeFileName = synopsisRequest.Term.MakeSafeFileName();

            // Get the markdown file

            var accountName = Environment.GetEnvironmentVariable(
                Constants.DocsGlossaryGitHubAccountVariableName);
            var repoName = Environment.GetEnvironmentVariable(
                Constants.DocsGlossaryGitHubRepoVariableName);

            var synopsisUrl = string.Format(
                Constants.GitHubSynopsisUrlTemplate,
                accountName,
                repoName,
                synopsisRequest.SafeFileName);

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
                error = ex.Message;
            }

            if (!string.IsNullOrEmpty(error))
            {
                await NotificationService.Notify(
                    "Invalid synopsis edit request",
                    $"We got the following request: {requestBody}",
                    log);

                log?.LogError($"Invalid request: {error} / {requestBody.Replace("\"", "'")}");

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
                    if (author.Email.ToLower() == synopsisRequest.SubmitterEmail)
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
                    $"We got the following request: {requestBody} but author is invalid",
                    log);

                return new BadRequestObjectResult(
                    $"Sorry but the author {synopsisRequest.SubmitterEmail} is not the original author");
            }

            var json = JsonConvert.SerializeObject(synopsis);
            return new OkObjectResult(json);
        }
    }
}