namespace MsGlossaryApp.DataModel
{
    public class Language : IEqual
    {
        public string Code { get; set; }

        public bool IsComplete
        {
            get
            {
                return !string.IsNullOrEmpty(Code)
                    && !string.IsNullOrEmpty(LanguageName);
            }
        }

        public string LanguageName { get; set; }

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

        public override string ToString()
        {
            return $"{Code} / {LanguageName}";
        }
    }
}