using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MsGlossaryApp.Model
{
    public static class TopicMaker
    {
        private static IList<AuthorInformation> MakeAuthors(
            string authorName,
            string email,
            string github,
            string twitter,
            ILogger log = null)
        {
            log?.LogInformationEx("In MakeAuthors", LogVerbosity.Verbose);

            var authorNames = authorName.Split(new char[]
            {
                Constants.Separator
            });

            var emails = email.Split(new char[]
            {
                Constants.Separator
            });

            var githubs = github.Split(new char[]
            {
                Constants.Separator
            });

            var twitters = twitter.Split(new char[]
            {
                Constants.Separator
            });

            if (authorNames.Length != emails.Length
                || authorNames.Length != githubs.Length
                || authorNames.Length != twitters.Length)
            {
                log?.LogError("Invalid author, email github or twitter lists");
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

            log?.LogInformationEx("Out MakeAuthors", LogVerbosity.Verbose);
            return result;
        }

        private static IList<LanguageInfo> MakeLanguages(
            string captions,
            ILogger log = null)
        {
            log?.LogInformationEx("In MakeLanguages", LogVerbosity.Verbose);

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

            log?.LogInformationEx("Out MakeLanguages", LogVerbosity.Verbose);
            return result;
        }

        private static string MakeDisambiguationTitleLink(
            KeywordInformation keyword,
            ILogger log = null)
        {
            log?.LogInformationEx("In MakeDisambiguationTitleLink", LogVerbosity.Verbose);
            return $"[{keyword.Keyword}](/glossary/topic/{keyword.Keyword.MakeSafeFileName()}/disambiguation)";
        }

        private static string MakeTitleLink(
            KeywordInformation keyword,
            ILogger log = null)
        {
            log?.LogInformationEx("In MakeTitleLink", LogVerbosity.Verbose);
            if (keyword.IsMainKeyword)
            {
                return $"[{keyword.Topic.Title}](/glossary/topic/{keyword.Topic.TopicName})";
            }
            else
            {
                return $"[{keyword.Topic.Title}](/glossary/topic/{keyword.Topic.TopicName}/{keyword.Keyword.MakeSafeFileName()})";
            }
        }

        private static string MakeTopicText(
            KeywordInformation keyword,
            ILogger log)
        {
            log?.LogInformationEx("In MakeTopicText", LogVerbosity.Verbose);
            var topic = keyword.Topic;

            var redirect = string.Empty;

            if (!keyword.IsMainKeyword)
            {
                redirect += $" (redirected from {keyword.Keyword})";
            }

            var dateString = topic.RecordingDate.ToShortDateString();

            var builder = new StringBuilder()
                .AppendLine("---")
                .Append($"title: {topic.Title}")
                .AppendLine(redirect)
                .AppendLine($"description: Microsoft Glossary definition for {topic.Title}")
                .AppendLine($"author: {topic.Authors.First().GitHub}")
                .AppendLine($"ms.date: {dateString}")
                .AppendLine($"ms.prod: non-product-specific")
                .AppendLine($"ms.topic: glossary")
                .AppendLine("---")
                .AppendLine()
                .Append(Constants.H1)
                .Append(MakeTitleLink(keyword, log))
                .AppendLine(redirect)
                .AppendLine()
                .AppendLine($"> {topic.Blurb}")
                .AppendLine()
                .AppendLine($"> [!VIDEO https://www.youtube.com/embed/{topic.YouTubeCode}]")
                .AppendLine()
                .AppendLine($"{Constants.H2}Download")
                .AppendLine()
                .AppendLine($"[You can download this video here](https://msglossarystore.blob.core.windows.net/videos/{topic.TopicName}.{topic.Language.Code}.mp4).")
                .AppendLine();

            if (topic.Captions != null
                && topic.Captions.Count > 0)
            {
                builder
                .AppendLine("## Languages")
                .AppendLine()
                .AppendLine("There are captions for the following language(s):")
                .AppendLine();

                foreach (var caption in keyword.Topic.Captions)
                {
                    builder.AppendLine($"- [{caption.Language}](https://msglossarystore.blob.core.windows.net/captions/{topic.TopicName}.{topic.Language.Code}.{caption.Code}.srt)");
                }

                builder.AppendLine()
                    .AppendLine("> Learn about [downloading and showing captions here](/glossary/captions).")
                    .AppendLine();
            }

            builder
                .AppendLine($"{Constants.H2}Links");

            foreach (var linkSection in topic.Links)
            {
                builder.AppendLine()
                    .AppendLine($"{Constants.H3}{linkSection.Key}")
                    .AppendLine();

                foreach (var link in linkSection.Value)
                {
                    builder.AppendLine(link);
                }
            }

            builder.AppendLine()
                .AppendLine($"{Constants.H2}Transcript")
                .AppendLine()
                .AppendLine(topic.Transcript)
                .AppendLine();

            if (topic.Authors != null
                && topic.Authors.Count > 0)
            {
                builder
                    .AppendLine($"{Constants.H2}Authors")
                    .AppendLine()
                    .Append("This topic was created by ");

                foreach (var author in topic.Authors)
                {
                    builder.Append($"[{author.Name}](http://twitter.com/{author.Twitter}), ");
                }

                builder.Remove(builder.Length - 2, 2);
            }

            builder.AppendLine();
            log?.LogInformationEx("Out MakeTopicText", LogVerbosity.Verbose);
            return builder.ToString();
        }

        public static async Task<TopicInformation> CreateTopic(
            Uri uri, 
            ILogger log)
        {
            log?.LogInformationEx("In CreateTopic", LogVerbosity.Verbose);

            var topic = new TopicInformation
            {
                Uri = uri
            };

            var topicBlob = new CloudBlockBlob(uri);
            topic.TopicName = Path.GetFileNameWithoutExtension(topicBlob.Name);
            topic.TopicName = Path.GetFileNameWithoutExtension(topic.TopicName);

            log?.LogInformationEx($"Topic: {topic.TopicName}", LogVerbosity.Verbose);

            string oldMarkdown = await topicBlob.DownloadTextAsync();
            var markdownReader = new StringReader(oldMarkdown);

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
            var isTranscript = false;
            var isLinks = false;
            var transcript = new StringBuilder();
            var links = new Dictionary<string, IList<string>>();
            IList<string> currentLinksSection = null;
            string line;

            while ((line = markdownReader.ReadLine()) != null)
            {
                if (line.StartsWith(Constants.Input.TranscriptMarker))
                {
                    isLinks = false;
                    isTranscript = true;
                    continue;
                }
                else if (line.StartsWith(Constants.Input.LinksMarker))
                {
                    isLinks = true;
                    isTranscript = false;
                    continue;
                }
                else if (isTranscript)
                {
                    transcript.AppendLine(line);
                }
                else if (isLinks)
                {
                    if (string.IsNullOrEmpty(line.Trim()))
                    {
                        continue;
                    }

                    if (line.StartsWith(Constants.H3))
                    {
                        currentLinksSection = new List<string>();
                        links.Add(line.Substring(Constants.H3.Length).Trim(), currentLinksSection);
                        continue;
                    }

                    currentLinksSection.Add(line);
                }
                else if (line.StartsWith(Constants.H1))
                {
                    topicTitle = line
                        .Substring(Constants.H1.Length)
                        .Trim();
                }
                else if (line.StartsWith(Constants.Input.YouTubeMarker))
                {
                    youTubeCode = line.Substring(Constants.Input.YouTubeMarker.Length).Trim();
                    log?.LogInformationEx($"youTubeCode: {youTubeCode}", LogVerbosity.Debug);
                }
                else if (line.StartsWith(Constants.Input.KeywordsMarker))
                {
                    keywordsLine = line.Substring(Constants.Input.KeywordsMarker.Length).Trim();
                    log?.LogInformationEx($"keywordsLine: {keywordsLine}", LogVerbosity.Debug);
                }
                else if (line.StartsWith(Constants.Input.BlurbMarker))
                {
                    blurb = line.Substring(Constants.Input.BlurbMarker.Length).Trim();
                    log?.LogInformationEx($"blurb: {blurb}", LogVerbosity.Debug);
                }
                else if (line.StartsWith(Constants.Input.CaptionsMarker))
                {
                    captions = line.Substring(Constants.Input.CaptionsMarker.Length).Trim();
                    log?.LogInformationEx($"captions: {captions}", LogVerbosity.Debug);
                }
                else if (line.StartsWith(Constants.Input.LanguageMarker))
                {
                    language = line.Substring(Constants.Input.LanguageMarker.Length).Trim();
                    log?.LogInformationEx($"language: {language}", LogVerbosity.Debug);
                }
                else if (line.StartsWith(Constants.Input.AuthorNameMarker))
                {
                    authorName = line.Substring(Constants.Input.AuthorNameMarker.Length).Trim();
                    log?.LogInformationEx($"authorName: {authorName}", LogVerbosity.Debug);
                }
                else if (line.StartsWith(Constants.Input.EmailMarker))
                {
                    email = line.Substring(Constants.Input.EmailMarker.Length).Trim();
                    log?.LogInformationEx($"email: {email}", LogVerbosity.Debug);
                }
                else if (line.StartsWith(Constants.Input.GitHubMarker))
                {
                    github = line.Substring(Constants.Input.GitHubMarker.Length).Trim();
                    log?.LogInformationEx($"github: {github}", LogVerbosity.Debug);
                }
                else if (line.StartsWith(Constants.Input.TwitterMarker))
                {
                    twitter = line.Substring(Constants.Input.TwitterMarker.Length).Trim();
                    log?.LogInformationEx($"twitter: {twitter}", LogVerbosity.Debug);
                }
                else if (line.StartsWith(Constants.Input.RecordingDateMarker))
                {
                    var dateString = line.Substring(Constants.Input.RecordingDateMarker.Length).Trim();
                    log?.LogInformationEx($"dateString: {dateString}", LogVerbosity.Debug);
                    recordingDate = DateTime.ParseExact(dateString, "yyyyMMdd", CultureInfo.InvariantCulture);
                }
            }

            topic.Title = topicTitle;
            topic.Transcript = transcript.ToString().Trim();
            topic.Links = links;
            topic.RecordingDate = recordingDate;
            topic.YouTubeCode = youTubeCode;
            topic.Blurb = blurb;
            topic.Authors = MakeAuthors(authorName, email, github, twitter, log);
            topic.Captions = MakeLanguages(captions, log);
            topic.Language = MakeLanguages(language, log).First();
            topic.Keywords = keywordsLine.Split(new char[]
            {
                ','
            }, StringSplitOptions.RemoveEmptyEntries)
                .Select(k => k.Trim())
                .ToList();

            log?.LogInformationEx("Out CreateTopic", LogVerbosity.Verbose);
            return topic;
        }

        public static Task<IList<KeywordInformation>> SortDisambiguations(
            IList<KeywordInformation> keywords,
            ILogger log = null)
        {
            log?.LogInformationEx("In SortDisambiguations", LogVerbosity.Verbose);

            var tcs = new TaskCompletionSource<IList<KeywordInformation>>();

            var disambiguations = keywords
                .Where(k => k.MustDisambiguate)
                .GroupBy(k => k.Keyword.ToLower())
                .ToList();

            foreach (var group in disambiguations)
            {
                keywords.Add(new KeywordInformation
                {
                    IsMainKeyword = false,
                    Keyword = group.First().Keyword,
                    MustDisambiguate = false,
                    Topic = null,
                    IsDisambiguation = true
                });
            }

            tcs.SetResult(keywords);
            log?.LogInformationEx("Out SortDisambiguations", LogVerbosity.Verbose);
            return tcs.Task;
        }

        // TODO Replace with committing to GitHub
        public static async Task<string> SaveDisambiguation(
            IList<KeywordInformation> keywords, 
            ILogger log = null)
        {
            log?.LogInformationEx("In SaveDisambiguation", LogVerbosity.Verbose);

            try
            {
                var firstKeyword = keywords.First();

                string name = $"{firstKeyword.Keyword.MakeSafeFileName()}_disambiguation.md";

                string text = MakeDisambiguationText(keywords, log);

                var account = CloudStorageAccount.Parse(
                    Environment.GetEnvironmentVariable(Constants.AzureWebJobsStorageVariableName));

                var client = account.CreateCloudBlobClient();
                var helper = new BlobHelper(client, log);
                var targetContainer = helper.GetContainer(Constants.OutputContainerVariableName);
                var targetBlob = targetContainer.GetBlockBlobReference(name);

                await targetBlob.UploadTextAsync(text);
            }
            catch (Exception ex)
            {
                log?.LogError(ex, "Error in SaveDisambiguation");
                return ex.Message;
            }

            log?.LogInformationEx("Out SaveDisambiguation", LogVerbosity.Verbose);
            return null;
        }

        private static string MakeDisambiguationText(
            IList<KeywordInformation> keywords,
            ILogger log = null)
        {
            log?.LogInformationEx("In MakeDisambiguationText", LogVerbosity.Verbose);

            var lastKeyword = keywords.OrderByDescending(k => k.Topic.RecordingDate).First();

            var dateString = lastKeyword.Topic.RecordingDate.ToShortDateString();

            var builder = new StringBuilder()
                .AppendLine("---")
                .Append($"title: {lastKeyword.Keyword}")
                .AppendLine(" (disambiguation)")
                .AppendLine($"description: Microsoft Glossary disambiguation for {lastKeyword.Keyword}")
                .AppendLine($"author: {lastKeyword.Topic.Authors.First().GitHub}")
                .AppendLine($"ms.date: {dateString}")
                .AppendLine($"ms.prod: non-product-specific")
                .AppendLine($"ms.topic: glossary")
                .AppendLine("---")
                .AppendLine()
                .Append(Constants.H1)
                .Append(MakeDisambiguationTitleLink(lastKeyword, log))
                .AppendLine(" (disambiguation)")
                .AppendLine()
                .Append(Constants.H2)
                .AppendLine($"`{lastKeyword.Keyword}` can be used in different contexts")
                .AppendLine();

            foreach (var keyword in keywords.OrderBy(k => k.Topic.Title))
            {
                builder
                    .AppendLine($"- In {MakeTitleLink(keyword, log)}, {keyword.Topic.Blurb}");
            }

            builder.AppendLine();
            log?.LogInformationEx("Out MakeDisambiguationText", LogVerbosity.Verbose);
            return builder.ToString();
        }

        // TODO Replace with committing to GitHub
        public static async Task<string> SaveKeyword(
            KeywordInformation keyword, 
            ILogger log = null)
        {
            log?.LogInformationEx("In SaveKeyword", LogVerbosity.Verbose);

            try
            {
                string name = null;

                if (keyword.IsMainKeyword)
                {
                    name = $"{keyword.Topic.TopicName.MakeSafeFileName()}_index.md";
                }
                else
                {
                    name = $"{keyword.Topic.TopicName.MakeSafeFileName()}_{keyword.Keyword.MakeSafeFileName()}.md";
                }

                string text = MakeTopicText(keyword, log);

                var account = CloudStorageAccount.Parse(
                    Environment.GetEnvironmentVariable(Constants.AzureWebJobsStorageVariableName));

                var client = account.CreateCloudBlobClient();
                var helper = new BlobHelper(client, log);
                var targetContainer = helper.GetContainer(Constants.OutputContainerVariableName);
                var targetBlob = targetContainer.GetBlockBlobReference(name);

                await targetBlob.UploadTextAsync(text);
            }
            catch (Exception ex)
            {
                log?.LogError(ex, "Error in SaveKeyword");
                return ex.Message;
            }

            log?.LogInformationEx("Out SaveKeyword", LogVerbosity.Verbose);
            return null;
        }

        public static Task<IList<KeywordInformation>> SortKeywords(
            IList<TopicInformation> allTopics,
            TopicInformation currentTopic,
            ILogger log = null)
        {
            log?.LogInformationEx("In SortKeywords", LogVerbosity.Verbose);

            var tcs = new TaskCompletionSource<IList<KeywordInformation>>();

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

                if (newKeyword.Keyword.MakeSafeFileName().ToLower() == currentTopic.TopicName.ToLower())
                {
                    newKeyword.IsMainKeyword = true;
                }

                result.Add(newKeyword);
            }

            if (!result.Any(k => k.IsMainKeyword))
            {
                var mainKeyword = new KeywordInformation
                {
                    IsMainKeyword = true,
                    Keyword = currentTopic.Title,
                    Topic = currentTopic
                };

                result.Add(mainKeyword);
            }

            tcs.SetResult(result);
            log?.LogInformationEx("Out SortKeywords", LogVerbosity.Verbose);
            return tcs.Task;
        }
    }
}