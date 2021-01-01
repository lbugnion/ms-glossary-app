namespace MsGlossaryApp.DataModel
{
    public class Link : IEqual
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

        public bool IsEqualTo(IEqual other)
        {
            var link = other as Link;

            if (link == null)
            {
                return false;
            }

            return link.Text == Text
                && link.Url == Url
                && link.Note == Note;
        }

        public string ToMarkdown()
        {
            var markdown = Text.MakeLink(Url);

            if (!string.IsNullOrEmpty(Note))
            {
                markdown += " " + Note;
            }

            return markdown;
        }
    }
}
