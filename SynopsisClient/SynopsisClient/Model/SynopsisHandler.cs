using MsGlossaryApp.DataModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace SynopsisClient.Model
{
    public class SynopsisHandler
    {
        private static Term Synopsis
        {
            get;
            set;
        }

        public async Task<Term> GetSynopsis()
        {
            if (Synopsis == null)
            {
                var client = new HttpClient();
                Synopsis = await client.GetFromJsonAsync<Term>("https://localhost:44395/sample-data/test-topic-15.json");
            }

            return Synopsis;
        }
    }
}
