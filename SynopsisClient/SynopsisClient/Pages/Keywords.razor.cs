using System;
using System.Threading.Tasks;

namespace SynopsisClient.Pages
{
    public partial class Keywords
    {
        protected override async Task OnInitializedAsync()
        {
            Console.WriteLine("Keywords.OnInitializedAsync");
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
                Handler.DefineList(Handler.Synopsis.Keywords);
            }
            else
            {
                Nav.NavigateTo("/");
            }
        }
    }
}