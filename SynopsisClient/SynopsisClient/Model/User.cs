using System.ComponentModel.DataAnnotations;

namespace SynopsisClient.Model
{
    public class User
    {
        [Required]
        [EmailAddress]
        public string Email
        {
            get;
            set;
        }

        [Required]
        [MinLength(1)]
        public string SynopsisName
        {
            get;
            set;
        }
    }
}