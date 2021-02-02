using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using MsGlossaryApp.DataModel;
using MsGlossaryApp.Model;
using MsGlossaryApp.Model.GitHub;
using MsGlossaryApp.Model.Pass;
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

            var (userEmail, fileName, hash, commitMessage) = req.GetUserInfoFromHeaders();

            if (string.IsNullOrEmpty(hash))
            {
                log?.LogError("No hash found in header");
                return new UnauthorizedObjectResult("Unauthorized");
            }
            
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

            if (string.IsNullOrEmpty(commitMessage))
            {
                log?.LogError("No commit message found in header");
                return new BadRequestObjectResult("No commit message found in header");
            }

            log?.LogDebug($"userEmail {userEmail}");
            log?.LogDebug($"fileName {fileName}");
            log?.LogDebug($"commitMessage {commitMessage}");
            log?.LogDebug($"hash {hash}");

            var connectionString = Environment.GetEnvironmentVariable(
                Constants.AzureWebJobsStorageVariableName);

            var handler = new PassHandler(connectionString);
            var (isValid, _) = await handler.Verify(userEmail, fileName, hash, log);

            if (!isValid)
            {
                return new UnauthorizedObjectResult("Unauthorized");
            }

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            var synopsis = JsonConvert.DeserializeObject<Synopsis>(requestBody);
            synopsis.CastTranscriptLines();

            // Perform validation

            // TODO PERFORM VALIDATION

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

            Exception error = null;
            var result = new SaveSynopsisResult();

            try
            {
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "MsGlossaryApp");

                var helper = new GitHubHelper(client);

                var markdownResult = await helper.GetTextFile(
                    accountName,
                    repoName,
                    fileName.ToLower(),
                    string.Format(Constants.SynopsisPathMask, fileName),
                    token,
                    log);

                if (!string.IsNullOrEmpty(markdownResult.ErrorMessage))
                {
                    await NotificationService.Notify(
                        "Invalid synopsis save request",
                        $"We got the following request: {userEmail} / {fileName}",
                        log);

                    if (markdownResult.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        return new BadRequestObjectResult($"Not found: {fileName}. Make sure to use the file name and NOT the Synopsis title!");
                    }

                    return new BadRequestObjectResult(markdownResult.ErrorMessage);
                }

                var oldSynopsis = SynopsisMaker.ParseSynopsis(
                    synopsis.Uri,
                    markdownResult.TextContent,
                    log);

                // Verify that logged in author is an existing author

                var isAuthorValid = false;
                var isLoggedInEmailStillValid = false;

                foreach (var author in oldSynopsis.Authors)
                {
                    if (author.Email.ToLower() == userEmail.ToLower())
                    {
                        isAuthorValid = true;

                        foreach (var newAuthor in synopsis.Authors)
                        {
                            if (newAuthor.Email.ToLower() == userEmail.ToLower())
                            {
                                isLoggedInEmailStillValid = true;
                                break;
                            }
                        }

                        break;
                    }
                }

                // TODO Check the password with the saved one.

                if (!isAuthorValid)
                {
                    await NotificationService.Notify(
                        "Invalid author for synopsis save request",
                        $"We got the following SAVE request: {userEmail} / {fileName} but author is invalid",
                        log);

                    log?.LogError($"Invalid author: {userEmail} / {fileName}");

                    return new BadRequestObjectResult(
                        $"Sorry but the author {userEmail} is not listed as one of the original author");
                }

                if (!isLoggedInEmailStillValid)
                {
                    // Inform client that a logout will be required.
                    result.LoggedInEmailHasChanged = true;
                }

                var newFile = SynopsisMaker.PrepareNewSynopsis(synopsis, oldSynopsis, log);

                if (newFile.MustSave)
                {
                    // Save file to GitHub
                    result.Message = await FileSaver.SaveFile(
                        accountName,
                        repoName,
                        synopsis.FileName,
                        token,
                        newFile,
                        $"Saved changes to {newFile.Path}. {commitMessage} by {userEmail}",
                        log);

                    if (!string.IsNullOrEmpty(result.Message))
                    {
                        result.Message = $"Save request for {synopsis.FileName} received from {userEmail}, but file wasn't saved: {result.Message}";
                        await NotificationService.Notify(
                            "Synopsis NOT saved to GitHub",
                            result.Message,
                            log);

                        log?.LogInformation(result.Message);
                        return new UnprocessableEntityObjectResult(result);
                    }

                    log?.LogInformation("Synopsis was saved");
                }
                else
                {
                    result.Message = $"Save request for {synopsis.FileName} received from {userEmail}, but file hasn't changed";
                    await NotificationService.Notify(
                        "Synopsis NOT saved to GitHub",
                        result.Message,
                        log);

                    log?.LogInformation($"result {result.Message}");
                    return new OkObjectResult(result);
                }
            }
            catch (Exception ex)
            {
                error = ex;
            }

            if (error != null)
            {
                result.Message = $"{synopsis.FileName} was NOT saved to GitHub: {error.Message} / {userEmail}";

                await NotificationService.Notify(
                    "Synopsis NOT saved to GitHub",
                    result.Message,
                    log);

                log.LogError(error, $"Error saving synopsis {synopsis.FileName} to GitHub");

                return new UnprocessableEntityObjectResult(result);
            }

            var location = FileSaver.GetSavingLocation();

            var successMessage = $"Synopsis {synopsis.FileName} edited by {userEmail} was saved";

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
            return new OkObjectResult(result);
        }
    }
}