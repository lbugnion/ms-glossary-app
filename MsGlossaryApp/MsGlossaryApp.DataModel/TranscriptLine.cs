using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace MsGlossaryApp.DataModel
{
    public class TranscriptLine
    {
        public virtual string Markdown
        {
            get;
            set;
        }

        public static TranscriptLine GetEntry(string line)
        {
            if (line.IsNote())
            {
                var note = new TranscriptNote
                {
                    Markdown = line
                };
                return note;
            }

            if (line.IsImage())
            {
                var image = new TranscriptImage
                {
                    Markdown = line
                };
                return image;
            }

            var simpleLine = new TranscriptSimpleLine
            {
                Markdown = line
            };
            return simpleLine;
        }
    }

    public class TranscriptImage : TranscriptLine
    {
        [Required]
        [JsonIgnore]
        public Image Image
        {
            get;
            private set;
        }

        public TranscriptImage()
        {
            Image = new Image();
        }

        public override string Markdown
        {
            get
            {
                return Image.ToMarkdown();
            }

            set
            {
                Image = value.ParseImage();
            }
        }
    }

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

        public TranscriptSimpleLine()
        {
            Line = DefaultText;
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
    }

    public class TranscriptNote : TranscriptLine
    {
        private const string DefaultText = "Enter a production note to help you create the video";

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
    }
}
