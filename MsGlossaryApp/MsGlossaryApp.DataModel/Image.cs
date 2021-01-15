using System;
using System.ComponentModel.DataAnnotations;

namespace MsGlossaryApp.DataModel
{
    public class Image
    {
        [Required]
        public string Title
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

        public override bool Equals(object obj)
        {
            return obj is Image image 
                && Title == image.Title 
                && Url == image.Url;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Title, Url);
        }

        public Image()
        {
            Title = "Image title";
            Url = "http://domain.com/image.jpg";
        }

        public string ToMarkdown()
        {
            var markdown = Title.MakeImage(Url);
            return markdown;
        }
    }
}