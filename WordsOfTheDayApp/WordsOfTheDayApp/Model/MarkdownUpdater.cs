using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordsOfTheDayApp.Model
{
    public static class MarkdownUpdater
    {
        private const string YouTubeEmbed = "<iframe width=\"560\" height=\"560\" src=\"https://www.youtube.com/embed/{0}\" frameborder=\"0\" allow=\"accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture\" allowfullscreen></iframe>";
        private const string YouTubeMarker = "> YouTube: ";
        private const string KeywordsMarker = "> Keywords: ";
        private const string YouTubeEmbedMarker = "<!--YOUTUBEEMBED -->";
        private const string H1 = "# ";
        private const string SideBarTemplate = "- [{0}](/topic/{1}/{2})";
        private const string SideBarBoldTemplate = "- [**{0}**](/topic/{1})";

        public static async Task<string> Update(Uri uri, ILogger log)
        {
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

                if (line == null)
                {
                    log.LogError($"Invalid markdown file: {topic}");
                    await NotificationService.Notify("ERROR in UpdateMarkdown", $"Invalid markdown file: {topic}", log);
                    return topic;
                }

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

                    var existingPairs = keywordsDictionary.Values
                        .SelectMany(pair => pair)
                        .Where(pair => pair.Topic.ToLower() == topic.ToLower())
                        .ToList();

                    foreach (var existingPair in existingPairs)
                    {
                        var key = existingPair.Keyword.ToUpper()[0];

                        // Just making sure                            
                        if (keywordsDictionary.ContainsKey(key))
                        {
                            var list = keywordsDictionary[key];
                            if (list.Contains(existingPair))
                            {
                                list.Remove(existingPair);
                            }
                        }
                    }

                    foreach (var k in newKeywords)
                    {
                        var trimmedKeyword = k.Trim();
                        var existingPair = keywordsDictionary.Values
                            .SelectMany(pair => pair)
                            .FirstOrDefault(pair => pair.Keyword.ToLower() == trimmedKeyword.ToLower());

                        if (existingPair != null)
                        {
                            // We got a problem, notify the process owner, add anyway
                            // (this creates a duplicate in the topics bar).

                            await NotificationService.Notify(
                                "Duplicate found in new markdown",
                                $"Keyword:{trimmedKeyword} / Old topic: {existingPair.Topic} / New topic: {topic}",
                                log);
                        }
                    }
                }
                else
                {
                    keywordsDictionary = new Dictionary<char, List<KeywordPair>>();
                }

                foreach (var newKeyword in newKeywords.Select(k => k.Trim()))
                {
                    var pair = new KeywordPair(topic, newKeyword.ToLower().Replace(' ', '-'), newKeyword);
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
                    md.AppendLine($"#### {pair.Key}");
                    md.AppendLine();

                    log.LogInformation($"Side bar: {pair.Key}");

                    foreach (var k in pair.Value.OrderBy(v => v.Keyword))
                    {
                        log.LogInformation($"Side bar: {k.Keyword} | {k.Topic}");

                        if (k.Topic == k.Subtopic)
                        {
                            md.AppendLine(string.Format(SideBarBoldTemplate, k.Keyword, k.Topic));
                        }
                        else
                        {
                            md.AppendLine(string.Format(SideBarTemplate, k.Keyword, k.Topic, k.Subtopic));
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
            return topic;
        }
    }
}
