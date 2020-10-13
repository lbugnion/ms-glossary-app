using Newtonsoft.Json;

namespace MsGlossaryApp.Model.GitHub
{
    public class NewBranchInfo : ShaInfo
    {
        public const string RefMask = "refs/heads/{0}";

        [JsonProperty("ref")]
        public string Ref
        {
            get;
            set;
        }
    }
}