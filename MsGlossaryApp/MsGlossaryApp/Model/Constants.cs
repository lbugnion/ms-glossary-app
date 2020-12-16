namespace MsGlossaryApp.Model
{
    public static class Constants
    {
        public const string AzureWebJobsStorageVariableName = "AzureWebJobsStorage";
        public const string BlobStoreNameVariableName = "BlobStoreName";
        public const string DocsGlossaryGitHubAccountVariableName = "DocsGlossaryGitHubAccount";
        public const string DocsGlossaryGitHubMainBranchNameVariableName = "DocsGlossaryGitHubMainBranchName";
        public const string DocsGlossaryGitHubRepoVariableName = "DocsGlossaryGitHubRepo";
        public const string GitHubTokenVariableName = "GitHubToken";
        public const string H1 = "# ";
        public const string H2 = "## ";
        public const string H3 = "### ";
        public const string ListOfTermsUrlMask = "https://{0}.blob.core.windows.net/{1}/{2}";
        public const string MsGlossaryGitHubAccountVariableName = "MsGlossaryGitHubAccount";
        public const string MsGlossaryGitHubMainBranchName = "MsGlossaryGitHubMainBranchName";
        public const string MsGlossaryGitHubRepoVariableName = "MsGlossaryGitHubRepo";
        public const string NotifyFunctionCodeVariableName = "NotifyFunctionCode";
        public const string OutputContainerVariableName = "OutputContainer";
        public const string SavingLocationVariableName = "SavingLocation";
        public const char Separator = '|';
        public const string SettingsContainerVariableName = "SettingsContainer";
        public const string TermsContainerVariableName = "TermsContainer";
        public const string TermsSettingsFileName = "terms.en.json";

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
    }
}