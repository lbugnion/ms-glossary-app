using MarkdownSharp;
using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace AzureWordsOfTheDay.Model
{
    public class MarkdownHelper
    {
        private const string MainTopicListUrl = "https://wordsoftheday.blob.core.windows.net/{0}/topics.json";
        private const string TopicsBarUrl = "https://wordsoftheday.blob.core.windows.net/{0}/keywords.md";
        private const string TopicUrlMask = "https://wordsoftheday.blob.core.windows.net/{0}/{1}.md";
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
            string filePath,
            ILogger logger = null)
        {
            logger?.LogInformation($"In MarkdownLoader.LoadLocalMarkdown: {filePath}");

            var fileContent = "Nothing found";

            try
            {
                using (var stream = File.OpenRead(filePath))
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
                fileContent = $"Error: {ex.Message}";
                return new HtmlString(fileContent);
            }

            var md = new Markdown();
            var html = md.Transform(fileContent);

            return new HtmlString(html);
        }

        public async Task<HtmlString> LoadMarkdown(string topic, ILogger logger = null)
        {
            logger?.LogInformation($"In MarkdownLoader.LoadMarkdown: topic = {topic}");

            var topicsContainer = Startup.Configuration[Constants.TopicsContainerVariableName];
            logger?.LogInformation($"topicsContainer: {topicsContainer}");

            var uri = new Uri(
                string.Format(
                    TopicUrlMask,
                    topicsContainer,
                    topic));
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

        public async Task<HtmlString> LoadRandomTopic(ILogger logger = null)
        {
            logger?.LogInformation("In MarkdownLoader.LoadRandomTopic");

            var url = string.Format(MainTopicListUrl, Startup.Configuration[Constants.SettingsContainerVariableName]);
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

                return await LoadMarkdown(topic, logger);
            }
            catch (Exception ex)
            {
                logger?.LogError($"Error loading random topic: {ex.Message}");
            }

            return null;
        }

        public async Task<HtmlString> LoadTopicsBar(ILogger logger = null)
        {
            logger?.LogInformation("In MarkdownLoader.LoadTopicsBar");

            var settingsFolder = Startup.Configuration[Constants.SettingsContainerVariableName];
            logger?.LogInformation($"settingsFolder: {settingsFolder}");

            var uri = new Uri(string.Format(TopicsBarUrl, settingsFolder));
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