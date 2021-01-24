using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace SynopsisClient.Pages
{
    public partial class Phonetics
    {
        [CascadingParameter]
        private IModalService Modal
        {
            get;
            set;
        }

        protected override async Task OnInitializedAsync()
        {
            Log.LogInformation("-> OnInitializedAsync");

            UserManager.DefineLog(Log);
            Handler.DefineLog(Log);

            await UserManager.CheckLogin();

            if (!UserManager.IsLoggedIn)
            {
                Log.LogWarning("not logged in");
                Nav.NavigateTo("/");
                return;
            }

            var success = await Handler.InitializePage();

            if (success)
            {
                Handler.DefineModal(Modal);
            }
            else
            {
                Log.LogWarning("Failed initializing page");
                Handler.DefineModal(null);
                Nav.NavigateTo("/");
            }

            Log.LogInformation("OnInitializedAsync ->");
        }
    }
}