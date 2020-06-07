using Newtonsoft.Json;

namespace WordsOfTheDayApp.Model.NewTopic
{
    public class UpdateReferenceInfo : ShaInfo
    {
        [JsonProperty("force")]
        public bool Force => true;

        public UpdateReferenceInfo(string sha)
        {
            Sha = sha;
        }
    }
}