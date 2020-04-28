using AzureWordsOfTheDay.Model;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace AzureWordsOfTheDay.Pages
{
    public class IndexModel : PageModel
    {
        private MarkdownHelper _markdown;
        private readonly ILogger _logger;

        public HtmlString TopicBarHtml
        {
            get;
            private set;
        }

        public IndexModel(
            ILogger<IndexModel> logger,
            MarkdownHelper markdown)
        {
            _logger = logger;
            _markdown = markdown;
        }

        public async Task OnGet()
        {
            _logger.LogInformation($"OnGet in Index");

            TopicBarHtml = await _markdown.LoadTopicsBar(_logger);

            _logger.LogInformation("Done rendering in Index");
        }
    }
}
