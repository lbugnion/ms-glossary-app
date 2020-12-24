namespace MsGlossaryApp.DataModel
{
    public class Keyword
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

        public string KeywordName
        {
            get;
            set;
        }

        public bool MustDisambiguate
        {
            get;
            set;
        }

        public Term Term
        {
            get;
            set;
        }

        public string TermSafeFileName
        {
            get;
            set;
        }

        public override string ToString()
        {
            return $"{KeywordName} - {TermSafeFileName}";
        }
    }
}