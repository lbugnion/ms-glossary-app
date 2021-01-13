using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace MsGlossaryApp.DataModel
{
    public class LinksCollectionOptional : LinksCollectionBase
    {
        public LinksCollectionOptional()
            : this("N/A", "N/A")
        {

        }

        public LinksCollectionOptional(string synopsisTitle, string termTitle)
            : base(synopsisTitle, termTitle)
        {
            Links = new List<Link>();
        }

        /// <summary>
        /// Required but may be empty.
        /// </summary>
        [Required]
        public IList<Link> Links
        {
            get;
            set;
        }

        public override bool Equals(object obj)
        {
            if (!base.Equals(obj))
            {
                return false;
            }

            var collection = obj as LinksCollection;

            if (Links.Count != collection.Links.Count)
            {
                return false;
            }

            for (var index = 0; index < Links.Count; index++)
            {
                var link1 = Links.ElementAt(index);
                var link2 = collection.Links.ElementAt(index);

                if (!link1.Equals(link2))
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), Links);
        }
    }
}