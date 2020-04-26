namespace WordsOfTheDayApp.Model
{
    public static class Constants
    {
        public const string AzureWebJobsStorage = "AzureWebJobsStorage";
        public const string OriginalMarkdownContainer = "markdown";
        public const string TargetMarkdownContainer = "markdown-transformed";
        public const string SettingsContainer = "settings";
        public const string KeywordsBlob = "keywords.json";
        public const string QueueName = "markdown-to-process";
    }
}
