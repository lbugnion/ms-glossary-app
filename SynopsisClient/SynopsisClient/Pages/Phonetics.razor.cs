using System;
using System.Threading.Tasks;

namespace SynopsisClient.Pages
{
    public partial class Phonetics
    {
        protected override async Task OnInitializedAsync()
        {
            Console.WriteLine("Phonetics.OnInitializedAsync");
            await Handler.InitializePage();
        }
    }
}
