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
        private MarkdownHelper _markdown;
        private readonly ILogger _logger;
        private IHostingEnvironment _env;

        public HtmlString TopicBarHtml
        {
            get;
            private set;
        }

        public HtmlString SelectedTopicHtml
        {
            get;
            private set;
        }

        public HtmlString IndexContent
        {
            get;
            private set;
        }

        public IndexModel(
            ILogger<IndexModel> logger,
            MarkdownHelper markdown,
            IHostingEnvironment env)
        {
            _env = env;
            _logger = logger;
            _markdown = markdown;
        }

        public async Task OnGet()
        {
            _logger.LogInformation($"OnGet in Index");

            var docName = "index.md";
            var root = new DirectoryInfo(Path.Combine(_env.WebRootPath));
            var folder = Path.Combine(root.Parent.FullName, Constants.LocalMarkdownFolderName);
            var file = Path.Combine(folder, docName);
            IndexContent = _markdown.LoadLocalMarkdown(file);

            TopicBarHtml = await _markdown.LoadTopicsBar(_logger);

            SelectedTopicHtml = await _markdown.LoadRandomTopic();

            _logger.LogInformation("Done rendering in Index");
        }
    }
}
