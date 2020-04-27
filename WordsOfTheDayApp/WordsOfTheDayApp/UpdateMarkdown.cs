// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName=UpdateMarkdown
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;
using WordsOfTheDayApp.Model;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace WordsOfTheDayApp
{
    public static class UpdateMarkdown
    {
        private const string YouTubeEmbed = "<iframe width=\"560\" height=\"560\" src=\"https://www.youtube.com/embed/{0}\" frameborder=\"0\" allow=\"accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture\" allowfullscreen></iframe>";
        private const string YouTubeMarker = "> YouTube: ";
        private const string KeywordsMarker = "> Keywords: ";
        private const string YouTubeEmbedMarker = "<!--YOUTUBEEMBED -->";
        private const string H1 = "# ";

#if DEBUG
        public const string SemaphorePath = "c:\\temp\\semaphore.txt";
#endif

        [FunctionName("UpdateMarkdown")]
        public static async Task Run(
            [EventGridTrigger]
            EventGridEvent eventGridEvent, 
            ILogger log)
        {
#if DEBUG
            if (File.Exists(SemaphorePath))
            {
                log.LogError($"Semaphore found at {SemaphorePath}");
                return;
                //return new BadRequestObjectResult("Already running in DEBUG mode");
            }

            File.CreateText(SemaphorePath);
#endif
            
            log.LogInformation(eventGridEvent.Data.ToString());

            if (eventGridEvent.Data is JObject blobEvent)
            {
                var uri = new Uri(blobEvent["url"].Value<string>());
                var oldBlob = new CloudBlockBlob(uri);
                var topic = Path.GetFileNameWithoutExtension(oldBlob.Name);

                var account = CloudStorageAccount.Parse(
                    Environment.GetEnvironmentVariable(Constants.AzureWebJobsStorage));

                string oldMarkdown = await oldBlob.DownloadTextAsync();
                var markdownReader = new StringReader(oldMarkdown);

                var done = false;
                string youTubeCode = null;
                string keywordsLine = null;

                while (!done)
                {
                    var line = markdownReader.ReadLine();

                    if (line.StartsWith(H1))
                    {
                        oldMarkdown = oldMarkdown.Substring(oldMarkdown.IndexOf(H1));
                        done = true;
                    }
                    else if (line.StartsWith(YouTubeMarker))
                    {
                        youTubeCode = line.Substring(YouTubeMarker.Length).Trim();
                    }
                    else if (line.StartsWith(KeywordsMarker))
                    {
                        keywordsLine = line.Substring(KeywordsMarker.Length).Trim();
                    }
                }

                var newMarkdown = oldMarkdown.Replace(
                    YouTubeEmbedMarker,
                    string.Format(YouTubeEmbed, youTubeCode));

                var client = account.CreateCloudBlobClient();

                // Process keywords first
                if (!string.IsNullOrEmpty(keywordsLine))
                {
                    var jsonContainer = client.GetContainerReference(
                        Constants.SettingsContainer);
                    var jsonBlob = jsonContainer.GetBlockBlobReference(Constants.KeywordsBlob);

                    string json = null;
                    List<KeywordPair> keywordsList;

                    if (await jsonBlob.ExistsAsync())
                    {
                        json = await jsonBlob.DownloadTextAsync();
                        keywordsList = JsonConvert.DeserializeObject<List<KeywordPair>>(json);
                        var keyWordsForThisTopic = keywordsList.Where(k => k.Topic == topic).ToList();

                        foreach (var k in keyWordsForThisTopic)
                        {
                            var existingKeyword = keywordsList
                                .FirstOrDefault(k2 => k2.Keyword == k.Keyword && k2.Topic == k.Topic);

                            if (existingKeyword != null)
                            {
                                keywordsList.Remove(existingKeyword);
                            }
                        }
                    }
                    else
                    {
                        keywordsList = new List<KeywordPair>();
                    }

                    var newKeywords = keywordsLine.Split(new char[]
                    {
                        ','
                    }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var newKeyword in newKeywords)
                    {
                        var pair = new KeywordPair(topic, newKeyword.Trim());
                        keywordsList.Add(pair);
                    }

                    json = JsonConvert.SerializeObject(keywordsList);
                    await jsonBlob.UploadTextAsync(json);
                }

                var newContainer = client.GetContainerReference(
                    Constants.TargetMarkdownContainer);
                var newBlob = newContainer.GetBlockBlobReference($"{topic}.md");

                await newBlob.DeleteIfExistsAsync();
                await newBlob.UploadTextAsync(newMarkdown);

                await NotificationService.Notify(
                    "Uploaded", 
                    $"{topic}.md: Markdown file updated and uploaded", 
                    log);
            }
        }
    }
}
