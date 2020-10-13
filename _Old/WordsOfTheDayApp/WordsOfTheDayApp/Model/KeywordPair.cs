namespace WordsOfTheDayApp.Model
{
    public class KeywordPair
    {
        public string Blurb
        {
            get;
        }

        public bool IsDisambiguation
        {
            get;
            set;
        }

        public string Keyword
        {
            get;
        }

        public string LanguageCode
        {
            get;
        }

        public bool MustDisambiguate
        {
            get;
            set;
        }

        public string Subtopic
        {
            get;
        }

        public string Topic
        {
            get;
        }

        public string TopicTitle
        {
            get;
        }

        public KeywordPair(
            string languageCode,
            string topicTitle,
            string topic,
            string subtopic,
            string keyword,
            string blurb)
        {
            LanguageCode = languageCode;
            TopicTitle = topicTitle;
            Topic = topic;
            Subtopic = subtopic;
            Keyword = keyword;
            Blurb = blurb;
        }
    }
}