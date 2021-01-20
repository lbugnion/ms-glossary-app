using Blazored.LocalStorage;
using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
        private const string FunctionCodeHeaderKey = "x-functions-key";
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
        private readonly HttpClient _http;
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

        private ILogger Log 
        { 
            get;
            set;
        }

        public SynopsisHandler(
            ILocalStorageService localStorage,
            NavigationManager nav,
            HttpClient http,
            IConfiguration configuration,
            UserManager userManager)
        {
            _localStorage = localStorage;
            _nav = nav;
            _http = http;
            _configuration = configuration;
            _userManager = userManager;
        }

        private async Task<bool> Confirm<TComponent>(string title)
            where TComponent : IComponent
        {
            if (_modal == null)
            {
                Log.LogWarning("Modal is not set");
                return false;
            }

            var formModal = _modal.Show<TComponent>(title);
            var result = await formModal.Result;

            Log.LogDebug($"Confirm: cancelled: {result.Cancelled}");

            if (!result.Cancelled
                && result.Data != null
                && (bool)result.Data)
            {
                Log.LogDebug("Confirm: confirmed");
                return true;
            }

            return false;
        }

        private void CurrentEditContextOnFieldChanged(
            object sender,
            FieldChangedEventArgs e)
        {
            Log.LogInformation("CurrentEditContextOnFieldChanged");
            CannotSave = false;
        }

        private void CurrentEditContextOnValidationStateChanged(
            object sender,
            ValidationStateChangedEventArgs e)
        {
            Log.LogInformation("CurrentEditContextOnValidationStateChanged");
            Log.LogDebug($"CurrentEditContext.IsModified(): {CurrentEditContext.IsModified()}");
            Log.LogDebug($"_isModified: {IsModified}");
            Log.LogDebug($"CurrentEditContext.GetValidationMessages().Count(): {CurrentEditContext.GetValidationMessages().Count()}");

            if ((CurrentEditContext.IsModified() || IsModified)
                && !CurrentEditContext.GetValidationMessages().Any())
            {
                Log.LogInformation("can save");
                CannotSave = false;
            }
            else
            {
                Log.LogInformation("cannot save");
                CannotSave = true;
            }
        }

        private async Task ExecuteReloadFromCloud()
        {
            Log.LogInformation("SynopsisHandler.ExecuteReloadFromCloud");
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
            Log.LogInformation($"GetSynopsis local {forceRefreshLocal}, online {forceRefreshOnline}");

            if (!forceRefreshOnline
                && (Synopsis == null
                || forceRefreshLocal))
            {
                Log.LogInformation("Loading synopsis from local storage");

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
                    Log.LogDebug("Synopsis is null");
                }
                else
                {
                    Log.LogDebug("Synopsis is not null");
                }
            }

            if (forceRefreshOnline
                || Synopsis == null)
            {
                Log.LogInformation("Removing Synopsis from local storage");
                await _localStorage.RemoveItemAsync(LocalStorageKey);

                Log.LogInformation("Attempting to load synopsis from network");

                if (_userManager.CurrentUser == null
                    || string.IsNullOrEmpty(_userManager.CurrentUser.Email)
                    || string.IsNullOrEmpty(_userManager.CurrentUser.SynopsisName))
                {
                    Log.LogDebug("User is null or incomplete");
                    CannotReloadFromCloud = true;
                    return null;
                }

                var url = _configuration.GetValue<string>(GetSynopsisUrlKey);
                Log.LogDebug($"URL: {url}");
                var functionKey = _configuration.GetValue<string>(GetSynopsisUrlFunctionKeyKey);
                Log.LogDebug($"Function Key: {functionKey}");

                Log.LogInformation("Creating client");

                Log.LogInformation("Creating request");
                var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
                httpRequest.Headers.Add(UserEmailHeaderKey, _userManager.CurrentUser.Email);
                httpRequest.Headers.Add(FileNameHeaderKey, _userManager.CurrentUser.SynopsisName);
                httpRequest.Headers.Add(FunctionCodeHeaderKey, functionKey);

                HttpResponseMessage response;

                try
                {
                    Log.LogInformation("Sending request");
                    response = await _http.SendAsync(httpRequest);

                    if (!response.IsSuccessStatusCode)
                    {
                        Log.LogDebug($"Invalid response: {response.StatusCode}");

                        CannotLoadErrorMessage = await response.Content.ReadAsStringAsync();
                        Log.LogDebug($"CannotLoadErrorMessage: {CannotLoadErrorMessage}");

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
                    Log.LogInformation("New Synopsis loaded and saved");
                }
                catch (Exception ex)
                {
                    Log.LogWarning("ERROR deserializing synopsis");
                    Log.LogDebug(ex.GetType().FullName);
                    Log.LogDebug(ex.Message);
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
                Log.LogDebug($"11 -----> {Synopsis.LinksInstructions.First().Key}");
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
            Log.LogInformation($"ShowHideReloadDialog: {show}");

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
            Log.LogInformation("SynopsisHandler.AddItem");
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
            Log.LogInformation("CheckSaveSynopsis");
            Log.LogDebug($"CurrentEditContext.IsModified: {CurrentEditContext.IsModified()}");
            Log.LogDebug($"_isModified: {IsModified}");

            if ((IsModified || CurrentEditContext.IsModified())
                && !CannotSave)
            {
                Log.LogInformation("Must save");

                // do NOT use the automatic serialization in _localStorage to avoid
                // issues with Dictionary keys being forced to lower caps.
                var json = JsonConvert.SerializeObject(Synopsis);
                await _localStorage.SetItemAsync(LocalStorageKey, json);
                CurrentEditContext.MarkAsUnmodified();
                CannotSave = true;
                IsModified = false;
                Log.LogInformation("Saved and invoked event");
            }
        }

        public async Task CheckSaveSynopsisToCloud()
        {
            Log.LogInformation("CheckSaveSynopsisToCloud");
            Log.LogDebug($"CurrentEditContext.IsModified: {CurrentEditContext.IsModified()}");
            Log.LogDebug($"_isModified: {IsModified}");

            if (Synopsis != null
                && Synopsis.LinksInstructions != null
                && Synopsis.LinksInstructions.Count > 0)
            {
                Log.LogDebug($"01 -----> {Synopsis.LinksInstructions.First().Key}");
            }

            await ShowHideBusyDialog(true, "Saving...");

            await CheckSaveSynopsis();

            if (Synopsis != null
                && Synopsis.LinksInstructions != null
                && Synopsis.LinksInstructions.Count > 0)
            {
                Log.LogDebug($"02 -----> {Synopsis.LinksInstructions.First().Key}");
            }

            if (Synopsis == null
                || _userManager.CurrentUser == null
                || string.IsNullOrEmpty(_userManager.CurrentUser.Email)
                || string.IsNullOrEmpty(_userManager.CurrentUser.SynopsisName)
                || CurrentEditContext.IsModified()
                || IsModified)
            {
                CannotSaveErrorMessage = "There is a problem, please contact support";
                Log.LogWarning("Still modified, cannot save to cloud");
                _nav.NavigateTo("/");
                await ShowHideBusyDialog(false);
                return;
            }

            var url = _configuration.GetValue<string>(SaveSynopsisUrlKey);
            Log.LogDebug($"URL: {url}");
            var functionKey = _configuration.GetValue<string>(SaveSynopsisUrlFunctionKeyKey);
            Log.LogDebug($"Function Key: {functionKey}");

            Log.LogInformation("Creating client");

            Log.LogInformation("Creating request with headers");

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
            httpRequest.Headers.Add(UserEmailHeaderKey, _userManager.CurrentUser.Email);
            httpRequest.Headers.Add(FileNameHeaderKey, _userManager.CurrentUser.SynopsisName);
            httpRequest.Headers.Add(FunctionCodeHeaderKey, functionKey);

            Log.LogInformation("Serializing Synopsis");

            var json = JsonConvert.SerializeObject(Synopsis);

            Log.LogDebug(json);
            if (Synopsis != null
                && Synopsis.LinksInstructions != null
                && Synopsis.LinksInstructions.Count > 0)
            {
                Log.LogDebug($"03 -----> {Synopsis.LinksInstructions.First().Key}");
            }

            httpRequest.Content = new StringContent(json);

            HttpResponseMessage response;

            try
            {
                Log.LogInformation("Sending request");
                response = await _http.SendAsync(httpRequest);

                if (!response.IsSuccessStatusCode)
                {
                    Log.LogDebug($"Invalid response: {response.StatusCode}");
                    CannotSaveErrorMessage = await response.Content.ReadAsStringAsync();
                    Log.LogDebug($"ErrorMessage: {CannotSaveErrorMessage}");
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
            Log.LogInformation("Synopsis was saved successfully");
            _nav.NavigateTo("/");
            await ShowHideBusyDialog(false);

            if (Synopsis != null
                && Synopsis.LinksInstructions != null
                && Synopsis.LinksInstructions.Count > 0)
            {
                Log.LogDebug($"04 -----> {Synopsis.LinksInstructions.First().Key}");
            }
        }

        public void DefineList<T>(IList<T> items)
            where T : class, new()
        {
            Log.LogInformation("SynopsisHandler.DefineList");
            _listHandler = new ListHandler<T>(this, items, Log);
        }

        public void DefineModal(IModalService modal)
        {
            _modal = modal;
        }

        public async Task Delete<T>(T item)
            where T : class
        {
            Log.LogInformation("SynopsisHandler.Delete");

            if (await Confirm<ConfirmDeleteDialog>(DeleteDialogTitle))
            {
                Log.LogInformation("SynopsisHandler: Asking List handler to delete");
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

        public async Task<bool> InitializePage(ILogger log)
        {
            Log = log;

            Log.LogInformation("SynopsisHandler.InitializePage");

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
            Log.LogInformation("Handler.ReloadFromCloud");

            if (await Confirm<ConfirmReloadDialog>(ReloadFromCloudDialogTitle))
            {
                await ExecuteReloadFromCloud();
            }
        }

        public async Task ReloadLocal()
        {
            Log.LogInformation("Handler.ReloadLocal");

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
            Log.LogInformation("Triggering Validation");

            if (CurrentEditContext != null)
            {
                var isValid = CurrentEditContext.Validate();
                Log.LogDebug("TriggerValidation: " + isValid);
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

            private ILogger Log { get; }

            public ListHandler(SynopsisHandler parent, IList<T> items, ILogger log)
                : base(parent)
            {
                Items = items;
                Log = log;
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
                Log.LogInformation("ListHandler.Delete");

                var casted = item as T;

                if (casted == null)
                {
                    Log.LogDebug($"Casted is null");
                }
                else
                {
                    Log.LogDebug($"Casted is not null");
                }

                if (Items == null)
                {
                    Log.LogDebug($"Items is null");
                }
                else
                {
                    Log.LogDebug($"Items is not null");

                    if (Items.Contains(casted))
                    {
                        Log.LogDebug("Items contains casted");
                    }
                    else
                    {
                        Log.LogDebug("Items does NOT contain casted");
                    }
                }

                if (Items != null
                    && Items.Contains(casted))
                {
                    Log.LogDebug("Casted found in Items");
                    Items.Remove(casted);
                    _parent.IsModified = true;
                    Log.LogInformation("Item removed");
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
                _parent = parent;
            }

            public abstract void AddItem();

            public abstract void Delete<T2>(T2 item)
                            where T2 : class;
        }
    }
}