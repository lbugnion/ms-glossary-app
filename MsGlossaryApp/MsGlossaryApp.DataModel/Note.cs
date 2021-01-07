namespace MsGlossaryApp.DataModel
{
    public class Note
    {
        public string Content
        {
            get;
            set;
        }

        public Note(string content)
        {
            Content = content;
        }

        public Note()
        {
            Content = "Enter a note";
        }
    }
}