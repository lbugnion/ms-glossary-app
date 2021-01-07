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

            var synopsis = JsonConvert.DeserializeObject<Synopsis>(requestBody);

            // Perform validation

            // TODO CONTINUE

            //if (!isValid)
            //{
            //    // TODO return messages
            //}

            //foreach (var author in synopsis.Authors)
            //{
            //    validationContext = new ValidationContext(author);

            //    isValid = Validator.TryValidateObject(
            //        author,
            //        validationContext,
            //        results,
            //        true);

            //    if (!isValid)
            //    {
            //        // TODO return messages
            //    }
            //}

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
                    var result = await FileSaver.SaveFile(
                        newFile,
                        $"Saved changes to {newFile.Path}",
                        synopsis.SafeFileName,
                        log);

                    if (!string.IsNullOrEmpty(result))
                    {
                        var message = $"Save request for {synopsis.SafeFileName} received, but file wasn't saved: {result}";
                        await NotificationService.Notify(
                            "Synopsis NOT saved to GitHub",
                            message,
                            log);

                        return new OkObjectResult(message);
                    }
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

            var location = FileSaver.GetSavingLocation();

            var successMessage = $"Synopsis {synopsis.SafeFileName} was saved";

            if (location == SavingLocations.Both
                || location == SavingLocations.GitHub)
            {
                successMessage += " to GitHub in branch";

                if (location == SavingLocations.Both)
                {
                    successMessage += " and";
                }
            }

            if (location == SavingLocations.Both
                || location == SavingLocations.Storage)
            {
                successMessage += " in storage";
            }

            await NotificationService.Notify(
                "Synopsis saved",
                successMessage,
                log);

            return new OkObjectResult(successMessage);
        }
    }
}