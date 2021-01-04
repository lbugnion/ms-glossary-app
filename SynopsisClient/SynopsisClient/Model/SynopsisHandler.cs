using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Forms;
using MsGlossaryApp.DataModel;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace SynopsisClient.Model
{
    public class SynopsisHandler
    {
        private const string Key = "Current-Synopsis";

        private ILocalStorageService _localStorage;

        private static Term Synopsis
        {
            get;
            set;
        }

        public SynopsisHandler(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        public async Task<Term> GetSynopsis(bool forceRefresh)
        {
            if (Synopsis == null
                || forceRefresh)
            {
                if (!forceRefresh)
                {
                    Console.WriteLine("Loading synopsis from local storage");
                    Synopsis = await _localStorage.GetItemAsync<Term>(Key);

                    if (Synopsis == null)
                    {
                        Console.WriteLine("Synopsis is null");
                    }
                    else
                    {
                        Console.WriteLine("Synopsis is not null");
                    }
                }

                if (Synopsis == null)
                {
                    Console.WriteLine("Loading synopsis from network");
                    var client = new HttpClient();
                    Synopsis = await client.GetFromJsonAsync<Term>("https://localhost:44395/sample-data/test-topic-15.json");


                    if (Synopsis == null)
                    {
                        Console.WriteLine("Synopsis is null");
                    }
                    else
                    {
                        Console.WriteLine("Synopsis is not null");
                    }
                }
            }

            return Synopsis;
        }

        public async Task SaveSynopsisLocally(Term synopsis)
        {
            Console.WriteLine("Saving synopsis");
            await _localStorage.SetItemAsync(Key, synopsis);
        }
    }
}
