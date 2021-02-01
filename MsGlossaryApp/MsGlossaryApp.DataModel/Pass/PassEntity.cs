using Microsoft.Azure.Cosmos.Table;

namespace MsGlossaryApp.DataModel.Pass
{
    public class PassEntity : TableEntity
    {
        public string Hash
        {
            get;
            set;
        }

        public string FileName
        {
            get;
            set;
        }

        public bool FirstLogin
        {
            get;
            set;
        }
    }
}