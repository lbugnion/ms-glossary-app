// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName=UpdateMarkdown

// /blobServices/default/containers/markdown/blobs
// /blobServices/default/containers/test-markdown/blobs

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
using System.Text;
using System.Globalization;

namespace WordsOfTheDayApp
{
    public static class UpdateMarkdown
    {
        private const string YouTubeEmbed = "<iframe width=\"560\" height=\"560\" src=\"https://www.youtube.com/embed/{0}\" frameborder=\"0\" allow=\"accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture\" allowfullscreen></iframe>";
        private const string YouTubeMarker = "> YouTube: ";
        private const string KeywordsMarker = "> Keywords: ";
        private const string YouTubeEmbedMarker = "<!--YOUTUBEEMBED -->";
        private const string H1 = "# ";
        private const string SideBarTemplate = "- [{0}](/topic/{1})";
        private const string SideBarBoldTemplate = "- [**{0}**](/topic/{1})";

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
            }

            File.CreateText(SemaphorePath);
#endif

            log.LogInformation("Executing UpdateMarkdown");
            log.LogInformation(eventGridEvent.Data.ToString());

            if (eventGridEvent.Data is JObject blobEvent)
            {
                var uri = new Uri(blobEvent["url"].Value<string>());
                var oldBlob = new CloudBlockBlob(uri);
                var topic = Path.GetFileNameWithoutExtension(oldBlob.Name);
                log.LogInformation($"Topic: {topic}");

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
                        log.LogInformation($"youTubeCode: {youTubeCode}");
                    }
                    else if (line.StartsWith(KeywordsMarker))
                    {
                        keywordsLine = line.Substring(KeywordsMarker.Length).Trim();
                        log.LogInformation($"keywordsLine: {keywordsLine}");
                    }
                }

                var newMarkdown = oldMarkdown.Replace(
                    YouTubeEmbedMarker,
                    string.Format(YouTubeEmbed, youTubeCode));

                var client = account.CreateCloudBlobClient();

                // Process keywords first
                if (!string.IsNullOrEmpty(keywordsLine))
                {
                    var settingsContainer = client.GetContainerReference(
                        Environment.GetEnvironmentVariable("SettingsFolder"));

                    log.LogInformation($"settingsContainer: {settingsContainer.Uri}");

                    var keywordsBlob = settingsContainer.GetBlockBlobReference(Constants.KeywordsBlob);
                    var sideBarMarkdownBlob = settingsContainer.GetBlockBlobReference(Constants.SideBarMarkdownBlob);

                    string json = null;
                    Dictionary<char, List<KeywordPair>> keywordsDictionary;

                    var newKeywords = keywordsLine.Split(new char[]
                    {
                        ','
                    }, StringSplitOptions.RemoveEmptyEntries);

                    if (await keywordsBlob.ExistsAsync())
                    {
                        json = await keywordsBlob.DownloadTextAsync();
                        keywordsDictionary = JsonConvert.DeserializeObject<Dictionary<char, List<KeywordPair>>>(json);

                        var duplicates = string.Empty;

                        foreach (var k in newKeywords)
                        {
                            var key = k.ToUpper()[0];

                            if (keywordsDictionary.ContainsKey(key))
                            {
                                var list = keywordsDictionary[key];

                                var items = list
                                    .Where(
                                        k2 => k2.Keyword.ToLower() == k.ToLower())
                                    .ToList();

                                foreach (var item in items)
                                {
                                    duplicates += $"{item.Keyword} / {item.Topic} | ";
                                    list.Remove(item);
                                }

                                if (!string.IsNullOrEmpty(duplicates))
                                {
                                    log.LogInformation($"{oldBlob.Name}: Duplicates found: {duplicates}");

                                    await NotificationService.Notify(
                                        $"{oldBlob.Name}: Duplicates found",
                                        duplicates,
                                        log);
                                }
                            }
                        }
                    }
                    else
                    {
                        keywordsDictionary = new Dictionary<char, List<KeywordPair>>();
                    }

                    foreach (var newKeyword in newKeywords)
                    {
                        var pair = new KeywordPair(topic, newKeyword);
                        var letter = newKeyword.ToUpper()[0];

                        List<KeywordPair> list;
                        if (keywordsDictionary.ContainsKey(letter))
                        {
                            list = keywordsDictionary[letter];
                        }
                        else
                        {
                            list = new List<KeywordPair>();
                            keywordsDictionary.Add(letter, list);
                        }

                        list.Add(pair);
                    }

                    json = JsonConvert.SerializeObject(keywordsDictionary);

                    var md = new StringBuilder();
                    foreach (var pair in keywordsDictionary.OrderBy(p => p.Key))
                    {
                        md.AppendLine($"# {pair.Key}");
                        md.AppendLine();

                        log.LogInformation($"Side bar: {pair.Key}");

                        foreach (var k in pair.Value)
                        {
                            log.LogInformation($"Side bar: {k.Keyword} | {k.Topic}");

                            if (k.Topic.ToLower().Replace('-', ' ') == k.Keyword.ToLower())
                            {
                                md.AppendLine(string.Format(SideBarBoldTemplate, k.Keyword, k.Topic));
                            }
                            else
                            {
                                md.AppendLine(string.Format(SideBarTemplate, k.Keyword, k.Topic));
                            }
                        }

                        md.AppendLine();
                    }

                    await sideBarMarkdownBlob.UploadTextAsync(md.ToString());
                    await keywordsBlob.UploadTextAsync(json);
                    log.LogInformation("Saved the keywords");
                }

                var newContainer = client.GetContainerReference(
                    Environment.GetEnvironmentVariable("MarkdownTransformedFolder"));
                log.LogInformation($"newContainer: {newContainer.Uri}");

                var newBlob = newContainer.GetBlockBlobReference($"{topic}.md");

                await newBlob.DeleteIfExistsAsync();
                await newBlob.UploadTextAsync(newMarkdown);

                log.LogInformation($"Sending notification");

                await NotificationService.Notify(
                    "Uploaded", 
                    $"{topic}.md: Markdown file updated and uploaded", 
                    log);

                log.LogInformation($"Done");
            }
        }
    }
}
