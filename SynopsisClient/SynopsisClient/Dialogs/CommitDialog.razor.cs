using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace SynopsisClient.Dialogs
{
    public partial class CommitDialog
    {
        private bool _showWarning;

        [CascadingParameter]
        private BlazoredModalInstance ModalInstance
        {
            get;
            set;
        }

        private CommitModel Model
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
            if (string.IsNullOrEmpty(Model.Message))
            {
                _showWarning = true;
                return;
            }

            await ModalInstance.CloseAsync(ModalResult.Ok(Model.Message));
        }

        protected override void OnInitialized()
        {
            Model = new CommitModel();
        }

        public class CommitModel
        {
            public string Message
            {
                get;
                set;
            }

            public CommitModel()
            {
                Message = string.Empty;
            }
        }
    }
}