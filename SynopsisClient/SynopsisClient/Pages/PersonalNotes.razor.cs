using MsGlossaryApp.DataModel;
using System;
using System.Threading.Tasks;

namespace SynopsisClient.Pages
{
    public partial class PersonalNotes
    {
        private bool _showConfirmDeleteNoteDialog;

        public Note SelectedNote
        {
            get;
            private set;
        }

        private void Delete(Note note)
        {
            Console.WriteLine("In Delete");
            SelectedNote = note;
            _showConfirmDeleteNoteDialog = true;
        }

        private void DeleteNoteConfirmationOkCancelClicked(bool confirm)
        {
            _showConfirmDeleteNoteDialog = false;

            if (!confirm
                || SelectedNote == null)
            {
                return;
            }

            Console.WriteLine("Execute DeleteNote");

            Handler.DeleteNote(SelectedNote);
            SelectedNote = null;
        }

        protected override async Task OnInitializedAsync()
        {
            Console.WriteLine("Authors.OnInitializedAsync");
            await Handler.InitializePage();
        }
    }
}