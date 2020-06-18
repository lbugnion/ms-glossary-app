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
        private ILogger<IndexModel> _logger;
        private ContentHelper _contentHelper;
        private readonly IHostingEnvironment _env;

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
            ContentHelper contentHelper,
            IHostingEnvironment env)
        {
            _env = env;
            _logger = logger;
            _contentHelper = contentHelper;
        }

        public async Task OnGet(string languageCode)
        {
            _logger.LogInformation($"OnGet in Captions");

            ViewData["LanguageCode"] = languageCode;
            ViewData["SiteTitle"] = Texts.ResourceManager.GetString($"{languageCode}.SiteTitle");

            TopicBarHtml = await _contentHelper.LoadTopicsBar(languageCode, _logger);

            var docName = "captions.md";
            var root = new DirectoryInfo(Path.Combine(_env.WebRootPath));
            var folder = Path.Combine(root.Parent.FullName, Constants.LocalMarkdownFolderName);
            BodyHtml = _contentHelper.LoadLocalMarkdown(
                folder,
                languageCode,
                docName,
                _logger);

            _logger.LogInformation("Done rendering in Captions");
        }
    }
}