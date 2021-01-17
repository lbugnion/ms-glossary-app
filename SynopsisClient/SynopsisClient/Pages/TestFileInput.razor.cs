using BlazorInputFile;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SynopsisClient.Pages
{
    public partial class TestFileInput
    {
        private const int MaxFileSize = maxKiloBytes * 1024;
        private const int maxKiloBytes = 500;
        // 500KB

        private string _status = $"Load a file max {maxKiloBytes} kBytes";

        private async Task HandleSelection(IFileListEntry[] files)
        {
            var file = files.FirstOrDefault();

            if (file == null)
            {
                return;
            }
            else if (file.Size > MaxFileSize)
            {
                _status = $"That's too big. Max size: 500 kBytes.";
                Console.WriteLine(_status);
            }
            else
            {
                // Just load into .NET memory to show it can be done
                // Alternatively it could be saved to disk, or parsed in memory, or similar
                var ms = new MemoryStream();
                await file.Data.CopyToAsync(ms);

                _status = $"Starting upload";
                Console.WriteLine(_status);

                Console.WriteLine(UserManager == null ? "UserManager is null" : "UserManager is not null");

                if (UserManager != null)
                {
                    Console.WriteLine(UserManager.CurrentUser == null ? "CurrentUser is null" : "CurrentUser is not null");

                    if (UserManager.CurrentUser != null)
                    {
                        Console.WriteLine(UserManager.CurrentUser == null ? "CurrentUser is null" : "CurrentUser is not null");
                        Console.WriteLine(UserManager.CurrentUser.Email);
                    }
                }

                var client = new HttpClient();
                var content = new StreamContent(ms);
                var response = await client.PostAsync(
                    $"http://localhost:7071/api/UploadFile?e={UserManager.CurrentUser.Email}&f={file.Name}",
                    content);

                _status = await response.Content.ReadAsStringAsync();
            }
        }

        protected override async Task OnInitializedAsync()
        {
            await UserManager.CheckLogin();

            if (!UserManager.IsLoggedIn)
            {
                Console.WriteLine("not logged in");
                Nav.NavigateTo("/");
                return;
            }
        }
    }
}