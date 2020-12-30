using System;
using System.Collections.Generic;
using System.Linq;

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

        public bool IsEqualTo(Term term)
        {
            if (!IsListEqualTo(
                term.Authors.Select(a => (IEqual)a).ToList(), 
                Authors.Select(a => (IEqual)a).ToList()))
            {
                return false;
            }

            if (!IsStringsListEqualTo(term.AuthorsInstructions, AuthorsInstructions))
            {
                return false;
            }

            if ((term.Captions == null
                && Captions != null)
                || (term.Captions != null
                    && Captions == null))
            {
                return false;
            }

            if (Captions != null)
            {
                if (!IsListEqualTo(
                    term.Captions.Select(a => (IEqual)a).ToList(),
                    Captions.Select(a => (IEqual)a).ToList()))
                {
                    return false;
                }
            }

            if (!IsStringsListEqualTo(term.Demos, Demos))
            {
                return false;
            }

            if (!IsStringsListEqualTo(term.DemosInstructions, DemosInstructions))
            {
                return false;
            }

            if (!IsStringsListEqualTo(term.Keywords, Keywords))
            {
                return false;
            }

            if (!IsStringsListEqualTo(term.KeywordsInstructions, KeywordsInstructions))
            {
                return false;
            }

            if (term.Language != null
                && Language != null
                && !term.Language.IsEqualTo(Language))
            {
                return false;
            }

            if ((term.Links == null
                && Links != null)
                || (term.Links != null
                    && Links == null))
            {
                return false;
            }

            if (Links != null)
            {
                if (term.Links.Count != Links.Count)
                {
                    return false;
                }

                for (var index = 0; index < term.Links.Keys.Count; index++)
                {
                    var key1 = term.Links.Keys.ElementAt(index);
                    var key2 = Links.Keys.ElementAt(index);

                    if (key1 != key2)
                    {
                        return false;
                    }

                    if (!IsListEqualTo(
                        term.Links[key1].Select(a => (IEqual)a).ToList(),
                        Links[key2].Select(a => (IEqual)a).ToList()))
                    {
                        return false;
                    }
                }
            }

            if ((term.LinksInstructions == null
                && LinksInstructions != null)
                || (term.LinksInstructions != null
                    && LinksInstructions == null))
            {
                return false;
            }

            if (LinksInstructions != null)
            {
                if (term.LinksInstructions.Count != LinksInstructions.Count)
                {
                    return false;
                }

                for (var index = 0; index < term.LinksInstructions.Keys.Count; index++)
                {
                    var key1 = term.LinksInstructions.Keys.ElementAt(index);
                    var key2 = LinksInstructions.Keys.ElementAt(index);

                    if (key1 != key2)
                    {
                        return false;
                    }

                    if (!IsStringsListEqualTo(
                        term.LinksInstructions[key1],
                        LinksInstructions[key2]))
                    {
                        return false;
                    }
                }
            }

            if (!IsStringsListEqualTo(term.PersonalNotes, PersonalNotes))
            {
                return false;
            }

            if (!IsStringsListEqualTo(term.PersonalNotesInstructions, PersonalNotesInstructions))
            {
                return false;
            }

            if (term.Phonetics != Phonetics)
            {
                return false;
            }

            if (!IsStringsListEqualTo(term.PhoneticsInstructions, PhoneticsInstructions))
            {
                return false;
            }

            if (term.RecordingDate != RecordingDate)
            {
                return false;
            }

            if (term.ShortDescription != ShortDescription)
            {
                return false;
            }

            if (!IsStringsListEqualTo(term.ShortDescriptionInstructions, ShortDescriptionInstructions))
            {
                return false;
            }

            if (term.Title != Title)
            {
                return false;
            }

            if (!IsStringsListEqualTo(term.TitleInstructions, TitleInstructions))
            {
                return false;
            }

            if (term.Transcript != Transcript)
            {
                return false;
            }

            if (!IsStringsListEqualTo(term.TranscriptInstructions, TranscriptInstructions))
            {
                return false;
            }

            if (term.Uri != Uri)
            {
                return false;
            }

            if (term.YouTubeCode != YouTubeCode)
            {
                return false;
            }

            return true;
        }

        private bool IsStringsListEqualTo(IList<string> list1, IList<string> list2)
        {
            if ((list1 == null
                && list2 != null)
                || (list1 != null
                    && list2 == null))
            {
                return false;
            }

            if (list1 == null
                && list2 == null)
            {
                return true;
            }

            if (list1.Count != list2.Count)
            {
                return false;
            }

            for (var index = 0; index < list2.Count; index++)
            {
                if (list1[index] != list2[index])
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsListEqualTo(IList<IEqual> list1, IList<IEqual> list2)
        {
            if ((list1 == null
                && list2 != null)
                || (list1 != null
                    && list2 == null))
            {
                return false;
            }

            if (list1 == null
                && list2 == null)
            {
                return true;
            }

            if (list1.Count != list2.Count)
            {
                return false;
            }

            for (var index = 0; index < list2.Count; index++)
            {
                if (!list1[index].IsEqualTo(list2[index]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}