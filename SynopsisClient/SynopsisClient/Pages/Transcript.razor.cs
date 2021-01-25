using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using MsGlossaryApp.DataModel;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SynopsisClient.Pages
{
    public partial class Transcript : IDisposable
    {
        private const int MaxWordsInTranscript = 320;
        private const int MinWordsInTranscript = 280;
        private const string WordsCountBadClass = "transcript-words-count-bad";
        private const string WordsCountGoodClass = "transcript-words-count-good";
        private const string WordsInfoBadClass = "transcript-words-info-bad";
        private const string WordsInfoGoodClass = "transcript-words-info-good";
        private int _words;
        private string _wordsInfoClass = WordsInfoGoodClass;
        private string _wordsSpanClass = WordsCountGoodClass;
        private Func<int, int, int> AddFunc = (int i1, int i2) => i1 + i2;

        [CascadingParameter]
        private IModalService Modal
        {
            get;
            set;
        }

        private void CountWords()
        {
            _words = Handler.Synopsis
                .TranscriptLines
                .Where(l => l is TranscriptSimpleLine)
                .Select(l => l.Markdown.Split(new char[]
                {
                    ' '
                }, StringSplitOptions.RemoveEmptyEntries))
                .Select(w => w.Count())
                .Aggregate(AddFunc);

            if (_words < MinWordsInTranscript
                || _words > MaxWordsInTranscript)
            {
                _wordsInfoClass = WordsInfoBadClass;
                _wordsSpanClass = WordsCountBadClass;
            }
            else
            {
                _wordsInfoClass = WordsInfoGoodClass;
                _wordsSpanClass = WordsCountGoodClass;
            }

            Log.LogDebug($"{_words} words after");

            StateHasChanged();
        }

        private void CurrentEditContextOnValidationStateChanged(
            object sender,
            ValidationStateChangedEventArgs e)
        {
            CountWords();
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
                DefineList();
                Handler.DefineModal(Modal);
            }
            else
            {
                Log.LogWarning("Failed initializing page");
                Handler.DefineModal(null);
                Nav.NavigateTo("/");
                return;
            }

            CountWords();
            Handler.CurrentEditContext.OnValidationStateChanged
                += CurrentEditContextOnValidationStateChanged;

            Log.LogInformation("OnInitializedAsync ->");
        }

        public void Dispose()
        {
            Handler.CurrentEditContext.OnValidationStateChanged
                -= CurrentEditContextOnValidationStateChanged;
        }

        private void DefineList()
        {
            Log.LogInformation("-> DefineList");

            if (Handler.Synopsis != null)
            {
                Log.LogTrace("Synopsis is not null");
                Handler.DefineList(Handler.Synopsis.TranscriptLines);
            }
        }

        private async Task ReloadFromCloud()
        {
            Log.LogInformation("-> ReloadFromCloud");

            Handler.CurrentEditContext.OnValidationStateChanged
                -= CurrentEditContextOnValidationStateChanged;

            await Handler.ReloadFromCloud();

            Handler.CurrentEditContext.OnValidationStateChanged
                += CurrentEditContextOnValidationStateChanged;

            DefineList();
            CountWords();
            Log.LogInformation("ReloadFromCloud ->");
        }
    }
}