using System;

namespace SynopsisClient.Shared
{
    public partial class NavMenu : IDisposable
    {
        private bool _collapseNavMenu = true;
        private bool _showNavWarning = false;

        private string NavMenuCssClass => _collapseNavMenu ? "collapse" : null;

        private void CheckNavigateTo(string uri)
        {
            if (Handler.CurrentEditContext != null
                && Handler.CurrentEditContext.IsModified())
            {
                _showNavWarning = true;
                Console.WriteLine("Edit Context is modified --> No nav");
                return;
            }

            _showNavWarning = false;
            Nav.NavigateTo(uri);
        }

        protected override void OnInitialized()
        {
            Handler.WasSaved += HandlerWasSaved;
        }

        private void HandlerWasSaved(object sender, EventArgs e)
        {
            Console.WriteLine("HandlerWasSaved");
            _showNavWarning = false;
            StateHasChanged();
        }

        private void NavigateAuthors()
        {
            CheckNavigateTo("/authors");
        }

        private void NavigateHome()
        {
            CheckNavigateTo("/");
        }

        private void NavigatePersonalNotes()
        {
            CheckNavigateTo("/personal-notes");
        }

        private void ToggleNavMenu()
        {
            _collapseNavMenu = !_collapseNavMenu;
        }

        public void Dispose()
        {
            Handler.WasSaved -= HandlerWasSaved;
        }
    }
}