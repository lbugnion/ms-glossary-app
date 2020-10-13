using Newtonsoft.Json;

namespace MsGlossaryApp.Model
{
    public class NewTopicInfo
    {
        [JsonProperty("ref")]
        public string Ref
        {
            get;
            set;
        }

        [JsonProperty("safetopic")]
        public string SafeTopic
        {
            get;
            set;
        }

        [JsonProperty("shortdescription")]
        public string ShortDescription
        {
            get;
            set;
        }

        [JsonProperty("submitteremail")]
        public string SubmitterEmail
        {
            get;
            set;
        }

        [JsonProperty("submittername")]
        public string SubmitterName
        {
            get;
            set;
        }

        [JsonProperty("submittertwitter")]
        public string SubmitterTwitter
        {
            get;
            set;
        }

        [JsonProperty("topic")]
        public string Topic
        {
            get;
            set;
        }

        [JsonProperty("url")]
        public string Url
        {
            get;
            set;
        }
    }
}
