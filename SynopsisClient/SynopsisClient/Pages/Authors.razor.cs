using Microsoft.AspNetCore.Components.Forms;
using MsGlossaryApp.DataModel;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SynopsisClient.Pages
{
    public partial class Authors
    {
        private Synopsis _synopsis;
        private EditContext _editContext;

        protected override async Task OnInitializedAsync()
        {
            Console.WriteLine("In OnInitializedAsync");
            _synopsis = await Handler.GetSynopsis(false);
            _editContext = new EditContext(_synopsis);
            _editContext.OnValidationStateChanged += EditContextOnValidationStateChanged;
        }

        private async Task CheckSaveSynopsis()
        {
            Console.WriteLine("CheckSaveSynopsis");

            if (_editContext.IsModified()
                && _editContext.GetValidationMessages().Count() == 0)
            {
                Console.WriteLine("Must save");
                await Handler.SaveSynopsisLocally(_synopsis);
                _editContext.MarkAsUnmodified();
            }
        }

        private async void EditContextOnValidationStateChanged(
            object sender, 
            ValidationStateChangedEventArgs args)
        {
            await CheckSaveSynopsis();
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
