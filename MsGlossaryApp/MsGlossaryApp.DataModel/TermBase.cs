using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace MsGlossaryApp.DataModel
{
    public class TermBase
    {
        [Required]
        [MinLength(1, ErrorMessage = "There must be at least one author")]
        public IList<Author> Authors { get; set; }

        public IList<string> Keywords { get; set; }

        [Required]
        [MinLength(2, ErrorMessage = "You need to define links for Docs and Learn at least")]
        public Dictionary<string, IList<Link>> Links { get; set; }

        public bool MustSave { get; set; }

        [Required]
        public string SafeFileName { get; set; }

        [Required]
        public string ShortDescription { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Transcript { get; set; }

        [Required]
        public Uri Uri { get; set; }

        [Required]
        [Url]
        public string Url
        {
            get
            {
                return Uri?.ToString();
            }
        }

        protected bool IsListEqualTo(IList<object> list1, IList<object> list2)
        {
            if ((list1 == null
                && list2 != null)
                || (list1 != null
                    && list2 == null))
            {
                return false;
            }

            if (list1 == null
                && list2 == null)
            {
                return true;
            }

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
            if ((list1 == null
                && list2 != null)
                || (list1 != null
                    && list2 == null))
            {
                return false;
            }

            if (list1 == null
                && list2 == null)
            {
                return true;
            }

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

            if (!IsStringsListEqualTo(term.Keywords, Keywords))
            {
                return false;
            }

            if ((term.Links == null
                && Links != null)
                || (term.Links != null
                    && Links == null))
            {
                return false;
            }

            if (Links != null)
            {
                if (term.Links.Count != Links.Count)
                {
                    return false;
                }

                for (var index = 0; index < term.Links.Keys.Count; index++)
                {
                    var key1 = term.Links.Keys.ElementAt(index);
                    var key2 = Links.Keys.ElementAt(index);

                    if (key1 != key2)
                    {
                        return false;
                    }

                    if (!IsListEqualTo(
                        term.Links[key1].Select(a => (object)a).ToList(),
                        Links[key2].Select(a => (object)a).ToList()))
                    {
                        return false;
                    }
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
            }

            return true;
        }

        public override string ToString()
        {
            return Title;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Authors, Keywords, Links, SafeFileName, ShortDescription, Title, Transcript, Uri);
        }
    }
}
