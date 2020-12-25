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

            var synopsis = new Term
            {
                Uri = uri,
                Stage = Term.TermStage.Synopsis
            };

            log?.LogInformationEx($"Synopsis: {synopsis.Uri}", LogVerbosity.Verbose);

            var markdownReader = new StringReader(markdown);

            string keywordsLine = null;
            string title = null;
            string shortDescription = null;
            string authorName = null;
            string email = null;
            string twitter = null;
            string github = null;
            var isTranscript = false;
            var isLinks = false;
            var transcript = new StringBuilder();
            var links = new Dictionary<string, IList<string>>();
            IList<string> currentLinksSection = null;
            string line;

            while ((line = markdownReader.ReadLine()) != null)
            {
                line = line.Trim();

                if (line.IsNote()
                    || string.IsNullOrEmpty(line))
                {
                    // Ignore the notes, we will make sure to preserve them when 
                    // we save the Synopsis back to GitHub
                    continue;
                }

                if (line.StartsWith(Constants.SynopsisMarkdownMarkers.TranscriptMarker))
                {
                    isLinks = false;
                    isTranscript = true;
                    continue;
                }
                else if (line.StartsWith(Constants.SynopsisMarkdownMarkers.LinksToDocsMarker)
                    || line.StartsWith(Constants.SynopsisMarkdownMarkers.LinksToLearnMarker)
                    || (line.IsH2()
                        && line.ToLower().Contains("links")))
                {
                    isLinks = true;
                    isTranscript = false;

                    line = line.ParseH2();

                    if (!links.ContainsKey(line))
                    {
                        links.Add(line, new List<string>());
                    }

                    currentLinksSection = links[line];
                    continue;
                }
                else if (isTranscript)
                {
                    transcript.AppendLine(line);
                }
                else if (isLinks)
                {
                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }

                    currentLinksSection.Add(line.ParseListItem());
                }
            }

            return synopsis;
        }
    }
}
