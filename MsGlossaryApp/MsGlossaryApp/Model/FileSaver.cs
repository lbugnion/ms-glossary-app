using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using MsGlossaryApp.DataModel;
using MsGlossaryApp.Model.GitHub;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public static async Task<string> SaveFiles(
            IList<GlossaryFile> files,
            string commitMessage,
            ILogger log)
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
                log?.LogInformationEx("Committing to GitHub", LogVerbosity.Verbose);

                var accountName = Environment.GetEnvironmentVariable(
                    Constants.DocsGlossaryGitHubAccountVariableName);
                var repoName = Environment.GetEnvironmentVariable(
                    Constants.DocsGlossaryGitHubRepoVariableName);
                var branchName = Environment.GetEnvironmentVariable(
                    Constants.DocsGlossaryGitHubMainBranchNameVariableName);
                var token = Environment.GetEnvironmentVariable(
                    Constants.GitHubTokenVariableName);

                log?.LogInformationEx($"accountName: {accountName}", LogVerbosity.Debug);
                log?.LogInformationEx($"repoName: {repoName}", LogVerbosity.Debug);
                log?.LogInformationEx($"branchName: {branchName}", LogVerbosity.Debug);
                log?.LogInformationEx($"token: {token}", LogVerbosity.Debug);

                // Commit only files who have changed
                var commitContent = filesToCommit
                    .Select(f => (f.Path, f.Content))
                    .ToList();

                var helper = new GitHubHelper();

                var result = await helper.CommitFiles(
                    accountName,
                    repoName,
                    branchName,
                    token,
                    commitMessage,
                    commitContent,
                    log: log);

                errorMessage = result.ErrorMessage;
                log?.LogInformationEx("Done committing to GitHub", LogVerbosity.Verbose);
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
                log?.LogInformationEx("Saving to storage", LogVerbosity.Verbose);

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
                        log?.LogInformationEx($"Uploaded {name} to storage", LogVerbosity.Debug);
                    }
                }
                catch (Exception ex)
                {
                    log?.LogError($"Error when committing files: {ex.Message}");
                    return ex.Message;
                }

                log?.LogInformationEx("Done saving to storage", LogVerbosity.Verbose);
            }

            return null;
        }
    }
}
