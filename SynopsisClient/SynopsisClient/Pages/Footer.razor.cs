using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace SynopsisClient.Pages
{
    public partial class Footer
    {
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