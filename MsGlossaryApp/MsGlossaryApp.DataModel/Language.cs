namespace MsGlossaryApp.DataModel
{
    public class Language : IEqual
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

        public bool IsEqualTo(IEqual other)
        {
            var language = other as Language;

            if (language == null)
            {
                return false;
            }

            return language.Code == Code
                && language.LanguageName == LanguageName;
        }
    }
}