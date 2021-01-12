using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace MsGlossaryApp.DataModel
{
    public abstract class LinksCollectionBase
    {
        [Required]
        public string SynopsisTitle
        {
            get;
            set;
        }

        [Required]
        public string TermTitle
        {
            get;
            set;
        }

        public abstract IList<Link> Links
        {
            get;
            set;
        }

        public LinksCollectionBase(string synopsisTitle, string termTitle)
        {
            SynopsisTitle = synopsisTitle;
            TermTitle = termTitle;
            Links = new List<Link>();
        }

        public override bool Equals(object obj)
        {
            var collection = obj as LinksCollectionBase;

            if (collection == null
                || SynopsisTitle != collection.SynopsisTitle
                || TermTitle != collection.TermTitle)
            {
                return false;
            }

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

            //return obj is LinksCollection collection
            //    && SynopsisTitle == collection.SynopsisTitle
            //    && TermTitle == collection.TermTitle
            //    && EqualityComparer<IList<Link>>.Default.Equals(Links, collection.Links);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(SynopsisTitle, TermTitle, Links);
        }
    }
}