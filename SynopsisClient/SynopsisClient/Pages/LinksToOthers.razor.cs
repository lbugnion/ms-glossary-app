using System;
using System.Threading.Tasks;

namespace SynopsisClient.Pages
{
    public partial class LinksToOthers
    {
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
                Handler.DefineList(Handler.Synopsis.LinksToOthers);
            }
            else
            {
                Nav.NavigateTo("/");
            }
        }
    }
}
