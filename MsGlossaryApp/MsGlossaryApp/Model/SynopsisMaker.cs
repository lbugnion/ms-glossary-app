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
        private const string SaveToGitHubPathMask = "synopsis/{0}.md";

        private static string MakeSynopsisText(
            Synopsis synopsis,
            ILogger log)
        {
            // TODO Validate the Synopsis, throw exception if it is invalid

            log?.LogInformation("In MakeSynopsisText");

            var builder = new StringBuilder()
                .Append(Constants.SynopsisMarkdownMarkers.TitleMarker)
                .AppendLine(synopsis.Title)
                .AppendLine();

            foreach (var instruction in synopsis.TitleInstructions)
            {
                builder
                    .AppendLine(instruction.MakeNote())
                    .AppendLine();
            }

            builder
                .AppendLine(Constants.SynopsisMarkdownMarkers.SubmittedByMarker)
                .AppendLine();

            foreach (var instruction in synopsis.AuthorsInstructions)
            {
                builder
                    .AppendLine(instruction.MakeNote())
                    .AppendLine();
            }

            var names = new StringBuilder().Append(Constants.SynopsisMarkdownMarkers.NameMarker);
            var emails = new StringBuilder().Append(Constants.SynopsisMarkdownMarkers.EmailMarker);
            var githubs = new StringBuilder().Append(Constants.SynopsisMarkdownMarkers.GitHubMarker);
            var twitters = new StringBuilder().Append(Constants.SynopsisMarkdownMarkers.TwitterMarker);

            foreach (var author in synopsis.Authors)
            {
                names.Append(author.Name).Append(", ");
                emails.Append(author.Email).Append(", ");
                twitters.Append(author.Twitter).Append(", ");
                githubs.Append(author.GitHub).Append(", ");
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

            foreach (var instruction in synopsis.ShortDescriptionInstructions)
            {
                builder
                    .AppendLine(instruction.MakeNote())
                    .AppendLine();
            }

            builder
                .AppendLine(synopsis.ShortDescription)
                .AppendLine()
                .AppendLine(Constants.SynopsisMarkdownMarkers.PhoneticsMarker)
                .AppendLine();

            foreach (var instruction in synopsis.PhoneticsInstructions)
            {
                builder
                    .AppendLine(instruction.MakeNote())
                    .AppendLine();
            }

            builder
                .AppendLine(synopsis.Phonetics)
                .AppendLine()
                .AppendLine(Constants.SynopsisMarkdownMarkers.PersonalNotesMarker)
                .AppendLine();

            foreach (var instruction in synopsis.PersonalNotesInstructions)
            {
                builder
                    .AppendLine(instruction.MakeNote())
                    .AppendLine();
            }

            foreach (var note in synopsis.PersonalNotes)
            {
                builder
                    .AppendLine(note.Content.MakeListItem());
            }

            builder
                .AppendLine()
                .AppendLine(Constants.SynopsisMarkdownMarkers.KeywordsMarker)
                .AppendLine();

            foreach (var instruction in synopsis.KeywordsInstructions)
            {
                builder
                    .AppendLine(instruction.MakeNote())
                    .AppendLine();
            }

            if (synopsis.Keywords.Count > 0)
            {
                builder
                    .AppendLine(TermMaker.MakeKeywordsLine(synopsis.Keywords))
                    .AppendLine();
            }

            builder
                .AppendLine(Constants.SynopsisMarkdownMarkers.DemosMarker)
                .AppendLine();

            foreach (var instruction in synopsis.DemosInstructions)
            {
                builder
                    .AppendLine(instruction.MakeNote())
                    .AppendLine();
            }

            foreach (var demo in synopsis.Demos)
            {
                builder
                    .AppendLine(demo.Content.MakeListItem());
            }

            builder.AppendLine();

            CreateLinksSection(
                builder,
                Constants.SynopsisMarkdownMarkers.LinksToDocsMarker,
                synopsis.LinksToDocs,
                synopsis.LinksInstructions);
            CreateLinksSection(
                builder,
                Constants.SynopsisMarkdownMarkers.LinksToLearnMarker,
                synopsis.LinksToLearn,
                synopsis.LinksInstructions);
            CreateLinksSection(
                builder,
                Constants.SynopsisMarkdownMarkers.LinksToOthersMarker,
                synopsis.LinksToOthers,
                synopsis.LinksInstructions);

            static void CreateLinksSection(
                StringBuilder builder,
                string title,
                IList<Link> links,
                Dictionary<string, IList<string>> instructions)
            {
                builder
                    .AppendLine(title)
                    .AppendLine();

                var key = title.ParseH2();

                if (instructions.ContainsKey(key)
                    && instructions[key] != null)
                {
                    foreach (var instruction in instructions[key])
                    {
                        builder
                            .AppendLine(instruction.MakeNote())
                            .AppendLine();
                    }
                }

                foreach (var link in links)
                {
                    builder
                        .AppendLine(link.ToMarkdown().MakeListItem());
                }

                if (links.Count > 0)
                {
                    builder.AppendLine();
                }
            }

            builder
                .AppendLine(Constants.SynopsisMarkdownMarkers.TranscriptMarker)
                .AppendLine();

            foreach (var instruction in synopsis.TranscriptInstructions)
            {
                builder
                    .AppendLine(instruction.MakeNote())
                    .AppendLine();
            }

            foreach (var line in synopsis.TranscriptLines)
            {
                builder
                    .AppendLine(line.Markdown)
                    .AppendLine();
            }

            log?.LogInformation("Out MakeSynopsisText");
            return builder.ToString();
        }

        public static Synopsis ParseSynopsis(
            Uri uri,
            string markdown,
            ILogger log)
        {
            // TODO Once the Synopsis client is published, remove the instructions from the template and force only Synopsis client to be used

            log?.LogInformation("In ParseSynopsis");
            log?.LogInformation($"Synopsis: {uri}");

            var synopsis = new Synopsis
            {
                Uri = uri
            };

            var markdownReader = new StringReader(markdown);

            string authorNames = null;
            string emails = null;
            string twitters = null;
            string githubs = null;
            IList<Link> currentLinksSection = null;
            IList<string> currentInstructionsSection = null;
            var shortDescription = new StringBuilder();
            bool isSubmittedBy = false,
                 isShortDescription = false,
                 isPhonetics = false,
                 isPersonalNotes = false,
                 isKeywords = false,
                 isDemos = false,
                 isLinks = false,
                 isTranscript = false;
            var transcriptStarted = false;
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
                    continue;
                }

                switch (line)
                {
                    case Constants.SynopsisMarkdownMarkers.SubmittedByMarker:
                        currentInstructionsSection = synopsis.AuthorsInstructions;
                        isSubmittedBy = true;
                        isShortDescription = false;
                        isPhonetics = false;
                        isPersonalNotes = false;
                        isKeywords = false;
                        isDemos = false;
                        isLinks = false;
                        isTranscript = false;
                        continue;

                    case Constants.SynopsisMarkdownMarkers.ShortDescriptionMarker:
                        currentInstructionsSection = synopsis.ShortDescriptionInstructions;
                        isSubmittedBy = false;
                        isShortDescription = true;
                        isPhonetics = false;
                        isPersonalNotes = false;
                        isKeywords = false;
                        isDemos = false;
                        isLinks = false;
                        isTranscript = false;
                        continue;

                    case Constants.SynopsisMarkdownMarkers.PhoneticsMarker:
                        currentInstructionsSection = synopsis.PhoneticsInstructions;
                        isSubmittedBy = false;
                        isShortDescription = false;
                        isPhonetics = true;
                        isPersonalNotes = false;
                        isKeywords = false;
                        isDemos = false;
                        isLinks = false;
                        isTranscript = false;
                        continue;

                    case Constants.SynopsisMarkdownMarkers.PersonalNotesMarker:
                        currentInstructionsSection = synopsis.PersonalNotesInstructions;
                        isSubmittedBy = false;
                        isShortDescription = false;
                        isPhonetics = false;
                        isPersonalNotes = true;
                        isKeywords = false;
                        isDemos = false;
                        isLinks = false;
                        isTranscript = false;
                        continue;

                    case Constants.SynopsisMarkdownMarkers.KeywordsMarker:
                        currentInstructionsSection = synopsis.KeywordsInstructions;
                        isSubmittedBy = false;
                        isShortDescription = false;
                        isPhonetics = false;
                        isPersonalNotes = false;
                        isKeywords = true;
                        isDemos = false;
                        isLinks = false;
                        isTranscript = false;
                        continue;

                    case Constants.SynopsisMarkdownMarkers.DemosMarker:
                        currentInstructionsSection = synopsis.DemosInstructions;
                        isSubmittedBy = false;
                        isShortDescription = false;
                        isPhonetics = false;
                        isPersonalNotes = false;
                        isKeywords = false;
                        isDemos = true;
                        isLinks = false;
                        isTranscript = false;
                        continue;

                    case Constants.SynopsisMarkdownMarkers.LinksToDocsMarker:
                    case Constants.SynopsisMarkdownMarkers.LinksToLearnMarker:
                    case Constants.SynopsisMarkdownMarkers.LinksToOthersMarker:
                        currentInstructionsSection = GetLinkInstructionSection(line);
                        isSubmittedBy = false;
                        isShortDescription = false;
                        isPhonetics = false;
                        isPersonalNotes = false;
                        isKeywords = false;
                        isDemos = false;
                        isLinks = true;
                        isTranscript = false;

                        switch (line)
                        {
                            case Constants.SynopsisMarkdownMarkers.LinksToDocsMarker:
                                currentLinksSection = synopsis.LinksToDocs;
                                break;

                            case Constants.SynopsisMarkdownMarkers.LinksToLearnMarker:
                                currentLinksSection = synopsis.LinksToLearn;
                                break;

                            case Constants.SynopsisMarkdownMarkers.LinksToOthersMarker:
                                currentLinksSection = synopsis.LinksToOthers;
                                break;
                        }

                        continue;

                    case Constants.SynopsisMarkdownMarkers.TranscriptMarker:
                        currentInstructionsSection = null;
                        isSubmittedBy = false;
                        isShortDescription = false;
                        isPhonetics = false;
                        isPersonalNotes = false;
                        isKeywords = false;
                        isDemos = false;
                        isLinks = false;
                        isTranscript = true;
                        continue;
                }

                IList<string> GetLinkInstructionSection(string line)
                {
                    var key = line.ParseH2();

                    if (!synopsis.LinksInstructions.ContainsKey(key))
                    {
                        synopsis.LinksInstructions.Add(key, new List<string>());
                    }

                    return synopsis.LinksInstructions[key];
                }

                if (isSubmittedBy)
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
                    synopsis.PersonalNotes.Add(new ContentEntry(line.ParseListItem()));
                }
                else if (isKeywords)
                {
                    synopsis.Keywords = TermMaker.MakeKeywords(line);
                }
                else if (isDemos)
                {
                    synopsis.Demos.Add(new ContentEntry(line.ParseListItem()));
                }
                else if (isLinks)
                {
                    currentLinksSection.Add(line.ParseListItem().ParseLink());
                }
                else if (isTranscript)
                {
                    if (string.IsNullOrEmpty(line))
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
                        synopsis.TranscriptLines.Add(TranscriptLine.GetEntry(line));
                    }
                }
            }

            synopsis.Authors = TermMaker.MakeAuthors(
                authorNames,
                emails,
                githubs,
                twitters);

            synopsis.ShortDescription = shortDescription.ToString().Trim();
            synopsis.FileName = Path.GetFileNameWithoutExtension(uri.LocalPath);

            return synopsis;
        }

        public static GlossaryFile PrepareNewSynopsis(
            Synopsis synopsis,
            Synopsis oldSynopsis,
            ILogger log)
        {
            var file = new GlossaryFile
            {
                Path = string.Format(SaveToGitHubPathMask, synopsis.FileName)
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