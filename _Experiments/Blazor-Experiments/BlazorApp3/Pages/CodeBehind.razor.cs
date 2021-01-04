using Microsoft.AspNetCore.Components.Forms;
using System;
using System.ComponentModel.DataAnnotations;

namespace BlazorApp3.Pages
{
    public partial class CodeBehind
    {
        private EditContext _editContext;
        private Example _example = new Example
        {
            Name = "Laurent Bugnion"
        };

        private void HandleValidSubmit()
        {
            Console.WriteLine("Submitted");
        }

        protected override void OnInitialized()
        {
            _editContext = new EditContext(_example);
            _editContext.OnFieldChanged += EditContextOnFieldChanged;
        }

        private void EditContextOnFieldChanged(object sender, FieldChangedEventArgs args)
        {
            Console.WriteLine("Field has changed " + args.FieldIdentifier.FieldName);

            var example = (Example)args.FieldIdentifier.Model;

            Console.WriteLine("New value: " + example.Name);
        }

        public class Example
        {
            [Required]
            [StringLength(10, ErrorMessage = "Name is too long.")]
            public string Name
            {
                get;
                set;
            }
        }
    }
}
