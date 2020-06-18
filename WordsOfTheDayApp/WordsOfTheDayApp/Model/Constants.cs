namespace WordsOfTheDayApp.Model
{
    public static class Constants
    {
        public const string AzureWebJobsStorageVariableName = "AzureWebJobsStorage";
        public const string CaptionsContainerVariableName = "CaptionsContainer";
        public const string KeywordsBlob = "keywords.{0}.json";
        public const string NotifyFunctionCodeVariableName = "NotifyFunctionCode";
        public const string SettingsContainerVariableName = "SettingsContainer";
        public const string SideBarMarkdownBlob = "side-bar.{0}.md";
        public const string TopicsBlob = "topics.{0}.json";
        public const string TopicsContainerVariableName = "TopicsContainer";
        public const string TopicsUploadContainerVariableName = "TopicsUploadContainer";
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
            public const string ThisPageIsAlsoAvailableIn = "ThisPageIsAlsoAvailableIn";
            public const string CopyrightInfo = "CopyrightInfo";
            public const string TwitterUrl = "TwitterUrl";
            public const string LastModified = "LastModified";
            public const string DownloadTitle = "DownloadTitle";
            public const string VideoDownloadLink = "VideoDownloadLink";
            public const string CaptionsDownloadTitle = "CaptionsDownloadTitle";
            public const string CaptionsDownload = "CaptionsDownload";
            public const string VideoDownload = "VideoDownload";
            public const string LanguagesTitle = "LanguagesTitle";
        }
    }
}