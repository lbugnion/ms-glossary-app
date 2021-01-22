using Newtonsoft.Json;

namespace MsGlossaryApp.Model.GitHub
{
    public class GetTextFileResult : ErrorResult
    {
        [JsonProperty("name")]
        public string Name
        {
            get;
            set;
        }

        [JsonProperty("path")]
        public string Path
        {
            get;
            set;
        }

        [JsonProperty("html_url")]
        public string HtmlUrl
        {
            get;
            set;
        }

        [JsonProperty("type")]
        public string Type
        {
            get;
            set;
        }

        [JsonProperty("content")]
        public string EncodedContent
        {
            get;
            set;
        }

        [JsonIgnore]
        public string TextContent
        {
            get;
            set;
        }
    }
}