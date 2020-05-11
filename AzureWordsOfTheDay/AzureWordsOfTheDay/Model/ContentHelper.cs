using MarkdownSharp;
using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AzureWordsOfTheDay.Model
{
    public class ContentHelper
    {
        private const string MainTopicListUrlMask = "https://wordsoftheday.blob.core.windows.net/{0}/{1}.{2}.json";
        private const string LanguagesListUrlMask = "https://wordsoftheday.blob.core.windows.net/{0}/languages.txt";
        private const string TopicsBarUrlMask = "https://wordsoftheday.blob.core.windows.net/{0}/{1}.{2}.md";
        private const string TopicUrlMask = "https://wordsoftheday.blob.core.windows.net/{0}/{1}.{2}.md";
        private HttpClient _client;

        private HttpClient Client
        {
            get
            {
                if (_client == null)
                {
                    _client = new HttpClient();
                }

                return _client;
            }
        }

        public HtmlString LoadLocalMarkdown(
            string folderPath,
            string languageCode,
            string fileName,
            ILogger logger = null)
        {
            logger?.LogInformation($"In MarkdownLoader.LoadLocalMarkdown: {folderPath} {languageCode} {fileName}");

            var fileContent = "Nothing found";

            try
            {
                var fullPath = Path.Combine(folderPath, languageCode);
                fullPath = Path.Combine(fullPath, fileName);

                using (var stream = File.OpenRead(fullPath))
                {
                    using (var reader = new StreamReader(stream))
                    {
                        fileContent = reader.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex.Message);
                return null;
            }

            var md = new Markdown();
            var html = md.Transform(fileContent);

            return new HtmlString(html);
        }

        public async Task<HtmlString> LoadOtherLanguages(string languageCode, ILogger logger = null)
        {
            logger?.LogInformation($"In MarkdownLoader.LoadOtherLanguages: {languageCode}");

            var settingsContainer = Startup.Configuration[Constants.SettingsContainerVariableName];
            logger?.LogInformation($"settingsContainer: {settingsContainer}");

            var uri = new Uri(
                string.Format(
                    LanguagesListUrlMask,
                    settingsContainer));

            string txt = null;

            try
            {
                logger?.LogInformation("Trying to get the languages JSON");
                txt = await Client.GetStringAsync(uri);
            }
            catch (Exception ex)
            {
                logger?.LogError($"Error when getting the languages JSON: {ex.Message}");
                // TODO Show error if 404
            }

            if (!string.IsNullOrEmpty(txt))
            {
                return MakeLanguages(
                    txt, 
                    "<a href=\"/{0}/\" title=\"{1}\">{2}</a>",
                    languageCode);
            }

            return null;
        }

        public HtmlString MakeLanguages(
            string languageLine,
            string linkTemplate,
            string languageCodeToAvoid = null)
        {
            var languages = MakeLanguageList(languageLine);

            if (languages?.Count > 0)
            {
                var builder = new StringBuilder();
                var count = 0;

                if (languages.Count > 3)
                {
                    foreach (var language in languages.OrderBy(l => l.Code))
                    {
                        if (language.Code != languageCodeToAvoid)
                        {
                            builder.AppendLine(
                                string.Format(linkTemplate, language.Code, language.Language, language.Code));

                            count++;

                            if (count < languages.Count - 1)
                            {
                                builder.AppendLine("/");
                            }
                        }
                    }
                }
                else
                {
                    foreach (var language in languages.OrderBy(l => l.Language))
                    {
                        if (language.Code != languageCodeToAvoid)
                        {
                            builder.AppendLine(
                                string.Format(linkTemplate, language.Code, language.Language, language.Language));

                            count++;

                            if (count < languages.Count - 1)
                            {
                                builder.AppendLine("/");
                            }
                        }
                    }
                }

                return new HtmlString(builder.ToString());
            }

            return null;
        }

        public async Task<(string languagesLine, HtmlString topicHtml)> LoadMarkdown(string languageCode, string topic, ILogger logger = null)
        {
            logger?.LogInformation($"In MarkdownLoader.LoadMarkdown: topic = {languageCode}/{topic}");

            var topicsContainer = Startup.Configuration[Constants.TopicsContainerVariableName];
            logger?.LogInformation($"topicsContainer: {topicsContainer}");

            var uri = new Uri(
                string.Format(
                    TopicUrlMask,
                    topicsContainer,
                    topic,
                    languageCode));

            logger?.LogInformation($"uri: {uri}");

            string markdown = null;

            try
            {
                logger?.LogInformation("Trying to get the topic markdown");
                markdown = await Client.GetStringAsync(uri);
            }
            catch (Exception ex)
            {
                logger?.LogError($"Error when getting the topic markdown: {ex.Message}");
                // TODO Show error if 404
            }

            if (!string.IsNullOrEmpty(markdown))
            {
                logger?.LogInformation("Topic markdown loaded, rendering...");

                string languagesLine = null;

                if (markdown.Trim().StartsWith(">"))
                {
                    var reader = new StringReader(markdown);
                    languagesLine = reader.ReadLine();
                    markdown = reader.ReadToEnd().Trim();
                }

                var md = new Markdown();
                var html = md.Transform(markdown);
                logger?.LogInformation("Done in MarkdownHelper.LoadMarkdown");
                return (languagesLine, new HtmlString(html));
            }

            return (null, null);
        }

        public IList<LanguageInfo> MakeLanguageList(string languageLine)
        {
            var lineParts = languageLine.Split(new char[]
            {
                ':'
            });

            if (lineParts.Length != 2)
            {
                return null;
            }

            var languages = lineParts[1].Split(new char[]
                {
                    ','
                }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Split(new char[]
                {
                    '/'
                }))
                .Select(a => new LanguageInfo
                {
                    Code = a[0].Trim(),
                    Language = a[1].Trim()
                })
                .ToList();

            return languages;
        }

        public async Task<HtmlString> LoadRandomTopic(string languageCode, ILogger logger = null)
        {
            logger?.LogInformation($"In MarkdownLoader.LoadRandomTopic {languageCode}");

            var url = string.Format(
                MainTopicListUrlMask, 
                Startup.Configuration[Constants.SettingsContainerVariableName],
                Constants.TopicsBlob,
                languageCode);

            logger?.LogInformation($"url: {url}");

            try
            {
                var json = await Client.GetStringAsync(url);

                logger?.LogInformation($"JSON loaded: {json}");

                var list = JsonConvert.DeserializeObject<List<string>>(json);

                logger?.LogInformation($"List loaded: {list.Count} topics found");

                var random = new Random();
                var index = random.Next(0, list.Count);
                var topic = list[index];

                logger?.LogInformation($"Random topic: {topic}");

                var result = await LoadMarkdown(languageCode, topic, logger);
                return result.topicHtml;
            }
            catch (Exception ex)
            {
                logger?.LogError($"Error loading random topic: {ex.Message}");
            }

            return null;
        }

        public async Task<HtmlString> LoadTopicsBar(string languageCode, ILogger logger = null)
        {
            logger?.LogInformation($"In MarkdownLoader.LoadTopicsBar {languageCode}");

            var settingsFolder = Startup.Configuration[Constants.SettingsContainerVariableName];
            logger?.LogInformation($"settingsFolder: {settingsFolder}");

            var uri = new Uri(string.Format(TopicsBarUrlMask, settingsFolder, Constants.SideBarMarkdownBlob, languageCode));
            logger?.LogInformation($"uri: {uri}");

            string markdown = null;

            try
            {
                logger?.LogInformation("Trying to get the topic bar markdown");
                markdown = await Client.GetStringAsync(uri);
            }
            catch (Exception ex)
            {
                logger?.LogError($"Error when getting the topic bar markdown: {ex.Message}");
                // TODO What should we show instead?
            }

            if (!string.IsNullOrEmpty(markdown))
            {
                logger?.LogInformation("Topic bar markdown loaded, rendering...");
                var md = new Markdown();
                var html = md.Transform(markdown);
                logger?.LogInformation("Done in MarkdownHelper.LoadTopicsBar");
                return new HtmlString(html);
            }

            return null;
        }
    }
}