namespace WordsOfTheDayApp.Model
{
    public static class TextHelper
    {
        public static string GetText(string languageCode, string key)
        {
            var text = Texts.ResourceManager.GetString($"{languageCode}.{key}");

            if (string.IsNullOrEmpty(text))
            {
                text = Texts.ResourceManager.GetString($"en.{key}");
            }

            if (string.IsNullOrEmpty(text))
            {
                return $"Not found: {key}";
            }

            return text;
        }
    }
}