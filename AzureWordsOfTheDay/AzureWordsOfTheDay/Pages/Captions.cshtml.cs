using AzureWordsOfTheDay.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;

namespace AzureWordsOfTheDay.Pages
{
    public class CaptionsModel : PageModel
    {
        private IHostingEnvironment _env;
        private ILogger<IndexModel> _logger;
        private MarkdownHelper _markdown;

        public HtmlString BodyHtml
        {
            get;
            private set;
        }

        public HtmlString TopicBarHtml
        {
            get;
            private set;
        }

        public CaptionsModel(
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
            _logger.LogInformation($"OnGet in Captions");

            var docName = "captions.md";
            var root = new DirectoryInfo(Path.Combine(_env.WebRootPath));
            var folder = Path.Combine(root.Parent.FullName, Constants.LocalMarkdownFolderName);
            var file = Path.Combine(folder, docName);
            BodyHtml = _markdown.LoadLocalMarkdown(file);

            TopicBarHtml = await _markdown.LoadTopicsBar(_logger);

            _logger.LogInformation("Done rendering in Captions");
        }
    }
}