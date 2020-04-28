using MarkdownSharp;
using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace AzureWordsOfTheDay.Model
{
    public class MarkdownHelper
    {
        private const string TopicUrlMask = "https://wordsoftheday.blob.core.windows.net/{0}/{1}.md";
        private const string TopicsBarUrl = "https://wordsoftheday.blob.core.windows.net/{0}/keywords.md";
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

        public async Task<HtmlString> LoadMarkdown(string topic, ILogger logger = null)
        {
            logger?.LogInformation($"In MarkdownLoader.LoadMarkdown: topic = {topic}");

            var topicsFolder = Startup.Configuration["TopicsFolder"];
            logger?.LogInformation($"topicsFolder: {topicsFolder}");

            var uri = new Uri(string.Format(TopicUrlMask, Startup.Configuration["TopicsFolder"], topic));
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

        public async Task<HtmlString> LoadTopicsBar(ILogger logger = null)
        {
            logger?.LogInformation("In MarkdownLoader.LoadTopicsBar");

            var settingsFolder = Startup.Configuration["SettingsFolder"];
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
