using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
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

        private const string ReloadLocalTitle = "Are you sure? Reload local";
        private const string ReloadFromCloudTitle = "Are you sure? Reload frm Cloud";

        public event EventHandler WasSaved;

        public const string LocalStorageKey = "Current-Synopsis";

        private ListHandlerBase _listHandler;

        private ILocalStorageService _localStorage;
        private readonly NavigationManager _nav;
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

        public bool IsReloading
        {
            get;
            private set;
        }

        public void ResetDialogs()
        {
            ShowConfirmDeleteItemDialog = false;
            ShowConfirmReloadDialog = false;
            ErrorMessage = null;
            IsReloading = false;
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

        public bool ShowConfirmReloadDialog
        {
            get;
            private set;
        }

        public string ConfirmReloadDialogTitle 
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
            NavigationManager nav,
            IConfiguration configuration,
            UserManager userManager)
        {
            _localStorage = localStorage;
            _nav = nav;
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
                Console.WriteLine("Removing Synopsis from local storage");
                await _localStorage.RemoveItemAsync(LocalStorageKey);

                Console.WriteLine("Attempting to load synopsis from network");

                if (_userManager.CurrentUser == null
                    || string.IsNullOrEmpty(_userManager.CurrentUser.Email)
                    || string.IsNullOrEmpty(_userManager.CurrentUser.SynopsisName))
                {
                    Console.WriteLine("User is null or incomplete");
                    CannotReloadFromCloud = true;
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

                HttpResponseMessage response = null;

                try
                {
                    Console.WriteLine("Sending request");
                    response = await client.SendAsync(httpRequest);

                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Invalid response: {response.StatusCode}");

                        ErrorMessage = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"ErrorMessage: {ErrorMessage}");

                        return null;
                    }
                }
                catch (Exception)
                {
                    ErrorMessage = "Failed getting Synopsis. Is the service down?";
                    return null;
                }

                try
                {
                    Synopsis = await response?.Content.ReadFromJsonAsync<Synopsis>();
                    await _localStorage.SetItemAsync(LocalStorageKey, Synopsis);
                    Console.WriteLine("New Synopsis loaded and saved");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR deserializing synopsis");
                    Console.WriteLine(ex.GetType());
                    Console.WriteLine(ex.Message);
                    ErrorMessage = ex.Message;
                    await _localStorage.RemoveItemAsync(LocalStorageKey);
                    return null;
                }
            }

            var originalLines = Synopsis.TranscriptLines;
            Synopsis.TranscriptLines = new List<TranscriptLine>();

            Console.WriteLine("Reloading lines");

            foreach (var line in originalLines)
            {
                Console.WriteLine($"Found {line.Markdown}");
                var typedLine = TranscriptLine.GetEntry(line.Markdown);
                Console.WriteLine(typedLine.GetType().Name);
                Synopsis.TranscriptLines.Add(typedLine);
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

            if (Synopsis != null)
            {
                CurrentEditContext = new EditContext(Synopsis);
                CurrentEditContext.OnFieldChanged += CurrentEditContextOnFieldChanged;
                CurrentEditContext.OnValidationStateChanged += CurrentEditContextOnValidationStateChanged;
            }

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

        public string ErrorMessage
        {
            get;
            private set;
        }

        public async Task<bool> InitializePage()
        {
            Console.WriteLine("SynopsisHandler.InitializePage");

            try
            {
                IsReloading = true;
                Synopsis = await GetSynopsis(true, false);
                SetContext();

                if (Synopsis == null)
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }



            IsReloading = false;
            return true;
        }

        private bool _reloadFromCloud;
        private bool _reloadLocal;

        public void ReloadFromCloud()
        {
            Console.WriteLine("In ReloadFromCloud");
            _reloadFromCloud = true;
            ShowConfirmReloadDialog = true;
            ConfirmReloadDialogTitle = ReloadFromCloudTitle;
        }

        public async Task ReloadConfirmationOkCancelClicked(bool confirm)
        {
            ShowConfirmReloadDialog = false;

            if (confirm)
            {
                if (_reloadFromCloud)
                {
                    _reloadFromCloud = false;
                    await ExecuteReloadFromCloud();
                }

                if (_reloadLocal)
                {
                    _reloadLocal = false;
                    ExecuteReloadLocal();
                }
            }
        }

        private void ExecuteReloadLocal()
        {
            _nav.NavigateTo(_nav.Uri, forceLoad: true);
        }

        private async Task ExecuteReloadFromCloud()
        {
            Console.WriteLine("SynopsisHandler.ExecuteReloadFromCloud");
            IsReloading = true;
            var success = true;

            try
            {
                Synopsis = await GetSynopsis(false, true);
                SetContext();

                if (Synopsis == null)
                {
                    success = false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                success = false;
            }

            if (success)
            {
                IsReloading = false;
            }
            else
            {
                _nav.NavigateTo("/");
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

        public void ReloadLocal()
        {
            Console.WriteLine("In ReloadLocal");
            _reloadLocal = true;
            ShowConfirmReloadDialog = true;
            ConfirmReloadDialogTitle = ReloadLocalTitle;
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