using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
            if (File.Exists(path))
            {
                log.LogError($"Semaphore found at {path}");
                return;
            }

            File.CreateText(path);
#endif

            log.LogInformation($"C# Queue trigger function processed: {file}");

            var account = CloudStorageAccount.Parse(
                Environment.GetEnvironmentVariable(Constants.AzureWebJobsStorage));

            var client = account.CreateCloudBlobClient();

            var jsonContainer = client.GetContainerReference(
                Constants.SettingsContainer);
            var jsonBlob = jsonContainer.GetBlockBlobReference(Constants.KeywordsBlob);

            if (!await jsonBlob.ExistsAsync())
            {
                return;
            }

            var json = await jsonBlob.DownloadTextAsync();
            var keywordsList = JsonConvert.DeserializeObject<List<KeywordPair>>(json);

            var newContainer = client.GetContainerReference(
                Constants.TargetMarkdownContainer);
            var newBlob = newContainer.GetBlockBlobReference($"{file}.md");
            var markdown = await newBlob.DownloadTextAsync();

            var replacer = new KeywordReplacer();

            var newMarkdown = replacer.ReplaceInMarkdown(markdown, keywordsList, file);

            await NotificationService.Notify("Replaced keywords", $"Replaced all found keywords in {file}.md", log);
        }
    }
}
