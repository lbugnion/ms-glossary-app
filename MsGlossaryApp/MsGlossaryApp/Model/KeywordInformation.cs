namespace MsGlossaryApp.Model
{
    public class KeywordInformation
    {
        public string Keyword
        {
            get;
            set;
        }

        public TopicInformation Topic
        {
            get;
            set;
        }

        public bool MustDisambiguate
        {
            get;
            set;
        }

        public bool IsMainKeyword 
        { 
            get; 
            set; 
        }
    }
}
