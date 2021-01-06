using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Forms;
using MsGlossaryApp.DataModel;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace SynopsisClient.Model
{
    public class SynopsisHandler
    {
        private const string Key = "Current-Synopsis";

        private ILocalStorageService _localStorage;

        public Synopsis Synopsis
        {
            get;
            private set;
        }

        public EditContext CurrentEditContext
        {
            get;
            set;
        }

        public async Task InitializePage()
        {
            //await _localStorage.SetItemAsync<Synopsis>(Key, null);

            // Reset changes every time that the page changes

            Console.WriteLine("SynopsisHandler.InitializePage");

            Synopsis = await GetSynopsis(true, false);
            SetContext(Synopsis);            
        }

        private void SetContext(Synopsis synopsis)
        {
            if (CurrentEditContext != null)
            {
                CurrentEditContext.OnFieldChanged -= CurrentEditContextOnFieldChanged;
                CurrentEditContext.OnValidationStateChanged -= CurrentEditContextOnValidationStateChanged;
            }

            CurrentEditContext = new EditContext(Synopsis);
            CurrentEditContext.OnFieldChanged += CurrentEditContextOnFieldChanged;
            CurrentEditContext.OnValidationStateChanged += CurrentEditContextOnValidationStateChanged;
            CannotSave = true;
        }

        public void DeleteNote(Note note)
        {
            Console.WriteLine("SynopsisHandler.DeleteNote");

            if (Synopsis.PersonalNotes.Contains(note))
            {
                Synopsis.PersonalNotes.Remove(note);
                _isModified = true;
                CannotSave = false; // No validation here
                Console.WriteLine($"SynopsisHandler.DeleteNote deleted");
            }
        }

        private async Task ExecuteReloadFromCloud()
        {
            Console.WriteLine("SynopsisHandler.ReloadFromCloud");
            Synopsis = await GetSynopsis(false, true);
            SetContext(Synopsis);
            await _localStorage.SetItemAsync(Key, Synopsis);
            Console.WriteLine($"{Synopsis.Authors.Count} authors found");
        }

        public void TriggerValidation()
        {
            if (CurrentEditContext != null)
            {
                Console.WriteLine("Triggering Validation");
                var isValid = CurrentEditContext.Validate();
                Console.WriteLine("TriggerValidation: " + isValid);
            }
        }

        private void CurrentEditContextOnValidationStateChanged(
            object sender, 
            ValidationStateChangedEventArgs e)
        {
            Console.WriteLine("CurrentEditContextOnValidationStateChanged");

            if ((CurrentEditContext.IsModified() || _isModified)
                && CurrentEditContext.GetValidationMessages().Count() == 0)
            {
                Console.WriteLine("can save");
                CannotSave = false;
            }
            else
            {
                Console.WriteLine("cannot save");
                CannotSave = true;
            }
        }

        private void CurrentEditContextOnFieldChanged(
            object sender, 
            FieldChangedEventArgs e)
        {
            Console.WriteLine("CurrentEditContextOnFieldChanged");
            CannotSave = false;
        }

        public SynopsisHandler(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        private async Task<Synopsis> GetSynopsis(
            bool forceRefreshLocal, 
            bool forcefreshOnline)
        {
            if (!forcefreshOnline
                && (Synopsis == null
                || forceRefreshLocal))
            {
                Console.WriteLine("Loading synopsis from local storage");

                try
                {
                    Synopsis = await _localStorage.GetItemAsync<Synopsis>(Key);
                }
                catch
                {
                    Synopsis = null;
                }

                if (Synopsis == null)
                {
                    Console.WriteLine("Synopsis is null");
                }
                else
                {
                    Console.WriteLine("Synopsis is not null");
                }
            }

            if (forcefreshOnline
                || Synopsis == null)
            {
                Console.WriteLine("Loading synopsis from network");
                var client = new HttpClient();
                Synopsis = await client.GetFromJsonAsync<Synopsis>("https://localhost:44395/sample-data/test-topic-15.json?ticks=" + DateTime.Now.Ticks);
            }

            return Synopsis;
        }

        public bool CannotSave
        {
            get;
            private set;
        }

        private bool _isModified;

        public async Task CheckSaveSynopsis()
        {
            Console.WriteLine("CheckSaveSynopsis");
            Console.WriteLine($"CurrentEditContext.IsModified: {CurrentEditContext.IsModified()}");
            Console.WriteLine($"_isModified: {_isModified}");

            if ((_isModified || CurrentEditContext.IsModified())
                && !CannotSave)
            {
                Console.WriteLine("Must save");
                await _localStorage.SetItemAsync(Key, Synopsis);
                CurrentEditContext.MarkAsUnmodified();
                CannotSave = true;
                _isModified = false;
            }
        }

        public void DeleteAuthor(Author author)
        {
            Console.WriteLine("SynopsisHandler.DeleteAuthor");

            if (Synopsis.Authors.Contains(author))
            {
                Synopsis.Authors.Remove(author);
                _isModified = true;
                Console.WriteLine($"SynopsisHandler.DeleteAuthor deleted");
            }
        }

        public bool ShowConfirmReloadFromCloudDialog
        {
            get;
            private set;
        }

        public void ReloadFromCloud()
        {
            Console.WriteLine("In ReloadFromCloud");
            ShowConfirmReloadFromCloudDialog = true;
        }

        public async Task ReloadFromCloudConfirmationOkCancelClicked(bool confirm)
        {
            ShowConfirmReloadFromCloudDialog = false;

            if (confirm)
            {
                await ExecuteReloadFromCloud();
            }
        }

    }
}
