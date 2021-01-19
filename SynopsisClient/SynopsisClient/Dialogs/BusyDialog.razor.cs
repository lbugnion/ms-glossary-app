using Blazored.Modal;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace SynopsisClient.Dialogs
{
    public partial class BusyDialog
    {
        private static BusyDialog _instance;
        private static ILogger _staticLog;

        [CascadingParameter]
        private BlazoredModalInstance ModalInstance
        {
            get;
            set;
        }

        protected override async Task OnInitializedAsync()
        {
            _staticLog = Log;
            Log.LogInformation("BusyDialog.OnInitializedAsync");

            if (_instance != null)
            {
                Log.LogDebug("Found instance, dismissing");
                await _instance.Dismiss();
            }

            _instance = this;
            base.OnInitialized();
            Log.LogInformation("Initialized");
        }

        public async static Task DismissAll()
        {
            _staticLog?.LogInformation("BusyDialog.DismissAll");

            if (_instance != null)
            {
                await _instance.Dismiss();
            }
        }

        public async Task Dismiss()
        {
            Log.LogInformation("BusyDialog.Dismiss");
            await ModalInstance.CancelAsync();
        }
    }
}