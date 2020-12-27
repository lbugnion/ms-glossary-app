using Microsoft.Extensions.Logging;
using MsGlossaryApp.DataModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MsGlossaryApp.Model
{
    public class SynopsisMaker
    {
        private static IList<string> _currentLinksSection;

        public static Term ParseSynopsis(
            Uri uri, 
            string markdown, 
            ILogger log)
        {
            log?.LogInformationEx("In ParseSynopsis", LogVerbosity.Verbose);
            log?.LogInformationEx($"Synopsis: {uri}", LogVerbosity.Verbose);

            var markdownReader = new StringReader(markdown);

            string title = null;
            string authorNames = null;
            string emails = null;
            string twitters = null;
            string githubs = null;
            string shortDescription = null;
            string phonetics = null;
            var personalNotes = new List<string>();
            IList<string> keywords = null;
            var demos = new List<string>();
            var links = new Dictionary<string, IList<Link>>();
            IList<Link> currentLinksSection = null;
            var transcript = new StringBuilder();

            var isSubmittedBy = false;
            var isShortDescription = false;
            var isPhonetics = false;
            var isPersonalNotes = false;
            var isKeywords = false;
            var isDemos = false;
            var isLinks = false;
            var isTranscript = false;

            string line;

            while ((line = markdownReader.ReadLine()) != null)
            {
                line = line.Trim();

                if (!isTranscript
                    && line.IsNote()
                        || (!isTranscript && string.IsNullOrEmpty(line)))
                {
                    // Ignore the notes, we will make sure to preserve them when 
                    // we save the Synopsis back to GitHub
                    continue;
                }

                if (line.StartsWith(Constants.SynopsisMarkdownMarkers.TitleMarker))
                {
                    title = line.Substring(Constants.SynopsisMarkdownMarkers.TitleMarker.Length);
                }
                else if (line == Constants.SynopsisMarkdownMarkers.SubmittedByMarker)
                {
                    isSubmittedBy = true;
                    isShortDescription = false;
                    isPhonetics = false;
                    isPersonalNotes = false;
                    isKeywords = false;
                    isDemos = false;
                    isLinks = false;
                    isTranscript = false;
                }
                else if (line == Constants.SynopsisMarkdownMarkers.ShortDescriptionMarker)
                {
                    isSubmittedBy = false;
                    isShortDescription = true;
                    isPhonetics = false;
                    isPersonalNotes = false;
                    isKeywords = false;
                    isDemos = false;
                    isLinks = false;
                    isTranscript = false;
                }
                else if (line == Constants.SynopsisMarkdownMarkers.PhoneticsMarker)
                {
                    isSubmittedBy = false;
                    isShortDescription = false;
                    isPhonetics = true;
                    isPersonalNotes = false;
                    isKeywords = false;
                    isDemos = false;
                    isLinks = false;
                    isTranscript = false;
                }
                else if (line == Constants.SynopsisMarkdownMarkers.PersonalNotesMarker)
                {
                    isSubmittedBy = false;
                    isShortDescription = false;
                    isPhonetics = false;
                    isPersonalNotes = true;
                    isKeywords = false;
                    isDemos = false;
                    isLinks = false;
                    isTranscript = false;
                }
                else if (line == Constants.SynopsisMarkdownMarkers.KeywordsMarker)
                {
                    isSubmittedBy = false;
                    isShortDescription = false;
                    isPhonetics = false;
                    isPersonalNotes = false;
                    isKeywords = true;
                    isDemos = false;
                    isLinks = false;
                    isTranscript = false;
                }
                else if (line == Constants.SynopsisMarkdownMarkers.DemosMarker)
                {
                    isSubmittedBy = false;
                    isShortDescription = false;
                    isPhonetics = false;
                    isPersonalNotes = false;
                    isKeywords = false;
                    isDemos = true;
                    isLinks = false;
                    isTranscript = false;
                }
                else if (line == Constants.SynopsisMarkdownMarkers.LinksToDocsMarker
                    || line == Constants.SynopsisMarkdownMarkers.LinksToLearnMarker
                    || (line.IsH2()
                        && line.ToLower().Contains("links")))
                {
                    isSubmittedBy = false;
                    isShortDescription = false;
                    isPhonetics = false;
                    isPersonalNotes = false;
                    isKeywords = false;
                    isDemos = false;
                    isLinks = true;
                    isTranscript = false;

                    line = line.ParseH2();

                    if (!links.ContainsKey(line))
                    {
                        links.Add(line, new List<Link>());
                    }

                    currentLinksSection = links[line];
                }
                else if (line == Constants.SynopsisMarkdownMarkers.TranscriptMarker)
                {
                    isSubmittedBy = false;
                    isShortDescription = false;
                    isPhonetics = false;
                    isPersonalNotes = false;
                    isKeywords = false;
                    isDemos = false;
                    isLinks = false;
                    isTranscript = true;
                }
                else if (isSubmittedBy)
                {
                    if (line.StartsWith(Constants.SynopsisMarkdownMarkers.NameMarker))
                    {
                        authorNames = line.Substring(Constants.SynopsisMarkdownMarkers.NameMarker.Length).Trim();
                    }
                    else if (line.StartsWith(Constants.SynopsisMarkdownMarkers.EmailMarker))
                    {
                        emails = line.Substring(Constants.SynopsisMarkdownMarkers.EmailMarker.Length).Trim();
                    }
                    else if (line.StartsWith(Constants.SynopsisMarkdownMarkers.GitHubMarker))
                    {
                        githubs = line.Substring(Constants.SynopsisMarkdownMarkers.GitHubMarker.Length).Trim();
                    }
                    else if (line.StartsWith(Constants.SynopsisMarkdownMarkers.TwitterMarker))
                    {
                        twitters = line
                            .Substring(Constants.SynopsisMarkdownMarkers.TwitterMarker.Length)
                            .Replace("@", "");
                    }
                }
                else if (isShortDescription)
                {
                    shortDescription = line;
                }
                else if (isPhonetics)
                {
                    phonetics = line;
                }
                else if (isPersonalNotes)
                {
                    personalNotes.Add(line.ParseListItem());
                }
                else if (isKeywords)
                {
                    keywords = TermMaker.MakeKeywords(line);
                }
                else if (isDemos)
                {
                    demos.Add(line.ParseListItem());
                }
                else if (isLinks)
                {
                    currentLinksSection.Add(line.ParseListItem().ParseLink());
                }
                else if (isTranscript)
                {
                    transcript.AppendLine(line);
                }
            }

            IList<Author> authors = TermMaker.MakeAuthors(
                authorNames,
                emails,
                githubs,
                twitters);

            var synopsis = new Term
            {
                Uri = uri,
                Stage = Term.TermStage.Synopsis,
                Authors = authors,
                Keywords = keywords,
                Links = links,
                SafeFileName = title?.MakeSafeFileName(),
                ShortDescription = shortDescription,
                Title = title,
                Transcript = transcript.ToString().Trim()
            };

            return synopsis;
        }
    }
}
