using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using SynopsisClient.Model;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SynopsisClient.Dialogs;

namespace SynopsisClient.Pages
{
    public partial class Index
    {
        [CascadingParameter]
        private IModalService Modal
        {
            get;
            set;
        }

        public EditContext CurrentEditContext
        {
            get;
            set;
        }

        private void CurrentEditContextOnValidationStateChanged(
            object sender,
            ValidationStateChangedEventArgs e)
        {
            Log.LogInformation("-> CurrentEditContextOnValidationStateChanged");

            if ((UserManager.IsModified || CurrentEditContext.IsModified())
                && !CurrentEditContext.GetValidationMessages().Any())
            {
                Log.LogTrace("can load");
                UserManager.CannotLogIn = false;
            }
            else
            {
                Log.LogTrace("cannot load");
                UserManager.CannotLogIn = false;
            }
        }

        protected override async Task OnInitializedAsync()
        {
            Log.LogInformation("-> OnInitializedAsync");

            Log.LogDebug($"Handler.CannotLoadErrorMessage: {Handler.CannotLoadErrorMessage}");
            Log.LogDebug($"Handler.CannotSaveErrorMessage: {Handler.CannotSaveErrorMessage}");

            UserManager.DefineLog(Log);
            Handler.DefineLog(Log);

            Log.LogTrace("Initializing UserManager");
            UserManager.Initialize();
            CurrentEditContext = new EditContext(UserManager.CurrentUser);
            CurrentEditContext.OnValidationStateChanged += CurrentEditContextOnValidationStateChanged;

            Log.LogTrace("Passing modal in SynopsisHandler");
            Handler.DefineModal(Modal);

            Log.LogTrace("Checking login");
            await UserManager.CheckLogin();

            Log.LogInformation("OnInitializedAsync ->");
        }

        public async Task LogIn()
        {
            Log.LogInformation("-> LogIn");

            await UserManager.LogIn();

            if (UserManager.IsLoggedIn)
            {
                await Handler.ResetDialogs();
            }

            Log.LogInformation("LogIn ->");
        }

        public async Task LogOut()
        {
            Log.LogInformation("-> LogOut");

            var formModal = Modal.Show<ConfirmReloadDialog>("Are you sure you want to log out?");
            var result = await formModal.Result;

            Log.LogDebug($"Confirm: cancelled: {result.Cancelled}");

            if (result.Cancelled
                || result.Data == null
                || !(bool)result.Data)
            {
                Log.LogTrace("Confirm: cancelled");
                return;
            }

            await UserManager.LogOut();
            await Handler.DeleteLocalSynopsis();

            if (CurrentEditContext != null)
            {
                CurrentEditContext.OnValidationStateChanged -= CurrentEditContextOnValidationStateChanged;
            }

            CurrentEditContext = new EditContext(UserManager.CurrentUser);
            CurrentEditContext.OnValidationStateChanged += CurrentEditContextOnValidationStateChanged;

            Log.LogInformation("LogOut ->");
        }
    }
}