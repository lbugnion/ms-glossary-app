namespace MsGlossaryApp.DataModel
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

        public TermInformation Term
        {
            get;
            set;
        }

        public string TermName
        {
            get;
            set;
        }

        public override string ToString()
        {
            return $"{Keyword} - {TermName}";
        }
    }
}