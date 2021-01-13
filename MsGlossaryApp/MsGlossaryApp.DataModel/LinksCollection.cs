using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MsGlossaryApp.DataModel
{
    public class LinksCollection : LinksCollectionBase
    {
        public LinksCollection()
            : base("N/A", "N/A")
        {

        }

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
    }
}