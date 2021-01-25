using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BlazorApp3.Pages
{
    public partial class List
    {
        private EditContext _editContext;
        private Term _synopsis = new Term
        {
            Authors = new List<Author>
            {
                new Author
                {
                    Name = "Laurent Bugnion"
                },
                new Author
                {
                    Name = "Scott Cate"
                }
            }
        };

        private void HandleValidSubmit()
        {
            Console.WriteLine("Submitted");
        }

        protected override void OnInitialized()
        {
            _editContext = new EditContext(_synopsis);
            _editContext.OnFieldChanged += EditContextOnFieldChanged;
        }

        private void EditContextOnFieldChanged(object sender, FieldChangedEventArgs args)
        {
            Console.WriteLine("Field has changed " + args.FieldIdentifier.FieldName);

            var author = (Author)args.FieldIdentifier.Model;
            Console.WriteLine("New value: " + author.Name);

            var synopsis = (Term)_editContext.Model;
            foreach (var a in synopsis.Authors)
            {
                Console.WriteLine("Name: " + a.Name);
            }
        }

        public class Term
        {
            public IList<Author> Authors
            {
                get;
                set;
            }
        }

        public class Author
        {
            [Required]
            [StringLength(20, ErrorMessage = "Name is too long")]
            public string Name
            {
                get;
                set;
            }
        }
    }
}
