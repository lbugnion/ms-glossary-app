using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace MsGlossaryApp.DataModel
{
    public class Synopsis : TermBase
    {
        [Required]
        public IList<string> AuthorsInstructions { get; set; }

        [Required]
        public IList<ContentEntry> Demos { get; set; }

        [Required]
        public IList<string> DemosInstructions { get; set; }

        [Required]
        public IList<string> KeywordsInstructions { get; set; }

        [Required]
        public Dictionary<string, IList<string>> LinksInstructions { get; set; }

        [Required]
        public IList<ContentEntry> PersonalNotes { get; set; }

        [Required]
        public IList<string> PersonalNotesInstructions { get; set; }

        [Required]
        public string Phonetics { get; set; }

        [Required]
        public IList<string> PhoneticsInstructions { get; set; }

        [Required]
        public IList<string> ShortDescriptionInstructions { get; set; }

        [Required]
        public IList<string> TitleInstructions { get; set; }

        [Required]
        public IList<string> TranscriptInstructions { get; set; }

        public Synopsis()
        {
            AuthorsInstructions = new List<string>();
            Demos = new List<ContentEntry>();
            DemosInstructions = new List<string>();
            KeywordsInstructions = new List<string>();
            LinksInstructions = new Dictionary<string, IList<string>>();
            PersonalNotes = new List<ContentEntry>();
            PersonalNotesInstructions = new List<string>();
            PhoneticsInstructions = new List<string>();
            ShortDescriptionInstructions = new List<string>();
            TitleInstructions = new List<string>();
            TranscriptInstructions = new List<string>();
        }

        public override bool Equals(object obj)
        {
            if (!base.Equals(obj))
            {
                return false;
            }

            if (!(obj is Synopsis synopsis))
            {
                return false;
            }

            if (!IsStringsListEqualTo(synopsis.AuthorsInstructions, AuthorsInstructions))
            {
                return false;
            }

            if (!IsListEqualTo(
                synopsis.Demos.Select(d => (object)d).ToList(),
                Demos.Select(d => (object)d).ToList()))
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

            if (!IsListEqualTo(
                synopsis.PersonalNotes.Select(n => (object)n).ToList(),
                PersonalNotes.Select(n => (object)n).ToList()))
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
            hash.Add(LinksToDocs);
            hash.Add(LinksToLearn);
            hash.Add(LinksToOthers);
            hash.Add(MustSave);
            hash.Add(FileName);
            hash.Add(ShortDescription);
            hash.Add(Title);
            hash.Add(Transcript);
            hash.Add(Uri);
            hash.Add(Url);
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