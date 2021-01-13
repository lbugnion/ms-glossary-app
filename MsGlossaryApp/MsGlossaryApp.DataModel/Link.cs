using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace MsGlossaryApp.DataModel
{
    public class Link
    {
        /// <summary>
        /// Use this property for the Synopsis client.
        /// </summary>
        [JsonIgnore]
        [Required]
        [Url]
        public string EditingUrl
        {
            get
            {
                return Url;
            }
            set
            {
                if (Url != value)
                {
                    Text = Constants.SynopsisMarkdownMarkers.LinkNoText;
                    Url = value;
                }
            }
        }

        public string Note
        {
            get;
            set;
        }

        [Required]
        public string Text
        {
            get;
            set;
        }

        [Required]
        [Url]
        public string Url
        {
            get;
            set;
        }

        public Link()
        {
            EditingUrl = "http://domain.com/path";
        }

        public override bool Equals(object obj)
        {
            return obj is Link link
                && Note == link.Note
                && Text == link.Text
                && Url == link.Url;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Note, Text, Url);
        }

        public string ToMarkdown()
        {
            var markdown = Text.MakeLink(Url);

            if (!string.IsNullOrEmpty(Note))
            {
                markdown += " " + Note;
            }

            return markdown;
        }
    }
}