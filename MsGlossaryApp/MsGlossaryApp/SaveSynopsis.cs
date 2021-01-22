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
using System.Linq;
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

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            var synopsis = JsonConvert.DeserializeObject<Synopsis>(requestBody);
            synopsis.CastTranscriptLines();

            log?.LogDebug($"-----> {synopsis.LinksInstructions.First().Key}");

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
                Constants.MsGlossaryGitHubAccountVariableName);
            var repoName = Environment.GetEnvironmentVariable(
                Constants.MsGlossaryGitHubRepoVariableName);

            var synopsisUrl = string.Format(
                Constants.GitHubSynopsisUrlTemplate,
                accountName,
                repoName,
                synopsis.FileName);

            Exception error = null;

            try
            {
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "MsGlossaryApp");
                var oldMarkdown = await client.GetStringAsync(synopsisUrl);
                var oldSynopsis = SynopsisMaker.ParseSynopsis(
                    synopsis.Uri,
                    oldMarkdown,
                    log);

                // Check if the author is trying to edit the synopsis

                // TODO Check the password with the saved one.
                //var isAuthorValid = false;

                //if (!isAuthorValid)
                //{
                //    await NotificationService.Notify(
                //        "Invalid author for synopsis edit request",
                //        $"We got the following request: {userEmail} / {fileName} but author is invalid",
                //        log);

                //    log?.LogError($"Invalid author: {userEmail} / {fileName}");

                //    return new BadRequestObjectResult(
                //        $"Sorry but the author {userEmail} is not listed as one of the original author");
                //}

                var newFile = SynopsisMaker.PrepareNewSynopsis(synopsis, oldSynopsis, log);

                if (newFile.MustSave)
                {
                    // Save file to GitHub
                    var result = await FileSaver.SaveFile(
                        newFile,
                        $"Saved changes to {newFile.Path}",
                        synopsis.FileName,
                        log);

                    if (!string.IsNullOrEmpty(result))
                    {
                        var message = $"Save request for {synopsis.FileName} received, but file wasn't saved: {result}";
                        await NotificationService.Notify(
                            "Synopsis NOT saved to GitHub",
                            message,
                            log);

                        log?.LogInformation($"Synopsis was NOT saved {message}");
                        return new OkObjectResult(message);
                    }

                    log?.LogInformation("Synopsis was saved");
                }
                else
                {
                    var result = $"Save request for {synopsis.FileName} received, but file hasn't changed";
                    await NotificationService.Notify(
                        "Synopsis NOT saved to GitHub",
                        result,
                        log);

                    log?.LogInformation($"Synopsis was NOT saved {result}");
                    return new OkObjectResult(result);
                }
            }
            catch (Exception ex)
            {
                error = ex;
            }

            if (error != null)
            {
                var errorMessage = $"{synopsis.FileName} was NOT saved to GitHub: {error.Message}";

                await NotificationService.Notify(
                    "Synopsis NOT saved to GitHub",
                    errorMessage,
                    log);

                log.LogError(error, $"Error saving synopsis {synopsis.FileName} to GitHub");

                return new OkObjectResult(errorMessage);
            }

            var location = FileSaver.GetSavingLocation();

            var successMessage = $"Synopsis {synopsis.FileName} was saved";

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

            log?.LogInformation(successMessage);
            return new OkObjectResult(string.Empty);
        }
    }
}