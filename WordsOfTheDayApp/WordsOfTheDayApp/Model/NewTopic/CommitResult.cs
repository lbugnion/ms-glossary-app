using Newtonsoft.Json;

namespace WordsOfTheDayApp.Model.NewTopic
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
