using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace SynopsisClient.Pages
{
    public partial class Authors
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
                Handler.DefineList(Handler.Synopsis.Authors);
            }
        }

        private async Task ReloadFromCloud()
        {
            await Handler.ReloadFromCloud();
            DefineList();
        }

        protected override async Task OnInitializedAsync()
        {
            Log.LogInformation("Authors.OnInitializedAsync");

            await UserManager.CheckLogin(Log);

            if (!UserManager.IsLoggedIn)
            {
                Log.LogInformation("not logged in");
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