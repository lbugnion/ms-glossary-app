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
                Log.LogTrace("Confirm: confirmed");
                return true;
            }

            return false;
        }

        private void CurrentEditContextOnFieldChanged(
            object sender,
            FieldChangedEventArgs e)
        {
            Log.LogInformation("-> SynopsisHandler.CurrentEditContextOnFieldChanged");
            CannotSave = false;
        }

        private void CurrentEditContextOnValidationStateChanged(
            object sender,
            ValidationStateChangedEventArgs e)
        {
            Log.LogInformation("-> SynopsisHandler.CurrentEditContextOnValidationStateChanged");

            Log.LogDebug($"CurrentEditContext.IsModified(): {CurrentEditContext.IsModified()}");
            Log.LogDebug($"_isModified: {IsModified}");
            Log.LogDebug($"CurrentEditContext.GetValidationMessages().Count(): {CurrentEditContext.GetValidationMessages().Count()}");

            if ((CurrentEditContext.IsModified() || IsModified)
                && !CurrentEditContext.GetValidationMessages().Any())
            {
                Log.LogTrace("can save");
                CannotSave = false;
            }
            else
            {
                Log.LogTrace("cannot save");
                CannotSave = true;
            }
        }

        private async Task ExecuteReloadFromCloud()
        {
            Log.LogInformation("-> SynopsisHandler.ExecuteReloadFromCloud");
            await ShowHideBusyDialog(true, "Reloading...");
            IsModified = false;
            var success = true;

            try
            {
                Log.LogTrace("Calling GetSynopsis and setting context");
                Synopsis = await GetSynopsis(false, true);
                SetContext();

                if (Synopsis == null)
                {
                    Log.LogWarning("Synopsis is null");
                    success = false;
                }
            }
            catch (Exception ex)
            {
                Log.LogError(ex, "Error getting synopsis");
                CannotLoadErrorMessage = ex.Message;
                success = false;
            }

            if (success)
            {
                Log.LogTrace("Hiding dialog");
                await ShowHideBusyDialog(false);
            }
            else
            {
                Log.LogTrace("Navigating home");
                _nav.NavigateTo("/");
            }
        }

        private async Task<Synopsis> GetSynopsis(
            bool forceRefreshLocal,
            bool forceRefreshOnline)
        {
            Log.LogInformation($"-> SynopsisHandler.GetSynopsis");
            Log.LogDebug($"local {forceRefreshLocal}, online {forceRefreshOnline}");
            Log.LogDebug($"Synopsis is null: {Synopsis == null}");

            Log.LogDebug((!forceRefreshOnline
                && (Synopsis == null
                || forceRefreshLocal)).ToString());

            if (!forceRefreshOnline
                && (Synopsis == null
                || forceRefreshLocal))
            {
                Log.LogTrace("Loading synopsis from local storage");

                try
                {
                    // do NOT use the automatic deserialization in _localStorage to avoid
                    // issues with Dictionary keys being forced to lower caps.
                    Log.LogDebug($"Loading from storage with key {LocalStorageKey}");


                    var json = await _localStorage.GetItemAsStringAsync(LocalStorageKey);

                    //var json = await _localStorage.GetItemAsync<string>(LocalStorageKey);

                    Log.LogDebug($"JSON loaded from storage is null: {string.IsNullOrEmpty(json)}");
                    Log.LogDebug(json);

                    if (!string.IsNullOrEmpty(json))
                    {
                        Log.LogTrace("Deserializing JSON");
                        Synopsis = JsonConvert.DeserializeObject<Synopsis>(json);
                        Log.LogTrace("Done deserializing JSON");
                    }
                }
                catch (Exception ex)
                {
                    Log.LogError(ex, $"Error getting or deserializing synopsis: {ex.Message}");
                    Synopsis = null;
                }

                if (Synopsis == null)
                {
                    Log.LogTrace("Synopsis is null");
                }
                else
                {
                    Log.LogTrace("Synopsis is not null");
                }
            }
            else
            {
                Log.LogTrace("Not loading from local");
            }

            if (forceRefreshOnline
                || Synopsis == null)
            {
                Log.LogTrace("Removing Synopsis from local storage");
                await _localStorage.RemoveItemAsync(LocalStorageKey);

                Log.LogTrace("Attempting to load synopsis from network");

                if (_userManager.CurrentUser == null
                    || string.IsNullOrEmpty(_userManager.CurrentUser.Email)
                    || string.IsNullOrEmpty(_userManager.CurrentUser.SynopsisName))
                {
                    Log.LogWarning("User is null or incomplete");
                    CannotReloadFromCloud = true;
                    return null;
                }

                var url = _configuration.GetValue<string>(GetSynopsisUrlKey);
                Log.LogDebug($"URL: {url}");
                var functionKey = _configuration.GetValue<string>(GetSynopsisUrlFunctionKeyKey);
                Log.LogDebug($"Function Key: {functionKey}");

                Log.LogTrace("Creating request");
                var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
                httpRequest.Headers.Add(UserEmailHeaderKey, _userManager.CurrentUser.Email);
                httpRequest.Headers.Add(FileNameHeaderKey, _userManager.CurrentUser.SynopsisName);
                httpRequest.Headers.Add(FunctionCodeHeaderKey, functionKey);

                HttpResponseMessage response;

                try
                {
                    Log.LogTrace("Sending request");
                    response = await _http.SendAsync(httpRequest);

                    if (!response.IsSuccessStatusCode)
                    {
                        Log.LogDebug($"Invalid response: {response.StatusCode}");

                        if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                        {
                            CannotLoadErrorMessage = response.ReasonPhrase;
                            Log.LogDebug($"CannotLoadErrorMessage: {CannotLoadErrorMessage}");
                            return null;
                        }

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
                    Log.LogTrace("New Synopsis loaded from cloud");
                    Log.LogDebug(json);

                    // do NOT use the automatic serialization in _localStorage to avoid
                    // issues with Dictionary keys being forced to lower caps.
                    await _localStorage.SetItemAsync(LocalStorageKey, json);
                    Log.LogDebug($"New Synopsis saved in storage under {LocalStorageKey}");
                }
                catch (Exception ex)
                {
                    Log.LogError("ERROR deserializing synopsis");
                    Log.LogDebug(ex.GetType().FullName);
                    Log.LogDebug(ex.Message);
                    CannotLoadErrorMessage = ex.Message;
                    await _localStorage.RemoveItemAsync(LocalStorageKey);
                    return null;
                }
            }
            else
            {
                Log.LogTrace("Not loading from cloud");
            }

            Log.LogTrace("Casting transcript lines");
            Synopsis.CastTranscriptLines();

            Log.LogInformation($"SynopsisHandler.GetSynopsis ->");
            return Synopsis;
        }

        private void SetContext()
        {
            Log.LogInformation($"-> SynopsisHandler.SetContext");

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
            Log.LogInformation($"SynopsisHandler.SetContext ->");
        }

        private async Task ShowHideBusyDialog(bool show, string title = null)
        {
            Log.LogInformation($"-> SynopsisHandler.ShowHideReloadDialog: {show}");

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

            Log.LogInformation("SynopsisHandler.ShowHideReloadDialog ->");
        }

        public void AddItem()
        {
            Log.LogInformation("-> SynopsisHandler.AddItem");
            _listHandler?.AddItem();
            Log.LogInformation("SynopsisHandler.AddItem ->");
        }

        public void AddTranscriptLineAfter<T>(TranscriptLine previousLine)
            where T : TranscriptLine, new()
        {
            Log.LogInformation("-> SynopsisHandler.AddTranscriptLineAfter");

            if (_listHandler is not ListHandler<TranscriptLine> castedListHandler)
            {
                Log.LogWarning("List handler cannot be casted");
                return;
            }

            if (previousLine == null)
            {
                Log.LogTrace("previousLine is null");
                castedListHandler.InsertItemAt(0, new T());
            }
            else
            {
                Log.LogDebug($"previousLine is {previousLine}");
                var previousIndex = castedListHandler.GetIndexOf(previousLine);
                castedListHandler.InsertItemAt(previousIndex + 1, new T());
            }

            Log.LogInformation("SynopsisHandler.AddTranscriptLineAfter ->");
        }

        public async Task CheckSaveSynopsis()
        {
            Log.LogInformation("-> SynopsisHandler.CheckSaveSynopsis");
            Log.LogDebug($"CurrentEditContext.IsModified: {CurrentEditContext.IsModified()}");
            Log.LogDebug($"_isModified: {IsModified}");

            if ((IsModified || CurrentEditContext.IsModified())
                && !CannotSave)
            {
                Log.LogTrace("SynopsisHandler.Must save");

                // do NOT use the automatic serialization in _localStorage to avoid
                // issues with Dictionary keys being forced to lower caps.
                var json = JsonConvert.SerializeObject(Synopsis);
                await _localStorage.SetItemAsync(LocalStorageKey, json);
                CurrentEditContext.MarkAsUnmodified();
                CannotSave = true;
                IsModified = false;
                Log.LogTrace("Saved and invoked event");
            }

            Log.LogInformation("SynopsisHandler.CheckSaveSynopsis ->");
        }

        public async Task CheckSaveSynopsisToCloud()
        {
            Log.LogInformation("-> SynopsisHandler.CheckSaveSynopsisToCloud");
            Log.LogDebug($"CurrentEditContext.IsModified: {CurrentEditContext.IsModified()}");
            Log.LogDebug($"_isModified: {IsModified}");

            if (IsModified || CurrentEditContext.IsModified())
            {
                Log.LogTrace("Synopsis must be saved first");

                var parameters = new ModalParameters();
                parameters.Add(
                    nameof(MessageDialog.Message),
                    "Please save the Synopsis before you commit to the cloud");

                _modal.Show<MessageDialog>("Cannot commit yet", parameters);
                return;
            }

            await ShowHideBusyDialog(true, "Saving...");

            await CheckSaveSynopsis();

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

            Log.LogTrace("Creating request with headers");

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
            httpRequest.Headers.Add(UserEmailHeaderKey, _userManager.CurrentUser.Email);
            httpRequest.Headers.Add(FileNameHeaderKey, _userManager.CurrentUser.SynopsisName);
            httpRequest.Headers.Add(FunctionCodeHeaderKey, functionKey);

            Log.LogTrace("Serializing Synopsis");

            var json = JsonConvert.SerializeObject(Synopsis);

            Log.LogDebug(json);

            httpRequest.Content = new StringContent(json);

            HttpResponseMessage response;

            try
            {
                Log.LogTrace("Sending request");
                response = await _http.SendAsync(httpRequest);

                if (!response.IsSuccessStatusCode)
                {
                    Log.LogWarning($"Invalid response: {response.StatusCode}");
                    CannotSaveErrorMessage = await response.Content.ReadAsStringAsync();
                    Log.LogWarning($"ErrorMessage: {CannotSaveErrorMessage}");
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
            Log.LogTrace("Synopsis was saved successfully");
            _nav.NavigateTo("/");
            await ShowHideBusyDialog(false);
            Log.LogInformation("SynopsisHandler.CheckSaveSynopsisToCloud ->");
        }

        public void DefineList<T>(IList<T> items)
            where T : class, new()
        {
            Log.LogInformation("-> SynopsisHandler.DefineList");
            _listHandler = new ListHandler<T>(this, items, Log);
        }

        public void DefineModal(IModalService modal)
        {
            Log.LogInformation("-> SynopsisHandler.DefineModal");
            _modal = modal;
        }

        public async Task Delete<T>(T item)
            where T : class
        {
            Log.LogInformation("-> SynopsisHandler.Delete");

            if (await Confirm<ConfirmDeleteDialog>(DeleteDialogTitle))
            {
                Log.LogTrace("Asking List handler to delete");
                _listHandler?.Delete(item);
            }
        }

        public async Task DeleteLocalSynopsis()
        {
            Log.LogInformation("-> SynopsisHandler.DeleteLocalSynopsis");
            Synopsis = null;
            await _localStorage.RemoveItemAsync(SynopsisHandler.LocalStorageKey);
        }

        public void ExecuteReloadLocal()
        {
            Log.LogInformation("-> SynopsisHandler.ExecuteReloadLocal");
            // TODO Reload synopsis local without reloading the page
            _nav.NavigateTo(_nav.Uri, forceLoad: true);
        }

        public void DefineLog(ILogger log)
        {
            log.LogInformation("-> SynopsisHandler.DefineLog");
            Log = log;
        }

        public async Task<bool> InitializePage()
        {
            Log.LogInformation("-> SynopsisHandler.InitializePage");

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
            Log.LogInformation("-> SynopsisHandler.ReloadFromCloud");

            if (await Confirm<ConfirmReloadDialog>(ReloadFromCloudDialogTitle))
            {
                Log.LogTrace("Executing reload from cloud");
                await ExecuteReloadFromCloud();
            }
        }

        public async Task ReloadLocal()
        {
            Log.LogInformation("-> SynopsisHandler.ReloadLocal");

            if (await Confirm<ConfirmReloadDialog>(ReloadLocalDialogTitle))
            {
                Log.LogTrace("Executing reload local");
                ExecuteReloadLocal();
            }
        }

        public async Task ResetDialogs()
        {
            Log.LogInformation("-> SynopsisHandler.ResetDialogs");

            CannotLoadErrorMessage = null;
            CannotSaveErrorMessage = null;
            ShowSavedToCloudSuccessMessage = false;
            await ShowHideBusyDialog(false);
        }

        public void TriggerValidation()
        {
            Log.LogInformation("-> SynopsisHandler.TriggerValidation");

            if (CurrentEditContext != null)
            {
                var isValid = CurrentEditContext.Validate();
                Log.LogDebug($"TriggerValidation: {isValid}");
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
                Log.LogInformation("-> ListHandler.AddItem");

                if (Items == null)
                {
                    Log.LogWarning("Items is null");
                    return;
                }

                var newItem = new T();
                Items.Add(newItem);
                _parent.TriggerValidation();
                _parent.IsModified = true;
                Log.LogInformation("ListHandler.AddItem ->");
            }

            public override void Delete<T2>(T2 item)
            {
                Log.LogInformation("-> ListHandler.Delete");

                var casted = item as T;

                if (casted == null)
                {
                    Log.LogTrace($"Casted is null");
                }
                else
                {
                    Log.LogTrace($"Casted is not null");
                }

                if (Items == null)
                {
                    Log.LogTrace($"Items is null");
                }
                else
                {
                    Log.LogTrace($"Items is not null");

                    if (Items.Contains(casted))
                    {
                        Log.LogTrace("Items contains casted");
                    }
                    else
                    {
                        Log.LogTrace("Items does NOT contain casted");
                    }
                }

                if (Items != null
                    && Items.Contains(casted))
                {
                    Log.LogTrace("Casted found in Items");
                    Items.Remove(casted);
                    _parent.IsModified = true;
                    Log.LogTrace("Item removed");
                }

                _parent.TriggerValidation();
                Log.LogInformation("ListHandler.Delete ->");
            }

            public int GetIndexOf(T item)
            {
                Log.LogInformation("-> ListHandler.GetIndexOf");
                return Items.IndexOf(item);
            }

            public void InsertItemAt<T2>(int index, T2 newItem)
                                        where T2 : T
            {
                Log.LogInformation("-> ListHandler.InsertItemAt");

                if (Items == null)
                {
                    Log.LogWarning("Items is null");
                    return;
                }

                Items.Insert(index, newItem);
                _parent.TriggerValidation();
                _parent.IsModified = true;
                Log.LogInformation("ListHandler.InsertItemAt ->");
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