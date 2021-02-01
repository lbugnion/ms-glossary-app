namespace MsGlossaryApp.DataModel.Pass
{
    public class PassResult
    {
        public string ErrorMessage
        {
            get;
            set;
        }

        public bool PassOk
        {
            get;
            set;
        }

        public bool IsFirstLogin
        {
            get;
            set;
        }
    }
}
