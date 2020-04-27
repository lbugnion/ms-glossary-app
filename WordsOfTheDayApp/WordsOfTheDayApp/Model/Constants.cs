namespace WordsOfTheDayApp.Model
{
    public static class Constants
    {
        public const string AzureWebJobsStorage = "AzureWebJobsStorage";
        public const string KeywordsBlob = "keywords.json";

#if DEBUG
        public const string QueueName = "test-markdown-to-process";
#else
        public const string QueueName = "markdown-to-process";
#endif
    }
}
