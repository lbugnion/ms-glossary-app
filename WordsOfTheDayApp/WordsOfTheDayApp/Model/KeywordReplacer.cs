using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace WordsOfTheDayApp.Model
{
    public class KeywordReplacer
    {
        public const string KeywordLinkTemplate = "[{0}](/topic/{1}/{2})";

        public string ReplaceInMarkdown(
            string markdown, 
            List<KeywordPair> keywordsList,
            string currentFile = null,
            ILogger log = null)
        {
            log?.LogInformation("In ReplaceInMarkdown");
            var builder = new StringBuilder(markdown);

            var indexOfTranscript = markdown.IndexOf(Environment.NewLine + "## Transcript"+ Environment.NewLine);

            foreach (var k in keywordsList)
            {
                log?.LogInformation($"{k.Keyword} / {k.Topic}");

                if (k.Topic == currentFile)
                {
                    log?.LogInformation($"{k.Topic} is current file");
                    continue;
                }

                var previousIndexOfKeyword = -1;
                var indexOfKeyword = -1;
                var stop = false;

                do
                {
                    indexOfKeyword = markdown.IndexOf(
                        k.Keyword,
                        previousIndexOfKeyword + 1,
                        StringComparison.InvariantCultureIgnoreCase);

                    if (indexOfKeyword > -1
                        && indexOfKeyword > indexOfTranscript)
                    {
                        // Preserve casing
                        var oldKeyword = markdown.Substring(indexOfKeyword, k.Keyword.Length);
                        log?.LogInformation($"oldKeyword: {oldKeyword}");

                        var newUrl = string.Format(KeywordLinkTemplate, oldKeyword, k.Topic, k.Subtopic);
                        log?.LogInformation($"newUrl: {newUrl}");

                        var foundOpeningSquare = false;
                        var foundOpening = false;
                        var indexOfOpening = -1;
                        var foundClosingSquare = false;
                        var indexOfClosingSquare = -1;
                        var foundClosing = false;
                        var doNotEncode = false;

                        for (var index = indexOfKeyword - 1; index >= 0; index--)
                        {
                            if (doNotEncode)
                            {
                                break;
                            }

                            if (foundOpening
                                && markdown[index] != ']')
                            {
                                break;
                            }

                            if (markdown[index] == '[')
                            {
                                foundOpeningSquare = true;

                                for (var index2 = indexOfKeyword + oldKeyword.Length;
                                     index2 < markdown.Length;
                                     index2++)
                                {
                                    if (markdown[index2] == ']')
                                    {
                                        foundClosingSquare = true;
                                        indexOfClosingSquare = index2;
                                        continue;
                                    }

                                    if (markdown[index2] == '('
                                        && index2 == indexOfClosingSquare + 1)
                                    {
                                        doNotEncode = true;
                                        break;
                                    }
                                }

                                continue;
                            }

                            if (markdown[index] == ']')
                            {
                                foundClosingSquare = true;

                                if (foundOpening
                                    && indexOfOpening == index + 1)
                                {
                                    doNotEncode = true;
                                    break;
                                }

                                continue;
                            }

                            if (markdown[index] == '(')
                            {
                                foundOpening = true;
                                indexOfOpening = index;

                                if (foundClosing
                                    && index > 0
                                    && markdown[index - 1] == ']')
                                {
                                    break;
                                }

                                continue;
                            }

                            if (markdown[index] == ')')
                            {
                                foundClosing = true;
                                continue;
                            }
                        }

                        if (doNotEncode)
                        {
                            previousIndexOfKeyword = indexOfKeyword;
                            continue;
                        }

                        builder.Replace(oldKeyword, newUrl, indexOfKeyword, oldKeyword.Length);
                        stop = true;
                        break;
                    }

                    previousIndexOfKeyword = indexOfKeyword;
                    markdown = builder.ToString();
                }
                while (indexOfKeyword > -1 && !stop);
            }

            log?.LogInformation("Done replacing keywords");
            return builder.ToString();
        }
    }
}
