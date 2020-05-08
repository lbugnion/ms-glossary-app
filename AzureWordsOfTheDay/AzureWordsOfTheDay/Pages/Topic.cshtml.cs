using AzureWordsOfTheDay.Model;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AzureWordsOfTheDay.Pages
{
    public class TopicModel : PageModel
    {
        private readonly ILogger _logger;
        private readonly MarkdownHelper _markdown;

        public string Subtopic
        {
            get;
            private set;
        }

        public string Topic
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

        public async Task<IActionResult> OnGet(string fullTopic)
        {
            _logger.LogInformation($"OnGet in Topic: {fullTopic}");

            var parts = fullTopic.Split(new char[]
            {
                '_'
            }, StringSplitOptions.RemoveEmptyEntries);

            Topic = parts[0].ToLower();

            if (string.IsNullOrEmpty(Topic))
            {
                _logger.LogInformation("No topic found, redirecting to index");
                return Redirect("/");
            }

            TopicHtml = await _markdown.LoadMarkdown(fullTopic, _logger);
            TopicBarHtml = await _markdown.LoadTopicsBar(_logger);

            _logger.LogInformation("Done rendering in Topic");

            return null;
        }
    }
}