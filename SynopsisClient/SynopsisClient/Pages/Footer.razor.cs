using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace SynopsisClient.Pages
{
    public partial class Footer
    {
        private const string GitHubMask = "https://github.com/{0}/{1}/blob/{2}/synopsis/{2}.md";

        [Parameter]
        public EventCallback ReloadFromCloudClicked
        {
            get;
            set;
        }

        private async Task ReloadFromCloud()
        {
            await ReloadFromCloudClicked.InvokeAsync();
        }
    }
}
