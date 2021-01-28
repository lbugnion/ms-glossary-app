using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using MsGlossaryApp.DataModel;
using SynopsisClient.Dialogs;
using SynopsisClient.Model;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SynopsisClient.Pages
{
    public partial class Transcript : IDisposable
    {
        private int _words;
        private string _wordsInfoClass = ClientConstants.Css.WordsInfoGoodClass;
        private string _wordsSpanClass = ClientConstants.Css.WordsCountGoodClass;

        [CascadingParameter]
        private IModalService Modal
        {
            get;
            set;
        }

        private void CountWords()
        {
            _words = Handler.CountTranscriptWords();

            if (_words < Constants.MinWordsInTranscript
                || _words > Constants.MaxWordsInTranscript)
            {
                _wordsInfoClass = ClientConstants.Css.WordsInfoBadClass;
                _wordsSpanClass = ClientConstants.Css.WordsCountBadClass;
            }
            else
            {
                _wordsInfoClass = ClientConstants.Css.WordsInfoGoodClass;
                _wordsSpanClass = ClientConstants.Css.WordsCountGoodClass;
            }

            Log.LogDebug($"{_words} words");

            StateHasChanged();
        }

        private void CurrentEditContextOnValidationStateChanged(
            object sender,
            ValidationStateChangedEventArgs e)
        {
            CountWords();
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

        private async Task DeleteTranscriptLine(int index)
        {
            if (Handler.Synopsis.TranscriptLines.Count >= index)
            {
                var line = Handler.Synopsis.TranscriptLines[index];

                if (line is TranscriptSimpleLine
                    && Handler.Synopsis.TranscriptLines.Where(t => t is TranscriptSimpleLine).Count() == 1)
                {
                    // Last transcript simple line cannot be deleted

                    var parameters = new ModalParameters();
                    parameters.Add(nameof(MessageDialog.Message), "You need at least one line in the transcript");

                    Modal.Show<MessageDialog>("Cannot delete", parameters);
                }
                else
                {
                    await Handler.DeleteTranscriptLine(index);
                }
            }
        }

        private void KeyPressed(InputText element, KeyboardEventArgs args)
        {
            if (args.Key == " ")
            {
                Log.LogTrace("Counting");
                CountWords();
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
    }
}