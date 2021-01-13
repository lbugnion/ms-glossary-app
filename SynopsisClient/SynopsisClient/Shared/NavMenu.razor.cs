using Microsoft.AspNetCore.Components.Web;
using System;

namespace SynopsisClient.Shared
{
    public partial class NavMenu : IDisposable
    {
        private bool _collapseNavMenu = true;
        private bool _showNavWarning = false;
        private bool _showLoginWarning;

        private bool ShowLoginWarning
        {
            get => _showLoginWarning;
            set
            {
                _showLoginWarning = value;
                StateHasChanged();
            }
        }

#if DEBUG
        private readonly bool _showDebug = true;
#else
        private readonly bool _showDebug = false;
#endif

        private string NavMenuCssClass => _collapseNavMenu ? "collapse" : null;

        private void CheckNavigateTo(string uri)
        {
            if ((Handler.CurrentEditContext != null
                && Handler.CurrentEditContext.IsModified())
                || Handler.IsModified)
            {
                _showNavWarning = true;
                Console.WriteLine("Edit Context is modified --> No nav");
                return;
            }

            if (!UserManager.IsLoggedIn)
            {
                ShowLoginWarning = true;
                return;
            }

            _showNavWarning = false;
            ShowLoginWarning = false;
            Handler.ResetDialogs();
            Nav.NavigateTo(uri);
        }

        private void HandlerWasSaved(object sender, EventArgs e)
        {
            Console.WriteLine("HandlerWasSaved");
            _showNavWarning = false;
            StateHasChanged();
        }

        private void ToggleNavMenu()
        {
            _collapseNavMenu = !_collapseNavMenu;
        }

        protected override void OnInitialized()
        {
            Handler.WasSaved += HandlerWasSaved;
            UserManager.LoggedInChanged += UserManagerLoggedInChanged;
        }

        private void UserManagerLoggedInChanged(object sender, bool e)
        {
            ShowLoginWarning = !e;
        }

        public void Dispose()
        {
            Handler.WasSaved -= HandlerWasSaved;
            UserManager.LoggedInChanged -= UserManagerLoggedInChanged;
        }
    }
}