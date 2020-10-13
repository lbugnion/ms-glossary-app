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
        private readonly ContentHelper _contentHelper;
        private readonly ILogger _logger;

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
            ContentHelper contentHelper)
        {
            _logger = logger;
            _contentHelper = contentHelper;
        }

        public async Task<IActionResult> OnGet(string languageCode, string fullTopic)
        {
            ViewData["LanguageCode"] = languageCode;
            ViewData["SiteTitle"] = Texts.ResourceManager.GetString($"{languageCode}.SiteTitle");

            _logger.LogInformation($"OnGet in Topic: {languageCode} / {fullTopic}");

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

            var topicHtml = await _contentHelper.LoadMarkdown(languageCode, fullTopic, _logger);

            TopicHtml = topicHtml;

            TopicBarHtml = await _contentHelper.LoadTopicsBar(languageCode, _logger);

            _logger.LogInformation("Done rendering in Topic");

            return null;
        }
    }
}