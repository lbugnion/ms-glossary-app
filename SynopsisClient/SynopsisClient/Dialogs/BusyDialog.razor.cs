using Blazored.Modal;
using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace SynopsisClient.Dialogs
{
    public partial class BusyDialog
    {
        private static BusyDialog _instance;

        [CascadingParameter]
        private BlazoredModalInstance ModalInstance
        {
            get;
            set;
        }

        protected override async Task OnInitializedAsync()
        {
            Console.WriteLine("BusyDialog.OnInitializedAsync");

            if (_instance != null)
            {
                Console.WriteLine("Found instance, dismissing");
                await _instance.Dismiss();
            }

            _instance = this;
            base.OnInitialized();
            Console.WriteLine("Initialized");
        }

        public async static Task DismissAll()
        {
            Console.WriteLine("BusyDialog.DismissAll");

            if (_instance != null)
            {
                await _instance.Dismiss();
            }
        }

        public async Task Dismiss()
        {
            Console.WriteLine("BusyDialog.Dismiss");
            await ModalInstance.CancelAsync();
        }
    }
}