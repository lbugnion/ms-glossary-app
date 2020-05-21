using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.FileIO;
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
        private const string LanguageMarker = "> Language: ";
        private const string TwitterMarker = "> Twitter: ";
        private const string DateTimeMarker = "<!-- DATETIME -->";
        private const string OtherLanguagesMarker = "<!-- OTHERLANGUAGES -->";
        private const string DownloadCaptionsMarker = "<!-- DOWNLOAD-CAPTIONS -->";
        private const string DownloadCaptionTemplate = "- [{0}](https://wordsoftheday.blob.core.windows.net/{1}/{2})";
        private const string LastChangeDateTimeFormat = "dd MMM yyyy HH:mm";
        private const string TwitterLinkMask = "http://twitter.com/{0}";

        public static async Task DeleteAllSettings(ILogger log)
        {
            var account = CloudStorageAccount.Parse(
                Environment.GetEnvironmentVariable(Constants.AzureWebJobsStorageVariableName));
            var client = account.CreateCloudBlobClient();
            var helper = new BlobHelper(client, log);
            var settingsContainer = helper.GetContainer(Constants.SettingsContainerVariableName);

            BlobContinuationToken continuationToken = null;

            do
            {
                var response = await settingsContainer.ListBlobsSegmentedAsync(continuationToken);
                continuationToken = response.ContinuationToken;

                foreach (CloudBlockBlob blob in response.Results)
                {
                    log?.LogInformation($"Deleting: {blob.Name}");
                    await blob.DeleteAsync();
                }
            }
            while (continuationToken != null);
        }

        public static async Task DeleteAllTopics(ILogger log)
        {
            var account = CloudStorageAccount.Parse(
                Environment.GetEnvironmentVariable(Constants.AzureWebJobsStorageVariableName));
            var client = account.CreateCloudBlobClient();
            var helper = new BlobHelper(client, log);
            var topicsContainer = helper.GetContainer(Constants.TopicsContainerVariableName);

            BlobContinuationToken continuationToken = null;

            do
            {
                var response = await topicsContainer.ListBlobsSegmentedAsync(continuationToken);
                continuationToken = response.ContinuationToken;

                foreach (CloudBlockBlob blob in response.Results)
                {
                    log?.LogInformation($"Deleting: {blob.Name}");
                    await blob.DeleteAsync();
                }
            }
            while (continuationToken != null);
        }

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

        public static async Task CreateDisambiguation(
            Dictionary<string, List<KeywordPair>> dic, 
            ILogger log)
        {
            if (dic.Keys.Count != 1
                || dic.Values.Count != 1
                || dic.Values.First().Count < 1)
            {
                log?.LogError("Invalid dictionary received in CreateDisambiguation");
                return;
            }

            var keyword = dic.Keys.First();
            var safeKeyword = keyword.MakeSafeFileName();

            var keywordInfos = dic.Values.First();

            var languageCode = dic.Values.First().First().LanguageCode;
            var link = $"{safeKeyword}_{Constants.Disambiguation}";
            var fileName = $"{link}.{languageCode}.md";

            var account = CloudStorageAccount.Parse(
                Environment.GetEnvironmentVariable(Constants.AzureWebJobsStorageVariableName));
            var client = account.CreateCloudBlobClient();
            var helper = new BlobHelper(client, log);
            var topicsContainer = helper.GetContainer(Constants.TopicsContainerVariableName);
            var disambiguationBlob = topicsContainer.GetBlockBlobReference(fileName);

            var builder = new StringBuilder();

            builder.AppendLine(
                TextHelper.GetText(languageCode, Constants.Texts.TopicHeader));

            builder.AppendLine(
                string.Format(
                        TextHelper.GetText(languageCode, Constants.Texts.DisambiguationTitle),
                        keyword,
                        languageCode,
                        link));

            builder.AppendLine(
                string.Format(
                        TextHelper.GetText(languageCode, Constants.Texts.DisambiguationIntro),
                        keyword));

            foreach (var keywordInfo in keywordInfos.OrderBy(k => k.TopicTitle))
            {
                builder.AppendLine(
                    string.Format(
                        TextHelper.GetText(languageCode, Constants.Texts.DisambiguationItem),
                        keywordInfo.TopicTitle,
                        keywordInfo.LanguageCode,
                        keywordInfo.Topic,
                        safeKeyword,
                        keywordInfo.Blurb));
            }

            await disambiguationBlob.UploadTextAsync(builder.ToString());
        }

        public static async Task CreateSubtopics(TopicInformation topic, ILogger log)
        {
            var account = CloudStorageAccount.Parse(
                Environment.GetEnvironmentVariable(Constants.AzureWebJobsStorageVariableName));
            var client = account.CreateCloudBlobClient();
            var helper = new BlobHelper(client, log);
            var topicsContainer = helper.GetContainer(Constants.TopicsContainerVariableName);
            var topicBlob = topicsContainer.GetBlockBlobReference($"{topic.TopicName}.{topic.Language.Code}.md");
            var topicMarkdown = await topicBlob.DownloadTextAsync();

            var reader = new StringReader(topicMarkdown);
            var header = new StringBuilder();
            var restOfFile = new StringBuilder();
            var foundH1 = false;
            string line;

            while ((line = reader.ReadLine()) != null)
            {
                header.AppendLine(line);

                if (line.StartsWith(H1))
                {
                    foundH1 = true;
                    break;
                }
            }
            
            if (!foundH1)
            {
                log?.LogError($"Invalid topic file: {topic.TopicName}.{topic.Language.Code}");
                return;
            }

            restOfFile.Append(reader.ReadToEnd());

            foreach (var pair in topic.Keywords)
            {
                var newBuilder = new StringBuilder(header.ToString());
                var text = string.Format(
                    TextHelper.GetText(topic.Language.Code, Constants.Texts.RedirectedFrom), 
                    pair.Keyword);

                newBuilder.AppendLine($"###### ({text})");
                newBuilder.Append(restOfFile.ToString());

                var subtopicBlob = topicsContainer.GetBlockBlobReference(
                    $"{topic.TopicName}_{pair.Subtopic}.{topic.Language.Code}.md");
                await subtopicBlob.UploadTextAsync(newBuilder.ToString());

                log?.LogInformation($"Updated subtopic file {topic.TopicName}_{pair.Subtopic}.{topic.Language.Code}.md");
            }
        }

        public static async Task<TopicInformation> CreateTopic(Uri uri, ILogger log)
        {
            var topic = new TopicInformation
            {
                Uri = uri
            };

            var oldMarkdownBlob = new CloudBlockBlob(uri);
            topic.TopicName = Path.GetFileNameWithoutExtension(oldMarkdownBlob.Name);
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
            string language = null;
            string twitter = null;

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
                else if (line.StartsWith(LanguageMarker))
                {
                    language = line.Substring(LanguageMarker.Length).Trim();
                    log?.LogInformation($"language: {language}");
                }
                else if (line.StartsWith(TwitterMarker))
                {
                    twitter = line.Substring(TwitterMarker.Length).Trim();
                    log?.LogInformation($"twitter: {twitter}");
                }
            }

            topic.Title = topicTitle;
            topic.YouTubeCode = youTubeCode;
            topic.Captions = MakeLanguages(captions);
            topic.Language = MakeLanguages(language).First();

            // Check SRT files
            var client = account.CreateCloudBlobClient();
            var helper = new BlobHelper(client, log);

            var captionsContainer = helper.GetContainer(Constants.CaptionsContainerVariableName);
            var captionsFilesList = new StringBuilder();
            log?.LogInformation($"Checking captions for {topic.TopicName}.");

            foreach (var captionLanguage in topic.Captions)
            {
                var captionFileName = $"{topic.TopicName}.{topic.Language.Code}.{captionLanguage.Code}.srt";
                var captionsBlob = captionsContainer.GetBlockBlobReference(captionFileName);
                if (await captionsBlob.ExistsAsync())
                {
                    log?.LogInformation($"Found caption {captionsBlob.Name} for {topic.TopicName}.");

                    captionsFilesList.AppendLine(
                        string.Format(
                            DownloadCaptionTemplate,
                            captionLanguage.Language,
                            captionsContainer.Name,
                            captionsBlob.Name));
                }
            }

            var newMarkdown = new StringBuilder()
                .AppendLine(TextHelper.GetText(topic.Language.Code, Constants.Texts.TopicHeader))
                .AppendLine(oldMarkdown)
                .Replace(
                    YouTubeEmbedMarker,
                    string.Format(YouTubeEmbed, youTubeCode))
                .Replace(
                    DownloadMarker,
                    string.Format(DownloadLinkTemplate, topic))
                .Replace(
                    DownloadCaptionsMarker,
                    captionsFilesList.ToString());

            var lastChange = string.Empty;

            if (!string.IsNullOrEmpty(twitter))
            {
                if (twitter.StartsWith("@"))
                {
                    twitter = twitter.Substring(1);
                }

                var twitterLink = string.Format(TwitterLinkMask, twitter);
                var byText = TextHelper.GetText(topic.Language.Code, Constants.Texts.By);

                lastChange = $"{DateTime.Now.ToString(LastChangeDateTimeFormat)} {byText} [@{twitter}]({twitterLink})";
            }
            else
            {
                lastChange = DateTime.Now.ToString(LastChangeDateTimeFormat);
            }

            var copyrightText = string.Format(
                TextHelper.GetText(
                    topic.Language.Code,
                    Constants.Texts.CopyrightInfo),
                DateTime.Now.Year,
                Texts.TwitterUrl);
            
            var lastModifiedText = string.Format(
                TextHelper.GetText(
                    topic.Language.Code,
                    Constants.Texts.LastModified),
                lastChange);

            newMarkdown
                .AppendLine()
                .AppendLine()
                .AppendLine(lastModifiedText)
                .AppendLine()
                .AppendLine(copyrightText);

            // Process keywords first
            if (!string.IsNullOrEmpty(keywordsLine))
            {
                var settingsContainer = helper.GetContainer(Constants.SettingsContainerVariableName);
                var keywordsBlob = settingsContainer.GetBlockBlobReference(
                    string.Format(Constants.KeywordsBlob, topic.Language.Code));

                string json = null;
                Dictionary<char, List<KeywordPair>> keywordsDictionary;

                var newKeywords = keywordsLine.Split(new char[]
                    {
                        ','
                    }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(k => k.Trim())
                    .ToList();

                var existingTitle = newKeywords.FirstOrDefault(k => k.ToLower() == topicTitle.ToLower());

                if (!string.IsNullOrEmpty(existingTitle))
                {
                    newKeywords.Remove(existingTitle);
                }

                var existingTopic = newKeywords.FirstOrDefault(k => k.ToLower() == topic.TopicName.ToLower());

                if (!string.IsNullOrEmpty(existingTopic))
                {
                    newKeywords.Remove(existingTopic);
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

                topic.Keywords = new List<KeywordPair>();

                foreach (var newKeyword in newKeywords)
                {
                    var ambiguousPairs = keywordsDictionary.Values
                        .SelectMany(pair => pair)
                        .Where(pair => pair.Keyword.ToLower() == newKeyword.ToLower())
                        .ToList();

                    var pair = new KeywordPair(
                        topic.Language.Code,
                        topic.Title,
                        topic.TopicName,
                        newKeyword.MakeSafeFileName(),
                        newKeyword,
                        blurb);

                    KeywordPair existingDisambiguation = null;

                    if (ambiguousPairs.Count > 0)
                    {
                        pair.MustDisambiguate = true;

                        foreach (var disambiguationPair in ambiguousPairs)
                        {
                            if (disambiguationPair.IsDisambiguation)
                            {
                                existingDisambiguation = disambiguationPair;
                            }
                            else
                            {
                                disambiguationPair.MustDisambiguate = true;
                            }
                        }

                        if (existingDisambiguation == null)
                        {
                            existingDisambiguation = new KeywordPair(
                                topic.Language.Code,
                                null,
                                newKeyword.MakeSafeFileName(),
                                Constants.Disambiguation,
                                newKeyword,
                                null)
                            {
                                IsDisambiguation = true
                            };

                            AddToKeywordsList(existingDisambiguation);
                        }
                    }

                    AddToKeywordsList(pair);
                    topic.Keywords.Add(pair);
                }

                var titlePair = new KeywordPair(
                        topic.Language.Code,
                        topic.Title,
                        topic.TopicName,
                        topic.TopicName,
                        topicTitle,
                        blurb);
                AddToKeywordsList(titlePair);

                json = JsonConvert.SerializeObject(keywordsDictionary);
                await keywordsBlob.UploadTextAsync(json);
            }

            var newContainer = helper.GetContainer(Constants.TopicsContainerVariableName);
            var newBlob = newContainer.GetBlockBlobReference($"{topic.TopicName}.{topic.Language.Code}.md");
            await newBlob.UploadTextAsync(newMarkdown.ToString());
            return topic;
        }

        public static async Task UpdateOtherLanguages(
            IList<TopicInformation> topicsByLanguage,
            ILogger log)
        {
            var account = CloudStorageAccount.Parse(
                Environment.GetEnvironmentVariable(Constants.AzureWebJobsStorageVariableName));
            var client = account.CreateCloudBlobClient();
            var helper = new BlobHelper(client, log);

            var topicsContainer = helper.GetContainer(Constants.TopicsContainerVariableName);

            var allLanguages = topicsByLanguage
                .Select(g => g.Language)
                .ToList();

            foreach (var topic in topicsByLanguage)
            {
                var languagesBuilder = new StringBuilder();

                languagesBuilder
                    .AppendLine(TextHelper.GetText(
                        topic.Language.Code, 
                        Constants.Texts.ThisPageIsAlsoAvailableIn))
                    .AppendLine();

                foreach (var language in allLanguages)
                {
                    if (language.Code == topic.Language.Code)
                    {
                        continue;
                    }

                    languagesBuilder
                        .AppendLine($"- [{language.Language}](/topic/{language.Code}/{topic.TopicName})");
                }

                var topicsFileName = $"{topic.TopicName}.{topic.Language.Code}.md";
                var topicBlob = topicsContainer.GetBlockBlobReference(topicsFileName);

                var markdown = await topicBlob.DownloadTextAsync();
                var builder = new StringBuilder(markdown);
                builder.Replace(OtherLanguagesMarker, languagesBuilder.ToString());

                await topicBlob.UploadTextAsync(builder.ToString());
            }
        }
    }
}