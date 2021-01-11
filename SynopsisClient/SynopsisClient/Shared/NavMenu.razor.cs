using Microsoft.AspNetCore.Components.Web;
using System;

namespace SynopsisClient.Shared
{
    public partial class NavMenu : IDisposable
    {
        private bool _collapseNavMenu = true;
        private bool _showNavWarning = false;

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

            _showNavWarning = false;
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
        }

        public void Dispose()
        {
            Handler.WasSaved -= HandlerWasSaved;
        }
    }
}