using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using MsGlossaryApp.DataModel;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SynopsisClient.Pages
{
    public partial class Transcript
    {
        private int _words;

        [CascadingParameter]
        private IModalService Modal
        {
            get;
            set;
        }

        protected override async Task OnInitializedAsync()
        {
            Log.LogInformation("-> OnInitializedAsync");

            UserManager.DefineLog(Log);
            Handler.DefineLog(Log);

            await UserManager.CheckLogin();

            if (!UserManager.IsLoggedIn)
            {
                Log.LogWarning("not logged in");
                Nav.NavigateTo("/");
                return;
            }

            var success = await Handler.InitializePage();

            if (success)
            {
                Handler.DefineList(Handler.Synopsis.TranscriptLines);
                Handler.DefineModal(Modal);
            }
            else
            {
                Log.LogWarning("Failed initializing page");
                Handler.DefineModal(null);
                Nav.NavigateTo("/");
                return;
            }

            Log.LogInformation("OnInitializedAsync ->");
        }

        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);
            CountWords();
        }

        private void CountWords()
        {
            var words = Handler.Synopsis
                .TranscriptLines
                .Where(l => l is TranscriptSimpleLine)
                .Select(l => l.Markdown.Split(new char[]
                {
                    ' '
                }, StringSplitOptions.RemoveEmptyEntries))
                .Select(w => w.Count())
                .Aggregate(AddFunc);

            Log.LogDebug($"{_words} words");
        }

        private Func<int, int, int> AddFunc = (int i1, int i2) => i1 + i2;
    }
}