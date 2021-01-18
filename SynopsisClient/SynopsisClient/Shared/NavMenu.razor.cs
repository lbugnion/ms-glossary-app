using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using SynopsisClient.Dialogs;
using System.Threading.Tasks;

namespace SynopsisClient.Shared
{
    public partial class NavMenu
    {
        private readonly bool _showDebug = true;
        private bool _collapseNavMenu = true;

        private string NavMenuCssClass => _collapseNavMenu ? "collapse" : null;

        [CascadingParameter]
        public IModalService Modal
        {
            get;
            set;
        }

#if DEBUG
#else
        private readonly bool _showDebug = false;
#endif

        private async Task CheckNavigateTo(string uri)
        {
            var cannotNavigate = false;
            string message = null;

            if ((Handler.CurrentEditContext != null
                && Handler.CurrentEditContext.IsModified())
                || Handler.IsModified)
            {
                cannotNavigate = true;
                message = "You cannot navigate now, please save or fix the Synopsis first.";
            }

            if (!UserManager.IsLoggedIn)
            {
                cannotNavigate = true;
                message = "You cannot navigate now, please log in first.";
            }

            if (cannotNavigate)
            {
                var parameters = new ModalParameters();
                parameters.Add(
                    nameof(MessageDialog.Message),
                    message);
                Modal.Show<MessageDialog>("Cannot navigate", parameters);
                return;
            }

            await Handler.ResetDialogs();
            Handler.DefineModal(null);
            Nav.NavigateTo(uri);
        }

        private void ToggleNavMenu()
        {
            _collapseNavMenu = !_collapseNavMenu;
        }
    }
}