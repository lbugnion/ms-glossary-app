using Blazored.LocalStorage;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace SynopsisClient.Model
{
    public class UserManager
    {
        private const string CurrentUserKey = "CurrentUser";
        private const string DefaultEmail = "user@domain.com";
        private const string DefaultSynopsisName = "this-is-an-example";
        private readonly ILocalStorageService _localStorage;

        public bool CannotLogIn
        {
            get;
            set;
        }

        public bool CannotLogOut
        {
            get;
            set;
        }

        private ILogger Log 
        { 
            get; 
            set; 
        }

        public User CurrentUser
        {
            get;
            set;
        }

        public bool IsLoggedIn
        {
            get;
            private set;
        }

        public bool IsModified
        {
            get;
            set;
        }

        public UserManager(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        public void DefineLog(ILogger log)
        {
            Log = log;
        }

        public async Task<bool> CheckLogin()
        {
            Log.LogInformation("-> UserManager.CheckLogin");
            Log.LogDebug($"CurrentUser is null: {CurrentUser == null}");

            if (CurrentUser == null)
            {
                Initialize();
            }

            CannotLogIn = true;

            var savedUser = await _localStorage.GetItemAsync<User>(CurrentUserKey);

            if (savedUser != null)
            {
                Log.LogDebug($"Found user in storage: {savedUser.Email} / {savedUser.SynopsisName}");
                CurrentUser.Email = savedUser.Email;
                CurrentUser.SynopsisName = savedUser.SynopsisName;
                IsModified = false;
                CannotLogOut = false;
                IsLoggedIn = true;
                return true;
            }
            else
            {
                Log.LogTrace($"No user found in storage");
                IsModified = true;
                CannotLogOut = true;
                IsLoggedIn = false;
                return false;
            }

            Log.LogInformation("UserManager.CheckLogin ->");
        }

        public void Initialize()
        {
            Log.LogInformation("-> UserManager.Initialize");

            CurrentUser = new User
            {
                Email = DefaultEmail,
                SynopsisName = DefaultSynopsisName
            };

            IsLoggedIn = false;
        }

        public async Task LogIn()
        {
            Log.LogInformation("-> UserManager.Login");
            await _localStorage.SetItemAsync(CurrentUserKey, CurrentUser);
            IsModified = false;
            CannotLogOut = false;
            IsLoggedIn = true;
        }

        public async Task LogOut()
        {
            Log.LogInformation("-> UserManager.Logout");
            await _localStorage.RemoveItemAsync(CurrentUserKey);

            CurrentUser = new User
            {
                Email = "user@domain.com",
                SynopsisName = "this-is-an-example"
            };

            IsModified = true;
            CannotLogOut = true;
            IsLoggedIn = false;
        }
    }
}