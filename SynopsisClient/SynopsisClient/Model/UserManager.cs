using Blazored.LocalStorage;
using System;
using System.Threading.Tasks;

namespace SynopsisClient.Model
{
    public class UserManager
    {
        private ILocalStorageService _localStorage;
        private bool isLoggedIn;
        private const string CurrentUserKey = "CurrentUser";

        private const string DefaultEmail = "user@domain.com";
        private const string DefaultSynopsisName = "this-is-an-example";

        public User CurrentUser
        {
            get;
            set;
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

        public bool IsModified
        {
            get;
            set;
        }

        public async Task CheckLogin()
        {
            Console.WriteLine("CheckLogin");

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
            }
            else
            {
                Console.WriteLine($"No user found in storage");
                IsModified = true;
                CannotLogOut = true;
                IsLoggedIn = false;
            }
        }

        public async Task LogIn()
        {
            Console.WriteLine("UserManager.Login");
            await _localStorage.SetItemAsync(CurrentUserKey, CurrentUser);

            // TODO Load Synopsis here?



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

        public bool IsLoggedIn
        {
            get => isLoggedIn;
            private set
            {
                isLoggedIn = value;
                LoggedInChanged?.Invoke(this, value);
            }
        }

        public event EventHandler<bool> LoggedInChanged;

        public UserManager(
            ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }
    }
}
