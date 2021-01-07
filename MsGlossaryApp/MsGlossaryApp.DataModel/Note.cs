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
            Content = "Enter a note";
        }

        public string Content
        {
            get;
            set;
        }
    }
}
