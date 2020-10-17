namespace MsGlossaryApp.Model
{
    public class AuthorInformation
    {
        public AuthorInformation(string name, string email, string github, string twitter)
        {
            Name = name;
            Email = email;
            GitHub = github;
            Twitter = twitter;
        }

        public string Name
        {
            get;
            set;
        }

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

        public string Twitter
        {
            get;
            set;
        }
    }
}
