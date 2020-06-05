using Newtonsoft.Json;

namespace WordsOfTheDayApp.Model
{
    public class NewTopicInfo
    {
        [JsonProperty("submittername")]
        public string SubmitterName
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

        [JsonProperty("url")]
        public string Url
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
    }
}
