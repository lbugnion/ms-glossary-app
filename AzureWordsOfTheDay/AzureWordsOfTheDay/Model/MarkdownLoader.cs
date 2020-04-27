using MarkdownSharp;
using Microsoft.AspNetCore.Html;
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

        public async Task<HtmlString> LoadMarkdown(string topic)
        {
            var uri = new Uri(string.Format(TopicUrlMask, Startup.Configuration["TopicsFolder"], topic));
            string markdown = null;

            try
            {
                markdown = await Client.GetStringAsync(uri);
            }
            catch (Exception ex)
            {
                // TODO Show error if 404
            }

            if (!string.IsNullOrEmpty(markdown))
            {
                var md = new Markdown();
                var html = md.Transform(markdown);
                return new HtmlString(html);
            }

            return null;
        }

        public async Task<HtmlString> LoadTopicsBar()
        {
            var uri = new Uri(string.Format(TopicsBarUrl, Startup.Configuration["SettingsFolder"]));
            string markdown = null;

            try
            {
                markdown = await Client.GetStringAsync(uri);
            }
            catch (Exception ex)
            {
                // TODO What to do if this fails?
            }

            if (!string.IsNullOrEmpty(markdown))
            {
                var md = new Markdown();
                var html = md.Transform(markdown);
                return new HtmlString(html);
            }

            return null;
        }
    }
}
