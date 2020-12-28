﻿namespace MsGlossaryApp.DataModel
{
    public class Author
    {
        public string Email
        {
            get;
            set;
        }

        public string GitHub
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public string Twitter
        {
            get;
            set;
        }
        
        public bool IsComplete
        {
            get
            {
                return !string.IsNullOrEmpty(Email)
                    && !string.IsNullOrEmpty(GitHub)
                    && !string.IsNullOrEmpty(Name)
                    && !string.IsNullOrEmpty(Twitter);
            }
        }

        public Author(string name, string email, string github, string twitter)
        {
            Name = name;
            Email = email;
            GitHub = github;
            Twitter = twitter;
        }
    }
}