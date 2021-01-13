using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Configuration;
using MsGlossaryApp.DataModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace SynopsisClient.Model
{
    public class SynopsisHandler
    {
        private const string GetSynopsisUrlKey = "GetSynopsisUrl";
        private const string GetSynopsisUrlFunctionKeyKey = "GetSynopsisUrlFunctionKey";
        private const string SaveSynopsisUrlKey = "SaveSynopsisUrl";
        private const string SaveSynopsisUrlFunctionKeyKey = "SaveSynopsisUrlFunctionKey";
        private const string UserEmailHeaderKey = "x-glossary-user-email";
        private const string FileNameHeaderKey = "x-glossary-file-name";

        public event EventHandler WasSaved;

        public const string LocalStorageKey = "Current-Synopsis";

        private ListHandlerBase _listHandler;

        private ILocalStorageService _localStorage;
        private IConfiguration _configuration;
        private UserManager _userManager;

        public bool CannotSave
        {
            get;
            private set;
        }

        public bool CannotReloadFromCloud
        {
            get;
            private set;
        }

        public EditContext CurrentEditContext
        {
            get;
            set;
        }

        public void ResetDialogs()
        {
            ShowConfirmDeleteItemDialog = false;
            ShowConfirmReloadFromCloudDialog = false;
        }

        public bool IsModified
        {
            get;
            private set;
        }

        public bool ShowConfirmDeleteItemDialog
        {
            get;
            private set;
        }

        public bool ShowConfirmReloadFromCloudDialog
        {
            get;
            private set;
        }

        public Synopsis Synopsis
        {
            get;
            private set;
        }

        public SynopsisHandler(
            ILocalStorageService localStorage,
            IConfiguration configuration,
            UserManager userManager)
        {
            _localStorage = localStorage;
            _configuration = configuration;
            _userManager = userManager;
        }

        private void CurrentEditContextOnFieldChanged(
            object sender,
            FieldChangedEventArgs e)
        {
            Console.WriteLine("CurrentEditContextOnFieldChanged");
            CannotSave = false;
        }

        private void CurrentEditContextOnValidationStateChanged(
            object sender,
            ValidationStateChangedEventArgs e)
        {
            Console.WriteLine("CurrentEditContextOnValidationStateChanged");
            Console.WriteLine($"CurrentEditContext.IsModified(): {CurrentEditContext.IsModified()}");
            Console.WriteLine($"_isModified: {IsModified}");
            Console.WriteLine($"CurrentEditContext.GetValidationMessages().Count(): {CurrentEditContext.GetValidationMessages().Count()}");

            if ((CurrentEditContext.IsModified() || IsModified)
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

        public async Task ExecuteReloadFromCloud()
        {
            Console.WriteLine("SynopsisHandler.ExecuteReloadFromCloud");
            Synopsis = await GetSynopsis(false, true);
            SetContext();
        }

        private async Task<Synopsis> GetSynopsis(
            bool forceRefreshLocal,
            bool forcefreshOnline)
        {
            Console.WriteLine("GetSynopsis");

            if (!forcefreshOnline
                && (Synopsis == null
                || forceRefreshLocal))
            {
                Console.WriteLine("Loading synopsis from local storage");

                try
                {
                    Synopsis = await _localStorage.GetItemAsync<Synopsis>(LocalStorageKey);
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
                Console.WriteLine("Attempting to load synopsis from network");

                if (_userManager.CurrentUser == null
                    || string.IsNullOrEmpty(_userManager.CurrentUser.Email)
                    || string.IsNullOrEmpty(_userManager.CurrentUser.SynopsisName))
                {
                    Console.WriteLine("User is null or incomplete");
                    CannotReloadFromCloud = true;
                    await _localStorage.RemoveItemAsync(LocalStorageKey);
                    return null;
                }

                var url = _configuration.GetValue<string>(GetSynopsisUrlKey);
                Console.WriteLine($"URL: {url}");
                var functionKey = _configuration.GetValue<string>(GetSynopsisUrlFunctionKeyKey);
                Console.WriteLine($"Function Key: {functionKey}");

                Console.WriteLine("Creating client");

                var client = new HttpClient();

                Console.WriteLine("Creating request");
                var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
                httpRequest.Headers.Add(UserEmailHeaderKey, _userManager.CurrentUser.Email);
                httpRequest.Headers.Add(FileNameHeaderKey, _userManager.CurrentUser.SynopsisName);

                Console.WriteLine("Sending request");
                var response = await client.SendAsync(httpRequest);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Invalid response: {response.StatusCode}");
                    return null;
                }

                Synopsis = await client.GetFromJsonAsync<Synopsis>("https://localhost:44395/sample-data/test-topic-15.json?ticks=" + DateTime.Now.Ticks);
                await _localStorage.SetItemAsync(LocalStorageKey, Synopsis);
                Console.WriteLine("New Synopsis loaded and saved");
            }

            return Synopsis;
        }

        private void SetContext()
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

        public void AddItem()
        {
            Console.WriteLine("SynopsisHandler.AddItem");
            _listHandler?.AddItem();
        }

        public async Task CheckSaveSynopsis()
        {
            Console.WriteLine("CheckSaveSynopsis");
            Console.WriteLine($"CurrentEditContext.IsModified: {CurrentEditContext.IsModified()}");
            Console.WriteLine($"_isModified: {IsModified}");

            if ((IsModified || CurrentEditContext.IsModified())
                && !CannotSave)
            {
                Console.WriteLine("Must save");
                await _localStorage.SetItemAsync(LocalStorageKey, Synopsis);
                CurrentEditContext.MarkAsUnmodified();
                CannotSave = true;
                IsModified = false;
                WasSaved?.Invoke(this, EventArgs.Empty);
                Console.WriteLine("Saved and invoked event");
            }
        }

        public void DefineList<T>(IList<T> items)
            where T : class, new()
        {
            Console.WriteLine("SynopsisHandler.DefineList");
            _listHandler = new ListHandler<T>(this, items);
        }

        public void Delete<T>(T item)
            where T : class
        {
            Console.WriteLine("SynopsisHandler.Delete");
            _listHandler?.StartDelete(item);
        }

        public void DeletItemConfirmationOkCancelClicked(bool confirm)
        {
            _listHandler?.DeleteItemConfirmationOkCancelClicked(confirm);
        }

        public async Task InitializePage()
        {
            Console.WriteLine("SynopsisHandler.InitializePage");
            Synopsis = await GetSynopsis(true, false);
            SetContext();
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

        public void TriggerValidation()
        {
            Console.WriteLine("Triggering Validation");

            if (CurrentEditContext != null)
            {
                var isValid = CurrentEditContext.Validate();
                Console.WriteLine("TriggerValidation: " + isValid);
            }
        }

        private class ListHandler<T> : ListHandlerBase
            where T : class, new()
        {
            public IList<T> Items
            {
                get;
                set;
            }

            public T SelectedItem
            {
                get;
                set;
            }

            public ListHandler(SynopsisHandler parent, IList<T> items)
                                        : base(parent)
            {
                Console.WriteLine($"ListHandler.ctor: {items.Count} items");
                Items = items;
            }

            public override void AddItem()
            {
                if (Items == null)
                {
                    return;
                }

                var newItem = new T();
                Items.Add(newItem);
                SelectedItem = newItem;
                _parent.TriggerValidation();
                _parent.IsModified = true;
            }

            public override void DeleteItemConfirmationOkCancelClicked(bool confirm)
            {
                Console.WriteLine("ListHandler.DeleteItemConfirmationOkCancelClicked");
                _parent.ShowConfirmDeleteItemDialog = false;

                if (!confirm
                    || SelectedItem == null)
                {
                    Console.WriteLine("deletion cancelled");
                    return;
                }

                Console.WriteLine("Execute deletion");

                if (Items != null
                    && Items.Contains(SelectedItem))
                {
                    Items.Remove(SelectedItem);
                    _parent.IsModified = true;
                    Console.WriteLine("item removed");
                }

                _parent.TriggerValidation();
                SelectedItem = default(T);
            }

            public override void StartDelete<T2>(T2 item)
                                        where T2 : class
            {
                Console.WriteLine("ListHandler.StartDelete");

                var casted = item as T;

                if (casted != null
                    && Items != null
                    && Items.Contains(casted))
                {
                    Console.WriteLine("must show delete dialog");
                    _parent.ShowConfirmDeleteItemDialog = true;
                    SelectedItem = casted;
                }
            }
        }

        private abstract class ListHandlerBase
        {
            protected SynopsisHandler _parent;

            public ListHandlerBase(SynopsisHandler parent)
            {
                Console.WriteLine("ListHandlerBase.ctor");
                _parent = parent;
            }

            public abstract void AddItem();

            public abstract void DeleteItemConfirmationOkCancelClicked(bool confirm);

            public abstract void StartDelete<T2>(T2 item)
                                        where T2 : class;
        }
    }
}