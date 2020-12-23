using System;
using System.Collections.Generic;

namespace MsGlossaryApp.DataModel
{
    public class TermInformation
    {
        public IList<AuthorInformation> Authors { get; set; }

        public string Blurb { get; set; }

        public IList<LanguageInfo> Captions { get; set; }

        public IList<string> Keywords { get; set; }

        public LanguageInfo Language { get; set; }

        public Dictionary<string, IList<string>> Links { get; set; }

        public bool MustSave { get; set; }

        public DateTime RecordingDate { get; set; }

        public string TermName { get; set; }

        public string Title { get; set; }

        public string Transcript { get; set; }

        public Uri Uri { get; set; }

        public string YouTubeCode { get; set; }

        public override string ToString()
        {
            return Title;
        }
    }
}