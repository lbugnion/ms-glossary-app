using Microsoft.AspNetCore.Components.Forms;
using SynopsisClient.Model;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SynopsisClient.Pages
{
    public partial class Index
    {
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
                && CurrentEditContext.GetValidationMessages().Count() == 0)
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
            Console.WriteLine($"Handler.ErrorMessage: {Handler.ErrorMessage}");
            UserManager.Initialize();
            CurrentEditContext = new EditContext(UserManager.CurrentUser);
            CurrentEditContext.OnValidationStateChanged += CurrentEditContextOnValidationStateChanged;

            await UserManager.CheckLogin();
        }

        public async Task LogIn()
        {
            await UserManager.LogIn();

            if (UserManager.IsLoggedIn)
            {
                Handler.ResetDialogs();
            }
        }

        public async Task LogOut()
        {
            await UserManager.LogOut();

            if (CurrentEditContext != null)
            {
                CurrentEditContext.OnValidationStateChanged -= CurrentEditContextOnValidationStateChanged;
            }

            CurrentEditContext = new EditContext(UserManager.CurrentUser);
            CurrentEditContext.OnValidationStateChanged += CurrentEditContextOnValidationStateChanged;
        }
    }
}