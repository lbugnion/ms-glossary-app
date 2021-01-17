using Blazored.LocalStorage;
using System;
using System.Threading.Tasks;

namespace SynopsisClient.Model
{
    public class UserManager
    {
        public event EventHandler<bool> LoggedInChanged;

        private const string CurrentUserKey = "CurrentUser";
        private const string DefaultEmail = "user@domain.com";
        private const string DefaultSynopsisName = "this-is-an-example";
        private ILocalStorageService _localStorage;
        private bool isLoggedIn;

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

        public User CurrentUser
        {
            get;
            set;
        }

        public bool IsLoggedIn
        {
            get => isLoggedIn;
            private set
            {
                isLoggedIn = value;
                LoggedInChanged?.Invoke(this, value);
            }
        }

        public bool IsModified
        {
            get;
            set;
        }

        public UserManager(
            ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        public async Task<bool> CheckLogin()
        {
            Console.WriteLine($"CheckLogin: CurrentUser is null: {CurrentUser == null}");

            if (CurrentUser == null)
            {
                Initialize();
            }

            CannotLogIn = true;

            var savedUser = await _localStorage.GetItemAsync<User>(CurrentUserKey);

            if (savedUser != null)
            {
                Console.WriteLine($"Found user in storage: {savedUser.Email} / {savedUser.SynopsisName}");
                CurrentUser.Email = savedUser.Email;
                CurrentUser.SynopsisName = savedUser.SynopsisName;
                IsModified = false;
                CannotLogOut = false;
                IsLoggedIn = true;
                return true;
            }
            else
            {
                Console.WriteLine($"No user found in storage");
                IsModified = true;
                CannotLogOut = true;
                IsLoggedIn = false;
                return false;
            }
        }

        public void Initialize()
        {
            CurrentUser = new User
            {
                Email = DefaultEmail,
                SynopsisName = DefaultSynopsisName
            };

            IsLoggedIn = false;
        }

        public async Task LogIn()
        {
            Console.WriteLine("UserManager.Login");
            await _localStorage.SetItemAsync(CurrentUserKey, CurrentUser);
            IsModified = false;
            CannotLogOut = false;
            IsLoggedIn = true;
        }

        public async Task LogOut()
        {
            Console.WriteLine("UserManager.Logout");
            await _localStorage.RemoveItemAsync(CurrentUserKey);
            await _localStorage.RemoveItemAsync(SynopsisHandler.LocalStorageKey);

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