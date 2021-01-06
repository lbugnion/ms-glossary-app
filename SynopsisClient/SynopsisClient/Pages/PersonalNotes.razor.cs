using MsGlossaryApp.DataModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SynopsisClient.Pages
{
    public partial class PersonalNotes
    {
        private bool _showConfirmDeleteNoteDialog;
        private bool _showConfirmReloadFromCloudDialog;

        protected override async Task OnInitializedAsync()
        {
            Console.WriteLine("Authors.OnInitializedAsync");
            await Handler.InitializePage();
        }

        private void Delete(Note note)
        {
            Console.WriteLine("In Delete");
            SelectedNote = note;
            _showConfirmDeleteNoteDialog = true;
        }

        public Note SelectedNote
        {
            get;
            private set;
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

        private void ReloadFromCloud()
        {
            Console.WriteLine("In ReloadFromCloud");
            _showConfirmReloadFromCloudDialog = true;
        }

        private async Task ReloadFromCloudConfirmationOkCancelClicked(bool confirm)
        {
            _showConfirmReloadFromCloudDialog = false;

            if (confirm)
            {
                await Handler.ReloadFromCloud();
            }
        }
    }
}
