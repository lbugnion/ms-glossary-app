using Microsoft.AspNetCore.Components.Forms;
using MsGlossaryApp.DataModel;
using SynopsisClient.Shared;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SynopsisClient.Pages
{
    public partial class Authors
    {
        private Synopsis _synopsis;
        private EditContext _editContext;
        private bool _cannotSave;
        private bool _cannotCommit;
        private bool _cannotSubmit;

        protected override async Task OnInitializedAsync()
        {
            Console.WriteLine("In OnInitializedAsync");

            _synopsis = await Handler.GetSynopsis(false);
            _cannotSave = true;
            _cannotCommit = false;
            _cannotSubmit = false;
            _editContext = new EditContext(_synopsis);
            _editContext.OnValidationStateChanged += EditContextOnValidationStateChanged;
            _editContext.OnFieldChanged += EditContextOnFieldChanged;
        }

        private void EditContextOnFieldChanged(object sender, FieldChangedEventArgs e)
        {
            Console.WriteLine("EditContextOnFieldChanged");
            _cannotSave = false;
        }

        private void EditContextOnValidationStateChanged(
            object sender, 
            ValidationStateChangedEventArgs e)
        {
            Console.WriteLine("EditContextOnValidationStateChanged");

            if (_editContext.GetValidationMessages().Count() == 0)
            {
                Console.WriteLine("can save");
                _cannotSave = false;
            }
            else
            {
                Console.WriteLine("cannot save");
                _cannotSave = true;
            }
        }

        private async Task CheckSaveSynopsis()
        {
            Console.WriteLine("CheckSaveSynopsis");

            if (_editContext.IsModified()
                && !_cannotSave)
            {
                Console.WriteLine("Must save");
                await Handler.SaveSynopsisLocally(_synopsis);
                _editContext.MarkAsUnmodified();
                _cannotSave = true;
            }
        }

        //private async Task CheckCommitSynopsis()
        //{
        //    Console.WriteLine("CheckCommitSynopsis");

        //    if (_canSave)
        //    {
        //        Console.WriteLine("Must Commit");
        //        // TODO Commit
        //    }
        //}

        //private async Task CheckSubmitSynopsis()
        //{
        //    Console.WriteLine("CheckSubmitSynopsis");

        //    if (_canSave)
        //    {
        //        Console.WriteLine("Must Submit");
        //        // TODO Submit
        //    }
        //}

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
