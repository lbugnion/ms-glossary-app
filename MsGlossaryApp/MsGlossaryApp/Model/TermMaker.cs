using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
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

        private static IList<AuthorInformation> MakeAuthors(
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

            var result = new List<AuthorInformation>();

            for (var index = 0; index < authorNames.Length; index++)
            {
                var author = new AuthorInformation(
                    authorNames[index].Trim(),
                    emails[index].Trim(),
                    githubs[index].Trim(),
                    twitters[index].Trim());

                result.Add(author);
            }

            log?.LogInformationEx("Out MakeAuthors", LogVerbosity.Verbose);
            return result;
        }

        private static string MakeDisambiguationText(
            IList<KeywordInformation> keywords,
            ILogger log = null)
        {
            log?.LogInformationEx("In MakeDisambiguationText", LogVerbosity.Verbose);

            var lastKeyword = keywords.OrderByDescending(k => k.Term.RecordingDate).First();

            var dateString = lastKeyword.Term.RecordingDate.ToShortDateString();

            var builder = new StringBuilder()
                .AppendLine("---")
                .Append($"title: {lastKeyword.Keyword}")
                .AppendLine($" ({TextHelper.GetText("TermDisambiguation")})")
                .AppendLine($"description: {string.Format(TextHelper.GetText("TermDescriptionDisambiguation"), lastKeyword.Keyword)}")
                .AppendLine($"author: {lastKeyword.Term.Authors.First().GitHub}")
                .AppendLine($"ms.date: {dateString}")
                .AppendLine($"ms.prod: {TextHelper.GetText("TermNonProductSpecific")}")
                .AppendLine("ms.topic: glossary")
                .AppendLine("---")
                .AppendLine()
                .Append(Constants.H1)
                .Append(MakeDisambiguationTitleLink(lastKeyword, log))
                .AppendLine($" ({TextHelper.GetText("TermDisambiguation")})")
                .AppendLine()
                .Append(Constants.H2)
                .AppendLine(string.Format(TextHelper.GetText("TermDifferentContexts"), lastKeyword.Keyword))
                .AppendLine();

            foreach (var keyword in keywords.OrderBy(k => k.Term.Title))
            {
                builder
                    .AppendLine(string.Format(TextHelper.GetText("TermIn"), MakeTitleLink(keyword, log), keyword.Term.Blurb));
            }

            builder.AppendLine();
            log?.LogInformationEx("Out MakeDisambiguationText", LogVerbosity.Verbose);
            return builder.ToString();
        }

        private static string MakeDisambiguationTitleLink(
            KeywordInformation keyword,
            ILogger log = null)
        {
            log?.LogInformationEx("In MakeDisambiguationTitleLink", LogVerbosity.Verbose);
            return $"[{keyword.Keyword}](/glossary/term/{keyword.Keyword.MakeSafeFileName()}/disambiguation)";
        }

        private static IList<LanguageInfo> MakeLanguages(
                            string captions,
            ILogger log = null)
        {
            log?.LogInformationEx("In MakeLanguages", LogVerbosity.Verbose);

            if (string.IsNullOrEmpty(captions))
            {
                return null;
            }

            var languages = captions.Split(new char[]
            {
                ','
            }, StringSplitOptions.RemoveEmptyEntries);

            var result = new List<LanguageInfo>();

            foreach (var language in languages)
            {
                var parts = language.Split(new char[]
                {
                    '/'
                });

                result.Add(new LanguageInfo
                {
                    Code = parts[0].Trim(),
                    Language = parts[1].Trim()
                });
            }

            log?.LogInformationEx("Out MakeLanguages", LogVerbosity.Verbose);
            return result;
        }

        private static string MakeTitleLink(
            KeywordInformation keyword,
            ILogger log = null)
        {
            log?.LogInformationEx("In MakeTitleLink", LogVerbosity.Verbose);
            if (keyword.IsMainKeyword)
            {
                return $"[{keyword.Term.Title}](/glossary/term/{keyword.Term.TermName})";
            }
            else
            {
                return $"[{keyword.Term.Title}](/glossary/term/{keyword.Term.TermName}/{keyword.Keyword.MakeSafeFileName()})";
            }
        }

        private static string MakeTocLink(
                    KeywordInformation keyword,
            ILogger log = null)
        {
            log?.LogInformationEx("In MakeTocLink", LogVerbosity.Verbose);

            if (keyword.IsMainKeyword)
            {
                if (keyword.IsDisambiguation)
                {
                    return $"term/{keyword.Keyword.MakeSafeFileName()}/disambiguation";
                }

                return $"term/{keyword.Term.TermName}";
            }
            else
            {
                return $"term/{keyword.Term.TermName}/{keyword.Keyword.MakeSafeFileName()}";
            }
        }

        private static string MakeTermText(
            KeywordInformation keyword,
            ILogger log)
        {
            log?.LogInformationEx("In MakeTermText", LogVerbosity.Verbose);
            var term = keyword.Term;

            var redirect = string.Empty;

            if (!keyword.IsMainKeyword)
            {
                redirect += $" ({string.Format(TextHelper.GetText("TermRedirectedFrom"), keyword.Keyword)})";
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
                .Append(Constants.H1)
                .Append(MakeTitleLink(keyword, log))
                .AppendLine(redirect)
                .AppendLine()
                .AppendLine($"> {term.Blurb}")
                .AppendLine()
                .AppendLine($"> [!VIDEO https://www.youtube.com/embed/{term.YouTubeCode}]")
                .AppendLine()
                .AppendLine($"{Constants.H2}{TextHelper.GetText("TermDownload")}")
                .AppendLine()
                .AppendLine($"[{TextHelper.GetText("TermDownloadHere")}](https://msglossarystore.blob.core.windows.net/videos/{term.TermName}.{term.Language.Code}.mp4).")
                .AppendLine();

            if (term.Captions != null
                && term.Captions.Count > 0)
            {
                builder
                .AppendLine($"{Constants.H2}{TextHelper.GetText("TermLanguages")}")
                .AppendLine()
                .AppendLine(TextHelper.GetText("TermCaptions"))
                .AppendLine();

                foreach (var caption in keyword.Term.Captions)
                {
                    builder.AppendLine($"- [{caption.Language}](https://msglossarystore.blob.core.windows.net/captions/{term.TermName}.{term.Language.Code}.{caption.Code}.srt)");
                }

                builder.AppendLine()
                    .AppendLine($"> {TextHelper.GetText("TermCaptionsLearn")}(/glossary/captions).")
                    .AppendLine();
            }

            builder
                .AppendLine($"{Constants.H2}{TextHelper.GetText("TermLinks")}");

            foreach (var linkSection in term.Links)
            {
                builder.AppendLine()
                    .AppendLine($"{Constants.H3}{linkSection.Key}")
                    .AppendLine();

                foreach (var link in linkSection.Value)
                {
                    builder.AppendLine(link);
                }
            }

            builder.AppendLine()
                .AppendLine($"{Constants.H2}{TextHelper.GetText("TermTranscript")}")
                .AppendLine()
                .AppendLine(term.Transcript)
                .AppendLine();

            if (term.Authors != null
                && term.Authors.Count > 0)
            {
                builder
                    .AppendLine($"{Constants.H2}{TextHelper.GetText("TermAuthors")}")
                    .AppendLine()
                    .Append($"{TextHelper.GetText("TermCreatedBy")} ");

                foreach (var author in term.Authors)
                {
                    builder.Append($"[{author.Name}](http://twitter.com/{author.Twitter}), ");
                }

                builder.Remove(builder.Length - 2, 2);
            }

            builder.AppendLine();
            log?.LogInformationEx("Out MakeTermText", LogVerbosity.Verbose);
            return builder.ToString();
        }

        private static string MakeTermTextWithoutVideo(
            KeywordInformation keyword,
            ILogger log)
        {
            log?.LogInformationEx("In MakeTermTextWithoutVideo", LogVerbosity.Verbose);
            var term = keyword.Term;

            var redirect = string.Empty;

            if (!keyword.IsMainKeyword)
            {
                redirect += $" ({string.Format(TextHelper.GetText("TermRedirectedFrom"), keyword.Keyword)}";
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
                .Append(Constants.H1)
                .Append(MakeTitleLink(keyword, log))
                .AppendLine(redirect)
                .AppendLine()
                .AppendLine($"> {term.Blurb}")
                .AppendLine()
                .AppendLine($"{Constants.H2}{TextHelper.GetText("TermDefinition")}")
                .AppendLine()
                .AppendLine(term.Transcript)
                .AppendLine();

            builder
                .AppendLine($"{Constants.H2}{TextHelper.GetText("TermLinks")}");

            foreach (var linkSection in term.Links)
            {
                builder.AppendLine()
                    .AppendLine($"{Constants.H3}{linkSection.Key}")
                    .AppendLine();

                foreach (var link in linkSection.Value)
                {
                    builder.AppendLine(link);
                }
            }

            if (term.Authors != null
                && term.Authors.Count > 0)
            {
                builder.AppendLine()
                    .AppendLine($"{Constants.H2}{TextHelper.GetText("TermAuthors")}")
                    .AppendLine()
                    .Append($"{TextHelper.GetText("TermCreatedBy")} ");

                foreach (var author in term.Authors)
                {
                    builder.Append($"[{author.Name}](http://twitter.com/{author.Twitter}), ");
                }

                builder.Remove(builder.Length - 2, 2);
            }

            builder.AppendLine();
            log?.LogInformationEx("Out MakeTermTextWithoutVideo", LogVerbosity.Verbose);
            return builder.ToString();
        }

        public static Task<GlossaryFileInfo> CreateDisambiguationFile(
            IList<KeywordInformation> keywords,
            ILogger log = null)
        {
            log?.LogInformationEx("In CreateDisambiguationFile", LogVerbosity.Verbose);
            var result = new GlossaryFileInfo();
            var tcs = new TaskCompletionSource<GlossaryFileInfo>();

            try
            {
                var firstKeyword = keywords.First();

                string path = $"glossary/term/{firstKeyword.Keyword.MakeSafeFileName()}/disambiguation.md";

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

        public static Task<GlossaryFileInfo> CreateKeywordFile(
            KeywordInformation keyword,
            ILogger log = null)
        {
            log?.LogInformationEx("In CreateKeywordFile", LogVerbosity.Verbose);

            var tcs = new TaskCompletionSource<GlossaryFileInfo>();
            var result = new GlossaryFileInfo();

            try
            {
                string path = null;

                if (keyword.IsMainKeyword)
                {
                    path = $"glossary/term/{keyword.Term.TermName.MakeSafeFileName()}/index.md";
                }
                else
                {
                    path = $"glossary/term/{keyword.Term.TermName.MakeSafeFileName()}/{keyword.Keyword.MakeSafeFileName()}.md";
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

        public static Task<GlossaryFileInfo> CreateTableOfContentsFile(
            IList<KeywordInformation> keywords,
            ILogger log = null)
        {
            log?.LogInformationEx("In CreateTableOfContentsFile", LogVerbosity.Verbose);
            var result = new GlossaryFileInfo();
            var tcs = new TaskCompletionSource<GlossaryFileInfo>();

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
                            .AppendLine($"- name: {mainKeyword.Keyword} ({TextHelper.GetText("TermDisambiguation")})")
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

                        foreach (var k in g.Where(k => !k.IsMainKeyword).OrderBy(k => k.Keyword))
                        {
                            tocBuilder
                                .AppendLine($"  - name: {k.Keyword}")
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

        public static async Task<TermInformation> CreateTerm(
            Uri uri,
            ILogger log)
        {
            log?.LogInformationEx("In CreateTerm", LogVerbosity.Verbose);

            var term = new TermInformation
            {
                Uri = uri
            };

            var termBlob = new CloudBlockBlob(uri);
            term.TermName = Path.GetFileNameWithoutExtension(termBlob.Name);
            term.TermName = Path.GetFileNameWithoutExtension(term.TermName);

            log?.LogInformationEx($"Term: {term.TermName}", LogVerbosity.Verbose);

            string oldMarkdown = await termBlob.DownloadTextAsync();
            var markdownReader = new StringReader(oldMarkdown);

            string youTubeCode = null;
            string keywordsLine = null;
            string termTitle = null;
            string blurb = null;
            string captions = null;
            string language = null;
            string authorName = null;
            string email = null;
            string twitter = null;
            string github = null;
            DateTime recordingDate = DateTime.MinValue;
            var isTranscript = false;
            var isLinks = false;
            var transcript = new StringBuilder();
            var links = new Dictionary<string, IList<string>>();
            IList<string> currentLinksSection = null;
            string line;

            while ((line = markdownReader.ReadLine()) != null)
            {
                if (line.StartsWith(Constants.Input.TranscriptMarker))
                {
                    isLinks = false;
                    isTranscript = true;
                    continue;
                }
                else if (line.StartsWith(Constants.Input.LinksMarker))
                {
                    isLinks = true;
                    isTranscript = false;
                    continue;
                }
                else if (isTranscript)
                {
                    transcript.AppendLine(line);
                }
                else if (isLinks)
                {
                    if (string.IsNullOrEmpty(line.Trim()))
                    {
                        continue;
                    }

                    if (line.StartsWith(Constants.H3))
                    {
                        currentLinksSection = new List<string>();
                        links.Add(line.Substring(Constants.H3.Length).Trim(), currentLinksSection);
                        continue;
                    }

                    currentLinksSection.Add(line);
                }
                else if (line.StartsWith(Constants.H1))
                {
                    termTitle = line
                        .Substring(Constants.H1.Length)
                        .Trim();
                }
                else if (line.StartsWith(Constants.Input.YouTubeMarker))
                {
                    youTubeCode = line.Substring(Constants.Input.YouTubeMarker.Length).Trim();
                    log?.LogInformationEx($"youTubeCode: {youTubeCode}", LogVerbosity.Debug);
                }
                else if (line.StartsWith(Constants.Input.KeywordsMarker))
                {
                    keywordsLine = line.Substring(Constants.Input.KeywordsMarker.Length).Trim();
                    log?.LogInformationEx($"keywordsLine: {keywordsLine}", LogVerbosity.Debug);
                }
                else if (line.StartsWith(Constants.Input.BlurbMarker))
                {
                    blurb = line.Substring(Constants.Input.BlurbMarker.Length).Trim();
                    log?.LogInformationEx($"blurb: {blurb}", LogVerbosity.Debug);
                }
                else if (line.StartsWith(Constants.Input.CaptionsMarker))
                {
                    captions = line.Substring(Constants.Input.CaptionsMarker.Length).Trim();
                    log?.LogInformationEx($"captions: {captions}", LogVerbosity.Debug);
                }
                else if (line.StartsWith(Constants.Input.LanguageMarker))
                {
                    language = line.Substring(Constants.Input.LanguageMarker.Length).Trim();
                    log?.LogInformationEx($"language: {language}", LogVerbosity.Debug);
                }
                else if (line.StartsWith(Constants.Input.AuthorNameMarker))
                {
                    authorName = line.Substring(Constants.Input.AuthorNameMarker.Length).Trim();
                    log?.LogInformationEx($"authorName: {authorName}", LogVerbosity.Debug);
                }
                else if (line.StartsWith(Constants.Input.EmailMarker))
                {
                    email = line.Substring(Constants.Input.EmailMarker.Length).Trim();
                    log?.LogInformationEx($"email: {email}", LogVerbosity.Debug);
                }
                else if (line.StartsWith(Constants.Input.GitHubMarker))
                {
                    github = line.Substring(Constants.Input.GitHubMarker.Length).Trim();
                    log?.LogInformationEx($"github: {github}", LogVerbosity.Debug);
                }
                else if (line.StartsWith(Constants.Input.TwitterMarker))
                {
                    twitter = line.Substring(Constants.Input.TwitterMarker.Length).Trim();
                    log?.LogInformationEx($"twitter: {twitter}", LogVerbosity.Debug);
                }
                else if (line.StartsWith(Constants.Input.RecordingDateMarker))
                {
                    var dateString = line.Substring(Constants.Input.RecordingDateMarker.Length).Trim();
                    log?.LogInformationEx($"dateString: {dateString}", LogVerbosity.Debug);
                    recordingDate = DateTime.ParseExact(dateString, "yyyyMMdd", CultureInfo.InvariantCulture);
                }
            }

            term.Title = termTitle;
            term.Transcript = transcript.ToString().Trim();
            term.Links = links;
            term.RecordingDate = recordingDate;
            term.YouTubeCode = youTubeCode;
            term.Blurb = blurb;
            term.Authors = MakeAuthors(authorName, email, github, twitter, log);
            term.Captions = MakeLanguages(captions, log);
            term.Language = MakeLanguages(language, log).First();
            term.Keywords = keywordsLine.Split(new char[]
            {
                ','
            }, StringSplitOptions.RemoveEmptyEntries)
                .Select(k => k.Trim())
                .ToList();

            log?.LogInformationEx("Out CreateTerm", LogVerbosity.Verbose);
            return term;
        }

        public static Task<IList<KeywordInformation>> SortDisambiguations(
            IList<KeywordInformation> keywords,
            ILogger log = null)
        {
            log?.LogInformationEx("In SortDisambiguations", LogVerbosity.Verbose);

            var tcs = new TaskCompletionSource<IList<KeywordInformation>>();

            var disambiguations = keywords
                .Where(k => k.MustDisambiguate)
                .GroupBy(k => k.Keyword.ToLower())
                .ToList();

            foreach (var group in disambiguations)
            {
                var first = group.First();
                var termName = first.Keyword.MakeSafeFileName();

                keywords.Add(new KeywordInformation
                {
                    IsMainKeyword = true,
                    Keyword = first.Keyword,
                    MustDisambiguate = false,
                    Term = new TermInformation
                    {
                        Title = first.Keyword,
                        TermName = "disambiguation"
                    },
                    TermName = "disambiguation",
                    IsDisambiguation = true
                });
            }

            tcs.SetResult(keywords);
            log?.LogInformationEx("Out SortDisambiguations", LogVerbosity.Verbose);
            return tcs.Task;
        }

        public static Task<IList<KeywordInformation>> SortKeywords(
            IList<TermInformation> allTerms,
            TermInformation currentTerm,
            ILogger log = null)
        {
            log?.LogInformationEx("In SortKeywords", LogVerbosity.Verbose);

            var tcs = new TaskCompletionSource<IList<KeywordInformation>>();

            var result = new List<KeywordInformation>();

            foreach (var keyword in currentTerm.Keywords)
            {
                var newKeyword = new KeywordInformation
                {
                    Keyword = keyword,
                    TermName = currentTerm.TermName
                };

                var sameKeywords = allTerms
                    .SelectMany(t => t.Keywords)
                    .Where(k => k.ToLower() == keyword.ToLower());

                if (sameKeywords.Count() > 1)
                {
                    newKeyword.MustDisambiguate = true;
                }

                if (newKeyword.Keyword.MakeSafeFileName().ToLower() == currentTerm.TermName.ToLower())
                {
                    newKeyword.IsMainKeyword = true;
                }

                result.Add(newKeyword);
            }

            if (!result.Any(k => k.IsMainKeyword))
            {
                var mainKeyword = new KeywordInformation
                {
                    IsMainKeyword = true,
                    Keyword = currentTerm.Title,
                    TermName = currentTerm.TermName
                };

                result.Add(mainKeyword);
            }

            tcs.SetResult(result);
            log?.LogInformationEx("Out SortKeywords", LogVerbosity.Verbose);
            return tcs.Task;
        }

        public static async Task<GlossaryFileInfo> VerifyFile(GlossaryFileInfo file)
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