using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using MsGlossaryApp.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MsGlossaryApp.Model
{
    public static class FileSaver
    {
        public static SavingLocations GetSavingLocation()
        {
            SavingLocations savingLocation = SavingLocations.GitHub;

            var savingLocationString = Environment.GetEnvironmentVariable(
                Constants.SavingLocationVariableName);

            if (!string.IsNullOrEmpty(savingLocationString))
            {
                var success = Enum.TryParse(
                    savingLocationString,
                    out savingLocation);

                if (!success)
                {
                    savingLocation = SavingLocations.GitHub;
                }
            }

            return savingLocation;
        }

        public static async Task<string> SaveFile(
            string accountName,
            string repoName,
            string branchName,
            string token,
            GlossaryFile file,
            string commitMessage,
            ILogger log = null)
        {
            return await SaveFiles(
                accountName,
                repoName,
                branchName,
                token,
                new List<GlossaryFile>
                {
                    file
                },
                commitMessage,
                log);
        }

        public static async Task<string> SaveFiles(
            string accountName,
            string repoName,
            string branchName,
            string token,
            IList<GlossaryFile> files,
            string commitMessage,
            ILogger log = null)
        {
            var savingLocation = GetSavingLocation();

            var filesToCommit = files
                .Where(f => f.MustSave)
                .ToList();

            if (savingLocation == SavingLocations.GitHub
                && filesToCommit.Count == 0)
            {
                return "No changes detected in the files to commit";
            }

            string errorMessage = null;

            if ((savingLocation == SavingLocations.GitHub
                    || savingLocation == SavingLocations.Both)
                && filesToCommit.Count > 0)
            {
                log?.LogInformation("Committing to GitHub");

                log?.LogDebug($"accountName: {accountName}");
                log?.LogDebug($"repoName: {repoName}");
                log?.LogDebug($"branchName: {branchName}");
                log?.LogDebug($"token: {token}");

                // Commit only files who have changed
                var commitContent = filesToCommit
                    .Select(f => (f.Path, f.Content))
                    .ToList();

                var helper = new GitHubHelper.GitHubHelper();

                var result = await helper.CommitFiles(
                    accountName,
                    repoName,
                    branchName,
                    token,
                    commitMessage,
                    commitContent);

                errorMessage = result.ErrorMessage;
                log?.LogInformation("Done committing to GitHub");
            }

            if (!string.IsNullOrEmpty(errorMessage))
            {
                log?.LogError($"Error when committing files: {errorMessage}");
                return errorMessage;
            }

            if ((savingLocation == SavingLocations.Storage
                    || savingLocation == SavingLocations.Both)
                && files.Count > 0)
            {
                log?.LogInformation("Saving to storage");

                try
                {
                    var account = CloudStorageAccount.Parse(
                        Environment.GetEnvironmentVariable(Constants.AzureWebJobsStorageVariableName));

                    var client = account.CreateCloudBlobClient();
                    var helper = new BlobHelper(client, log);
                    var targetContainer = helper.GetContainerFromVariable(Constants.OutputContainerVariableName);

                    // Always save all files
                    foreach (var file in files)
                    {
                        var name = file.Path.Replace("/", "_");
                        var targetBlob = targetContainer.GetBlockBlobReference(name);
                        await targetBlob.UploadTextAsync(file.Content);
                        log?.LogDebug($"Uploaded {name} to storage");
                    }
                }
                catch (Exception ex)
                {
                    log?.LogError($"Error when committing files: {ex.Message}");
                    return ex.Message;
                }

                log?.LogInformation("Done saving to storage");
            }

            return null;
        }
    }
}