using MarkdownSharp;
using Microsoft.AspNetCore.Html;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace AzureWordsOfTheDay.Model
{
    public class MarkdownHelper
    {
        private const string UrlMask = "https://raw.githubusercontent.com/lbugnion/wordsoftheday-md/master/{0}.md";
        private const string YouTubeMarker = "> YouTube: ";
        private const string YouTubeEmbedMarker = "<!--YOUTUBEEMBED -->";
        private const string H1 = "# ";

        private const string YouTubeEmbed = "<iframe width=\"560\" height=\"560\" src=\"https://www.youtube.com/embed/{0}\" frameborder=\"0\" allow=\"accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture\" allowfullscreen></iframe>";

        public async Task<HtmlString> LoadMarkdown(string topic)
        {
            var uri = new Uri(string.Format(UrlMask, topic));
            
            // TODO Cache HTTP client?
            var client = new HttpClient();

            string markdown = null;

            try
            {
                markdown = await client.GetStringAsync(uri);
            }
            catch (Exception ex)
            {
                // TODO Show error if 404
            }

            if (!string.IsNullOrEmpty(markdown))
            {
                var reader = new StringReader(markdown);
                var done = false;
                string youTubeCode = null;

                while (!done)
                {
                    var line = reader.ReadLine();

                    if (line.StartsWith(H1))
                    {
                        markdown = markdown.Substring(markdown.IndexOf(H1));
                        done = true;
                    }
                    else if (line.StartsWith(YouTubeMarker))
                    {
                        youTubeCode = line.Substring(YouTubeMarker.Length).Trim();
                    }
                }

                var md = new Markdown();
                var html = md.Transform(markdown);

                if (!string.IsNullOrEmpty(youTubeCode))
                {
                    html = html.Replace(
                        YouTubeEmbedMarker,
                        string.Format(YouTubeEmbed, youTubeCode));
                }

                return new HtmlString(html);
            }

            return null;
        }
    }
}
