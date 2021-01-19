using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace SynopsisClient.Pages
{
    public partial class LinksToOthers
    {
        [CascadingParameter]
        private IModalService Modal
        {
            get;
            set;
        }

        private void DefineList()
        {
            if (Handler.Synopsis != null)
            {
                Handler.DefineList(Handler.Synopsis.LinksToOthers);
            }
        }

        private async Task ReloadFromCloud()
        {
            await Handler.ReloadFromCloud();
            DefineList();
        }

        protected override async Task OnInitializedAsync()
        {
            Log.LogInformation("LinksToOthers.OnInitializedAsync");
            await UserManager.CheckLogin();

            if (!UserManager.IsLoggedIn)
            {
                Log.LogWarning("not logged in");
                Nav.NavigateTo("/");
                return;
            }

            var success = await Handler.InitializePage(Log);

            if (success)
            {
                DefineList();
                Handler.DefineModal(Modal);
            }
            else
            {
                Handler.DefineModal(null);
                Nav.NavigateTo("/");
            }
        }
    }
}