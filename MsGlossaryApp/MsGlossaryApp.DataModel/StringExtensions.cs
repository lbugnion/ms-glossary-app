using System;

namespace MsGlossaryApp.DataModel
{
    public static class MarkdownExtensions
    {
        private const string H1Marker = "# ";
        private const string H2Marker = "## ";
        private const string H3Marker = "### ";
        private const string LinkSeparator = "](";
        private const string LinkTextOpener = "[";
        private const string LinkUrlCloser = ")";
        private const string ListMarker = "-";
        private const string NoteMarker = ">";

        private static bool Is(string line, string marker)
        {
            return line.StartsWith(marker);
        }

        private static string Make(string line, string marker)
        {
            return $"{marker.Trim()} {line.Trim()}";
        }

        private static string Parse(string line, string marker)
        {
            if (Is(line, marker))
            {
                line = line.Substring(marker.Length).Trim();
            }

            return line;
        }

        public static bool IsH1(this string line)
        {
            return Is(line, H1Marker);
        }

        public static bool IsH2(this string line)
        {
            return Is(line, H2Marker);
        }

        public static bool IsH3(this string line)
        {
            return Is(line, H3Marker);
        }

        public static bool IsLink(this string text)
        {
            text = text.Trim();

            return text.StartsWith(LinkTextOpener)
                && text.Contains(LinkSeparator)
                && text.EndsWith(LinkUrlCloser);
        }

        public static bool IsListItem(this string line)
        {
            return Is(line, ListMarker);
        }

        public static bool IsNote(this string line)
        {
            return Is(line, NoteMarker);
        }

        public static string MakeH1(this string line)
        {
            return Make(line, H1Marker);
        }

        public static string MakeH2(this string line)
        {
            return Make(line, H2Marker);
        }

        public static string MakeH3(this string line)
        {
            return Make(line, H3Marker);
        }

        public static string MakeLink(this string text, string url)
        {
            return $"{LinkTextOpener}{text.Trim()}{LinkSeparator}{url.Trim()}{LinkUrlCloser}";
        }

        public static string MakeListItem(this string line)
        {
            return Make(line, ListMarker);
        }

        public static string MakeNote(this string line)
        {
            return Make(line, NoteMarker);
        }

        public static string MakeSafeFileName(this string term)
        {
            return term
                .Trim()
                .ToLower()
                .Replace(' ', '-')
                .Replace('/', '-')
                .Replace('.', '-');
        }

        public static string MakeYouTubeVideo(this string code)
        {
            return $"[!VIDEO https://www.youtube.com/embed/{code}]";
        }

        public static string ParseH1(this string line)
        {
            return Parse(line, H1Marker);
        }

        public static string ParseH2(this string line)
        {
            return Parse(line, H2Marker);
        }

        public static string ParseH3(this string line)
        {
            return Parse(line, H3Marker);
        }

        public static Link ParseLink(this string text)
        {
            text = text.Trim();

            if (!text.IsLink())
            {
                return null;
            }

            var parts = text.Split(new[]
                {
                    LinkSeparator
                },
                StringSplitOptions.RemoveEmptyEntries
            );

            string url = null, note = null;

            var indexOfEndOfUrl = parts[1].IndexOf(") ");

            if (indexOfEndOfUrl > -1)
            {
                url = parts[1].Substring(0, indexOfEndOfUrl + 1).Trim();
                note = parts[1].Substring(indexOfEndOfUrl + 1).Trim();
            }
            else
            {
                url = parts[1];
            }

            if (!parts[0].StartsWith(LinkTextOpener)
                || !url.EndsWith(LinkUrlCloser))
            {
                return null;
            }

            parts[0] = parts[0].Substring(LinkTextOpener.Length).Trim();
            url = url.Substring(0, url.Length - LinkUrlCloser.Length).Trim();

            return new Link
            {
                Text = parts[0],
                Url = url,
                Note = note
            };
        }

        public static string ParseListItem(this string line)
        {
            return Parse(line, ListMarker);
        }

        public static string ParseNote(this string line)
        {
            return Parse(line, NoteMarker);
        }
    }
}