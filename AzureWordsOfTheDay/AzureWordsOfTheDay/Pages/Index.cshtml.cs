using AzureWordsOfTheDay.Model;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace AzureWordsOfTheDay.Pages
{
    public class IndexModel : PageModel
    {
        private MarkdownHelper _markdown;

        public HtmlString TopicBarHtml
        {
            get;
            private set;
        }

        public IndexModel(
            MarkdownHelper markdown)
        {
            _markdown = markdown;
        }

        public async Task OnGet()
        {
            TopicBarHtml = await _markdown.LoadTopicsBar();
        }
    }
}
