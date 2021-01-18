using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
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

        protected override async Task OnInitializedAsync()
        {
            Console.WriteLine("LinksToOthers.OnInitializedAsync");
            await UserManager.CheckLogin();

            if (!UserManager.IsLoggedIn)
            {
                Console.WriteLine("not logged in");
                Nav.NavigateTo("/");
                return;
            }

            var success = await Handler.InitializePage();

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
    }
}