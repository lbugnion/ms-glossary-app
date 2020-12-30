using Microsoft.Extensions.Logging;
using MsGlossaryApp.DataModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MsGlossaryApp.Model
{
    public class SynopsisMaker
    {
        public static Term ParseSynopsis(
            Uri uri, 
            string markdown, 
            ILogger log)
        {
            log?.LogInformationEx("In ParseSynopsis", LogVerbosity.Verbose);
            log?.LogInformationEx($"Synopsis: {uri}", LogVerbosity.Verbose);

            var markdownReader = new StringReader(markdown);

            string authorNames = null;
            string emails = null;
            string twitters = null;
            string githubs = null;
            IList<Link> currentLinksSection = null;
            IList<string> currentInstructionsSection = null;
            var transcript = new StringBuilder();
            var shortDescription = new StringBuilder();

            var isSubmittedBy = false;
            var isShortDescription = false;
            var isPhonetics = false;
            var isPersonalNotes = false;
            var isKeywords = false;
            var isDemos = false;
            var isLinks = false;
            var isTranscript = false;
            var transcriptStarted = false;

            var synopsis = new Term
            {
                AuthorsInstructions = new List<string>(),
                Demos = new List<string>(),
                DemosInstructions = new List<string>(),
                KeywordsInstructions = new List<string>(),
                Links = new Dictionary<string, IList<Link>>(),
                LinksInstructions = new Dictionary<string, IList<string>>(),
                PersonalNotes = new List<string>(),
                PersonalNotesInstructions = new List<string>(),
                PhoneticsInstructions = new List<string>(),
                ShortDescriptionInstructions = new List<string>(),
                Stage = Term.TermStage.Synopsis,
                TitleInstructions = new List<string>(),
                TranscriptInstructions = new List<string>(),
                Uri = uri
            };

            string line;

            while ((line = markdownReader.ReadLine()) != null)
            {
                line = line.Trim();

                if (!isTranscript
                    && string.IsNullOrEmpty(line))
                {
                    continue;
                }

                if (line.IsNote()
                    && currentInstructionsSection != null)
                {
                    currentInstructionsSection.Add(line.ParseNote());
                    continue;
                }

                if (line.StartsWith(Constants.SynopsisMarkdownMarkers.TitleMarker))
                {
                    synopsis.Title = line.Substring(Constants.SynopsisMarkdownMarkers.TitleMarker.Length);
                    currentInstructionsSection = synopsis.TitleInstructions;
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
                    currentInstructionsSection = synopsis.AuthorsInstructions;
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
                    currentInstructionsSection = synopsis.ShortDescriptionInstructions;
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
                    currentInstructionsSection = synopsis.PhoneticsInstructions;
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
                    currentInstructionsSection = synopsis.PersonalNotesInstructions;
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
                    currentInstructionsSection = synopsis.KeywordsInstructions;
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
                    currentInstructionsSection = synopsis.DemosInstructions;
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

                    if (!synopsis.Links.ContainsKey(line))
                    {
                        synopsis.Links.Add(line, new List<Link>());
                    }

                    if (!synopsis.LinksInstructions.ContainsKey(line))
                    {
                        synopsis.LinksInstructions.Add(line, new List<string>());
                    }

                    currentLinksSection = synopsis.Links[line];
                    currentInstructionsSection = synopsis.LinksInstructions[line];
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
                    currentInstructionsSection = null;
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
                    shortDescription
                        .Append(line)
                        .Append(" ");
                }
                else if (isPhonetics)
                {
                    synopsis.Phonetics = line;
                }
                else if (isPersonalNotes)
                {
                    synopsis.PersonalNotes.Add(line.ParseListItem());
                }
                else if (isKeywords)
                {
                    synopsis.Keywords = TermMaker.MakeKeywords(line);
                }
                else if (isDemos)
                {
                    synopsis.Demos.Add(line.ParseListItem());
                }
                else if (isLinks)
                {
                    currentLinksSection.Add(line.ParseListItem().ParseLink());
                }
                else if (isTranscript)
                {
                    if (!transcriptStarted
                        && string.IsNullOrEmpty(line))
                    {
                        continue;
                    }

                    if (!line.IsNote())
                    {
                        transcriptStarted = true;
                    }

                    if (line.IsNote()
                        && !transcriptStarted)
                    {
                        synopsis.TranscriptInstructions.Add(line.ParseNote());
                    }

                    transcript.AppendLine(line);
                }
            }

            synopsis.Authors = TermMaker.MakeAuthors(
                authorNames,
                emails,
                githubs,
                twitters);

            synopsis.ShortDescription = shortDescription.ToString().Trim();
            synopsis.SafeFileName = synopsis.Title.MakeSafeFileName();

            return synopsis;
        }

        private const string SaveToGitHubPathMask = "glossary/synopsis/{0}.md";

        public async static Task<GlossaryFile> PrepareNewSynopsis(Term synopsis, string oldMarkdown)
        {
            if (synopsis.Stage != Term.TermStage.Synopsis)
            {
                throw new InvalidOperationException($"Invalid stage for {synopsis.SafeFileName}");
            }

            var file = new GlossaryFile
            {
                Path = string.Format(SaveToGitHubPathMask, synopsis.SafeFileName)
            };

            return null;
        }
    }
}
