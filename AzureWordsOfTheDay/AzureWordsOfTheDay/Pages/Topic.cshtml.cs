using AzureWordsOfTheDay.Model;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace AzureWordsOfTheDay.Pages
{
    public class TopicModel : PageModel
    {
        private MarkdownHelper _markdown;

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
            MarkdownHelper markdown)
        {
            _markdown = markdown;
        }

        public async Task<IActionResult> OnGet(string topic)
        {
            Topic = topic.ToLower();

            if (string.IsNullOrEmpty(Topic))
            {
                return Redirect("/");
            }

            TopicHtml = await _markdown.LoadMarkdown(Topic);
            TopicBarHtml = await _markdown.LoadTopicsBar();
            return null;
        }
    }
}