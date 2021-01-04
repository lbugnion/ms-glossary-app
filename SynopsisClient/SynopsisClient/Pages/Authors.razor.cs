using Microsoft.AspNetCore.Components.Forms;
using MsGlossaryApp.DataModel;
using System;
using System.Threading.Tasks;

namespace SynopsisClient.Pages
{
    public partial class Authors
    {
        private Term _synopsis;
        private EditContext _editContext;

        protected override async Task OnInitializedAsync()
        {
            Console.WriteLine("In OnInitializedAsync");
            _synopsis = await Handler.GetSynopsis(false);
            _editContext = new EditContext(_synopsis);
            _editContext.OnFieldChanged += EditContextOnFieldChanged;
        }

        private async void EditContextOnFieldChanged(object sender, FieldChangedEventArgs args)
        {
            Console.WriteLine("Field has changed: " + args.FieldIdentifier.FieldName);
            await Handler.SaveSynopsisLocally(_synopsis);
        }

        private void Delete(Author author)
        {
            // TODO Add confirmation dialog

            try
            {
                _synopsis.Authors.Remove(author);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
