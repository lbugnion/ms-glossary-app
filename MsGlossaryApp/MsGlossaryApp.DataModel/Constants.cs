namespace MsGlossaryApp.DataModel
{
    public static class Constants
    {
        public const string AzureWebJobsStorageVariableName = "AzureWebJobsStorage";
        public const string BlobStoreNameVariableName = "BlobStoreName";
        public const string DocsGlossaryGitHubAccountVariableName = "DocsGlossaryGitHubAccount";
        public const string DocsGlossaryGitHubMainBranchNameVariableName = "DocsGlossaryGitHubMainBranchName";
        public const string DocsGlossaryGitHubRepoVariableName = "DocsGlossaryGitHubRepo";
        public const string GitHubTokenVariableName = "GitHubToken";
        public const string ListOfTermsUrlMask = "https://{0}.blob.core.windows.net/{1}/{2}";
        public const string MsGlossaryGitHubAccountVariableName = "MsGlossaryGitHubAccount";
        public const string MsGlossaryGitHubMainBranchName = "MsGlossaryGitHubMainBranchName";
        public const string MsGlossaryGitHubRepoVariableName = "MsGlossaryGitHubRepo";
        public const string NotifyFunctionCodeVariableName = "NotifyFunctionCode";
        public const string OutputContainerVariableName = "OutputContainer";
        public const string SavingLocationVariableName = "SavingLocation";
        public const char Separator = ',';
        public const string SettingsContainerVariableName = "SettingsContainer";
        public const string TermsContainerVariableName = "TermsContainer";
        public const string TermsSettingsFileName = "terms.en.json";

        public static class TermMarkdownMarkers
        {
            public const string AuthorNameMarker = "> Author name: ";
            public const string ShortDescriptionMarker = "> Blurb: ";
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

        public static class SynopsisMarkdownMarkers
        {
            public const string TitleMarker = "# Microsoft Glossary Synopsis: ";
            public const string SubmittedByMarker = "## Submitted by";
            public const string NameMarker = "Name: ";
            public const string EmailMarker = "Email: ";
            public const string GitHubMarker = "GitHub: ";
            public const string TwitterMarker = "Twitter: ";
            public const string ShortDescriptionMarker = "## Short description";
            public const string PhoneticsMarker = "## Phonetics";
            public const string PersonalNotesMarker = "## Personal notes";
            public const string KeywordsMarker = "## Keywords";
            public const string DemosMarker = "## Demos";
            public const string LinksToDocsMarker = "## Links to docs";
            public const string LinksToLearnMarker = "## Links to Learn";
            public const string TranscriptMarker = "## Script";
        }
    }
}