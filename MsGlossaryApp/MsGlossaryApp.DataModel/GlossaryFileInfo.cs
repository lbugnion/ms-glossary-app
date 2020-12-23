namespace MsGlossaryApp.DataModel
{
    public class GlossaryFile
    {
        public string Content
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

        public string Path
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