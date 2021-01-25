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
            Log.LogInformation("-> CheckNavigateTo");
            Log.LogDebug(uri);

            var cannotNavigate = false;
            string message = null;

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