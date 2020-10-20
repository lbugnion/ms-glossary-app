namespace MsGlossaryApp.Model
{
    public class KeywordInformation
    {
        public bool IsMainKeyword
        {
            get;
            set;
        }

        public string Keyword
        {
            get;
            set;
        }

        public bool MustDisambiguate
        {
            get;
            set;
        }

        public TopicInformation Topic
        {
            get;
            set;
        }
    }
}