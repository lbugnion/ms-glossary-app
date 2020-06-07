namespace WordsOfTheDayApp.Model.NewTopic
{
    public class GetHeadResult
    {
        public GetHeadsResultObject Object
        {
            get;
            set;
        }

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