using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace MsGlossaryApp.DataModel
{
    public class TranscriptNote : TranscriptLine
    {
        private const string DefaultText = "Enter a production note to help you create the video";

        public override string Markdown
        {
            get
            {
                return Note.MakeNote();
            }

            set
            {
                Note = value.ParseNote();
            }
        }

        [Required]
        [JsonIgnore]
        public string Note
        {
            get;
            set;
        }

        public TranscriptNote()
        {
            Note = DefaultText;
        }

        public override bool Equals(object obj)
        {
            return obj is TranscriptNote note 
                && Note == note.Note;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Note);
        }
    }
}