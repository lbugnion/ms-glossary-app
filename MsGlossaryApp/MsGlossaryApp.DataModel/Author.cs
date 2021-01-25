using System;
using System.ComponentModel.DataAnnotations;

namespace MsGlossaryApp.DataModel
{
    public class Author
    {
        [Required]
        [EmailAddress]
        public string Email
        {
            get;
            set;
        }

        [Required]
        public string GitHub
        {
            get;
            set;
        }

        [Required]
        public string Name
        {
            get;
            set;
        }

        [Required]
        public string Twitter
        {
            get;
            set;
        }

        public Author(string name, string email, string github, string twitter)
        {
            Name = name;
            Email = email;
            GitHub = github;
            Twitter = twitter;
        }

        public Author()
        {
            Name = "Enter a name";
            Email = "user@domain.com";
            GitHub = "GitHubName";
            Twitter = "TwitterName";
        }

        public override bool Equals(object obj)
        {
            return obj is Author author
                && Email == author.Email
                && GitHub == author.GitHub
                && Name == author.Name
                && Twitter == author.Twitter;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Email, GitHub, Name, Twitter);
        }
    }
}