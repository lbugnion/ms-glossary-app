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

        public Term()
        {
            Captions = new List<Language>();
        }

        public override bool Equals(object obj)
        {
            if (!base.Equals(obj))
            {
                return false;
            }

            if (!(obj is Term term))
            {
                return false;
            }

            if (!IsListEqualTo(
                term.Captions.Select(a => (object)a).ToList(),
                Captions.Select(a => (object)a).ToList()))
            {
                return false;
            }

            if (!term.Language.Equals(Language))
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
            hash.Add(MustSave);
            hash.Add(Captions);
            hash.Add(Language);
            hash.Add(RecordingDate);
            hash.Add(YouTubeCode);
            return hash.ToHashCode();
        }
    }
}