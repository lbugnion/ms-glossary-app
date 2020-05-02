using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WordsOfTheDayApp.Model
{
    public static class TopicsListSaver
    {
        public static async Task Save(IList<string> topics, ILogger log)
        {
            log?.LogInformation("Saving topics");

            var account = CloudStorageAccount.Parse(
                Environment.GetEnvironmentVariable(Constants.AzureWebJobsStorageVariableName));
            var client = account.CreateCloudBlobClient();
            var helper = new BlobHelper(client, log);

            var settingsContainer = helper.GetContainer(Constants.SettingsContainerVariableName);
            var topicsJsonBlob = settingsContainer.GetBlockBlobReference(Constants.TopicsBlob);
            var json = JsonConvert.SerializeObject(topics);
            await topicsJsonBlob.UploadTextAsync(json);

            log?.LogInformation($"Saved topics {json} in {topicsJsonBlob.Uri}");
        }
    }
}