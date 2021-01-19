using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using SynopsisClient.Model;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

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
            Log.LogInformation("CurrentEditContextOnValidationStateChanged");

            if ((UserManager.IsModified || CurrentEditContext.IsModified())
                && !CurrentEditContext.GetValidationMessages().Any())
            {
                Log.LogInformation("can load");
                UserManager.CannotLogIn = false;
            }
            else
            {
                Log.LogInformation("cannot load");
                UserManager.CannotLogIn = false;
            }
        }

        protected override async Task OnInitializedAsync()
        {
            Log.LogInformation("OnInitialized");
            Log.LogDebug($"Handler.CannotLoadErrorMessage: {Handler.CannotLoadErrorMessage}");
            Log.LogDebug($"Handler.CannotSaveErrorMessage: {Handler.CannotSaveErrorMessage}");
            
            UserManager.Initialize();
            CurrentEditContext = new EditContext(UserManager.CurrentUser);
            CurrentEditContext.OnValidationStateChanged += CurrentEditContextOnValidationStateChanged;

            Handler.DefineModal(Modal);
            await UserManager.CheckLogin();

            if (Handler != null
                && Handler.Synopsis != null
                && Handler.Synopsis.LinksInstructions != null
                && Handler.Synopsis.LinksInstructions.Count > 0)
            {
                Log.LogDebug($"21 -----> {Handler.Synopsis.LinksInstructions.First().Key}");
            }
        }

        public async Task LogIn()
        {
            await UserManager.LogIn();

            if (UserManager.IsLoggedIn)
            {
                await Handler.ResetDialogs();
            }
        }

        public async Task LogOut()
        {
            await UserManager.LogOut();
            Handler.DeleteLocalSynopsis();

            if (CurrentEditContext != null)
            {
                CurrentEditContext.OnValidationStateChanged -= CurrentEditContextOnValidationStateChanged;
            }

            CurrentEditContext = new EditContext(UserManager.CurrentUser);
            CurrentEditContext.OnValidationStateChanged += CurrentEditContextOnValidationStateChanged;
        }
    }
}