using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordsOfTheDayApp.Model
{
    public static class MarkdownUpdater
    {
        private const string DownloadCaptionsMarker = "<!-- DOWNLOAD-CAPTIONS -->";
        private const string DownloadCaptionTemplate = "- [{0}](https://wordsoftheday.blob.core.windows.net/captions/{1})";
        private const string DownloadLinkTemplate = "https://wordsoftheday.blob.core.windows.net/videos/{0}.mp4";
        private const string DownloadMarker = "<!-- DOWNLOAD -->";
        private const string DateTimeMarker = "<!-- DATETIME -->";
        private const string H1 = "# ";
        private const string KeywordsMarker = "> Keywords: ";
        private const string YouTubeEmbed = "<iframe width=\"560\" height=\"560\" src=\"https://www.youtube.com/embed/{0}\" frameborder=\"0\" allow=\"accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture\" allowfullscreen></iframe>";
        private const string YouTubeEmbedMarker = "<!-- YOUTUBEEMBED -->";
        private const string YouTubeMarker = "> YouTube: ";

        public static async Task<string> Update(Uri uri, ILogger log)
        {
            var oldBlob = new CloudBlockBlob(uri);
            var topic = Path.GetFileNameWithoutExtension(oldBlob.Name);
            log?.LogInformation("In MarkdownUpdater.Update");
            log?.LogInformation($"Topic: {topic}");

            var account = CloudStorageAccount.Parse(
                Environment.GetEnvironmentVariable(Constants.AzureWebJobsStorageVariableName));

            string oldMarkdown = await oldBlob.DownloadTextAsync();
            var markdownReader = new StringReader(oldMarkdown);

            var done = false;
            string youTubeCode = null;
            string keywordsLine = null;
            string topicTitle = null;

            while (!done)
            {
                var line = markdownReader.ReadLine();

                if (line == null)
                {
                    log?.LogError($"Invalid markdown file: {topic}");
                    await NotificationService.Notify("ERROR in UpdateMarkdown", $"Invalid markdown file: {topic}", log);
                    return topic;
                }

                if (line.StartsWith(H1))
                {
                    topicTitle = line
                        .Substring(H1.Length)
                        .Substring(1);

                    topicTitle = topicTitle
                        .Substring(0, topicTitle.IndexOf(']'))
                        .Trim();

                    oldMarkdown = oldMarkdown.Substring(oldMarkdown.IndexOf(H1));
                    done = true;
                }
                else if (line.StartsWith(YouTubeMarker))
                {
                    youTubeCode = line.Substring(YouTubeMarker.Length).Trim();
                    log?.LogInformation($"youTubeCode: {youTubeCode}");
                }
                else if (line.StartsWith(KeywordsMarker))
                {
                    keywordsLine = line.Substring(KeywordsMarker.Length).Trim();
                    log?.LogInformation($"keywordsLine: {keywordsLine}");
                }
            }

            // Check SRT files
            BlobContinuationToken continuationToken = null;
            var client = account.CreateCloudBlobClient();
            var helper = new BlobHelper(client, log);

            var captionsContainer = helper.GetContainer(Constants.CaptionsContainerVariableName);
            var captionsFilesList = new StringBuilder();
            var textInfo = CultureInfo.InvariantCulture.TextInfo;
            log?.LogInformation($"Checking captions for {topic}.");

            do
            {
                var response = await captionsContainer.ListBlobsSegmentedAsync(continuationToken);
                continuationToken = response.ContinuationToken;

                foreach (CloudBlockBlob captionBlob in response.Results)
                {
                    log?.LogInformation($"Found caption {captionBlob.Name} for {topic}.");

                    var nameParts = captionBlob.Name.Split(new[]
                    {
                        '.'
                    });

                    if (nameParts.Length != 3
                        || !captionBlob.Name.EndsWith(".srt"))
                    {
                        log?.LogError($"Invalid SRT in {captionsContainer.Uri}: {captionBlob.Name}");
                        continue;
                    }

                    if (nameParts[0].ToLower() != $"{topic.ToLower()}")
                    {
                        log?.LogInformation($"{captionBlob.Name} is NOT used for {topic}.");
                        continue;
                    }

                    log?.LogInformation($"{captionBlob.Name} is used for {topic}.");

                    captionsFilesList.AppendLine(
                        string.Format(
                            DownloadCaptionTemplate,
                            textInfo.ToTitleCase(nameParts[1]),
                            captionBlob.Name));
                }
            }
            while (continuationToken != null);

            var newMarkdown = oldMarkdown
                .Replace(
                    YouTubeEmbedMarker,
                    string.Format(YouTubeEmbed, youTubeCode))
                .Replace(
                    DownloadMarker,
                    string.Format(DownloadLinkTemplate, topic))
                .Replace(
                    DownloadCaptionsMarker,
                    captionsFilesList.ToString())
                .Replace(
                    DateTimeMarker,
                    DateTime.Now.ToString("dd MMM yyyy HH:mm"));

            // Process keywords first
            if (!string.IsNullOrEmpty(keywordsLine))
            {
                var settingsContainer = helper.GetContainer(Constants.SettingsContainerVariableName);
                var keywordsBlob = settingsContainer.GetBlockBlobReference(Constants.KeywordsBlob);

                string json = null;
                Dictionary<char, List<KeywordPair>> keywordsDictionary;

                var newKeywords = keywordsLine.Split(new char[]
                    {
                        ','
                    }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();

                var title = newKeywords.FirstOrDefault(k => k.ToLower().Trim() == topicTitle.ToLower());

                if (!string.IsNullOrEmpty(title))
                {
                    newKeywords.Remove(title);
                }

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
                            var keywordsList = keywordsDictionary[key];
                            if (keywordsList.Contains(existingPair))
                            {
                                keywordsList.Remove(existingPair);
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
                            
                            // TODO Register the creation of a Disambiguation page

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

                void AddToKeywordsList(KeywordPair pair)
                {
                    var letter = pair.Keyword.ToUpper()[0];

                    List<KeywordPair> keywordsList;
                    if (keywordsDictionary.ContainsKey(letter))
                    {
                        keywordsList = keywordsDictionary[letter];
                    }
                    else
                    {
                        keywordsList = new List<KeywordPair>();
                        keywordsDictionary.Add(letter, keywordsList);
                    }

                    keywordsList.Add(pair);
                }

                foreach (var newKeyword in newKeywords.Select(k => k.Trim()))
                {
                    var pair = new KeywordPair(
                        topic, 
                        newKeyword.ToLower().Replace(' ', '-'), 
                        newKeyword);
                    AddToKeywordsList(pair);
                }

                var titlePair = new KeywordPair(topic, topic, topicTitle);
                AddToKeywordsList(titlePair);

                json = JsonConvert.SerializeObject(keywordsDictionary);
                await keywordsBlob.UploadTextAsync(json);
            }

            var newContainer = helper.GetContainer(Constants.TopicsContainerVariableName);
            var newBlob = newContainer.GetBlockBlobReference($"{topic}.md");
            await newBlob.DeleteIfExistsAsync();
            await newBlob.UploadTextAsync(newMarkdown);
            return topic;
        }
    }
}