namespace MsGlossaryApp.Model
{
    public class KeywordInformation
    {
        public bool IsDisambiguation
        {
            get;
            set;
        }

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

        public string TopicName
        {
            get;
            set;
        }

        public override string ToString()
        {
            return $"{Keyword} - {TopicName}";
        }
    }
}