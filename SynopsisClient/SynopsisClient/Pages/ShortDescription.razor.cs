using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using MsGlossaryApp.DataModel;
using SynopsisClient.Model;
using System;
using System.Threading.Tasks;

namespace SynopsisClient.Pages
{
    public partial class ShortDescription : IDisposable
    {
        private int _characters;
        private string _charInfoClass = ClientConstants.Css.WordsInfoGoodClass;
        private string _charSpanClass = ClientConstants.Css.WordsCountGoodClass;

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
                Handler.DefineModal(Modal);
            }
            else
            {
                Log.LogWarning("Failed initializing page");
                Handler.DefineModal(null);
                Nav.NavigateTo("/");
            }

            CountCharacters();
            Handler.CurrentEditContext.OnValidationStateChanged
                += CurrentEditContextOnValidationStateChanged;

            Log.LogInformation("OnInitializedAsync ->");
        }

        public void Dispose()
        {
            Handler.CurrentEditContext.OnValidationStateChanged
                -= CurrentEditContextOnValidationStateChanged;
        }

        private void CountCharacters()
        {
            _characters = Handler.Synopsis.ShortDescription.Length;

            if (_characters < Constants.MinCharactersInDescription
                || _characters > Constants.MaxCharactersInDescription)
            {
                _charInfoClass = ClientConstants.Css.WordsInfoBadClass;
                _charSpanClass = ClientConstants.Css.WordsCountBadClass;
            }
            else
            {
                _charInfoClass = ClientConstants.Css.WordsInfoGoodClass;
                _charSpanClass = ClientConstants.Css.WordsCountGoodClass;
            }

            Log.LogDebug($"HIGHLIGHT--{_characters} characters");

            StateHasChanged();
        }

        private void KeyPress()
        {
            CountCharacters();
        }

        private void CurrentEditContextOnValidationStateChanged(
            object sender,
            ValidationStateChangedEventArgs e)
        {
            Log.LogTrace("HIGHLIGHT--CurrentEditContextOnValidationStateChanged");
            CountCharacters();
        }

        private async Task ReloadFromCloud()
        {
            Log.LogInformation("-> ReloadFromCloud");

            Handler.CurrentEditContext.OnValidationStateChanged
                -= CurrentEditContextOnValidationStateChanged;

            await Handler.ReloadFromCloud();

            Handler.CurrentEditContext.OnValidationStateChanged
                += CurrentEditContextOnValidationStateChanged;

            CountCharacters();
            Log.LogInformation("ReloadFromCloud ->");
        }
    }
}