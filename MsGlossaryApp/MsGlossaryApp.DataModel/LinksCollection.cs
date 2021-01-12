using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace MsGlossaryApp.DataModel
{
    public class LinksCollection : LinksCollectionBase
    {
        public LinksCollection(string synopsisTitle, string termTitle)
            : base(synopsisTitle, termTitle)
        { 
        }

        [Required]
        [MinLength(1, ErrorMessage = "Enter at least one link for Docs and Learn")]
        public override IList<Link> Links
        {
            get;
            set;
        }

        //public override bool Equals(object obj)
        //{
        //    var collection = obj as LinksCollection;

        //    if (collection == null
        //        || SynopsisTitle != collection.SynopsisTitle
        //        || TermTitle != collection.TermTitle)
        //    {
        //        return false;
        //    }

        //    if (Links.Count != collection.Links.Count)
        //    {
        //        return false;
        //    }

        //    for (var index = 0; index < Links.Count; index++)
        //    {
        //        var link1 = Links.ElementAt(index);
        //        var link2 = collection.Links.ElementAt(index);

        //        if (!link1.Equals(link2))
        //        {
        //            return false;
        //        }
        //    }

        //    return true;

        //    //return obj is LinksCollection collection
        //    //    && SynopsisTitle == collection.SynopsisTitle
        //    //    && TermTitle == collection.TermTitle
        //    //    && EqualityComparer<IList<Link>>.Default.Equals(Links, collection.Links);
        //}

        //public override int GetHashCode()
        //{
        //    return HashCode.Combine(SynopsisTitle, TermTitle, Links);
        //}
    }
}