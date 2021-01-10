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

        private void NavigateTitle()
        {
            CheckNavigateTo("/title");
        }

        private void NavigateKeywords()
        {
            CheckNavigateTo("/keywords");
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

        protected override void OnInitialized()
        {
            Handler.WasSaved += HandlerWasSaved;
        }

        public void Dispose()
        {
            Handler.WasSaved -= HandlerWasSaved;
        }

        public void NavigateDescription()
        {
            CheckNavigateTo("/short-description");
        }

        public void NavigatePhonetics()
        {
            CheckNavigateTo("/phonetics");
        }

        public void NavigateDemos()
        {
            CheckNavigateTo("/demos");
        }
    }
}