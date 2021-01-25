using Newtonsoft.Json;

namespace MsGlossaryApp.DataModel
{
    public class NewSynopsis
    {
        [JsonProperty("filename")]
        public string FileName
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