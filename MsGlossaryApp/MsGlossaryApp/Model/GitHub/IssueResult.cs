using System.Collections.Generic;

namespace MsGlossaryApp.Model.GitHub
{
    public class IssueResult : ErrorResult
    {
        public IList<IssueInfo> Issues
        {
            get;
            set;
        }
    }
}