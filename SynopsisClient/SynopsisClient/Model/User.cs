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
        public string SynopsisName
        {
            get;
            set;
        }

        public bool ForceLogout
        {
            get;
            set;
        }
    }
}