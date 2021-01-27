using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using SynopsisClient.Dialogs;
using SynopsisClient.Model;
using System.Linq;
using System.Threading.Tasks;

namespace SynopsisClient.Pages
{
    public partial class Index
    {
        private const string QueryEdit = "edit=";

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

        [Parameter]
        public string Term
        {
            get;
            set;
        }

        private void CurrentEditContextOnValidationStateChanged(
            object sender,
            ValidationStateChangedEventArgs e)
        {
            Log.LogInformation("-> CurrentEditContextOnValidationStateChanged");

            if ((UserManager.IsModified || CurrentEditContext.IsModified())
                && !CurrentEditContext.GetValidationMessages().Any())
            {
                Log.LogTrace("can load");
                UserManager.CannotLogIn = false;
            }
            else
            {
                Log.LogTrace("cannot load");
                UserManager.CannotLogIn = false;
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender)
            {
                return;
            }

            Log.LogInformation("-> OnAfterRenderAsync");

            if (await UserManager.CheckLogin())
            {
                Log.LogTrace("User is currently logged in");

                if (!string.IsNullOrEmpty(Term)
                    && UserManager.CurrentUser.SynopsisName.ToLower() != Term.ToLower())
                {
                    Log.LogTrace("User asked for a different term, show dialog");

                    var parameters = new ModalParameters();
                    parameters.Add(
                        nameof(ConfirmLogOutDialog.CurrentTerm),
                        UserManager.CurrentUser.SynopsisName);
                    parameters.Add(
                        nameof(ConfirmLogOutDialog.NewTerm),
                        Term);

                    var options = new ModalOptions()
                    {
                        HideCloseButton = true,
                        DisableBackgroundCancel = true
                    };

                    var formModal = Modal.Show<ConfirmLogOutDialog>(
                        "You are currently editing!",
                        parameters,
                        options);

                    var result = await formModal.Result;

                    Log.LogDebug($"Confirm: cancelled: {result.Cancelled}");

                    if (!result.Cancelled
                        && result.Data != null
                        && (bool)result.Data)
                    {
                        await LogOut(true, Term);
                        StateHasChanged();
                    }
                }
            }

            Log.LogInformation("OnAfterRenderAsync ->");
        }

        protected override async Task OnInitializedAsync()
        {
            Log.LogInformation("HIGHLIGHT---> OnInitializedAsync");

            Log.LogDebug($"Handler.CannotLoadErrorMessage: {Handler.CannotLoadErrorMessage}");
            Log.LogDebug($"Handler.CannotSaveErrorMessage: {Handler.CannotSaveErrorMessage}");

            // Check query string
            if (string.IsNullOrEmpty(Term))
            {
                Log.LogDebug($"Term is NOT already defined in route: {Term}");
                Log.LogDebug($"URI: {Nav.Uri}");

                var query = Nav.ToAbsoluteUri(Nav.Uri).Query;
                var index = query.IndexOf(QueryEdit);

                if (index > -1)
                {
                    Log.LogTrace("Found edit query");

                    var indexOfAnd = query.IndexOf("&");

                    if (indexOfAnd > -1)
                    {
                        Term = query.Substring(index + QueryEdit.Length, indexOfAnd);
                    }
                    else
                    {
                        Term = query.Substring(index + QueryEdit.Length);
                    }

                    Log.LogDebug($"Term in query: {Term}");
                }
            }

            UserManager.DefineLog(Log);
            Handler.DefineLog(Log);

            Log.LogTrace("Initializing UserManager");
            UserManager.Initialize(Term);
            CurrentEditContext = new EditContext(UserManager.CurrentUser);
            CurrentEditContext.OnValidationStateChanged += CurrentEditContextOnValidationStateChanged;

            Log.LogTrace("Passing modal in SynopsisHandler");
            Handler.DefineModal(Modal);

            Log.LogTrace("HIGHLIGHT--Checking login");
            await UserManager.CheckLogin();

            if (UserManager.CurrentUser.ForceLogout)
            {
                Log.LogTrace("HIGHLIGHT--ForceLogout is true");
                var parameters = new ModalParameters();
                parameters.Add(nameof(MessageDialog.Message), "Because you changed your email address you must log out and log in again.");
                Modal.Show<MessageDialog>("Log out required", parameters);
            }
            else
            {
                Log.LogTrace("HIGHLIGHT--ForceLogout is false");
            }

            Log.LogInformation("OnInitializedAsync ->");
        }

        public async Task LogIn()
        {
            Log.LogInformation("-> LogIn");

            await UserManager.LogIn();

            if (UserManager.IsLoggedIn)
            {
                await Handler.ResetDialogs();
            }

            Log.LogInformation("LogIn ->");
        }

        public async Task LogOut(
            bool suppressConfirm = false,
            string term = null)
        {
            Log.LogInformation("-> LogOut");
            Log.LogDebug($"suppressConfirm: {suppressConfirm}");

            if (!suppressConfirm)
            {
                var formModal = Modal.Show<ConfirmReloadDialog>("Are you sure you want to log out?");
                var result = await formModal.Result;

                Log.LogDebug($"Confirm: cancelled: {result.Cancelled}");

                if (result.Cancelled
                    || result.Data == null
                    || !(bool)result.Data)
                {
                    Log.LogTrace("Confirm: cancelled");
                    return;
                }
            }

            await UserManager.LogOut(term);
            await Handler.DeleteLocalSynopsis();

            if (CurrentEditContext != null)
            {
                CurrentEditContext.OnValidationStateChanged -= CurrentEditContextOnValidationStateChanged;
            }

            CurrentEditContext = new EditContext(UserManager.CurrentUser);
            CurrentEditContext.OnValidationStateChanged += CurrentEditContextOnValidationStateChanged;

            Log.LogInformation("LogOut ->");
        }
    }
}