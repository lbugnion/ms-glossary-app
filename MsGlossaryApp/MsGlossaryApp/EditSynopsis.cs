using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using MsGlossaryApp.DataModel;
using System.Net.Http;
using System.Net.Http.Headers;
using MsGlossaryApp.Model;

namespace MsGlossaryApp
{
    public static class EditSynopsis
    {
        private const string GitHubSynopsisUrlTemplate = "https://raw.githubusercontent.com/{0}/{1}/{2}/synopsis/{2}.md";

        [FunctionName("EditSynopsis")]
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
                GitHubSynopsisUrlTemplate, 
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

            Term synopsis = SynopsisMaker.ParseSynopsis(
                new Uri(synopsisUrl),
                markdown,
                log);

            // Check if the author is trying to edit the synopsis

            var isAuthorValid = false;

            foreach (var author in synopsis.Authors)
            {
                if (author.Name.ToLower() == synopsisRequest.SubmitterName.ToLower()
                    && author.Email.ToLower() == synopsisRequest.SubmitterEmail)
                {
                    isAuthorValid = true;
                    break;
                }
            }

            if (!isAuthorValid)
            {
                await NotificationService.Notify(
                    "Invalid author for synopsis edit request",
                    $"We got the following request: {requestBody} but author is invalid",
                    log);

                return new BadRequestObjectResult(
                    $"Sorry but the author {synopsisRequest.SubmitterName} is not the original author");
            }

            var json = JsonConvert.SerializeObject(synopsis);
            return new OkObjectResult(json);
        }
    }
}
