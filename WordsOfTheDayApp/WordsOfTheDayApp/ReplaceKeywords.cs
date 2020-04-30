using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
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
                "%QueueName%", 
                Connection = Constants.AzureWebJobsStorage)]
            string file, 
            ILogger log)
        {
#if DEBUG
            var path = string.Format(SemaphorePath, file);
            
            if (Constants.UseSemaphores)
            {
                if (File.Exists(path))
                {
                    log.LogError($"Semaphore found at {path}");
                    return;
                }

                File.CreateText(path);
            }
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

            CloudBlockBlob newBlob = null;
            string markdown = null;
            Exception error = null;

            try
            {
                newBlob = newContainer.GetBlockBlobReference($"{file}.md");
                markdown = await newBlob.DownloadTextAsync();
            }
            catch (Exception ex)
            {
                error = ex;
            }

            if (error != null)
            {
                await NotificationService.Notify(
                    "Error in ReplaceKeywords",
                    $"Error when loading blob {file}.md : {error.Message}",
                    log);

                log.LogError($"Cannot load blob: {file}.md");
                return;
            }

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
                try
                {
                    log.LogInformation("Uploading");
                    await newBlob.UploadTextAsync(newMarkdown);
                    await NotificationService.Notify(
                        $"Replaced keywords in file {file}",
                        $"The following keywords were replaced: {replaced}",
                        log);
                }
                catch (Exception ex)
                {
                    error = ex;
                }

                if (error != null)
                {
                    await NotificationService.Notify(
                        "Error in ReplaceKeywords",
                        $"Error when uploading blob {file}.md : {error.Message}",
                        log);

                    log.LogError($"Cannot upload blob: {file}.md");
                    return;
                }
            }

            log.LogInformation($"Done");
        }
    }
}
