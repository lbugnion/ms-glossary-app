using System;
using System.Collections.Generic;

namespace MsGlossaryApp.DataModel
{
    public class Term : GlossaryItem
    {
        public IList<Author> Authors { get; set; }

        public IList<Language> Captions { get; set; }

        public Language Language { get; set; }

        public DateTime RecordingDate { get; set; }

        public Uri Uri { get; set; }

        public string YouTubeCode { get; set; }

        public override string ToString()
        {
            return Title;
        }
    }
}