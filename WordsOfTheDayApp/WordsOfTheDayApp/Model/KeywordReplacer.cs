using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WordsOfTheDayApp.Model
{
    public class KeywordReplacer
    {
        public const string KeywordLinkTemplate = "[{0}]({1})";
        public const string LinkTemplate = "/topic/{0}/{1}";
        public const string SingleWordCharacter = " [](){}*!&-_+=|/':;.,<>?\"";

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

                    if (indexOfKeyword > 0
                        && !SingleWordCharacter.Contains(markdown[indexOfKeyword - 1]))
                    {
                        previousIndexOfKeyword = indexOfKeyword;
                        continue;
                    }

                    if (indexOfKeyword + k.Keyword.Length < markdown.Length
                        && !SingleWordCharacter.Contains(markdown[indexOfKeyword + k.Keyword.Length]))
                    {
                        previousIndexOfKeyword = indexOfKeyword;
                        continue;
                    }

                    if (indexOfKeyword > -1
                        && indexOfKeyword > indexOfTranscript)
                    {
                        // Preserve casing
                        var oldKeyword = markdown.Substring(indexOfKeyword, k.Keyword.Length);
                        log?.LogInformation($"oldKeyword: {oldKeyword}");

                        var newUrlAlone = string.Format(LinkTemplate, k.Topic, k.Subtopic);
                        log?.LogInformation($"newUrlAlone: {newUrlAlone}");

                        var newUrl = string.Format(KeywordLinkTemplate, oldKeyword, newUrlAlone);
                        log?.LogInformation($"newUrl: {newUrl}");

                        var foundOpening = false;
                        var indexOfOpening = -1;
                        var indexOfClosingSquare = -1;
                        var foundClosing = false;
                        var foundLink = false;
                        var doNotEncode = false;

                        for (var index = indexOfKeyword - 1; index >= 0; index--)
                        {
                            if (doNotEncode
                                || stop)
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
                                for (var index2 = indexOfKeyword + oldKeyword.Length;
                                     index2 < markdown.Length;
                                     index2++)
                                {
                                    if (markdown[index2] == ']')
                                    {
                                        indexOfClosingSquare = index2;
                                        continue;
                                    }

                                    if (markdown[index2] == '('
                                        && index2 == indexOfClosingSquare + 1)
                                    {
                                        // Check if we need to replace the link
                                        indexOfOpening = index2;
                                        foundLink = true;
                                        continue;
                                    }

                                    if (foundLink)
                                    {
                                        if (markdown[index2] == '/')
                                        {
                                            // Replace the link
                                            var indexOfClosing = markdown.IndexOf(')', index2);
                                            builder.Replace(
                                                markdown.Substring(indexOfOpening + 1, indexOfClosing - indexOfOpening - 1),
                                                newUrlAlone,
                                                indexOfOpening + 1,
                                                indexOfClosing - indexOfOpening - 1);
                                            stop = true;
                                            break;
                                        }
                                        else
                                        {
                                            doNotEncode = true;
                                            break;
                                        }
                                    }
                                }

                                continue;
                            }

                            if (markdown[index] == ']')
                            {
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
                            doNotEncode = false;
                            continue;
                        }

                        if (!stop)
                        {
                            builder.Replace(oldKeyword, newUrl, indexOfKeyword, oldKeyword.Length);
                            stop = true;
                        }

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
