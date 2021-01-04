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
        private const string SaveToGitHubPathMask = "glossary/synopsis/{0}.md";

        private static string MakeSynopsisText(
            Synopsis synopsis,
            ILogger log)
        {
            log?.LogInformationEx("In MakeSynopsisText", LogVerbosity.Verbose);

            var builder = new StringBuilder()
                .Append(Constants.SynopsisMarkdownMarkers.TitleMarker)
                .AppendLine(synopsis.Title)
                .AppendLine();

            if (synopsis.TitleInstructions != null)
            {
                foreach (var instruction in synopsis.TitleInstructions)
                {
                    builder
                        .AppendLine(instruction.MakeNote())
                        .AppendLine();
                }
            }

            builder
                .AppendLine(Constants.SynopsisMarkdownMarkers.SubmittedByMarker)
                .AppendLine();

            if (synopsis.AuthorsInstructions != null)
            {
                foreach (var instruction in synopsis.AuthorsInstructions)
                {
                    builder
                        .AppendLine(instruction.MakeNote())
                        .AppendLine();
                }
            }

            var names = new StringBuilder().Append(Constants.SynopsisMarkdownMarkers.NameMarker);
            var emails = new StringBuilder().Append(Constants.SynopsisMarkdownMarkers.EmailMarker);
            var githubs = new StringBuilder().Append(Constants.SynopsisMarkdownMarkers.GitHubMarker);
            var twitters = new StringBuilder().Append(Constants.SynopsisMarkdownMarkers.TwitterMarker);

            if (synopsis.Authors != null)
            {
                foreach (var author in synopsis.Authors)
                {
                    names.Append(author.Name).Append(", ");
                    emails.Append(author.Email).Append(", ");
                    twitters.Append(author.Twitter).Append(", ");
                    githubs.Append(author.GitHub).Append(", ");
                }
            }

            builder
                .AppendLine(names.ToString().Substring(0, names.Length - 2))
                .AppendLine()
                .AppendLine(emails.ToString().Substring(0, emails.Length - 2))
                .AppendLine()
                .AppendLine(twitters.ToString().Substring(0, twitters.Length - 2))
                .AppendLine()
                .AppendLine(githubs.ToString().Substring(0, githubs.Length - 2))
                .AppendLine()
                .AppendLine(Constants.SynopsisMarkdownMarkers.ShortDescriptionMarker)
                .AppendLine();

            if (synopsis.ShortDescriptionInstructions != null)
            {
                foreach (var instruction in synopsis.ShortDescriptionInstructions)
                {
                    builder
                        .AppendLine(instruction.MakeNote())
                        .AppendLine();
                }
            }

            builder
                .AppendLine(synopsis.ShortDescription)
                .AppendLine()
                .AppendLine(Constants.SynopsisMarkdownMarkers.PhoneticsMarker)
                .AppendLine();

            if (synopsis.PhoneticsInstructions != null)
            {
                foreach (var instruction in synopsis.PhoneticsInstructions)
                {
                    builder
                        .AppendLine(instruction.MakeNote())
                        .AppendLine();
                }
            }

            builder
                .AppendLine(synopsis.Phonetics)
                .AppendLine()
                .AppendLine(Constants.SynopsisMarkdownMarkers.PersonalNotesMarker)
                .AppendLine();

            if (synopsis.PersonalNotesInstructions != null)
            {
                foreach (var instruction in synopsis.PersonalNotesInstructions)
                {
                    builder
                        .AppendLine(instruction.MakeNote())
                        .AppendLine();
                }
            }

            if (synopsis.PersonalNotes != null)
            {
                foreach (var note in synopsis.PersonalNotes)
                {
                    builder
                        .AppendLine(note.MakeListItem());
                }
            }

            builder
                .AppendLine()
                .AppendLine(Constants.SynopsisMarkdownMarkers.KeywordsMarker)
                .AppendLine();

            if (synopsis.KeywordsInstructions != null)
            {
                foreach (var instruction in synopsis.KeywordsInstructions)
                {
                    builder
                        .AppendLine(instruction.MakeNote())
                        .AppendLine();
                }
            }

            if (synopsis.Keywords != null)
            {
                builder
                    .AppendLine(TermMaker.MakeKeywordsLine(synopsis.Keywords))
                    .AppendLine();
            }

            builder
                .AppendLine(Constants.SynopsisMarkdownMarkers.DemosMarker)
                .AppendLine();

            if (synopsis.DemosInstructions != null)
            {
                foreach (var instruction in synopsis.DemosInstructions)
                {
                    builder
                        .AppendLine(instruction.MakeNote())
                        .AppendLine();
                }
            }

            if (synopsis.Demos != null)
            {
                foreach (var demo in synopsis.Demos)
                {
                    builder
                        .AppendLine(demo.MakeListItem());
                }
            }

            builder.AppendLine();

            if (synopsis.Links != null)
            {
                foreach (var key in synopsis.Links.Keys)
                {
                    builder
                        .AppendLine(key.MakeH2())
                        .AppendLine();

                    if (synopsis.LinksInstructions != null
                        && synopsis.LinksInstructions.ContainsKey(key)
                        && synopsis.LinksInstructions[key] != null)
                    {
                        foreach (var instruction in synopsis.LinksInstructions[key])
                        {
                            builder
                                .AppendLine(instruction.MakeNote())
                                .AppendLine();
                        }
                    }

                    if (synopsis.Links[key] != null
                        && synopsis.Links[key].Count > 0)
                    {
                        foreach (var link in synopsis.Links[key])
                        {
                            builder
                                .AppendLine(link.ToMarkdown().MakeListItem());
                        }

                        builder.AppendLine();
                    }
                }

                builder
                    .AppendLine(Constants.SynopsisMarkdownMarkers.TranscriptMarker)
                    .AppendLine();

                if (synopsis.TranscriptInstructions != null)
                {
                    foreach (var instruction in synopsis.TranscriptInstructions)
                    {
                        builder
                            .AppendLine(instruction.MakeNote())
                            .AppendLine();
                    }
                }

                if (!string.IsNullOrEmpty(synopsis.Transcript))
                {
                    builder.AppendLine(synopsis.Transcript);
                }
            }

            log?.LogInformationEx("Out MakeSynopsisText", LogVerbosity.Verbose);
            return builder.ToString();
        }

        public static Synopsis ParseSynopsis(
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

            var synopsis = new Synopsis
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
                    else
                    {
                        transcript.AppendLine(line);
                    }
                }
            }

            synopsis.Authors = TermMaker.MakeAuthors(
                authorNames,
                emails,
                githubs,
                twitters);

            synopsis.ShortDescription = shortDescription.ToString().Trim();
            synopsis.SafeFileName = synopsis.Title.MakeSafeFileName();
            synopsis.Transcript = transcript.ToString();

            return synopsis;
        }

        public static GlossaryFile PrepareNewSynopsis(
            Synopsis synopsis,
            string oldMarkdown,
            ILogger log)
        {
            var oldSynopsis = ParseSynopsis(
                synopsis.Uri,
                oldMarkdown,
                log);

            var file = new GlossaryFile
            {
                Path = string.Format(SaveToGitHubPathMask, synopsis.SafeFileName)
            };

            if (oldSynopsis.Equals(synopsis))
            {
                file.MustSave = false;
                return file;
            }

            file.MustSave = true;
            file.Content = MakeSynopsisText(synopsis, log);

            return file;
        }
    }
}