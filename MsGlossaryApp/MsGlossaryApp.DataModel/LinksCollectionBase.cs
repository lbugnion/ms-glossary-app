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

        public LinksCollectionBase(string synopsisTitle, string termTitle)
        {
            SynopsisTitle = synopsisTitle;
            TermTitle = termTitle;
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

            return true;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(SynopsisTitle, TermTitle);
        }
    }
}