using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace SynopsisClient.Dialogs
{
    public partial class MessageDialog
    {
        [Parameter]
        public RenderFragment ChildContent
        {
            get;
            set;
        }

        [Parameter]
        public string OkText
        {
            get;
            set;
        }

        [Parameter]
        public EventCallback OnOkClicked
        {
            get;
            set;
        }

        [Parameter]
        public bool ShowDialog
        {
            get;
            set;
        }

        [Parameter]
        public string Title
        {
            get;
            set;
        }

        private async Task OnOk()
        {
            await OnOkClicked.InvokeAsync(null);
            ShowDialog = false;
        }
    }
}