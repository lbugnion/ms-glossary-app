namespace MsGlossaryApp.DataModel
{
    public class Link
    {
        public string Text
        {
            get;
            set;
        }

        public string Url
        {
            get;
            set;
        }

        public string Note
        {
            get;
            set;
        }

        public string ToMarkdown()
        {
            return Text.MakeLink(Url);
        }
    }
}
