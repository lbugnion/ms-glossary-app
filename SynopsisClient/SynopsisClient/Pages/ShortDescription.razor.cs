using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Threading.Tasks;

namespace SynopsisClient.Pages
{
    public partial class ShortDescription
    {
        protected override async Task OnInitializedAsync()
        {
            Console.WriteLine("ShortDescription.OnInitializedAsync");
            await Handler.InitializePage();
        }
    }
}