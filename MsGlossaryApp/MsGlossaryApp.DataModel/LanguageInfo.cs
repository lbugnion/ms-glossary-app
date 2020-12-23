namespace MsGlossaryApp.DataModel
{
    public class Language
    {
        public string Code { get; set; }

        public string LanguageName { get; set; }

        public override string ToString()
        {
            return $"{Code} / {LanguageName}";
        }
    }
}