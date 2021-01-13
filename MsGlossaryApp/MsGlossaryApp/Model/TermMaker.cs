using Microsoft.Extensions.Logging;
using MsGlossaryApp.DataModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MsGlossaryApp.Model
{
    public static class TermMaker
    {
        private const string GitHubRawPathTemplate = "https://raw.githubusercontent.com/{0}/{1}/{2}/{3}";

        private static string MakeDisambiguationText(
            IList<Keyword> keywords,
            ILogger log = null)
        {
            log?.LogInformationEx("In MakeDisambiguationText", LogVerbosity.Verbose);

            var lastKeyword = keywords.OrderByDescending(k => k.Term.RecordingDate).First();

            var dateString = lastKeyword.Term.RecordingDate.ToShortDateString();

            var builder = new StringBuilder()
                .AppendLine("---")
                .Append($"title: {lastKeyword.KeywordName}")
                .AppendLine($" ({TextHelper.GetText("TermDisambiguation")})")
                .AppendLine($"description: {string.Format(TextHelper.GetText("TermDescriptionDisambiguation"), lastKeyword.KeywordName)}")
                .AppendLine($"author: {lastKeyword.Term.Authors.First().GitHub}")
                .AppendLine($"ms.date: {dateString}")
                .AppendLine($"ms.prod: {TextHelper.GetText("TermNonProductSpecific")}")
                .AppendLine("ms.topic: glossary")
                .AppendLine("---")
                .AppendLine()
                .AppendLine($"{MakeDisambiguationTitleLink(lastKeyword, log)} ({TextHelper.GetText("TermDisambiguation")})".MakeH1())
                .AppendLine()
                .AppendLine(string.Format(TextHelper.GetText("TermDifferentContexts"), lastKeyword.KeywordName).MakeH2())
                .AppendLine();

            foreach (var keyword in keywords.OrderBy(k => k.Term.Title))
            {
                builder
                    .AppendLine(string.Format(TextHelper.GetText("TermIn"), MakeTitleLink(keyword, log), keyword.Term.ShortDescription));
            }

            builder.AppendLine();
            log?.LogInformationEx("Out MakeDisambiguationText", LogVerbosity.Verbose);
            return builder.ToString();
        }

        private static string MakeDisambiguationTitleLink(
            Keyword keyword,
            ILogger log = null)
        {
            log?.LogInformationEx("In MakeDisambiguationTitleLink", LogVerbosity.Verbose);
            return keyword.KeywordName.MakeLink($"/glossary/term/{keyword.KeywordName.MakeSafeFileName()}/disambiguation");
        }

        private static IList<Language> ParseLanguages(
            string captions,
            ILogger log = null)
        {
            log?.LogInformationEx("In ParseLanguages", LogVerbosity.Verbose);

            if (string.IsNullOrEmpty(captions))
            {
                return null;
            }

            var languages = captions.Split(new char[]
            {
                ','
            }, StringSplitOptions.RemoveEmptyEntries);

            var result = new List<Language>();

            foreach (var language in languages)
            {
                var parts = language.Split(new char[]
                {
                    '/'
                });

                result.Add(new Language
                {
                    Code = parts[0].Trim(),
                    LanguageName = parts[1].Trim()
                });
            }

            log?.LogInformationEx("Out ParseLanguages", LogVerbosity.Verbose);
            return result;
        }

        private static string MakeTermText(
            Keyword keyword,
            ILogger log)
        {
            log?.LogInformationEx("In MakeTermText", LogVerbosity.Verbose);
            var term = keyword.Term;

            var redirect = string.Empty;

            if (!keyword.IsMainKeyword)
            {
                redirect += $" ({string.Format(TextHelper.GetText("TermRedirectedFrom"), keyword.KeywordName)})";
            }

            var dateString = term.RecordingDate.ToShortDateString();

            var builder = new StringBuilder()
                .AppendLine("---")
                .Append($"title: {term.Title}")
                .AppendLine(redirect)
                .AppendLine($"description: {string.Format(TextHelper.GetText("TermDescription"), term.Title)}")
                .AppendLine($"author: {term.Authors.First().GitHub}")
                .AppendLine($"ms.date: {dateString}")
                .AppendLine($"ms.prod: {TextHelper.GetText("TermNonProductSpecific")}")
                .AppendLine("ms.topic: glossary")
                .AppendLine("---")
                .AppendLine()
                .AppendLine($"{MakeTitleLink(keyword, log)}{redirect}".MakeH1())
                .AppendLine()
                .AppendLine(term.ShortDescription.MakeNote())
                .AppendLine()
                .AppendLine(term.YouTubeCode.MakeYouTubeVideo().MakeNote())
                .AppendLine()
                .AppendLine(TextHelper.GetText("TermDownload").MakeH2())
                .AppendLine()
                .AppendLine(TextHelper.GetText("TermDownloadHere").MakeLink($"https://msglossarystore.blob.core.windows.net/videos/{term.FileName}.{term.Language.Code}.mp4"))
                .AppendLine();

            if (term.Captions.Count > 0)
            {
                builder
                .AppendLine(TextHelper.GetText("TermLanguages").MakeH2())
                .AppendLine()
                .AppendLine(TextHelper.GetText("TermCaptions"))
                .AppendLine();

                foreach (var caption in keyword.Term.Captions)
                {
                    builder.AppendLine($"- [{caption.LanguageName}](https://msglossarystore.blob.core.windows.net/captions/{term.FileName}.{term.Language.Code}.{caption.Code}.srt)");
                }

                builder.AppendLine()
                    .AppendLine($"{TextHelper.GetText("TermCaptionsLearn")}(/glossary/captions).".MakeNote())
                    .AppendLine();
            }

            builder
                .AppendLine(TextHelper.GetText("TermLinks").MakeH2());

            var linksCollections = new Dictionary<string, IList<Link>>();
            linksCollections.Add(Constants.SynopsisMarkdownMarkers.LinksToDocsMarker, term.LinksToDocs);
            linksCollections.Add(Constants.SynopsisMarkdownMarkers.LinksToLearnMarker, term.LinksToLearn);
            linksCollections.Add(Constants.SynopsisMarkdownMarkers.LinksToOthersMarker, term.LinksToOthers);

            foreach (var key in linksCollections.Keys)
            {
                if (linksCollections[key].Count > 0)
                {
                    builder.AppendLine()
                        .AppendLine(key.MakeH3())
                        .AppendLine();
                }

                foreach (var link in linksCollections[key])
                {
                    builder.AppendLine(link.ToMarkdown());
                }
            }

            builder.AppendLine()
                .AppendLine(TextHelper.GetText("TermTranscript").MakeH2())
                .AppendLine()
                .AppendLine(term.Transcript)
                .AppendLine();

            if (term.Authors != null
                && term.Authors.Count > 0)
            {
                builder
                    .AppendLine(TextHelper.GetText("TermAuthors").MakeH2())
                    .AppendLine()
                    .Append($"{TextHelper.GetText("TermCreatedBy")} ");

                foreach (var author in term.Authors)
                {
                    builder
                        .Append(author.Name.MakeLink($"http://twitter.com/{author.Twitter}"))
                        .Append(", ");
                }

                builder.Remove(builder.Length - 2, 2);
            }

            builder.AppendLine();
            log?.LogInformationEx("Out MakeTermText", LogVerbosity.Verbose);
            return builder.ToString();
        }

        private static string MakeTermTextWithoutVideo(
            Keyword keyword,
            ILogger log)
        {
            log?.LogInformationEx("In MakeTermTextWithoutVideo", LogVerbosity.Verbose);
            var term = keyword.Term;

            var redirect = string.Empty;

            if (!keyword.IsMainKeyword)
            {
                redirect += $" ({string.Format(TextHelper.GetText("TermRedirectedFrom"), keyword.KeywordName)}";
            }

            var dateString = term.RecordingDate.ToShortDateString();

            var builder = new StringBuilder()
                .AppendLine("---")
                .Append($"title: {term.Title}")
                .AppendLine(redirect)
                .AppendLine($"description: {string.Format(TextHelper.GetText("TermDescription"), term.Title)}")
                .AppendLine($"author: {term.Authors.First().GitHub}")
                .AppendLine($"ms.date: {dateString}")
                .AppendLine($"ms.prod: {TextHelper.GetText("TermNonProductSpecific")}")
                .AppendLine($"ms.topic: glossary")
                .AppendLine("---")
                .AppendLine()
                .AppendLine($"{MakeTitleLink(keyword, log)}{redirect}".MakeH1())
                .AppendLine()
                .AppendLine(term.ShortDescription.MakeNote())
                .AppendLine()
                .AppendLine(TextHelper.GetText("TermDefinition").MakeH2())
                .AppendLine()
                .AppendLine(term.Transcript)
                .AppendLine();

            builder
                .AppendLine(TextHelper.GetText("TermLinks").MakeH2());

            builder.AppendLine()
                .AppendLine(Constants.SynopsisMarkdownMarkers.LinksToDocsMarker.MakeH3())
                .AppendLine();

            foreach (var link in term.LinksToDocs)
            {
                builder.AppendLine(link.ToMarkdown());
            }

            builder.AppendLine()
                .AppendLine(Constants.SynopsisMarkdownMarkers.LinksToLearnMarker.MakeH3())
                .AppendLine();

            foreach (var link in term.LinksToLearn)
            {
                builder.AppendLine(link.ToMarkdown());
            }

            if (term.LinksToOthers.Count > 0)
            {
                builder.AppendLine()
                    .AppendLine(Constants.SynopsisMarkdownMarkers.LinksToOthersMarker.MakeH3())
                    .AppendLine();

                foreach (var link in term.LinksToOthers)
                {
                    builder.AppendLine(link.ToMarkdown());
                }
            }

            if (term.Authors != null
                && term.Authors.Count > 0)
            {
                builder.AppendLine()
                    .AppendLine(TextHelper.GetText("TermAuthors").MakeH2())
                    .AppendLine()
                    .Append($"{TextHelper.GetText("TermCreatedBy")} ");

                foreach (var author in term.Authors)
                {
                    builder
                        .Append(author.Name.MakeLink($"http://twitter.com/{author.Twitter}"))
                        .Append(", ");
                }

                builder.Remove(builder.Length - 2, 2);
            }

            builder.AppendLine();
            log?.LogInformationEx("Out MakeTermTextWithoutVideo", LogVerbosity.Verbose);
            return builder.ToString();
        }

        private static string MakeTitleLink(
            Keyword keyword,
            ILogger log = null)
        {
            log?.LogInformationEx("In MakeTitleLink", LogVerbosity.Verbose);
            if (keyword.IsMainKeyword)
            {
                return keyword.Term.Title.MakeLink($"/glossary/term/{keyword.Term.FileName}");
            }
            else
            {
                return keyword.Term.Title.MakeLink($"/glossary/term/{keyword.Term.FileName}/{keyword.KeywordName.MakeSafeFileName()}");
            }
        }

        private static string MakeTocLink(
                    Keyword keyword,
            ILogger log = null)
        {
            log?.LogInformationEx("In MakeTocLink", LogVerbosity.Verbose);

            if (keyword.IsMainKeyword)
            {
                if (keyword.IsDisambiguation)
                {
                    return $"term/{keyword.KeywordName.MakeSafeFileName()}/disambiguation";
                }

                return $"term/{keyword.Term.FileName}";
            }
            else
            {
                return $"term/{keyword.Term.FileName}/{keyword.KeywordName.MakeSafeFileName()}";
            }
        }

        public static Task<GlossaryFile> CreateDisambiguationFile(
            IList<Keyword> keywords,
            ILogger log = null)
        {
            log?.LogInformationEx("In CreateDisambiguationFile", LogVerbosity.Verbose);
            var result = new GlossaryFile();
            var tcs = new TaskCompletionSource<GlossaryFile>();

            try
            {
                var firstKeyword = keywords.First();

                string path = $"glossary/term/{firstKeyword.KeywordName.MakeSafeFileName()}/disambiguation.md";

                string text = MakeDisambiguationText(keywords, log);

                result.Path = path;
                result.Content = text;
            }
            catch (Exception ex)
            {
                log?.LogError(ex, "Error in CreateDisambiguationFile");
                result.ErrorMessage = ex.Message;
            }

            log?.LogInformationEx("Out CreateDisambiguationFile", LogVerbosity.Verbose);
            tcs.SetResult(result);
            return tcs.Task;
        }

        public static Task<GlossaryFile> CreateKeywordFile(
            Keyword keyword,
            ILogger log = null)
        {
            log?.LogInformationEx("In CreateKeywordFile", LogVerbosity.Verbose);

            var tcs = new TaskCompletionSource<GlossaryFile>();
            var result = new GlossaryFile();

            try
            {
                string path = null;

                if (keyword.IsMainKeyword)
                {
                    path = $"glossary/term/{keyword.Term.FileName}/index.md";
                }
                else
                {
                    path = $"glossary/term/{keyword.Term.FileName}/{keyword.KeywordName.MakeSafeFileName()}.md";
                }

                string text = null;

                if (string.IsNullOrEmpty(keyword.Term.YouTubeCode))
                {
                    text = MakeTermTextWithoutVideo(keyword, log);
                }
                else
                {
                    text = MakeTermText(keyword, log);
                }

                result.Path = path;
                result.Content = text;
            }
            catch (Exception ex)
            {
                log?.LogError(ex, $"Error in CreateKeywordFile for {keyword}");
                result.ErrorMessage = ex.Message;
            }

            log?.LogInformationEx("Out CreateKeywordFile", LogVerbosity.Verbose);
            tcs.SetResult(result);
            return tcs.Task;
        }

        public static Task<GlossaryFile> CreateTableOfContentsFile(
            IList<Keyword> keywords,
            ILogger log = null)
        {
            log?.LogInformationEx("In CreateTableOfContentsFile", LogVerbosity.Verbose);
            var result = new GlossaryFile();
            var tcs = new TaskCompletionSource<GlossaryFile>();

            try
            {
                var tocBuilder = new StringBuilder()
                    .AppendLine($"- name: {TextHelper.GetText("TermTocTitle")}")
                    .AppendLine("  href: index.md")
                    .AppendLine();

                var groups = keywords
                    .GroupBy(k => k.Term.Title);

                foreach (var g in groups.OrderBy(g => g.Key))
                {
                    var mainKeyword = g.First(k => k.IsMainKeyword);

                    if (mainKeyword.IsDisambiguation)
                    {
                        tocBuilder
                            .AppendLine($"- name: {mainKeyword.KeywordName} ({TextHelper.GetText("TermDisambiguation")})")
                            .AppendLine($"  href: {MakeTocLink(mainKeyword)}");
                    }
                    else
                    {
                        tocBuilder
                            .AppendLine($"- name: {mainKeyword.Term.Title}")
                            .AppendLine($"  href: {MakeTocLink(mainKeyword)}");
                    }

                    var count = g.Count();

                    if (count > 1)
                    {
                        tocBuilder
                            .AppendLine("  items:");

                        foreach (var k in g.Where(k => !k.IsMainKeyword).OrderBy(k => k.KeywordName))
                        {
                            tocBuilder
                                .AppendLine($"  - name: {k.KeywordName}")
                                .AppendLine($"    href: {MakeTocLink(k)}");
                        }
                    }
                }

                result.Path = "glossary/TOC.yml";
                result.Content = tocBuilder.ToString();
            }
            catch (Exception ex)
            {
                log?.LogError("Error in CreateTableOfContentsFile", ex);
                result.ErrorMessage = ex.Message;
            }

            log?.LogInformationEx("Out CreateTableOfContentsFile", LogVerbosity.Verbose);
            tcs.SetResult(result);
            return tcs.Task;
        }

        public static IList<Author> MakeAuthors(
                                                                                            string authorName,
            string email,
            string github,
            string twitter,
            ILogger log = null)
        {
            log?.LogInformationEx("In MakeAuthors", LogVerbosity.Verbose);

            var authorNames = authorName.Split(new char[]
            {
                Constants.Separator
            });

            var emails = email.Split(new char[]
            {
                Constants.Separator
            });

            var githubs = github.Split(new char[]
            {
                Constants.Separator
            });

            var twitters = twitter.Split(new char[]
            {
                Constants.Separator
            });

            if (authorNames.Length != emails.Length
                || authorNames.Length != githubs.Length
                || authorNames.Length != twitters.Length)
            {
                log?.LogError("Invalid author, email github or twitter lists");
                throw new InvalidOperationException("Invalid author, email github or twitter lists");
            }

            var result = new List<Author>();

            for (var index = 0; index < authorNames.Length; index++)
            {
                var author = new Author(
                    authorNames[index].Trim(),
                    emails[index].Trim(),
                    githubs[index].Trim(),
                    twitters[index].Trim());

                result.Add(author);
            }

            log?.LogInformationEx("Out MakeAuthors", LogVerbosity.Verbose);
            return result;
        }

        public static IList<ContentEntry> MakeKeywords(string keywordsLine)
        {
            return keywordsLine.Split(new char[]
            {
                Constants.Separator
            }, StringSplitOptions.RemoveEmptyEntries)
                .Select(k => new ContentEntry(k.Trim()))
                .ToList();
        }

        public static string MakeKeywordsLine(IList<ContentEntry> keywords)
        {
            if (keywords == null)
            {
                return string.Empty;
            }

            return string.Join(", ", keywords.Select(k => k.Content));
        }

        public static Term ParseTerm(
            Uri uri,
            string markdown,
            ILogger log)
        {
            log?.LogInformationEx("In ParseTerm", LogVerbosity.Verbose);

            var term = new Term
            {
                Uri = uri,
            };

            log?.LogInformationEx($"Term: {term.Uri}", LogVerbosity.Verbose);

            var markdownReader = new StringReader(markdown);

            string youTubeCode = null;
            string keywordsLine = null;
            string title = null;
            string shortDescription = null;
            string captions = null;
            string language = null;
            string authorName = null;
            string email = null;
            string twitter = null;
            string github = null;
            DateTime recordingDate = DateTime.MinValue;
            var isTranscript = false;
            var isLinks = false;
            var isDocLinks = false;
            var isLearnLinks = false;
            var isOtherLinks = false;
            var transcript = new StringBuilder();
            string line;

            while ((line = markdownReader.ReadLine()) != null)
            {
                if (line.StartsWith(Constants.TermMarkdownMarkers.TranscriptMarker))
                {
                    isLinks = false;
                    isDocLinks = false;
                    isLearnLinks = false;
                    isOtherLinks = false;
                    isTranscript = true;
                    continue;
                }
                else if (line.StartsWith(Constants.TermMarkdownMarkers.LinksMarker))
                {
                    isLinks = true;
                    isDocLinks = false;
                    isLearnLinks = false;
                    isOtherLinks = false;
                    isTranscript = false;
                    continue;
                }
                else if (isLinks)
                {
                    if (string.IsNullOrEmpty(line.Trim()))
                    {
                        continue;
                    }
                    else if (line.StartsWith(Constants.TermMarkdownMarkers.LinksToDocsMarker))
                    {
                        isLinks = true;
                        isDocLinks = true;
                        isLearnLinks = false;
                        isOtherLinks = false;
                        isTranscript = false;
                        continue;
                    }
                    else if (line.StartsWith(Constants.TermMarkdownMarkers.LinksToLearnMarker))
                    {
                        isLinks = true;
                        isDocLinks = false;
                        isLearnLinks = true;
                        isOtherLinks = false;
                        isTranscript = false;
                        continue;
                    }
                    else if (line.StartsWith(Constants.TermMarkdownMarkers.LinksToOthersMarker))
                    {
                        isLinks = true;
                        isDocLinks = false;
                        isLearnLinks = false;
                        isOtherLinks = true;
                        isTranscript = false;
                        continue;
                    }
                }
                else if (isTranscript)
                {
                    transcript.AppendLine(line);
                }
                else if (isDocLinks)
                {
                    if (string.IsNullOrEmpty(line.Trim()))
                    {
                        continue;
                    }

                    term.LinksToDocs.Add(line.ParseListItem().ParseLink());
                }
                else if (isLearnLinks)
                {
                    if (string.IsNullOrEmpty(line.Trim()))
                    {
                        continue;
                    }

                    term.LinksToLearn.Add(line.ParseListItem().ParseLink());
                }
                else if (isOtherLinks)
                {
                    if (string.IsNullOrEmpty(line.Trim()))
                    {
                        continue;
                    }

                    term.LinksToOthers.Add(line.ParseListItem().ParseLink());
                }
                else if (line.IsH1())
                {
                    title = line.ParseH1();
                }
                else if (line.StartsWith(Constants.TermMarkdownMarkers.YouTubeMarker))
                {
                    youTubeCode = line.Substring(Constants.TermMarkdownMarkers.YouTubeMarker.Length).Trim();
                    log?.LogInformationEx($"youTubeCode: {youTubeCode}", LogVerbosity.Debug);
                }
                else if (line.StartsWith(Constants.TermMarkdownMarkers.KeywordsMarker))
                {
                    keywordsLine = line.Substring(Constants.TermMarkdownMarkers.KeywordsMarker.Length).Trim();
                    log?.LogInformationEx($"keywordsLine: {keywordsLine}", LogVerbosity.Debug);
                }
                else if (line.StartsWith(Constants.TermMarkdownMarkers.ShortDescriptionMarker))
                {
                    shortDescription = line.Substring(Constants.TermMarkdownMarkers.ShortDescriptionMarker.Length).Trim();
                    log?.LogInformationEx($"blurb: {shortDescription}", LogVerbosity.Debug);
                }
                else if (line.StartsWith(Constants.TermMarkdownMarkers.CaptionsMarker))
                {
                    captions = line.Substring(Constants.TermMarkdownMarkers.CaptionsMarker.Length).Trim();
                    log?.LogInformationEx($"captions: {captions}", LogVerbosity.Debug);
                }
                else if (line.StartsWith(Constants.TermMarkdownMarkers.LanguageMarker))
                {
                    language = line.Substring(Constants.TermMarkdownMarkers.LanguageMarker.Length).Trim();
                    log?.LogInformationEx($"language: {language}", LogVerbosity.Debug);
                }
                else if (line.StartsWith(Constants.TermMarkdownMarkers.AuthorNameMarker))
                {
                    authorName = line.Substring(Constants.TermMarkdownMarkers.AuthorNameMarker.Length).Trim();
                    log?.LogInformationEx($"authorName: {authorName}", LogVerbosity.Debug);
                }
                else if (line.StartsWith(Constants.TermMarkdownMarkers.EmailMarker))
                {
                    email = line.Substring(Constants.TermMarkdownMarkers.EmailMarker.Length).Trim();
                    log?.LogInformationEx($"email: {email}", LogVerbosity.Debug);
                }
                else if (line.StartsWith(Constants.TermMarkdownMarkers.GitHubMarker))
                {
                    github = line.Substring(Constants.TermMarkdownMarkers.GitHubMarker.Length).Trim();
                    log?.LogInformationEx($"github: {github}", LogVerbosity.Debug);
                }
                else if (line.StartsWith(Constants.TermMarkdownMarkers.TwitterMarker))
                {
                    twitter = line.Substring(Constants.TermMarkdownMarkers.TwitterMarker.Length).Trim();
                    log?.LogInformationEx($"twitter: {twitter}", LogVerbosity.Debug);
                }
                else if (line.StartsWith(Constants.TermMarkdownMarkers.RecordingDateMarker))
                {
                    var dateString = line.Substring(Constants.TermMarkdownMarkers.RecordingDateMarker.Length).Trim();
                    log?.LogInformationEx($"dateString: {dateString}", LogVerbosity.Debug);
                    recordingDate = DateTime.ParseExact(dateString, "yyyyMMdd", CultureInfo.InvariantCulture);
                }
            }

            term.Title = title;
            term.FileName = Path.GetFileNameWithoutExtension(uri.LocalPath);
            term.Transcript = transcript.ToString().Trim();
            term.RecordingDate = recordingDate;
            term.YouTubeCode = youTubeCode;
            term.ShortDescription = shortDescription;
            term.Authors = MakeAuthors(authorName, email, github, twitter, log);
            term.Captions = ParseLanguages(captions, log);
            term.Language = ParseLanguages(language, log).First();
            term.Keywords = MakeKeywords(keywordsLine);

            log?.LogInformationEx("Out CreateTerm", LogVerbosity.Verbose);
            return term;
        }

        public static Task<IList<Keyword>> SortDisambiguations(
            IList<Keyword> keywords,
            ILogger log = null)
        {
            log?.LogInformationEx("In SortDisambiguations", LogVerbosity.Verbose);

            var tcs = new TaskCompletionSource<IList<Keyword>>();

            var disambiguations = keywords
                .Where(k => k.MustDisambiguate)
                .GroupBy(k => k.KeywordName.ToLower())
                .ToList();

            foreach (var group in disambiguations)
            {
                var first = group.First();
                var termName = first.KeywordName.MakeSafeFileName();

                keywords.Add(new Keyword
                {
                    IsMainKeyword = true,
                    KeywordName = first.KeywordName,
                    MustDisambiguate = false,
                    Term = new Term
                    {
                        Title = first.KeywordName,
                        FileName = "disambiguation"
                    },
                    TermSafeFileName = "disambiguation",
                    IsDisambiguation = true
                });
            }

            tcs.SetResult(keywords);
            log?.LogInformationEx("Out SortDisambiguations", LogVerbosity.Verbose);
            return tcs.Task;
        }

        public static Task<IList<Keyword>> SortKeywords(
            IList<Term> allTerms,
            Term currentTerm,
            ILogger log = null)
        {
            log?.LogInformationEx("In SortKeywords", LogVerbosity.Verbose);

            var tcs = new TaskCompletionSource<IList<Keyword>>();

            var result = new List<Keyword>();

            foreach (var keyword in currentTerm.Keywords)
            {
                var newKeyword = new Keyword
                {
                    KeywordName = keyword.Content,
                    TermSafeFileName = currentTerm.FileName
                };

                var sameKeywords = allTerms
                    .SelectMany(t => t.Keywords)
                    .Where(k => k.Content.ToLower() == keyword.Content.ToLower());

                if (sameKeywords.Count() > 1)
                {
                    newKeyword.MustDisambiguate = true;
                }

                if (newKeyword.KeywordName.MakeSafeFileName().ToLower() == currentTerm.FileName.ToLower())
                {
                    newKeyword.IsMainKeyword = true;
                }

                result.Add(newKeyword);
            }

            if (!result.Any(k => k.IsMainKeyword))
            {
                var mainKeyword = new Keyword
                {
                    IsMainKeyword = true,
                    KeywordName = currentTerm.Title,
                    TermSafeFileName = currentTerm.FileName
                };

                result.Add(mainKeyword);
            }

            tcs.SetResult(result);
            log?.LogInformationEx("Out SortKeywords", LogVerbosity.Verbose);
            return tcs.Task;
        }

        public static async Task<GlossaryFile> VerifyFile(GlossaryFile file)
        {
            var account = Environment.GetEnvironmentVariable(Constants.DocsGlossaryGitHubAccountVariableName);
            var repo = Environment.GetEnvironmentVariable(Constants.DocsGlossaryGitHubRepoVariableName);
            var branch = Environment.GetEnvironmentVariable(Constants.DocsGlossaryGitHubMainBranchNameVariableName);

            var url = string.Format(
                GitHubRawPathTemplate,
                account,
                repo,
                branch,
                file.Path);

            try
            {
                var client = new HttpClient();
                var currentText = await client.GetStringAsync(url);

                if (currentText != file.Content)
                {
                    file.MustSave = true;
                }
            }
            catch (HttpRequestException)
            {
                file.MustSave = true;
            }

            return file;
        }
    }
}