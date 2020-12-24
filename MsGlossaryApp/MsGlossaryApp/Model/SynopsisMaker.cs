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
        public static Term CreateSynopsis(
            Uri uri, 
            string markdown, 
            ILogger log)
        {
            log?.LogInformationEx("In CreateSynopsis", LogVerbosity.Verbose);

            var synopsis = new Synopsis
            {
                Uri = uri
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
                if (line.StartsWith(Constants.SynopsisMarkdownMarkers.TranscriptMarker))
                {
                    isLinks = false;
                    isTranscript = true;
                    continue;
                }
            }
        }
    }
}
