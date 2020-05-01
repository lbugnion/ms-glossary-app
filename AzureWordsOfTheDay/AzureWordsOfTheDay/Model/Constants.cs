namespace AzureWordsOfTheDay.Model
{
    public static class Constants
    {
        public const string AzureWebJobsStorageVariableName = "AzureWebJobsStorage";
        public const string KeywordsBlob = "keywords.json";
        public const string TopicsBlob = "topics.json";
        public const string SideBarMarkdownBlob = "keywords.md";

        public const string TopicsUploadContainerVariableName = "TopicsUploadContainer";
        public const string TopicsContainerVariableName = "TopicsContainer";
        public const string SettingsContainerVariableName = "SettingsContainer";
        public const string QueueNameVariableName = "QueueName";
        public const string CaptionsContainerVariableName = "CaptionsContainer";
        public const string NotifyFunctionCodeVariableName = "NotifyFunctionCode";

        public const string LocalMarkdownFolderName = "Markdown";
    }
}
