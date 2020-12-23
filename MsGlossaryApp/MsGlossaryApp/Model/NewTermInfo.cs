using Newtonsoft.Json;

namespace MsGlossaryApp.Model
{
    public class TermInfo
    {
        [JsonProperty("ref")]
        public string Ref
        {
            get;
            set;
        }

        [JsonProperty("safeterm")]
        public string SafeTerm
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

        [JsonProperty("submittergithub")]
        public string SubmitterGithub
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

        [JsonProperty("term")]
        public string Term
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