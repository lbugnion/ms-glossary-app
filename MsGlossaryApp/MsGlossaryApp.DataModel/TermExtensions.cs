using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MsGlossaryApp.DataModel
{
    public static class TermExtensions
    {
        public static bool TryValidate(this Term term, IList<ValidationResult> results)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            var termBase = term as TermBase;

            var isValid = termBase.TryValidate(results);

            if (isValid)
            {
                var context = new ValidationContext(term);
                isValid = Validator.TryValidateObject(term, context, results, true);
            }

            if (isValid)
            {
                if (!string.IsNullOrEmpty(term.YouTubeCode))
                {
                    if (term.Captions == null
                        || term.Captions.Count < 1)
                    {
                        results.Add(new ValidationResult("You need at least one captioned language"));
                        return false;
                    }
                }
            }

            return isValid;
        }

        public static bool TryValidate(this TermBase term, IList<ValidationResult> results)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            var context = new ValidationContext(term);
            var isValid = Validator.TryValidateObject(term, context, results, true);

            if (isValid)
            {
                foreach (var author in term.Authors)
                {
                    context = new ValidationContext(author);
                    isValid = Validator.TryValidateObject(author, context, results, true);

                    if (!isValid)
                    {
                        break;
                    }
                }
            }

            if (isValid)
            {
                foreach (var link in term.Links.LinksToDocs.Links)
                {
                    context = new ValidationContext(link);
                    isValid = Validator.TryValidateObject(link, context, results, true);

                    if (!isValid)
                    {
                        break;
                    }
                }
            }

            if (isValid)
            {
                foreach (var link in term.Links.LinksToLearn.Links)
                {
                    context = new ValidationContext(link);
                    isValid = Validator.TryValidateObject(link, context, results, true);

                    if (!isValid)
                    {
                        break;
                    }
                }
            }

            if (isValid)
            {
                foreach (var link in term.Links.LinksToOthers.Links)
                {
                    context = new ValidationContext(link);
                    isValid = Validator.TryValidateObject(link, context, results, true);

                    if (!isValid)
                    {
                        break;
                    }
                }
            }

            return isValid;
        }

        public static bool TryValidate(this Synopsis synopsis, IList<ValidationResult> results)
        {
            var termBase = synopsis as TermBase;

            var isValid = termBase.TryValidate(results);

            if (isValid)
            {
                var context = new ValidationContext(synopsis);
                isValid = Validator.TryValidateObject(synopsis, context, results, true);
            }

            if (isValid)
            {
                // TODO CONTINUE
            }

            return isValid;
        }
    }
}