namespace WordsOfTheDayApp.Model
{
    public class KeywordPair
    {
        public string Topic
        {
            get;
        }

        public string Keyword
        {
            get;
        }

        public KeywordPair(string topic, string keyword)
        {
            Topic = topic;
            Keyword = keyword;
        }
    }
}