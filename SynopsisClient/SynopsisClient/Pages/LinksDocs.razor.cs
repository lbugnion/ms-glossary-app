using System;
using System.Threading.Tasks;

namespace SynopsisClient.Pages
{
    public partial class Links
    {
        protected override async Task OnInitializedAsync()
        {
            Console.WriteLine("Links.OnInitializedAsync");
            await Handler.InitializePage();
            Handler.DefineList(Handler.Synopsis.Keywords);
        }
    }
}
