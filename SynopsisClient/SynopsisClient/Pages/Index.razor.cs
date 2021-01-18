using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using SynopsisClient.Model;
using System;
using System.Linq;
using System.Threading.Tasks;

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
            Console.WriteLine("CurrentEditContextOnValidationStateChanged");

            if ((UserManager.IsModified || CurrentEditContext.IsModified())
                && !CurrentEditContext.GetValidationMessages().Any())
            {
                Console.WriteLine("can load");
                UserManager.CannotLogIn = false;
            }
            else
            {
                Console.WriteLine("cannot load");
                UserManager.CannotLogIn = false;
            }
        }

        protected override async Task OnInitializedAsync()
        {
            Console.WriteLine("OnInitialized");
            Console.WriteLine($"Handler.CannotLoadErrorMessage: {Handler.CannotLoadErrorMessage}");
            Console.WriteLine($"Handler.CannotSaveErrorMessage: {Handler.CannotSaveErrorMessage}");
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
                Console.WriteLine($"21 -----> {Handler.Synopsis.LinksInstructions.First().Key}");
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