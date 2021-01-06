namespace MsGlossaryApp.DataModel
{
    public class Note
    {
        public Note(string content)
        {
            Content = content;
        }

        public Note()
        {
            Content = string.Empty;
        }

        public string Content
        {
            get;
            set;
        }
    }
}
