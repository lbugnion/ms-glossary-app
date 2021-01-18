using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace MsGlossaryApp.DataModel
{
    public class TranscriptSimpleLine : TranscriptLine
    {
        private const string DefaultText = "Add one line of script";

        [Required]
        [JsonIgnore]
        public string Line
        {
            get;
            set;
        }

        public override string Markdown
        {
            get
            {
                return Line;
            }

            set
            {
                Line = value;
            }
        }

        public TranscriptSimpleLine()
        {
            Line = DefaultText;
        }

        public override bool Equals(object obj)
        {
            return obj is TranscriptSimpleLine line
                && Line == line.Line;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Line);
        }
    }
}