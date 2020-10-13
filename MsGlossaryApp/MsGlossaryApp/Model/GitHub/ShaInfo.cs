using Newtonsoft.Json;

namespace MsGlossaryApp.Model.GitHub
{
    public class ShaInfo
    {
        public string ErrorMessage
        {
            get;
            set;
        }

        [JsonProperty("sha")]
        public string Sha
        {
            get;
            set;
        }
    }
}