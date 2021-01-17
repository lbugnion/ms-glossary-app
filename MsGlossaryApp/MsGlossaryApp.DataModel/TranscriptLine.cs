namespace MsGlossaryApp.DataModel
{
    public class TranscriptLine
    {
        public virtual string Markdown
        {
            get;
            set;
        }

        public static TranscriptLine GetEntry(string line)
        {
            if (line.IsNote())
            {
                var note = new TranscriptNote
                {
                    Markdown = line
                };
                return note;
            }

            if (line.IsImage())
            {
                var image = new TranscriptImage
                {
                    Markdown = line
                };
                return image;
            }

            var simpleLine = new TranscriptSimpleLine
            {
                Markdown = line
            };
            return simpleLine;
        }
    }
}