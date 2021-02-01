using Microsoft.Azure.Cosmos.Table;

namespace MsGlossaryApp.Model
{
    public class PassInfo : TableEntity
    {
        public string OldHash
        {
            get;
            set;
        }

        public string NewHash
        {
            get;
            set;
        }

        public string FileName
        {
            get;
            set;
        }
    }
}