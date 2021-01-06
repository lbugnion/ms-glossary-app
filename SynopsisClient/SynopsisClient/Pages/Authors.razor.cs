using MsGlossaryApp.DataModel;
using System;
using System.Threading.Tasks;

namespace SynopsisClient.Pages
{
    public partial class Authors
    {
        private bool _showConfirmDeleteAuthorDialog;
        private bool _showConfirmReloadFromCloudDialog;

        protected override async Task OnInitializedAsync()
        {
            Console.WriteLine("Authors.OnInitializedAsync");
            await Handler.InitializePage();
        }

        private void Delete(Author author)
        {
            Console.WriteLine("In Delete");
            SelectedAuthor = author;
            _showConfirmDeleteAuthorDialog = true;
        }

        public Author SelectedAuthor
        { 
            get; 
            private set; 
        }

        private void DeleteAuthorConfirmationOkCancelClicked(bool confirm)
        {
            _showConfirmDeleteAuthorDialog = false;

            if (!confirm
                || SelectedAuthor == null)
            {
                return;
            }

            Console.WriteLine("Execute DeleteAuthor");

            Handler.DeleteAuthor(SelectedAuthor);
            Handler.TriggerValidation();
            SelectedAuthor = null;
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
