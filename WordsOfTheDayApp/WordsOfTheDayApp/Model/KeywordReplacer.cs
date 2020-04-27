using System;
using System.Collections.Generic;
using System.Text;

namespace WordsOfTheDayApp.Model
{
    public class KeywordReplacer
    {
        public const string KeywordLinkTemplate = "[{0}](https://wordsoftheday.azurewebsites.net/topic/{1})";

        public string ReplaceInMarkdown(
            string markdown, 
            List<KeywordPair> keywordsList,
            string currentFile = null)
        {
            var builder = new StringBuilder(markdown);

            foreach (var k in keywordsList)
            {
                if (k.Topic == currentFile)
                {
                    continue;
                }

                var indexOfKeyword = markdown.IndexOf(k.Keyword, StringComparison.InvariantCultureIgnoreCase);

                if (indexOfKeyword > -1)
                {
                    var oldKeyword = markdown.Substring(indexOfKeyword, k.Keyword.Length);
                    var newUrl = string.Format(KeywordLinkTemplate, oldKeyword, k.Topic);

                    var indexOfLink = markdown.IndexOf(
                        $"[{k.Keyword}](",
                        StringComparison.InvariantCultureIgnoreCase);

                    if (indexOfLink > -1)
                    {
                        // Link was already created ==> replace the URL
                        var indexOfUrl = indexOfLink + $"[{k.Keyword}](".Length;
                        var indexOfEndOfUrl = markdown.IndexOf(")", indexOfUrl) + 1;
                        var oldUrl = markdown.Substring(indexOfLink, indexOfEndOfUrl - indexOfLink);

                        if (oldUrl != newUrl)
                        {
                            builder.Replace(oldUrl, newUrl, indexOfLink, indexOfEndOfUrl - indexOfLink);
                        }
                    }
                    else
                    {
                        // Keyword was never encoded
                        builder.Replace(oldKeyword, newUrl, indexOfKeyword, oldKeyword.Length);
                    }
                }

                markdown = builder.ToString();
            }

            return builder.ToString();
        }
    }
}
