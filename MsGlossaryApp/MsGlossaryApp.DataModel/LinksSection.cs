using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MsGlossaryApp.DataModel
{
    public class LinksSection
    {
        [Required]
        public LinksCollection LinksToDocs
        {
            get;
            set;
        }

        [Required]
        public LinksCollection LinksToLearn
        {
            get;
            set;
        }

        [Required]
        public LinksCollectionOptional LinksToOthers
        {
            get;
            set;
        }

        public LinksSection()
        {
            LinksToDocs = new LinksCollection(
                Constants.SynopsisMarkdownMarkers.LinksToDocsMarker.ParseH2(),
                Constants.TermMarkdownMarkers.LinksToDocsMarker.ParseH3());
            LinksToLearn = new LinksCollection(
                Constants.SynopsisMarkdownMarkers.LinksToLearnMarker.ParseH2(),
                Constants.TermMarkdownMarkers.LinksToLearnMarker.ParseH3());
            LinksToOthers = new LinksCollectionOptional(
                Constants.SynopsisMarkdownMarkers.LinksToOthersMarker.ParseH2(),
                Constants.TermMarkdownMarkers.LinksToOthersMarker.ParseH3());
        }

        public override bool Equals(object obj)
        {
            return obj is LinksSection section 
                && EqualityComparer<LinksCollection>.Default.Equals(LinksToDocs, section.LinksToDocs) 
                && EqualityComparer<LinksCollection>.Default.Equals(LinksToLearn, section.LinksToLearn) 
                && EqualityComparer<LinksCollectionOptional>.Default.Equals(LinksToOthers, section.LinksToOthers);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(LinksToDocs, LinksToLearn, LinksToOthers);
        }
    }
}