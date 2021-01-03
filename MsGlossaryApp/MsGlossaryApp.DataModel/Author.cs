namespace MsGlossaryApp.DataModel
{
    public class Author : IEqual
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

        public Author(string name, string email, string github, string twitter)
        {
            Name = name;
            Email = email;
            GitHub = github;
            Twitter = twitter;
        }

        public Author()
        {

        }

        public bool IsEqualTo(IEqual other)
        {
            var author = other as Author;

            if (author == null)
            {
                return false;
            }

            return author.Name == Name
                && author.Email == Email
                && author.GitHub == GitHub
                && author.Twitter == Twitter;
        }
    }
}