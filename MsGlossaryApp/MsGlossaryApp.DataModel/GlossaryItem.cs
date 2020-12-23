using System.Collections.Generic;

namespace MsGlossaryApp.DataModel
{
    public class GlossaryItem
    {
        public string ShortDescription { get; set; }

        public IList<string> Keywords { get; set; }

        public Dictionary<string, IList<string>> Links { get; set; }

        public bool MustSave { get; set; }

        public string SafeFileName { get; set; }

        public string Title { get; set; }

        public string Transcript { get; set; }
    }
}
