using System;

namespace MsGlossaryApp.DataModel
{
    public static class Constants
    {
        public const string AzureWebJobsStorageVariableName = "AzureWebJobsStorage";
        public const string BlobStoreNameVariableName = "BlobStoreName";
        public const string HashHeaderKey = "x-glossary-hash";
        public const string CommitMessageHeaderKey = "x-glossary-commit-message";
        public const string DocsGlossaryGitHubAccountVariableName = "DocsGlossaryGitHubAccount";
        public const string DocsGlossaryGitHubMainBranchNameVariableName = "DocsGlossaryGitHubMainBranchName";
        public const string DocsGlossaryGitHubRepoVariableName = "DocsGlossaryGitHubRepo";
        public const string FileNameHeaderKey = "x-glossary-file-name";
        public const string FunctionCodeHeaderKey = "x-functions-key";
        public const string GitHubSynopsisUrlTemplate = "https://raw.githubusercontent.com/{0}/{1}/{2}/synopsis/{2}.md";
        public const string GitHubTokenVariableName = "GitHubToken";
        public const string CreateReleaseNotesForVariableName = "CreateReleaseNotesFor";
        public const string ReleaseNotesFoldersVariableName = "ReleaseNotesFolders";
        public const string ImageContainerVariableName = "ImageContainer";
        public const string ListOfTermsUrlMask = "https://{0}.blob.core.windows.net/{1}/{2}";
        public const int MaxCharactersInDescription = 150;
        public const int MaxWordsInTranscript = 320;
        public const int MinCharactersInDescription = 40;
        public const int MinWordsInTranscript = 280;
        public const string MsGlossaryGitHubAccountVariableName = "MsGlossaryGitHubAccount";
        public const string MsGlossaryGitHubMainBranchName = "MsGlossaryGitHubMainBranchName";
        public const string MsGlossaryGitHubRepoVariableName = "MsGlossaryGitHubRepo";
        public const string MsGlossaryAppGitHubAccountVariableName = "MsGlossaryAppGitHubAccount";
        public const string MsGlossaryAppGitHubMainBranchName = "MsGlossaryAppGitHubMainBranchName";
        public const string MsGlossaryAppGitHubRepoVariableName = "MsGlossaryAppGitHubRepo";
        public const string NotifyFunctionCodeVariableName = "NotifyFunctionCode";
        public const string OutputContainerVariableName = "OutputContainer";
        public const string SavingLocationVariableName = "SavingLocation";
        public const char Separator = ',';
        public const string SettingsContainerVariableName = "SettingsContainer";
        public const string SynopsisPathMask = "synopsis/{0}.md";
        public const string TermsContainerVariableName = "TermsContainer";
        public const string TermsSettingsFileName = "terms.en.json";
        public const string UserEmailHeaderKey = "x-glossary-user-email";

        public static class SynopsisMarkdownMarkers
        {
            public const string DemosMarker = "## Demos";
            public const string EmailMarker = "Email: ";
            public const string GitHubMarker = "GitHub: ";
            public const string KeywordsMarker = "## Keywords";
            public const string LinkNoText = "NO_LINK_TITLE";
            public const string LinksToDocsMarker = "## Links to docs";
            public const string LinksToLearnMarker = "## Links to Learn";
            public const string LinksToOthersMarker = "## Other Links (optional)";
            public const string NameMarker = "Name: ";
            public const string PersonalNotesMarker = "## Personal notes";
            public const string PhoneticsMarker = "## Phonetics";
            public const string ShortDescriptionMarker = "## Short description";
            public const string SubmittedByMarker = "## Submitted by";
            public const string TitleMarker = "# Microsoft Glossary Synopsis: ";
            public const string TranscriptMarker = "## Script";
            public const string TwitterMarker = "Twitter: ";
        }

        public static class TermMarkdownMarkers
        {
            public const string AuthorNameMarker = "> Author name: ";
            public const string CaptionsMarker = "> Captions: ";
            public const string EmailMarker = "> Email: ";
            public const string GitHubMarker = "> GitHub: ";
            public const string KeywordsMarker = "> Keywords: ";
            public const string LanguageMarker = "> Language: ";
            public const string LinksMarker = "## Links";
            public const string LinksToDocsMarker = "### Documentation";
            public const string LinksToLearnMarker = "### Microsoft Learn";
            public const string LinksToOthersMarker = "### Other links";
            public const string RecordingDateMarker = "> Recording date: ";
            public const string ShortDescriptionMarker = "> Blurb: ";
            public const string TranscriptMarker = "## Transcript";
            public const string TwitterMarker = "> Twitter: ";
            public const string YouTubeMarker = "> YouTube: ";
        }
    }
}