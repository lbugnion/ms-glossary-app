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
        private ContentHelper _contentHelper;

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

            _logger.LogInformation("Done rendering in Captions");
        }
    }
}