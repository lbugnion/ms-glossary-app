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
        public const string LanguagesBlob = "languages.txt";
        public const string TopicsContainerVariableName = "TopicsContainer";
        public const string TopicsUploadContainerVariableName = "TopicsUploadContainer";
        public const string SiteLanguages = "Site languages: ";
        public const string OtherLanguages = "> Other languages: ";
        public const string Disambiguation = "disambiguation";

#if DEBUG
        public const bool UseSemaphores = false;
#endif

        public static class Texts
        {
            public const string RedirectedFrom = "RedirectedFrom";
            public const string TopicHeader = "TopicHeader";
            public const string DisambiguationTitle = "DisambiguationTitle";
            public const string DisambiguationIntro = "DisambiguationIntro";
            public const string DisambiguationItem = "DisambiguationItem";
            public const string By = "By";
        }
    }
}