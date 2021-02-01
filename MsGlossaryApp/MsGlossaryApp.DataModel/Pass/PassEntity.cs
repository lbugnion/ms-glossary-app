using Microsoft.Azure.Cosmos.Table;

namespace MsGlossaryApp.Model.Pass
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
    }
}