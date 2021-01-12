using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MsGlossaryApp.DataModel
{
    public class LinksCollectionOptional : LinksCollectionBase
    {
        public LinksCollectionOptional(string synopsisTitle, string termTitle)
            : base(synopsisTitle, termTitle)
        {
        }

        /// <summary>
        /// Required but may be empty.
        /// </summary>
        [Required]
        public override IList<Link> Links
        {
            get;
            set;
        }

        //public override bool Equals(object obj)
        //{
        //    return obj is LinksCollectionOptional optional 
        //        && SynopsisTitle == optional.SynopsisTitle
        //        && TermTitle == optional.TermTitle
        //        && EqualityComparer<IList<Link>>.Default.Equals(Links, optional.Links);
        //}

        //public override int GetHashCode()
        //{
        //    return HashCode.Combine(SynopsisTitle, TermTitle, Links, Links);
        //}
    }
}