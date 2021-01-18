using Blazored.LocalStorage;
using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Configuration;
using MsGlossaryApp.DataModel;
using Newtonsoft.Json;
using SynopsisClient.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SynopsisClient.Model
{
    public class SynopsisHandler
    {
        private const string DeleteDialogTitle = "Are you sure? Deleting...";
        private const string FileNameHeaderKey = "x-glossary-file-name";
        private const string GetSynopsisUrlFunctionKeyKey = "GetSynopsisUrlFunctionKey";
        private const string GetSynopsisUrlKey = "GetSynopsisUrl";
        private const string ReloadFromCloudDialogTitle = "Are you sure? Reload from Cloud...";
        private const string ReloadLocalDialogTitle = "Are you sure? Reload local..";
        private const string SaveSynopsisUrlFunctionKeyKey = "SaveSynopsisUrlFunctionKey";
        private const string SaveSynopsisUrlKey = "SaveSynopsisUrl";
        private const string UserEmailHeaderKey = "x-glossary-user-email";
        private readonly IConfiguration _configuration;
        private readonly ILocalStorageService _localStorage;
        private readonly NavigationManager _nav;
        private readonly UserManager _userManager;
        private ListHandlerBase _listHandler;
        private IModalService _modal;

        public const string LocalStorageKey = "Current-Synopsis";

        public string CannotLoadErrorMessage
        {
            get;
            private set;
        }

        public bool CannotReloadFromCloud
        {
            get;
            private set;
        }

        public bool CannotSave
        {
            get;
            private set;
        }

        public string CannotSaveErrorMessage
        {
            get;
            private set;
        }

        public EditContext CurrentEditContext
        {
            get;
            set;
        }

        public bool IsModified
        {
            get;
            private set;
        }

        public string SaveResponseMessage
        {
            get;
            private set;
        }

        public bool ShowSavedToCloudSuccessMessage
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

        private async Task<bool> Confirm<TComponent>(string title)
            where TComponent : IComponent
        {
            if (_modal == null)
            {
                Console.WriteLine("Modal is not set");
                return false;
            }

            var formModal = _modal.Show<TComponent>(title);
            var result = await formModal.Result;

            Console.WriteLine($"Confirm: cancelled: {result.Cancelled}");

            if (!result.Cancelled
                && result.Data != null
                && (bool)result.Data)
            {
                Console.WriteLine("Confirm: confirmed");
                return true;
            }

            return false;
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
                && !CurrentEditContext.GetValidationMessages().Any())
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

        private async Task ExecuteReloadFromCloud()
        {
            Console.WriteLine("SynopsisHandler.ExecuteReloadFromCloud");
            await ShowHideBusyDialog(true, "Reloading...");
            IsModified = false;
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
                CannotLoadErrorMessage = ex.Message;
                success = false;
            }

            if (success)
            {
                await ShowHideBusyDialog(false);
            }
            else
            {
                _nav.NavigateTo("/");
            }
        }

        private async Task<Synopsis> GetSynopsis(
            bool forceRefreshLocal,
            bool forceRefreshOnline)
        {
            Console.WriteLine($"GetSynopsis local {forceRefreshLocal}, online {forceRefreshOnline}");

            if (!forceRefreshOnline
                && (Synopsis == null
                || forceRefreshLocal))
            {
                Console.WriteLine("Loading synopsis from local storage");

                try
                {
                    // do NOT use the automatic deserialization in _localStorage to avoid
                    // issues with Dictionary keys being forced to lower caps.
                    var json = await _localStorage.GetItemAsync<string>(LocalStorageKey);
                    Synopsis = JsonConvert.DeserializeObject<Synopsis>(json);
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

            if (forceRefreshOnline
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

                HttpResponseMessage response;

                try
                {
                    Console.WriteLine("Sending request");
                    response = await client.SendAsync(httpRequest);

                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Invalid response: {response.StatusCode}");

                        CannotLoadErrorMessage = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"CannotLoadErrorMessage: {CannotLoadErrorMessage}");

                        return null;
                    }
                }
                catch (Exception)
                {
                    CannotLoadErrorMessage = "Failed getting Synopsis. Is the service down?";
                    return null;
                }

                try
                {
                    var json = await response?.Content.ReadAsStringAsync();
                    Synopsis = JsonConvert.DeserializeObject<Synopsis>(json);

                    // do NOT use the automatic serialization in _localStorage to avoid
                    // issues with Dictionary keys being forced to lower caps.
                    await _localStorage.SetItemAsync(LocalStorageKey, json);
                    Console.WriteLine("New Synopsis loaded and saved");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR deserializing synopsis");
                    Console.WriteLine(ex.GetType());
                    Console.WriteLine(ex.Message);
                    CannotLoadErrorMessage = ex.Message;
                    await _localStorage.RemoveItemAsync(LocalStorageKey);
                    return null;
                }
            }

            Synopsis.CastTranscriptLines();

            if (Synopsis != null
                && Synopsis.LinksInstructions != null
                && Synopsis.LinksInstructions.Count > 0)
            {
                Console.WriteLine($"11 -----> {Synopsis.LinksInstructions.First().Key}");
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

        private async Task ShowHideBusyDialog(bool show, string title = null)
        {
            Console.WriteLine($"ShowHideReloadDialog: {show}");

            if (_modal != null)
            {
                if (show)
                {
                    var options = new ModalOptions()
                    {
                        HideCloseButton = true,
                        DisableBackgroundCancel = true
                    };

                    _modal.Show<BusyDialog>(title, options);
                }
                else
                {
                    await BusyDialog.DismissAll();
                }
            }
        }

        public void AddItem()
        {
            Console.WriteLine("SynopsisHandler.AddItem");
            _listHandler?.AddItem();
        }

        public void AddTranscriptLineAfter<T>(TranscriptLine previousLine)
            where T : TranscriptLine, new()
        {
            if (_listHandler is not ListHandler<TranscriptLine> castedListHandler)
            {
                return;
            }

            if (previousLine == null)
            {
                castedListHandler.InsertItemAt(0, new T());
            }
            else
            {
                var previousIndex = castedListHandler.GetIndexOf(previousLine);
                castedListHandler.InsertItemAt(previousIndex + 1, new T());
            }
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

                // do NOT use the automatic serialization in _localStorage to avoid
                // issues with Dictionary keys being forced to lower caps.
                var json = JsonConvert.SerializeObject(Synopsis);
                await _localStorage.SetItemAsync(LocalStorageKey, json);
                CurrentEditContext.MarkAsUnmodified();
                CannotSave = true;
                IsModified = false;
                Console.WriteLine("Saved and invoked event");
            }
        }

        public async Task CheckSaveSynopsisToCloud()
        {
            Console.WriteLine("CheckSaveSynopsisToCloud");
            Console.WriteLine($"CurrentEditContext.IsModified: {CurrentEditContext.IsModified()}");
            Console.WriteLine($"_isModified: {IsModified}");

            if (Synopsis != null
                && Synopsis.LinksInstructions != null
                && Synopsis.LinksInstructions.Count > 0)
            {
                Console.WriteLine($"01 -----> {Synopsis.LinksInstructions.First().Key}");
            }

            await ShowHideBusyDialog(true, "Saving...");

            await CheckSaveSynopsis();

            if (Synopsis != null
                && Synopsis.LinksInstructions != null
                && Synopsis.LinksInstructions.Count > 0)
            {
                Console.WriteLine($"02 -----> {Synopsis.LinksInstructions.First().Key}");
            }

            if (Synopsis == null
                || _userManager.CurrentUser == null
                || string.IsNullOrEmpty(_userManager.CurrentUser.Email)
                || string.IsNullOrEmpty(_userManager.CurrentUser.SynopsisName)
                || CurrentEditContext.IsModified()
                || IsModified)
            {
                CannotSaveErrorMessage = "There is a problem, please contact support";
                Console.WriteLine("Still modified, cannot save to cloud");
                _nav.NavigateTo("/");
                await ShowHideBusyDialog(false);
                return;
            }

            var url = _configuration.GetValue<string>(SaveSynopsisUrlKey);
            Console.WriteLine($"URL: {url}");
            var functionKey = _configuration.GetValue<string>(SaveSynopsisUrlFunctionKeyKey);
            Console.WriteLine($"Function Key: {functionKey}");

            Console.WriteLine("Creating client");

            var client = new HttpClient();

            Console.WriteLine("Creating request with headers");

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
            httpRequest.Headers.Add(UserEmailHeaderKey, _userManager.CurrentUser.Email);
            httpRequest.Headers.Add(FileNameHeaderKey, _userManager.CurrentUser.SynopsisName);

            Console.WriteLine("Serializing Synopsis");

            var json = JsonConvert.SerializeObject(Synopsis);

            Console.WriteLine(json);
            if (Synopsis != null
                && Synopsis.LinksInstructions != null
                && Synopsis.LinksInstructions.Count > 0)
            {
                Console.WriteLine($"03 -----> {Synopsis.LinksInstructions.First().Key}");
            }

            httpRequest.Content = new StringContent(json);

            HttpResponseMessage response;

            try
            {
                Console.WriteLine("Sending request");
                response = await client.SendAsync(httpRequest);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Invalid response: {response.StatusCode}");
                    CannotSaveErrorMessage = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"ErrorMessage: {CannotSaveErrorMessage}");
                    _nav.NavigateTo("/");
                    await ShowHideBusyDialog(false);
                    return;
                }
            }
            catch (Exception)
            {
                CannotSaveErrorMessage = "Failed saving Synopsis. Is the service down?";
                _nav.NavigateTo("/");
                await ShowHideBusyDialog(false);
                return;
            }

            ShowSavedToCloudSuccessMessage = true;
            SaveResponseMessage = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Synopsis was saved successfully");
            _nav.NavigateTo("/");
            await ShowHideBusyDialog(false);

            if (Synopsis != null
                && Synopsis.LinksInstructions != null
                && Synopsis.LinksInstructions.Count > 0)
            {
                Console.WriteLine($"04 -----> {Synopsis.LinksInstructions.First().Key}");
            }
        }

        public void DefineList<T>(IList<T> items)
            where T : class, new()
        {
            Console.WriteLine("SynopsisHandler.DefineList");
            _listHandler = new ListHandler<T>(this, items);
        }

        public void DefineModal(IModalService modal)
        {
            _modal = modal;
        }

        public async Task Delete<T>(T item)
            where T : class
        {
            Console.WriteLine("SynopsisHandler.Delete");

            if (await Confirm<ConfirmDeleteDialog>(DeleteDialogTitle))
            {
                Console.WriteLine("SynopsisHandler: Asking List handler to delete");
                _listHandler?.Delete(item);
            }
        }

        public async Task DeleteLocalSynopsis()
        {
            Synopsis = null;
            await _localStorage.RemoveItemAsync(SynopsisHandler.LocalStorageKey);
        }

        public void ExecuteReloadLocal()
        {
            // TODO Reload synopsis local without reloading the page
            _nav.NavigateTo(_nav.Uri, forceLoad: true);
        }

        public async Task<bool> InitializePage()
        {
            Console.WriteLine("SynopsisHandler.InitializePage");

            try
            {
                await ShowHideBusyDialog(true, "Reloading...");
                Synopsis = await GetSynopsis(false, false);
                SetContext();

                if (Synopsis == null)
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                await ShowHideBusyDialog(false);
                CannotLoadErrorMessage = ex.Message;
                return false;
            }

            await ShowHideBusyDialog(false);
            return true;
        }

        public async Task ReloadFromCloud()
        {
            Console.WriteLine("Handler.ReloadFromCloud");

            if (await Confirm<ConfirmReloadDialog>(ReloadFromCloudDialogTitle))
            {
                await ExecuteReloadFromCloud();
            }
        }

        public async Task ReloadLocal()
        {
            Console.WriteLine("Handler.ReloadLocal");

            if (await Confirm<ConfirmReloadDialog>(ReloadLocalDialogTitle))
            {
                ExecuteReloadLocal();
            }
        }

        public async Task ResetDialogs()
        {
            CannotLoadErrorMessage = null;
            CannotSaveErrorMessage = null;
            ShowSavedToCloudSuccessMessage = false;
            await ShowHideBusyDialog(false);
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
                _parent.TriggerValidation();
                _parent.IsModified = true;
            }

            public override void Delete<T2>(T2 item)
            {
                Console.WriteLine("ListHandler.Delete");

                var casted = item as T;

                if (casted == null)
                {
                    Console.WriteLine($"Casted is null");
                }
                else
                {
                    Console.WriteLine($"Casted is not null");
                }

                if (Items == null)
                {
                    Console.WriteLine($"Items is null");
                }
                else
                {
                    Console.WriteLine($"Items is not null");

                    if (Items.Contains(casted))
                    {
                        Console.WriteLine("Items contains casted");
                    }
                    else
                    {
                        Console.WriteLine("Items does NOT contain casted");
                    }
                }

                if (Items != null
                    && Items.Contains(casted))
                {
                    Console.WriteLine("Casted found in Items");
                    Items.Remove(casted);
                    _parent.IsModified = true;
                    Console.WriteLine("Item removed");
                }

                _parent.TriggerValidation();
            }

            public int GetIndexOf(T item)
            {
                return Items.IndexOf(item);
            }

            public void InsertItemAt<T2>(int index, T2 newItem)
                                        where T2 : T
            {
                if (Items == null)
                {
                    return;
                }

                Items.Insert(index, newItem);
                _parent.TriggerValidation();
                _parent.IsModified = true;
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

            public abstract void Delete<T2>(T2 item)
                            where T2 : class;
        }
    }
}