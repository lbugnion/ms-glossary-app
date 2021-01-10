using System;
using System.Threading.Tasks;

namespace SynopsisClient.Pages
{
    public partial class Title
    {
        protected override async Task OnInitializedAsync()
        {
            Console.WriteLine("Title.OnInitializedAsync");
            await Handler.InitializePage();
        }
    }
}
