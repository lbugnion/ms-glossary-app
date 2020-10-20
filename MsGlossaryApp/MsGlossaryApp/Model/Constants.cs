namespace MsGlossaryApp.Model
{
    public static class Constants
    {
        public const string AzureWebJobsStorageVariableName = "AzureWebJobsStorage";
        public const string NotifyFunctionCodeVariableName = "NotifyFunctionCode";
        public const string TopicsContainerVariableName = "TopicsContainer";
        public const string OutputContainerVariableName = "OutputContainer";

        public const string H1 = "# ";
        public const string H2 = "## ";
        public const string H3 = "### ";
        public const char Separator = '|';

        public static class Input
        {
            public const string YouTubeMarker = "> YouTube: ";
            public const string KeywordsMarker = "> Keywords: ";
            public const string BlurbMarker = "> Blurb: ";
            public const string CaptionsMarker = "> Captions: ";
            public const string LanguageMarker = "> Language: ";
            public const string TwitterMarker = "> Twitter: ";
            public const string GitHubMarker = "> GitHub: ";
            public const string RecordingDateMarker = "> Recording date: ";
            public const string TranscriptMarker = "## Transcript";
            public const string LinksMarker = "## Links";
            public const string EmailMarker = "> Email: ";
            public const string AuthorNameMarker = "> Author name: ";
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