using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace MsGlossaryApp.DataModel
{
    public class TranscriptImage : TranscriptLine
    {
        [Required]
        [JsonIgnore]
        public Image Image
        {
            get;
            private set;
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

        public TranscriptImage()
        {
            Image = new Image();
        }

        public override bool Equals(object obj)
        {
            return obj is TranscriptImage image
                && Image.Equals(image.Image);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Image);
        }
    }
}