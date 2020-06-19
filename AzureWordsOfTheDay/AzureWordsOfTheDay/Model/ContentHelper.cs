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

        public async Task<HtmlString> LoadMarkdown(string languageCode, string topic, ILogger logger = null)
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

                var md = new Markdown();
                var html = md.Transform(markdown);
                logger?.LogInformation("Done in MarkdownHelper.LoadMarkdown");
                return new HtmlString(html);
            }

            return null;
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

                int index;
                string topic = null;
                var stop = false;

                while (!stop)
                {
                    index = random.Next(0, list.Count);
                    topic = list[index];

                    if (topic != "another-test"
                        && topic != "test")
                    {
                        stop = true;
                    }
                }

                logger?.LogInformation($"Random topic: {topic}");

                var result = await LoadMarkdown(languageCode, topic, logger);

                // Remove first line. Later we won't have to do that
                var resultString = result.Value;
                var reader = new StringReader(resultString);
                var dummy = reader.ReadLine();
                resultString = reader.ReadToEnd();

                return new HtmlString(resultString);
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