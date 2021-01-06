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

        public void TriggerValidation()
        {
            if (CurrentEditContext != null)
            {
                var isValid = CurrentEditContext.Validate();
                Console.WriteLine("TriggerValidation: " + isValid);
            }
        }

        private void CurrentEditContextOnValidationStateChanged(
            object sender, 
            ValidationStateChangedEventArgs e)
        {
            Console.WriteLine("CurrentEditContextOnValidationStateChanged");

            if (CurrentEditContext.GetValidationMessages().Count() == 0)
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
                Synopsis = await _localStorage.GetItemAsync<Synopsis>(Key);

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
                Synopsis = await client.GetFromJsonAsync<Synopsis>("https://localhost:44395/sample-data/test-topic-15.json");
                await _localStorage.SetItemAsync<Synopsis>(Key, Synopsis);
            }

            return Synopsis;
        }

        public bool CannotSave
        {
            get;
            private set;
        }

        public async Task CheckSaveSynopsis()
        {
            Console.WriteLine("CheckSaveSynopsis");

            if (CurrentEditContext.IsModified()
                && !CannotSave)
            {
                Console.WriteLine("Must save");
                await _localStorage.SetItemAsync(Key, Synopsis);
                CurrentEditContext.MarkAsUnmodified();
                CannotSave = true;
            }
        }

    }
}
