using MsGlossaryApp.DataModel;
using System;
using System.Threading.Tasks;

namespace SynopsisClient.Pages
{
    public partial class Transcript
    {
        protected override async Task OnInitializedAsync()
        {
            Console.WriteLine("Title.OnInitializedAsync");
            await UserManager.CheckLogin();

            if (!UserManager.IsLoggedIn)
            {
                Console.WriteLine("not logged in");
                Nav.NavigateTo("/");
                return;
            }

            var success = await Handler.InitializePage();

            if (!success)
            {
                Nav.NavigateTo("/");
            }
        }

        private void Delete(TranscriptLine line)
        {
            Console.WriteLine($"Deleting line {line.Markdown}");

            if (Handler.Synopsis.TranscriptLines.Contains(line))
            {
                Handler.Synopsis.TranscriptLines.Remove(line);
            }
        }

        private void AddLineAfter(TranscriptLine line)
        {
            if (line == null)
            {
                Handler.Synopsis.TranscriptLines.Insert(0, new TranscriptSimpleLine());
                return;
            }

            if (Handler.Synopsis.TranscriptLines.Contains(line))
            {
                var index = Handler.Synopsis.TranscriptLines.IndexOf(line);
                Handler.Synopsis.TranscriptLines.Insert(index + 1, new TranscriptSimpleLine());
            }
        }

        private void AddImageAfter(TranscriptLine line)
        {
            if (line == null)
            {
                Handler.Synopsis.TranscriptLines.Insert(0, new TranscriptImage());
                return;
            }

            if (Handler.Synopsis.TranscriptLines.Contains(line))
            {
                var index = Handler.Synopsis.TranscriptLines.IndexOf(line);
                Handler.Synopsis.TranscriptLines.Insert(index + 1, new TranscriptImage());
            }
        }

        private void AddNoteAfter(TranscriptLine line)
        {
            if (line == null)
            {
                Handler.Synopsis.TranscriptLines.Insert(0, new TranscriptNote()); ;
                return;
            }

            if (Handler.Synopsis.TranscriptLines.Contains(line))
            {
                var index = Handler.Synopsis.TranscriptLines.IndexOf(line);
                Handler.Synopsis.TranscriptLines.Insert(index + 1, new TranscriptNote());
            }
        }
    }
}