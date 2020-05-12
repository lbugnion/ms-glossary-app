namespace WordsOfTheDayApp.Model
{
    public static class Constants
    {
        public const string AzureWebJobsStorageVariableName = "AzureWebJobsStorage";
        public const string CaptionsContainerVariableName = "CaptionsContainer";
        public const string KeywordsBlob = "keywords.{0}.json";
        public const string NotifyFunctionCodeVariableName = "NotifyFunctionCode";
        public const string QueueNameVariableName = "QueueName";
        public const string SettingsContainerVariableName = "SettingsContainer";
        public const string SideBarMarkdownBlob = "side-bar.{0}.md";
        public const string TopicsBlob = "topics.{0}.json";
        public const string TopicsContainerVariableName = "TopicsContainer";
        public const string TopicsUploadContainerVariableName = "TopicsUploadContainer";
#if DEBUG
        public const bool UseSemaphores = false;
#endif
    }
}