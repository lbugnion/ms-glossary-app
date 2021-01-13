using System;
using System.Threading.Tasks;

namespace SynopsisClient.Pages
{
    public partial class ShortDescription
    {
        protected override async Task OnInitializedAsync()
        {
            Console.WriteLine("ShortDescription.OnInitializedAsync");
            await UserManager.CheckLogin();

            if (!UserManager.IsLoggedIn)
            {
                Console.WriteLine("not logged in");
                Nav.NavigateTo("/");
                return;
            }

            var success = await Handler.InitializePage();

            if (!success)
            {
                Nav.NavigateTo("/");
            }
        }
    }
}