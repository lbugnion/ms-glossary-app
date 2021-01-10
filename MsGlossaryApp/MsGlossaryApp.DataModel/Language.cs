using System;
using System.ComponentModel.DataAnnotations;

namespace MsGlossaryApp.DataModel
{
    public class Language
    {
        [Required]
        public string Code { get; set; }

        [Required]
        public string LanguageName { get; set; }

        public override bool Equals(object obj)
        {
            return obj is Language language 
                && Code == language.Code 
                && LanguageName == language.LanguageName;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Code, LanguageName);
        }

        public override string ToString()
        {
            return $"{Code} / {LanguageName}";
        }
    }
}