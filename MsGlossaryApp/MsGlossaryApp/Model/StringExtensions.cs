namespace MsGlossaryApp.Model
{
    public static class StringExtensions
    {
        public static string MakeSafeFileName(this string topic)
        {
            return topic.ToLower()
                .Replace(' ', '-')
                .Replace('/', '-')
                .Replace('.', '-');
        }
    }
}