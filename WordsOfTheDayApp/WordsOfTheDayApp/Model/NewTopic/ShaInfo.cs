using Newtonsoft.Json;

namespace WordsOfTheDayApp.Model.NewTopic
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
