using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace SynopsisClient.Dialogs
{
    public partial class ConfirmDialog
    {
        [CascadingParameter]
        BlazoredModalInstance ModalInstance
        {
            get;
            set;
        }

        [Parameter]
        public string CancelText
        {
            get;
            set;
        }

        [Parameter]
        public string Message
        {
            get;
            set;
        }

        //[Parameter]
        //public RenderFragment ChildContent
        //{
        //    get;
        //    set;
        //}

        [Parameter]
        public string OkText
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
            await ModalInstance.CancelAsync();
        }

        private async Task OnOk()
        {
            await ModalInstance.CloseAsync(ModalResult.Ok(true));
        }
    }
}