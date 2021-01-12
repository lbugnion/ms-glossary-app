using System;
using System.Threading.Tasks;

namespace SynopsisClient.Pages
{
    public partial class PersonalNotes
    {
        protected override async Task OnInitializedAsync()
        {
            Console.WriteLine("PersonalNotes.OnInitializedAsync");
            await UserManager.CheckLogin();

            if (!UserManager.IsLoggedIn)
            {
                Console.WriteLine("not logged in");
                Nav.NavigateTo("/");
                return;
            }

            await Handler.InitializePage();
            Handler.DefineList(Handler.Synopsis.PersonalNotes);
        }
    }
}