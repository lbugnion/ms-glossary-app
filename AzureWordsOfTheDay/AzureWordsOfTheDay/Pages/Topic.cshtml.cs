using AzureWordsOfTheDay.Model;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AzureWordsOfTheDay.Pages
{
    public class TopicModel : PageModel
    {
        private readonly ILogger _logger;
        private readonly ContentHelper _contentHelper;

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

        public HtmlString LanguagesHtml
        {
            get;
            private set;
        }

        public TopicModel(
            ILogger<TopicModel> logger,
            ContentHelper contentHelper)
        {
            _logger = logger;
            _contentHelper = contentHelper;
        }

        public async Task<IActionResult> OnGet(string languageCode, string fullTopic)
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

            var (languagesLine, topicHtml) = await _contentHelper.LoadMarkdown(languageCode, fullTopic, _logger);

            if (!string.IsNullOrEmpty(languagesLine))
            {
                LanguagesHtml = _contentHelper.MakeLanguages(
                    languagesLine,
                    $"<a href=\"/topic/{{0}}/{Topic}\">{{1}}</a>"); 
            }

            TopicHtml = topicHtml;

            TopicBarHtml = await _contentHelper.LoadTopicsBar(languageCode, _logger);

            _logger.LogInformation("Done rendering in Topic");

            return null;
        }
    }
}