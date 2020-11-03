using System;
using System.Collections.Generic;
using System.Text;

namespace MsGlossaryApp.Model
{
    public class GlossaryFileInfo
    {
        public string Content
        {
            get;
            set;
        }

        public string Path
        {
            get;
            set;
        }

        public string ErrorMessage
        {
            get;
            set;
        }

        public bool MustSave
        {
            get;
            set;
        }

        public override string ToString()
        {
            return $"{Path} | {MustSave}";
        }
    }
}
