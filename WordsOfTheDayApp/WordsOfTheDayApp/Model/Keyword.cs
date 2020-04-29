namespace WordsOfTheDayApp.Model
{
    public class KeywordPair
    {
        public string Topic
        {
            get;
        }

        public string Subtopic
        {
            get;
        }

        public string Keyword
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