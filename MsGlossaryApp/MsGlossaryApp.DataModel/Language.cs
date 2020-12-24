namespace MsGlossaryApp.DataModel
{
    public class Language
    {
        public string Code { get; set; }

        public string LanguageName { get; set; }
        
        public bool IsComplete
        {
            get
            {
                return !string.IsNullOrEmpty(Code)
                    && !string.IsNullOrEmpty(LanguageName);
            }
        }

        public override string ToString()
        {
            return $"{Code} / {LanguageName}";
        }
    }
}