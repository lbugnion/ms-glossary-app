using System;
using System.Threading.Tasks;

namespace SynopsisClient.Pages
{
    public partial class ShortDescription
    {
        protected override async Task OnInitializedAsync()
        {
            Console.WriteLine("Authors.OnInitializedAsync");
            await Handler.InitializePage();
        }
    }
}
