using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using SynopsisClient.Dialogs;
using System.Threading.Tasks;

namespace SynopsisClient.Shared
{
    public partial class NavMenu
    {
        private bool _collapseNavMenu = true;
        private bool _showDebug;

        private string NavMenuCssClass => _collapseNavMenu ? "collapse" : null;

        [CascadingParameter]
        public IModalService Modal
        {
            get;
            set;
        }

        private async Task CheckNavigateTo(string uri, bool bypassLogin = false)
        {
            Log.LogInformation("HIGHLIGHT---> CheckNavigateTo");
            Log.LogDebug(uri);

            var cannotNavigate = false;
            string message = null;

            if (UserManager.CurrentUser.ForceLogout)
            {
                Log.LogTrace("HIGHLIGHT--Cannot navigate, ForceLogout is active");

                var parameters = new ModalParameters();
                parameters.Add(nameof(MessageDialog.Message), "We cannot navigate now because a log out is required");
                Modal.Show<MessageDialog>("Log out required", parameters);
                return;
            }

            if ((Handler.CurrentEditContext != null
                && Handler.CurrentEditContext.IsModified())
                || Handler.IsModified)
            {
                cannotNavigate = true;
                message = "You cannot navigate now, please save or fix the Synopsis first.";
                Log.LogWarning(message);
            }

            if (!bypassLogin
                && !UserManager.IsLoggedIn)
            {
                cannotNavigate = true;
                message = "You cannot navigate now, please log in first.";
                Log.LogWarning(message);
            }

            if (cannotNavigate)
            {
                var parameters = new ModalParameters();
                parameters.Add(
                    nameof(MessageDialog.Message),
                    message);
                Modal.Show<MessageDialog>("Cannot navigate", parameters);
                Log.LogInformation("Showing cannot navigate message");
                return;
            }

            Log.LogTrace("Resetting dialog");
            await Handler.ResetDialogs();
            Log.LogTrace("Resetting modal");
            Handler.DefineModal(null);
            Log.LogTrace($"Navigating to {uri}");
            Nav.NavigateTo(uri);

            Log.LogInformation("CheckNavigateTo ->");
        }

        private void ToggleNavMenu()
        {
            _collapseNavMenu = !_collapseNavMenu;
        }

        protected override void OnInitialized()
        {
#if DEBUG
            _showDebug = true;
#endif
        }
    }
}