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
            var accountName = Environment.GetEnvironmentVariable(
                Constants.DocsGlossaryGitHubAccountVariableName);
            var repoName = Environment.GetEnvironmentVariable(
                Constants.DocsGlossaryGitHubRepoVariableName);
            var token = Environment.GetEnvironmentVariable(
                Constants.GitHubTokenVariableName);

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var synopsisRequest = JsonConvert.DeserializeObject<NewSynopsis>(requestBody);

            synopsisRequest.SafeFileName = synopsisRequest.Term.MakeSafeFileName();

            // Get the markdown file

            var synopsisUrl = string.Format(
                GitHubSynopsisUrlTemplate, 
                accountName,
                repoName,
                synopsisRequest.SafeFileName);

            Synopsis synopsis = null;
            string error = null;

            try
            {
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "MsGlossaryApp");
                var markdown = await client.GetStringAsync(synopsisUrl);
                synopsis = SynopsisMaker.CreateSynopsis(
                    new Uri(synopsisUrl), 
                    markdown, 
                    log);
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return new OkObjectResult(responseMessage);
        }
    }
}
