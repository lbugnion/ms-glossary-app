using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace MsGlossaryApp.DataModel
{
    public class Term : TermBase
    {
        public IList<Language> Captions { get; set; }

        [Required]
        public Language Language { get; set; }

        [Required]
        public DateTime RecordingDate { get; set; }

        public string YouTubeCode { get; set; }

        public override bool Equals(object obj)
        {
            if (!base.Equals(obj))
            {
                return false;
            }

            var term = obj as Term;

            if (term == null)
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
                    term.Captions.Select(a => (object)a).ToList(),
                    Captions.Select(a => (object)a).ToList()))
                {
                    return false;
                }
            }

            if (term.Language != null
                && Language != null
                && !term.Language.Equals(Language))
            {
                return false;
            }

            if (term.RecordingDate != RecordingDate)
            {
                return false;
            }

            if (term.YouTubeCode != YouTubeCode)
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
            hash.Add(Captions);
            hash.Add(Language);
            hash.Add(RecordingDate);
            hash.Add(YouTubeCode);
            return hash.ToHashCode();
        }
    }
}