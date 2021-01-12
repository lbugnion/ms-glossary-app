using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace MsGlossaryApp.DataModel
{
    public class TermBase
    {
        [Required]
        [MinLength(1, ErrorMessage = "There must be at least one author")]
        public IList<Author> Authors { get; set; }

        [Required]
        public IList<ContentEntry> Keywords { get; set; }

        [Required]
        public LinksSection Links { get; set; }

        public bool MustSave { get; set; }

        [Required]
        public string FileName { get; set; }

        [Required]
        [MinLength(60, ErrorMessage = "The short description is too short")]
        [MaxLength(200, ErrorMessage = "The short description is too long")]
        public string ShortDescription { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "You need to define a script")]
        public string Transcript { get; set; }

        [Required]
        public Uri Uri { get; set; }

        [Required]
        [Url]
        [JsonIgnore]
        public string Url
        {
            get
            {
                return Uri?.ToString();
            }
        }

        public TermBase()
        {
            Authors = new List<Author>();
            Keywords = new List<ContentEntry>();
            Links = new LinksSection();
        }

        protected bool IsListEqualTo(IList<object> list1, IList<object> list2)
        {
            if (list1.Count != list2.Count)
            {
                return false;
            }

            for (var index = 0; index < list2.Count; index++)
            {
                if (!list1[index].Equals(list2[index]))
                {
                    return false;
                }
            }

            return true;
        }

        protected bool IsStringsListEqualTo(IList<string> list1, IList<string> list2)
        {
            if (list1.Count != list2.Count)
            {
                return false;
            }

            for (var index = 0; index < list2.Count; index++)
            {
                if (list1[index] != list2[index])
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            var term = obj as TermBase;

            if (term == null)
            {
                return false;
            }

            if (!IsListEqualTo(term.Authors.Select(a => (object)a).ToList(),
                Authors.Select(a => (object)a).ToList()))
            {
                return false;
            }

            if (!IsListEqualTo(term.Keywords.Select(k => (object)k).ToList(), 
                Keywords.Select(k => (object)k).ToList()))
            {
                return false;
            }

            if (!term.Links.Equals(Links))
            {
                return false;
            }

            if (term.ShortDescription != ShortDescription)
            {
                return false;
            }

            if (term.Title != Title)
            {
                return false;
            }

            if (term.Transcript != Transcript)
            {
                return false;
            }

            if (term.Uri != Uri)
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Authors, Keywords, Links, FileName, ShortDescription, Title, Transcript, Uri);
        }

        public override string ToString()
        {
            return Title;
        }
    }
}