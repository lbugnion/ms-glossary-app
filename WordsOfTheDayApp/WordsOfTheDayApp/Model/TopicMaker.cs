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
    public static class TopicMaker
    {
        private const string BlurbMarker = "> Blurb: ";
        private const string CaptionsMarker = "> Captions: ";
        private const string DateTimeMarker = "<!-- DATETIME -->";
        private const string DownloadCaptionsMarker = "<!-- DOWNLOAD-CAPTIONS -->";
        private const string DownloadCaptionTemplate = "- [{0}](https://wordsoftheday.blob.core.windows.net/captions/{1})";
        private const string DownloadLinkTemplate = "https://wordsoftheday.blob.core.windows.net/videos/{0}.mp4";
        private const string DownloadMarker = "<!-- DOWNLOAD -->";
        private const string H1 = "# ";
        private const string KeywordsMarker = "> Keywords: ";
        private const string YouTubeEmbed = "<iframe width=\"560\" height=\"560\" src=\"https://www.youtube.com/embed/{0}\" frameborder=\"0\" allow=\"accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture\" allowfullscreen></iframe>";
        private const string YouTubeEmbedMarker = "<!-- YOUTUBEEMBED -->";
        private const string YouTubeMarker = "> YouTube: ";

        private static IList<LanguageInfo> MakeLanguages(string captions)
        {
            if (string.IsNullOrEmpty(captions))
            {
                return null;
            }

            var languages = captions.Split(new char[]
            {
                ','
            }, StringSplitOptions.RemoveEmptyEntries);

            var result = new List<LanguageInfo>();

            foreach (var language in languages)
            {
                var parts = language.Split(new char[]
                {
                    '/'
                });

                result.Add(new LanguageInfo
                {
                    Code = parts[0].Trim(),
                    Language = parts[1].Trim()
                });
            }

            return result;
        }

        public static async Task CreateSubtopics(Uri blobUri, ILogger log)
        {

        }

        public static async Task<TopicInformation> CreateTopic(Uri uri, ILogger log)
        {
            var topic = new TopicInformation
            {
                Uri = uri
            };

            var oldMarkdownBlob = new CloudBlockBlob(uri);
            topic.TopicName = Path.GetFileNameWithoutExtension(oldMarkdownBlob.Name);

            topic.LanguageCode = Path.GetExtension(topic.TopicName).Substring(1);
            topic.TopicName = Path.GetFileNameWithoutExtension(topic.TopicName);

            log?.LogInformation("In MarkdownUpdater.CreateTopics");
            log?.LogInformation($"Topic: {topic.TopicName}");

            var account = CloudStorageAccount.Parse(
                Environment.GetEnvironmentVariable(Constants.AzureWebJobsStorageVariableName));

            string oldMarkdown = await oldMarkdownBlob.DownloadTextAsync();
            var markdownReader = new StringReader(oldMarkdown);

            var done = false;
            string youTubeCode = null;
            string keywordsLine = null;
            string topicTitle = null;
            string blurb = null;
            string captions = null;

            while (!done)
            {
                var line = markdownReader.ReadLine();

                if (line == null)
                {
                    log?.LogError($"Invalid markdown file: {topic.TopicName}");
                    await NotificationService.Notify("ERROR in UpdateMarkdown", $"Invalid markdown file: {topic.TopicName}", log);
                    return null;
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
                else if (line.StartsWith(BlurbMarker))
                {
                    blurb = line.Substring(BlurbMarker.Length).Trim();
                    log?.LogInformation($"blurb: {blurb}");
                }
                else if (line.StartsWith(CaptionsMarker))
                {
                    captions = line.Substring(CaptionsMarker.Length).Trim();
                    log?.LogInformation($"blurb: {blurb}");
                }
            }

            topic.Title = topicTitle;
            topic.YouTubeCode = youTubeCode;
            topic.Keywords = keywordsLine;
            topic.Blurb = blurb;
            topic.Captions = MakeLanguages(captions);

            // Check SRT files
            var client = account.CreateCloudBlobClient();
            var helper = new BlobHelper(client, log);

            var captionsContainer = helper.GetContainer(Constants.CaptionsContainerVariableName);
            var captionsFilesList = new StringBuilder();
            log?.LogInformation($"Checking captions for {topic.TopicName}.");

            foreach (var language in topic.Captions)
            {
                var captionFileName = $"{topic.TopicName}.{topic.LanguageCode}.{language.Code}.srt";
                var captionsBlob = captionsContainer.GetBlockBlobReference(captionFileName);
                if (await captionsBlob.ExistsAsync())
                {
                    log?.LogInformation($"Found caption {captionsBlob.Name} for {topic.TopicName}.");

                    captionsFilesList.AppendLine(
                        string.Format(
                            DownloadCaptionTemplate,
                            language.Language,
                            captionsBlob.Name));
                }
            }

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
                var keywordsBlob = settingsContainer.GetBlockBlobReference(
                    string.Format(Constants.KeywordsBlob, topic.LanguageCode));

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
                        .Where(pair => pair.Topic.ToLower() == topic.TopicName.ToLower())
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

                            if (keywordsList.Count == 0)
                            {
                                keywordsDictionary.Remove(key);
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

                            topic.MustDisambiguate = new List<string>();
                            topic.MustDisambiguate.Add(existingPair.Keyword);

                            await NotificationService.Notify(
                                "Duplicate found in new markdown",
                                $"Keyword:{trimmedKeyword} / Old topic: {existingPair.Topic} / New topic: {topic.TopicName}",
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
                        topic.TopicName,
                        newKeyword.ToLower().Replace(' ', '-'),
                        newKeyword,
                        topic.Blurb);
                    AddToKeywordsList(pair);
                }

                var titlePair = new KeywordPair(
                        topic.TopicName,
                        topic.TopicName,
                        topicTitle,
                        topic.Blurb);
                AddToKeywordsList(titlePair);

                json = JsonConvert.SerializeObject(keywordsDictionary);
                await keywordsBlob.UploadTextAsync(json);
            }

            var newContainer = helper.GetContainer(Constants.TopicsContainerVariableName);
            var newBlob = newContainer.GetBlockBlobReference($"{topic.TopicName}.md");
            await newBlob.DeleteIfExistsAsync();
            await newBlob.UploadTextAsync(newMarkdown);
            return topic;
        }
    }
}