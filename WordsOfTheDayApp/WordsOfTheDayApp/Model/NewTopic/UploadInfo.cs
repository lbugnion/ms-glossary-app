using Newtonsoft.Json;

namespace WordsOfTheDayApp.Model.NewTopic
{
    public class UploadInfo
    {
        public const string Utf8 = "utf-8";

        [JsonProperty("content")]
        public string Content
        {
            get;
            set;
        }

        [JsonProperty("encoding")]
        public string Encoding => Utf8;
    }
}