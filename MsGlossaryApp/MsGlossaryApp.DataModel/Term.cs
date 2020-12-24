using System;
using System.Collections.Generic;

namespace MsGlossaryApp.DataModel
{
    public class Term
    {
        public IList<Author> Authors { get; set; }

        public IList<Language> Captions { get; set; }

        public IList<string> Keywords { get; set; }

        public Language Language { get; set; }

        public Dictionary<string, IList<string>> Links { get; set; }

        public bool MustSave { get; set; }

        public DateTime RecordingDate { get; set; }

        public string SafeFileName { get; set; }

        public string ShortDescription { get; set; }

        public TermStage Stage
        {
            get;
            set;
        }

        public string Title { get; set; }

        public string Transcript { get; set; }

        public Uri Uri { get; set; }

        public string YouTubeCode { get; set; }

        public override string ToString()
        {
            return Title;
        }

        public enum TermStage
        {
            Synopsis,
            Ready
        }

        public bool CheckIsComplete()
        {
            if (Authors == null
                || Authors.Count == 0)
            {
                return false;
            }

            foreach (Author author in Authors)
            {
                if (!author.IsComplete)
                {
                    return false;
                }
            }

            if (Captions == null
                || Captions.Count == 0)
            {
                return false;
            }

            return Keywords != null
                && Keywords.Count > 0
                && Language != null
                && Language.IsComplete
                && Links != null
                && Links.Count >= 2
                && RecordingDate > DateTime.MinValue
                && !string.IsNullOrEmpty(SafeFileName)
                && !string.IsNullOrEmpty(ShortDescription)
                && !string.IsNullOrEmpty(Title)
                && !string.IsNullOrEmpty(Transcript)
                && Uri != null
                && !string.IsNullOrEmpty(YouTubeCode);
        }
    }
}