using System;
using System.Threading.Tasks;

namespace SynopsisClient.Pages
{
    public partial class Title
    {
        protected override async Task OnInitializedAsync()
        {
            Console.WriteLine("Title.OnInitializedAsync");
            await UserManager.CheckLogin();

            if (!UserManager.IsLoggedIn)
            {
                Console.WriteLine("not logged in");
                Nav.NavigateTo("/");
                return;
            }

            await Handler.InitializePage();
        }
    }
}
