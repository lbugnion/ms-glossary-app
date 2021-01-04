using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MsGlossaryApp.DataModel
{
    public class Synopsis : TermBase
    {
        public IList<string> AuthorsInstructions { get; set; }

        public IList<string> Demos { get; set; }

        public IList<string> DemosInstructions { get; set; }

        public IList<string> KeywordsInstructions { get; set; }

        public Dictionary<string, IList<string>> LinksInstructions { get; set; }

        public IList<string> PersonalNotes { get; set; }

        public IList<string> PersonalNotesInstructions { get; set; }

        public string Phonetics { get; set; }

        public IList<string> PhoneticsInstructions { get; set; }

        public IList<string> ShortDescriptionInstructions { get; set; }

        public IList<string> TitleInstructions { get; set; }

        public IList<string> TranscriptInstructions { get; set; }

        public override bool Equals(object obj)
        {
            if (!base.Equals(obj))
            {
                return false;
            }

            var synopsis = obj as Synopsis;

            if (synopsis == null)
            {
                return false;
            }

            if (!IsStringsListEqualTo(synopsis.AuthorsInstructions, AuthorsInstructions))
            {
                return false;
            }

            if (!IsStringsListEqualTo(synopsis.Demos, Demos))
            {
                return false;
            }

            if (!IsStringsListEqualTo(synopsis.DemosInstructions, DemosInstructions))
            {
                return false;
            }

            if (!IsStringsListEqualTo(synopsis.KeywordsInstructions, KeywordsInstructions))
            {
                return false;
            }

            if ((synopsis.LinksInstructions == null
                && LinksInstructions != null)
                || (synopsis.LinksInstructions != null
                    && LinksInstructions == null))
            {
                return false;
            }

            if (LinksInstructions != null)
            {
                if (synopsis.LinksInstructions.Count != LinksInstructions.Count)
                {
                    return false;
                }

                for (var index = 0; index < synopsis.LinksInstructions.Keys.Count; index++)
                {
                    var key1 = synopsis.LinksInstructions.Keys.ElementAt(index);
                    var key2 = LinksInstructions.Keys.ElementAt(index);

                    if (key1 != key2)
                    {
                        return false;
                    }

                    if (!IsStringsListEqualTo(
                        synopsis.LinksInstructions[key1],
                        LinksInstructions[key2]))
                    {
                        return false;
                    }
                }
            }

            if (!IsStringsListEqualTo(synopsis.PersonalNotes, PersonalNotes))
            {
                return false;
            }

            if (!IsStringsListEqualTo(synopsis.PersonalNotesInstructions, PersonalNotesInstructions))
            {
                return false;
            }

            if (synopsis.Phonetics != Phonetics)
            {
                return false;
            }

            if (!IsStringsListEqualTo(synopsis.PhoneticsInstructions, PhoneticsInstructions))
            {
                return false;
            }

            if (!IsStringsListEqualTo(synopsis.ShortDescriptionInstructions, ShortDescriptionInstructions))
            {
                return false;
            }

            if (!IsStringsListEqualTo(synopsis.TitleInstructions, TitleInstructions))
            {
                return false;
            }

            if (!IsStringsListEqualTo(synopsis.TranscriptInstructions, TranscriptInstructions))
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Add(base.GetHashCode());
            hash.Add(Authors);
            hash.Add(Keywords);
            hash.Add(Links);
            hash.Add(SafeFileName);
            hash.Add(ShortDescription);
            hash.Add(Title);
            hash.Add(Transcript);
            hash.Add(Uri);
            hash.Add(AuthorsInstructions);
            hash.Add(Demos);
            hash.Add(DemosInstructions);
            hash.Add(KeywordsInstructions);
            hash.Add(LinksInstructions);
            hash.Add(PersonalNotes);
            hash.Add(PersonalNotesInstructions);
            hash.Add(Phonetics);
            hash.Add(PhoneticsInstructions);
            hash.Add(ShortDescriptionInstructions);
            hash.Add(TitleInstructions);
            hash.Add(TranscriptInstructions);
            return hash.ToHashCode();
        }
    }
}
