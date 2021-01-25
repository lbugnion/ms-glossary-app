using System.ComponentModel.DataAnnotations;

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

        [Required]
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

        [Required]
        public Term Term
        {
            get;
            set;
        }

        [Required]
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