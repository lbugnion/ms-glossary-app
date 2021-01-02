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
    public static class SaveSynopsis
    {
        [FunctionName("SaveSynopsis")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "post",
                Route = "synopsis")]
            HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            var synopsis = JsonConvert.DeserializeObject<Term>(requestBody);
            synopsis.Stage = Term.TermStage.Synopsis;

            // Get the markdown file

            var accountName = Environment.GetEnvironmentVariable(
                Constants.DocsGlossaryGitHubAccountVariableName);
            var repoName = Environment.GetEnvironmentVariable(
                Constants.DocsGlossaryGitHubRepoVariableName);

            var synopsisUrl = string.Format(
                Constants.GitHubSynopsisUrlTemplate,
                accountName,
                repoName,
                synopsis.SafeFileName);

            Exception error = null;

            try
            {
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "MsGlossaryApp");
                var oldMarkdown = await client.GetStringAsync(synopsisUrl);

                var newFile = SynopsisMaker.PrepareNewSynopsis(synopsis, oldMarkdown, log);

                if (newFile.MustSave)
                {
                    // Save file to GitHub
                }
                else
                {
                    var result = $"Save request for {synopsis.SafeFileName} received, but file hasn't changed";
                    await NotificationService.Notify(
                        "Synopsis NOT saved to GitHub",
                        result,
                        log);

                    return new OkObjectResult(result);
                }
            }
            catch (Exception ex)
            {
                error = ex;
            }

            if (error != null)
            {
                var errorMessage = $"{synopsis.SafeFileName} was NOT saved to GitHub: {error.Message}";

                await NotificationService.Notify(
                    "Synopsis NOT saved to GitHub",
                    errorMessage,
                    log);

                log.LogError(error, $"Error saving synopsis {synopsis.SafeFileName} to GitHub");

                return new OkObjectResult(errorMessage);
            }

            var successMessage = $"Synopsis {synopsis.SafeFileName} was saved to GitHub in branch";

            await NotificationService.Notify(
                "Synopsis saved to GitHub",
                successMessage,
                log);

            return new OkObjectResult(successMessage);
        }
    }
}