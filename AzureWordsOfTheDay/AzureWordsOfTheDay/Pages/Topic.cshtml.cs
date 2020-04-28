using AzureWordsOfTheDay.Model;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Threading.Tasks;

namespace AzureWordsOfTheDay.Pages
{
    public class TopicModel : PageModel
    {
        private readonly MarkdownHelper _markdown;
        private readonly ILogger _logger;

        public string Topic
        {
            get;
            private set;
        }

        public string Subtopic
        {
            get;
            private set;
        }

        public HtmlString TopicBarHtml
        {
            get;
            private set;
        }

        public HtmlString TopicHtml
        {
            get;
            private set;
        }

        public TopicModel(
            ILogger<TopicModel> logger,
            MarkdownHelper markdown)
        {
            _logger = logger;
            _markdown = markdown;
        }

        public async Task<IActionResult> OnGet(string topic, string subtopic)
        {
            _logger.LogInformation($"OnGet in Topic: {topic} / {subtopic}");

            Topic = topic.ToLower();

            if (subtopic != topic)
            {
                var textInfo = CultureInfo.InvariantCulture.TextInfo;
                Subtopic = textInfo.ToTitleCase(subtopic.Replace('-', ' '));
            }
            else
            {
                Subtopic = string.Empty;
            }

            if (string.IsNullOrEmpty(Topic))
            {
                _logger.LogInformation("No topic found, redirecting to index");
                return Redirect("/");
            }

            TopicHtml = await _markdown.LoadMarkdown(Topic, _logger);
            TopicBarHtml = await _markdown.LoadTopicsBar(_logger);

            _logger.LogInformation("Done rendering in Topic");

            return null;
        }
    }
}