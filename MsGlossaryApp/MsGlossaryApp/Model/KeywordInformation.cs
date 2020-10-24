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

        public bool IsDisambiguation 
        { 
            get; 
            set; 
        }

        public override string ToString()
        {
            return $"{Keyword} - {Topic?.Title}";
        }
    }
}