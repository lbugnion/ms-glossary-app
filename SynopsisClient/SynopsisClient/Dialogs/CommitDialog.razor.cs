using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace SynopsisClient.Dialogs
{
    public partial class CommitDialog
    {
        private bool _showWarning;

        private CommitModel Model
        {
            get;
            set;
        }

        [CascadingParameter]
        private BlazoredModalInstance ModalInstance
        {
            get;
            set;
        }

        protected override void OnInitialized()
        {
            Model = new CommitModel();
        }

        private async Task OnCancel()
        {
            await ModalInstance.CancelAsync();
        }

        private async Task OnOk()
        {
            if (string.IsNullOrEmpty(Model.Message))
            {
                _showWarning = true;
                return;
            }

            await ModalInstance.CloseAsync(ModalResult.Ok(Model.Message));
        }

        public class CommitModel
        {
            public CommitModel()
            {
                Message = string.Empty;
            }

            public string Message
            {
                get;
                set;
            }
        }
    }
}