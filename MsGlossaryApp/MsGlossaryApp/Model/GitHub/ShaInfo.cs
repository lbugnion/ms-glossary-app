using Newtonsoft.Json;

namespace MsGlossaryApp.Model.GitHub
{
    public class ShaInfo
    {
        [JsonProperty("sha")]
        public string Sha
        {
            get;
            set;
        }
    }
}