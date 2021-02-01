using System.Collections.Generic;

namespace MsGlossaryApp.Model.GitHub
{
    public class ReleaseNotesResult : ErrorResult
    {
        public IList<ReleaseNotesPageInfo> CreatedPages
        {
            get;
            set;
        }
    }
}
