using System;
using System.Threading.Tasks;

namespace SynopsisClient.Pages
{
    public partial class Phonetics
    {
        protected override async Task OnInitializedAsync()
        {
            Console.WriteLine("Phonetics.OnInitializedAsync");
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