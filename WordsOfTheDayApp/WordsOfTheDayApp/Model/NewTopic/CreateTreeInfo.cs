using Newtonsoft.Json;
using System.Collections.Generic;

namespace WordsOfTheDayApp.Model.NewTopic
{
    public class CreateTreeInfo
    {
        private const string BlobMode = "100644";
        private const string BlobType = "blob";
        private const string PathMask = "synopsis/{0}.md";

        [JsonProperty("base_tree")]
        public string BaseTree
        {
            get;
            set;
        }

        [JsonProperty("tree")]
        public IList<TreeInfo> Tree
        {
            get;
            private set;
        }

        public CreateTreeInfo(string safeFileName, string blobUploadSha)
        {
            Tree = new List<TreeInfo>
            {
                new TreeInfo(safeFileName, blobUploadSha)
            };
        }

        public class TreeInfo : ShaInfo
        {
            [JsonProperty("mode")]
            public string Mode => BlobMode;

            [JsonProperty("path")]
            public string Path
            {
                get;
            }

            [JsonProperty("type")]
            public string Type => BlobType;

            public TreeInfo(string safeFileName, string blobUploadSha)
            {
                Path = string.Format(PathMask, safeFileName);
                Sha = blobUploadSha;
            }
        }
    }
}