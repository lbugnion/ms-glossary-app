using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MsGlossaryApp.Model
{
    public static class TopicMaker
    {
        private const string H1 = "# ";
        private const char Separator = '|';
        private const string YouTubeMarker = "> YouTube: ";
        private const string KeywordsMarker = "> Keywords: ";
        private const string BlurbMarker = "> Blurb: ";
        private const string CaptionsMarker = "> Captions: ";
        private const string LanguageMarker = "> Language: ";
        private const string TwitterMarker = "> Twitter: ";
        private const string GitHubMarker = "> GitHub: ";
        private const string RecordingDateMarker = "> Recording date: ";

        public static async Task<IList<KeywordInformation>> SortKeywords(
            IList<TopicInformation> allTopics,
            TopicInformation currentTopic,
            ILogger log = null)
        {
            var result = new List<KeywordInformation>();

            foreach (var keyword in currentTopic.Keywords)
            {
                var newKeyword = new KeywordInformation
                {
                    Keyword = keyword,
                    Topic = currentTopic
                };

                var sameKeywords = allTopics
                    .SelectMany(t => t.Keywords)
                    .Where(k => k.ToLower() == keyword.ToLower());

                if (sameKeywords.Count() > 1)
                {
                    newKeyword.MustDisambiguate = true;
                }

                result.Add(newKeyword);
            }

            return result;
        }

        //private const string LanguagesTitleMarker = "<!-- LANGUAGESTITLE -->";
        //private const string YouTubeEmbed = "> [!VIDEO https://www.youtube.com/embed/{0}]";
        //private const string DownloadLinkTemplate = "https://msglossarystory.blob.core.windows.net/videos/{0}.{1}.mp4";
        //private const string VideoDownloadLinkMarker = "LINK";
        //private const string DownloadTarget = "<a id=\"download\"></a>";
        //private const string YouTubeEmbedMarker = "<!-- YOUTUBEEMBED -->";
        //private const string DownloadMarker = "<!-- DOWNLOAD -->";
        //private const string DownloadCaptionsMarker = "<!-- DOWNLOAD-CAPTIONS -->";
        //private const string TwitterLinkMask = "http://twitter.com/{0}";
        //private const string LastChangeDateTimeFormat = "dd MMM yyyy HH:mm";
        private const string EmailMarker = "> Email: ";
        private const string AuthorNameMarker = "> Author name: ";

        public static async Task<TopicInformation> CreateTopic(Uri uri, ILogger log)
        {
            var topic = new TopicInformation
            {
                Uri = uri
            };

            var topicBlob = new CloudBlockBlob(uri);
            topic.TopicName = Path.GetFileNameWithoutExtension(topicBlob.Name);
            topic.TopicName = Path.GetFileNameWithoutExtension(topic.TopicName);

            log?.LogInformation("In MarkdownUpdater.CreateTopics");
            log?.LogInformation($"Topic: {topic.TopicName}");

            string oldMarkdown = await topicBlob.DownloadTextAsync();
            var markdownReader = new StringReader(oldMarkdown);

            var done = false;
            string youTubeCode = null;
            string keywordsLine = null;
            string topicTitle = null;
            string blurb = null;
            string captions = null;
            string language = null;
            string authorName = null;
            string email = null;
            string twitter = null;
            string github = null;
            DateTime recordingDate = DateTime.MinValue;

            while (!done)
            {
                var line = markdownReader.ReadLine();

                if (line == null)
                {
                    log?.LogError($"Invalid markdown file: {topic.TopicName}");
                    await NotificationService.Notify(
                        "ERROR in TopicMaker",
                        $"Invalid markdown file: {topic.TopicName}",
                        log);
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
                    log?.LogInformation($"captions: {captions}");
                }
                else if (line.StartsWith(LanguageMarker))
                {
                    language = line.Substring(LanguageMarker.Length).Trim();
                    log?.LogInformation($"language: {language}");
                }
                else if (line.StartsWith(AuthorNameMarker))
                {
                    authorName = line.Substring(AuthorNameMarker.Length).Trim();
                    log?.LogInformation($"authorName: {authorName}");
                }
                else if (line.StartsWith(EmailMarker))
                {
                    email = line.Substring(EmailMarker.Length).Trim();
                    log?.LogInformation($"email: {email}");
                }
                else if (line.StartsWith(GitHubMarker))
                {
                    github = line.Substring(GitHubMarker.Length).Trim();
                    log?.LogInformation($"github: {github}");
                }
                else if (line.StartsWith(TwitterMarker))
                {
                    twitter = line.Substring(TwitterMarker.Length).Trim();
                    log?.LogInformation($"twitter: {twitter}");
                }
                else if (line.StartsWith(RecordingDateMarker))
                {
                    var dateString = line.Substring(RecordingDateMarker.Length).Trim();
                    log?.LogInformation($"dateString: {dateString}");
                    recordingDate = DateTime.ParseExact(dateString, "yyyyMMdd", CultureInfo.InvariantCulture);
                }
            }

            topic.Title = topicTitle;
            topic.RecordingDate = recordingDate;
            topic.YouTubeCode = youTubeCode;
            topic.Blurb = blurb;
            topic.Authors = MakeAuthors(authorName, email, github, twitter);
            topic.Captions = MakeLanguages(captions);
            topic.Language = MakeLanguages(language).First();
            topic.Keywords = keywordsLine.Split(new char[]
            {
                ','
            }, StringSplitOptions.RemoveEmptyEntries)
            .Select(k => k.Trim())
            .ToList();

            // Prepare replacements

            //var youtubeEmbed = new StringBuilder();
            //var download = new StringBuilder();
            //var downloadCaptions = new StringBuilder();
            //var languagesTitle = LanguagesTitleMarker;

            //if (!string.IsNullOrEmpty(topic.YouTubeCode))
            //{
            //    youtubeEmbed
            //        .AppendLine(string.Format(YouTubeEmbed, youTubeCode))
            //        .AppendLine()
            //        .AppendLine(TextHelper.GetText(topic.Language.Code, Constants.Texts.VideoDownload));

            //    var videoUrl = string.Format(DownloadLinkTemplate, topic.TopicName, topic.Language.Code);
            //    var videoDownloadLink = TextHelper.GetText(topic.Language.Code, Constants.Texts.VideoDownloadLink)
            //        .Replace(VideoDownloadLinkMarker, videoUrl);

            //    download
            //        .AppendLine(DownloadTarget)
            //        .AppendLine()
            //        .AppendLine(TextHelper.GetText(topic.Language.Code, Constants.Texts.DownloadTitle))
            //        .AppendLine()
            //        .AppendLine(videoDownloadLink);
            //}

            //if (topic.Captions?.Count > 0)
            //{
            //    // TODO Captions

            //    //languagesTitle = TextHelper.GetText(topic.Language.Code, Constants.Texts.LanguagesTitle);

            //    //downloadCaptions
            //    //    .AppendLine(TextHelper.GetText(topic.Language.Code, Constants.Texts.CaptionsDownloadTitle))
            //    //    .AppendLine()
            //    //    .AppendLine(topic.Captions)
            //    //    .AppendLine()
            //    //    .AppendLine(TextHelper.GetText(topic.Language.Code, Constants.Texts.CaptionsDownload));
            //}

            //var newMarkdown = new StringBuilder()
            //    .AppendLine(TextHelper.GetText(topic.Language.Code, Constants.Texts.TopicHeader))
            //    .AppendLine(oldMarkdown)
            //    .Replace(
            //        YouTubeEmbedMarker,
            //        youtubeEmbed.ToString())
            //    .Replace(
            //        DownloadMarker,
            //        download.ToString())
            //    .Replace(
            //        LanguagesTitleMarker,
            //        languagesTitle)
            //    .Replace(
            //        DownloadCaptionsMarker,
            //        downloadCaptions.ToString());



            //// Process keywords first
            //if (!string.IsNullOrEmpty(keywordsLine))
            //{
            //    var settingsContainer = helper.GetContainer(Constants.SettingsContainerVariableName);
            //    var keywordsBlob = settingsContainer.GetBlockBlobReference(
            //        string.Format(Constants.KeywordsBlob, topic.Language.Code));

            //    string json = null;
            //    Dictionary<char, List<KeywordPair>> keywordsDictionary;

            //    var newKeywords = keywordsLine.Split(new char[]
            //        {
            //            ','
            //        }, StringSplitOptions.RemoveEmptyEntries)
            //        .Select(k => k.Trim())
            //        .ToList();

            //    var existingTitle = newKeywords.FirstOrDefault(k => k.ToLower() == topicTitle.ToLower());

            //    if (!string.IsNullOrEmpty(existingTitle))
            //    {
            //        newKeywords.Remove(existingTitle);
            //    }

            //    var existingTopic = newKeywords.FirstOrDefault(k => k.ToLower() == topic.TopicName.ToLower());

            //    if (!string.IsNullOrEmpty(existingTopic))
            //    {
            //        newKeywords.Remove(existingTopic);
            //    }

            //    if (await keywordsBlob.ExistsAsync())
            //    {
            //        json = await keywordsBlob.DownloadTextAsync();
            //        keywordsDictionary = JsonConvert.DeserializeObject<Dictionary<char, List<KeywordPair>>>(json);

            //        var duplicates = string.Empty;

            //        var existingPairs = keywordsDictionary.Values
            //            .SelectMany(pair => pair)
            //            .Where(pair => pair.Topic.ToLower() == topic.TopicName.ToLower())
            //            .ToList();

            //        foreach (var existingPair in existingPairs)
            //        {
            //            var key = existingPair.Keyword.ToUpper()[0];

            //            // Just making sure
            //            if (keywordsDictionary.ContainsKey(key))
            //            {
            //                var keywordsList = keywordsDictionary[key];
            //                if (keywordsList.Contains(existingPair))
            //                {
            //                    keywordsList.Remove(existingPair);
            //                }

            //                if (keywordsList.Count == 0)
            //                {
            //                    keywordsDictionary.Remove(key);
            //                }
            //            }
            //        }
            //    }
            //    else
            //    {
            //        keywordsDictionary = new Dictionary<char, List<KeywordPair>>();
            //    }

            //    void AddToKeywordsList(KeywordPair pair)
            //    {
            //        var letter = pair.Keyword.ToUpper()[0];

            //        List<KeywordPair> keywordsList;
            //        if (keywordsDictionary.ContainsKey(letter))
            //        {
            //            keywordsList = keywordsDictionary[letter];
            //        }
            //        else
            //        {
            //            keywordsList = new List<KeywordPair>();
            //            keywordsDictionary.Add(letter, keywordsList);
            //        }

            //        keywordsList.Add(pair);
            //    }

            //    topic.Keywords = new List<KeywordPair>();

            //    foreach (var newKeyword in newKeywords)
            //    {
            //        var ambiguousPairs = keywordsDictionary.Values
            //            .SelectMany(pair => pair)
            //            .Where(pair => pair.Keyword.ToLower() == newKeyword.ToLower())
            //            .ToList();

            //        var pair = new KeywordPair(
            //            topic.Language.Code,
            //            topic.Title,
            //            topic.TopicName,
            //            newKeyword.MakeSafeFileName(),
            //            newKeyword,
            //            blurb);

            //        KeywordPair existingDisambiguation = null;

            //        if (ambiguousPairs.Count > 0)
            //        {
            //            pair.MustDisambiguate = true;

            //            foreach (var disambiguationPair in ambiguousPairs)
            //            {
            //                if (disambiguationPair.IsDisambiguation)
            //                {
            //                    existingDisambiguation = disambiguationPair;
            //                }
            //                else
            //                {
            //                    disambiguationPair.MustDisambiguate = true;
            //                }
            //            }

            //            if (existingDisambiguation == null)
            //            {
            //                existingDisambiguation = new KeywordPair(
            //                    topic.Language.Code,
            //                    null,
            //                    newKeyword.MakeSafeFileName(),
            //                    Constants.Disambiguation,
            //                    newKeyword,
            //                    null)
            //                {
            //                    IsDisambiguation = true
            //                };

            //                AddToKeywordsList(existingDisambiguation);
            //            }
            //        }

            //        AddToKeywordsList(pair);
            //        topic.Keywords.Add(pair);
            //    }

            //    var titlePair = new KeywordPair(
            //            topic.Language.Code,
            //            topic.Title,
            //            topic.TopicName,
            //            topic.TopicName,
            //            topicTitle,
            //            blurb);
            //    AddToKeywordsList(titlePair);
            //}

            return topic;
        }

        private static IList<AuthorInformation> MakeAuthors(
            string authorName, 
            string email, 
            string github, 
            string twitter)
        {
            var authorNames = authorName.Split(new char[]
            {
                Separator
            });

            var emails = email.Split(new char[]
            {
                Separator
            });

            var githubs = github.Split(new char[]
            {
                Separator
            });

            var twitters = twitter.Split(new char[]
            {
                Separator
            });

            if (authorNames.Length != emails.Length
                || authorNames.Length != githubs.Length
                || authorNames.Length != twitters.Length)
            {
                throw new InvalidOperationException("Invalid author, email github or twitter lists");
            }

            var result = new List<AuthorInformation>();

            for (var index = 0; index < authorNames.Length; index++)
            {
                var author = new AuthorInformation(
                    authorNames[index].Trim(),
                    emails[index].Trim(),
                    githubs[index].Trim(),
                    twitters[index].Trim());

                result.Add(author);
            }

            return result;
        }

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
    }
}
