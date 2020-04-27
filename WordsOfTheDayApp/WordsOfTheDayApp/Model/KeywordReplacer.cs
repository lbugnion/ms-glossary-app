using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace WordsOfTheDayApp.Model
{
    public class KeywordReplacer
    {
        public const string KeywordLinkTemplate = "[{0}](/topic/{1})";

        public string ReplaceInMarkdown(
            string markdown, 
            List<KeywordPair> keywordsList,
            string currentFile = null,
            ILogger log = null)
        {
            log?.LogInformation("In ReplaceInMarkdown");
            var builder = new StringBuilder(markdown);

            foreach (var k in keywordsList)
            {
                log?.LogInformation($"{k.Keyword} / {k.Topic}");

                if (k.Topic == currentFile)
                {
                    log?.LogInformation($"{k.Topic} is current file");
                    continue;
                }

                var indexOfKeyword = markdown.IndexOf(k.Keyword, StringComparison.InvariantCultureIgnoreCase);

                if (indexOfKeyword > -1)
                {
                    var oldKeyword = markdown.Substring(indexOfKeyword, k.Keyword.Length);
                    log?.LogInformation($"oldKeyword: {oldKeyword}");
                    var newUrl = string.Format(KeywordLinkTemplate, oldKeyword, k.Topic);
                    log?.LogInformation($"newUrl: {newUrl}");

                    var indexOfLink = markdown.IndexOf(
                        $"[{k.Keyword}](",
                        StringComparison.InvariantCultureIgnoreCase);

                    if (indexOfLink > -1)
                    {
                        // Link was already created ==> replace the URL
                        var indexOfUrl = indexOfLink + $"[{k.Keyword}](".Length;
                        var indexOfEndOfUrl = markdown.IndexOf(")", indexOfUrl) + 1;
                        var oldUrl = markdown.Substring(indexOfLink, indexOfEndOfUrl - indexOfLink);
                        log?.LogInformation($"oldUrl: {oldUrl}");

                        if (oldUrl != newUrl)
                        {
                            builder.Replace(oldUrl, newUrl, indexOfLink, indexOfEndOfUrl - indexOfLink);
                            log?.LogInformation("Replaced!");
                        }
                    }
                    else
                    {
                        // Keyword was never encoded
                        builder.Replace(oldKeyword, newUrl, indexOfKeyword, oldKeyword.Length);
                        log?.LogInformation("Created!");
                    }
                }

                markdown = builder.ToString();
            }

            log?.LogInformation("Done replacing keywords");
            return builder.ToString();
        }
    }
}
