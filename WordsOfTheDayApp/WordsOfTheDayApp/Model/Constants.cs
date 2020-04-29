namespace WordsOfTheDayApp.Model
{
    public static class Constants
    {
        public const string AzureWebJobsStorage = "AzureWebJobsStorage";
        public const string KeywordsBlob = "keywords.json";
        public const string TopicsBlob = "topics.json";
        public const string SideBarMarkdownBlob = "keywords.md";

        public const string QueueName = "test-markdown-to-process";
        //public const string QueueName = "markdown-to-process";
    }
}
