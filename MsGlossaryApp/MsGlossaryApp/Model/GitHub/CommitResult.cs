using Newtonsoft.Json;

namespace MsGlossaryApp.Model.GitHub
{
    public class CommitResult : ShaInfo
    {
        [JsonProperty("tree")]
        public ShaInfo Tree
        {
            get;
            set;
        }
    }
}