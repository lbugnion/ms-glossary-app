﻿using AzureWordsOfTheDay.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;

namespace AzureWordsOfTheDay.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger _logger;
        private IHostingEnvironment _env;
        private ContentHelper _contentHelper;

        public HtmlString IndexContent
        {
            get;
            private set;
        }

        public HtmlString SelectedTopicHtml
        {
            get;
            private set;
        }

        public HtmlString TopicBarHtml
        {
            get;
            private set;
        }

        public HtmlString LanguagesHtml
        {
            get;
            private set;
        }

        public IndexModel(
            ILogger<IndexModel> logger,
            ContentHelper contentHelper,
            IHostingEnvironment env)
        {
            _env = env;
            _logger = logger;
            _contentHelper = contentHelper;
        }

        public async Task OnGet(string languageCode)
        {
            _logger.LogInformation($"OnGet in Index");

            var docName = "index.md";
            var root = new DirectoryInfo(Path.Combine(_env.WebRootPath));
            var folder = Path.Combine(root.Parent.FullName, Constants.LocalMarkdownFolderName);
            IndexContent = _contentHelper.LoadLocalMarkdown(
                folder, 
                languageCode, 
                docName, 
                _logger);

            TopicBarHtml = await _contentHelper.LoadTopicsBar(languageCode, _logger);

            _logger.LogInformation("Loading random topic");
            SelectedTopicHtml = await _contentHelper.LoadRandomTopic(languageCode, _logger);
            _logger.LogInformation("Done loading random topic");

            LanguagesHtml = await _contentHelper.LoadOtherLanguages(languageCode, _logger);

            _logger.LogInformation("Done rendering in Index");
        }
    }
}