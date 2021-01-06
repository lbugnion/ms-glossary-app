using System;

namespace SynopsisClient.Shared
{
    public partial class NavMenu
    {
        private bool collapseNavMenu = true;

        private string NavMenuCssClass => collapseNavMenu ? "collapse" : null;

        private void CheckNavigateTo(string uri)
        {
            if (Handler.CurrentEditContext != null
                && Handler.CurrentEditContext.IsModified())
            {
                // TODO Ask for confirmation
                Console.WriteLine("Edit Context is modified --> No nav");
                return;
            }

            Nav.NavigateTo(uri);
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
            collapseNavMenu = !collapseNavMenu;
        }
    }
}