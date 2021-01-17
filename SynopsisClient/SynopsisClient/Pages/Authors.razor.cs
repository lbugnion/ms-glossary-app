using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using SynopsisClient.Dialogs;
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

        protected override async Task OnInitializedAsync()
        {
            Console.WriteLine("Authors.OnInitializedAsync");

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
                Handler.DefineList(Handler.Synopsis.Authors);
                Handler.DefineModal(Modal);
            }
            else
            {
                Handler.DefineModal(null);
                Nav.NavigateTo("/");
            }
        }

        //private async Task ReloadLocal()
        //{
        //    Console.WriteLine("Authors.ReloadLocal");

        //    var parameters = new ModalParameters();
        //    parameters.Add(nameof(ConfirmDialog.OkText), "OK");
        //    parameters.Add(nameof(ConfirmDialog.CancelText), "Cancel");
        //    parameters.Add(nameof(ConfirmDialog.Message), new RenderFragment());

        //    var formModal = Modal.Show<ConfirmDialog>("Some title", parameters);
        //    var result = await formModal.Result;

        //    Console.WriteLine($"Result cancelled: {result.Cancelled}");
            
        //    if (!result.Cancelled)
        //    {
        //        Console.WriteLine($"Result confirmed: {(bool)result.Data}");
        //    }

        //    if (result.Cancelled)
        //    {
        //        Console.WriteLine("Cancelling reload local");
        //    }
        //    else if (result.Data != null
        //        && (bool)result.Data)
        //    {
        //        Console.WriteLine("Reloading local");
        //        Handler.ExecuteReloadLocal();
        //    }
        //}
    }
}