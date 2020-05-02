namespace WordsOfTheDayApp.Model
{
    public class KeywordPair
    {
        public string Keyword
        {
            get;
        }

        public string Subtopic
        {
            get;
        }

        public string Topic
        {
            get;
        }

        public KeywordPair(string topic, string subtopic, string keyword)
        {
            Topic = topic;
            Subtopic = subtopic;
            Keyword = keyword;
        }
    }
}