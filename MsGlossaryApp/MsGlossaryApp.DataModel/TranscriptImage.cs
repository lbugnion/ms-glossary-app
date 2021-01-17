using Newtonsoft.Json;
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
    }
}