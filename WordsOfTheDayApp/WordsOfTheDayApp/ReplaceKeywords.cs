using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WordsOfTheDayApp.Model;

namespace WordsOfTheDayApp
{
    public static class ReplaceKeywords
    {
#if DEBUG
        public const string SemaphorePath = "c:\\temp\\{0}.txt";
#endif

        [FunctionName("ReplaceKeywords")]
        public static async Task Run(
            [QueueTrigger(
                Constants.QueueName, 
                Connection = Constants.AzureWebJobsStorage)]
            string file, 
            ILogger log)
        {
#if DEBUG
            var path = string.Format(SemaphorePath, file);
            if (Constants.UseSemaphores && File.Exists(path))
            {
                log.LogError($"Semaphore found at {path}");
                return;
            }

            File.CreateText(path);
#endif

            log.LogInformation("Executing EnqueueMarkdownEdition");
            log.LogInformation($"File: {file}");

            var account = CloudStorageAccount.Parse(
                Environment.GetEnvironmentVariable(Constants.AzureWebJobsStorage));

            var client = account.CreateCloudBlobClient();

            var jsonContainer = client.GetContainerReference(
                Environment.GetEnvironmentVariable("SettingsFolder"));
            log.LogInformation($"jsonContainer: {jsonContainer.Uri}");

            var jsonBlob = jsonContainer.GetBlockBlobReference(Constants.KeywordsBlob);

            if (!await jsonBlob.ExistsAsync())
            {
                return;
            }

            var json = await jsonBlob.DownloadTextAsync();
            var keywordsDictionary = JsonConvert.DeserializeObject<Dictionary<char, List<KeywordPair>>>(json);

            var keywordsList = keywordsDictionary.Values
                .SelectMany(x => x)
                .ToList();

            var newContainer = client.GetContainerReference(
                Environment.GetEnvironmentVariable("MarkdownTransformedFolder"));
            log.LogInformation($"newContainer: {newContainer.Uri}");

            var newBlob = newContainer.GetBlockBlobReference($"{file}.md");
            var markdown = await newBlob.DownloadTextAsync();

            var replacer = new KeywordReplacer();

            var (newMarkdown, replaced) = replacer.ReplaceInMarkdown(markdown, keywordsList, file, log);

            if (newMarkdown == markdown)
            {
                await NotificationService.Notify(
                    $"No keywords replaced in file {file}",
                    "No keywords found",
                    log);
            }
            else
            {
                log.LogInformation("Uploading");
                await newBlob.UploadTextAsync(newMarkdown);
                await NotificationService.Notify(
                    $"Replaced keywords in file {file}",
                    $"The following keywords were replaced: {replaced}",
                    log);
            }

            log.LogInformation($"Done");
        }
    }
}
