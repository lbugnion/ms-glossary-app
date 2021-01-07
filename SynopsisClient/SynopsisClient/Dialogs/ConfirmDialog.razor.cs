using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace SynopsisClient.Dialogs
{
    public partial class ConfirmDialog
    {
        [Parameter]
        public string CancelText
        {
            get;
            set;
        }

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
        public EventCallback<bool> OnOkCancelClicked
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

        private async Task OnCancel()
        {
            await OnOkCancelClicked.InvokeAsync(false);
            ShowDialog = false;
        }

        private async Task OnOk()
        {
            await OnOkCancelClicked.InvokeAsync(true);
            ShowDialog = false;
        }
    }
}