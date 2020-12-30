using System;
using System.Collections.Generic;

namespace MsGlossaryApp.DataModel
{
    public class Term
    {
        public IList<string> AuthorsInstructions { get; set; }

        public IList<Author> Authors { get; set; }

        public IList<Language> Captions { get; set; }

        public IList<string> PersonalNotesInstructions { get; set; }

        public IList<string> PersonalNotes { get; set; }

        public IList<string> KeywordsInstructions { get; set; }

        public IList<string> Keywords { get; set; }

        public Language Language { get; set; }

        public Dictionary<string, IList<string>> LinksInstructions { get; set; }

        public Dictionary<string, IList<Link>> Links { get; set; }

        public bool MustSave { get; set; }

        public DateTime RecordingDate { get; set; }

        public string SafeFileName { get; set; }

        public IList<string> ShortDescriptionInstructions { get; set; }

        public string ShortDescription { get; set; }

        public TermStage Stage
        {
            get;
            set;
        }

        public IList<string> TitleInstructions { get; set; }

        public string Title { get; set; }

        public IList<string> TranscriptInstructions { get; set; }

        public string Transcript { get; set; }

        public Uri Uri { get; set; }

        public string YouTubeCode { get; set; }

        public IList<string> PhoneticsInstructions { get; set; }

        public string Phonetics { get; set; }

        public IList<string> Demos { get; set; }

        public IList<string> DemosInstructions { get; set; }

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

            if (!string.IsNullOrEmpty(YouTubeCode)
                && (Captions == null
                    || Captions.Count == 0))
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
                && Uri != null;
        }
    }
}