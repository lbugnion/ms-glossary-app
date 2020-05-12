using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WordsOfTheDayApp.Model
{
    public static class MarkdownReplacer
    {
        public static async Task ReplaceKeywords(
            TopicInformation topic,
            ILogger log)
        {
            var account = CloudStorageAccount.Parse(
                Environment.GetEnvironmentVariable(Constants.AzureWebJobsStorageVariableName));

            var client = account.CreateCloudBlobClient();
            var helper = new BlobHelper(client, log);
            var settingsContainer = helper.GetContainer(Constants.SettingsContainerVariableName);

            var jsonBlob = settingsContainer.GetBlockBlobReference(
                string.Format(Constants.KeywordsBlob, topic.LanguageCode));

            if (!await jsonBlob.ExistsAsync())
            {
                log?.LogError($"jsonBlob not found: {jsonBlob.Uri}");
            }

            var json = await jsonBlob.DownloadTextAsync();
            var keywordsDictionary = JsonConvert.DeserializeObject<Dictionary<char, List<KeywordPair>>>(json);

            var keywordsList = keywordsDictionary.Values
                .SelectMany(x => x)
                .ToList();

            var newContainer = helper.GetContainer(Constants.TopicsContainerVariableName);

            CloudBlockBlob newBlob = null;
            string markdown = null;
            Exception error = null;

            try
            {
                newBlob = newContainer.GetBlockBlobReference($"{topic.TopicName}.{topic.LanguageCode}.md");
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
                    $"Error when loading blob {topic.TopicName}.{topic.LanguageCode}.md : {error.Message}",
                    log);

                log?.LogError($"Cannot load blob: {topic.TopicName}.{topic.LanguageCode}.md");
            }

            var replacer = new KeywordReplacer();

            var (newMarkdown, replaced) = replacer.ReplaceInMarkdown(markdown, keywordsList, topic.TopicName, log);

            if (newMarkdown == markdown)
            {
                await NotificationService.Notify(
                    $"No keywords replaced in file {topic.TopicName}.{topic.LanguageCode}",
                    "No keywords found",
                    log);
            }
            else
            {
                try
                {
                    log?.LogInformation("Uploading");
                    await newBlob.UploadTextAsync(newMarkdown);
                    await NotificationService.Notify(
                        $"Replaced keywords in file {topic.TopicName}.{topic.LanguageCode}",
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
                        $"Error when uploading blob {topic.TopicName}.{topic.LanguageCode}.md : {error.Message}",
                        log);

                    log?.LogError($"Cannot upload blob: {topic.TopicName}.{topic.LanguageCode}.md");
                }
            }

            log?.LogInformation($"Done replacing keywords in {topic.TopicName}.{topic.LanguageCode}");
        }
    }
}