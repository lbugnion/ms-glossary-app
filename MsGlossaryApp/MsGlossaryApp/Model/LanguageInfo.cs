namespace MsGlossaryApp.Model
{
    public class LanguageInfo
    {
        public string Code { get; set; }

        public string Language { get; set; }

        public override string ToString()
        {
            return $"{Code} / {Language}";
        }
    }
}