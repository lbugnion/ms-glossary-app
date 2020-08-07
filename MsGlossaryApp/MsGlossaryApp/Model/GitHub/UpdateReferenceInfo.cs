using Newtonsoft.Json;

namespace MsGlossaryApp.Model.GitHub
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