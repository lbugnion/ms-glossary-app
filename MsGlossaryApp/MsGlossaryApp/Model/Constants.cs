namespace MsGlossaryApp.Model
{
    public static class Constants
    {
        public const string AzureWebJobsStorageVariableName = "AzureWebJobsStorage";
        public const string H1 = "# ";
        public const string H2 = "## ";
        public const string H3 = "### ";
        public const string NotifyFunctionCodeVariableName = "NotifyFunctionCode";
        public const string OutputContainerVariableName = "OutputContainer";
        public const char Separator = '|';
        public const string TopicsContainerVariableName = "TopicsContainer";
        public const string SettingsContainerVariableName = "SettingsContainer";
        public const string GitHubTokenVariableName = "GitHubToken";
        public const string BlobStoreNameVariableName = "BlobStoreName";
        public const string DocsGlossaryGitHubAccountVariableName = "DocsGlossaryGitHubAccount";
        public const string DocsGlossaryGitHubMainBranchNameVariableName = "DocsGlossaryGitHubMainBranchName";
        public const string DocsGlossaryGitHubRepoVariableName = "DocsGlossaryGitHubRepo";
        public const string ListOfTopicsUrlMask = "https://{0}.blob.core.windows.net/{1}/{2}";
        public const string TopicsSettingsFileName = "topics.en.json";
        public const string MsGlossaryGitHubAccountVariableName = "MsGlossaryGitHubAccount";
        public const string MsGlossaryGitHubMainBranchName = "MsGlossaryGitHubMainBranchName";
        public const string MsGlossaryGitHubRepoVariableName = "MsGlossaryGitHubRepo";

        public static class Input
        {
            public const string AuthorNameMarker = "> Author name: ";
            public const string BlurbMarker = "> Blurb: ";
            public const string CaptionsMarker = "> Captions: ";
            public const string EmailMarker = "> Email: ";
            public const string GitHubMarker = "> GitHub: ";
            public const string KeywordsMarker = "> Keywords: ";
            public const string LanguageMarker = "> Language: ";
            public const string LinksMarker = "## Links";
            public const string RecordingDateMarker = "> Recording date: ";
            public const string TranscriptMarker = "## Transcript";
            public const string TwitterMarker = "> Twitter: ";
            public const string YouTubeMarker = "> YouTube: ";
        }

        //public static class Texts
        //{
        //    public const string By = "By";
        //    public const string CaptionsDownload = "CaptionsDownload";
        //    public const string CaptionsDownloadTitle = "CaptionsDownloadTitle";
        //    public const string CopyrightInfo = "CopyrightInfo";
        //    public const string DisambiguationIntro = "DisambiguationIntro";
        //    public const string DisambiguationItem = "DisambiguationItem";
        //    public const string DisambiguationTitle = "DisambiguationTitle";
        //    public const string DownloadTitle = "DownloadTitle";
        //    public const string LanguagesTitle = "LanguagesTitle";
        //    public const string LastModified = "LastModified";
        //    public const string RedirectedFrom = "RedirectedFrom";
        //    public const string ThisPageIsAlsoAvailableIn = "ThisPageIsAlsoAvailableIn";
        //    public const string TopicHeader = "TopicHeader";
        //    public const string TwitterUrl = "TwitterUrl";
        //    public const string VideoDownload = "VideoDownload";
        //    public const string VideoDownloadLink = "VideoDownloadLink";
        //}
    }
}