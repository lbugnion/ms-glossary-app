using MsGlossaryApp.DataModel;
using System;
using System.Threading.Tasks;

namespace SynopsisClient.Pages
{
    public partial class Authors
    {
        public bool _showConfirmDeleteAuthorDialog;
        public bool _showNoAuthorsWarning;

        protected override async Task OnInitializedAsync()
        {
            Console.WriteLine("Authors.OnInitializedAsync");
            await Handler.InitializePage();

            if (Handler.Synopsis.Authors.Count == 0)
            {
                _showNoAuthorsWarning = true;
            }
        }

        private void Delete(Author author)
        {
            SelectedAuthor = author;
            _showConfirmDeleteAuthorDialog = true;
        }

        public Author SelectedAuthor
        { 
            get; 
            private set; 
        }

        public async Task SaveAuthorConfirmationOkCancelClicked(bool confirm)
        {
            _showConfirmDeleteAuthorDialog = false;

            if (!confirm)
            {
                return;
            }

            Console.WriteLine("ExecuteDeleteAuthor");

            Handler.Synopsis.Authors.Remove(SelectedAuthor);
            Handler.TriggerValidation();

            if (Handler.Synopsis.Authors.Count == 0)
            {
                Console.WriteLine("No remaining authors");
                _showNoAuthorsWarning = true;
            }
            else
            {
                Console.WriteLine("More remaining authors");
                // TODO Save?
            }
        }
    }
}
