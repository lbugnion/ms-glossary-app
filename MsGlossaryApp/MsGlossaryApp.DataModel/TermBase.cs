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
        public string FileName { get; set; }

        [Required]
        public IList<ContentEntry> Keywords { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "There must be at least one link to Docs")]
        public IList<Link> LinksToDocs { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "There must be at least one link to Learn")]
        public IList<Link> LinksToLearn { get; set; }

        [Required]
        public IList<Link> LinksToOthers { get; set; }

        public bool MustSave { get; set; }

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
            LinksToDocs = new List<Link>();
            LinksToLearn = new List<Link>();
            LinksToOthers = new List<Link>();
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
            if (!(obj is TermBase term))
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

            if (!IsListEqualTo(term.LinksToDocs.Select(a => (object)a).ToList(),
                LinksToDocs.Select(a => (object)a).ToList()))
            {
                return false;
            }

            if (!IsListEqualTo(term.LinksToLearn.Select(a => (object)a).ToList(),
                LinksToLearn.Select(a => (object)a).ToList()))
            {
                return false;
            }

            if (!IsListEqualTo(term.LinksToOthers.Select(a => (object)a).ToList(),
                LinksToOthers.Select(a => (object)a).ToList()))
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
            HashCode hash = new HashCode();
            hash.Add(Authors);
            hash.Add(Keywords);
            hash.Add(LinksToDocs);
            hash.Add(LinksToLearn);
            hash.Add(LinksToOthers);
            hash.Add(MustSave);
            hash.Add(FileName);
            hash.Add(ShortDescription);
            hash.Add(Title);
            hash.Add(Transcript);
            hash.Add(Uri);
            hash.Add(Url);
            return hash.ToHashCode();
        }

        public override string ToString()
        {
            return Title;
        }
    }
}