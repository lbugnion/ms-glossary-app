﻿using Newtonsoft.Json;

namespace MsGlossaryApp.Model.GitHub
{
    public class GetHeadResult
    {
        public string ErrorMessage
        {
            get;
            set;
        }

        [JsonProperty("object")]
        public GetHeadsResultObject Object
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

        public class GetHeadsResultObject : ShaInfo
        {
            public string Url
            {
                get;
                set;
            }
        }
    }
}