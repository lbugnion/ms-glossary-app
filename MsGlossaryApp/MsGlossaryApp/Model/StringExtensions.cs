namespace MsGlossaryApp.Model
{
    public static class StringExtensions
    {
        public static string MakeSafeFileName(this string term)
        {
            return term.ToLower()
                .Replace(' ', '-')
                .Replace('/', '-')
                .Replace('.', '-');
        }
    }
}