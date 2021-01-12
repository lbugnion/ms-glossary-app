using System;
using System.Threading.Tasks;

namespace SynopsisClient.Pages
{
    public partial class Authors
    {
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

            await Handler.InitializePage();
            Handler.DefineList(Handler.Synopsis.Authors);
        }
    }
}