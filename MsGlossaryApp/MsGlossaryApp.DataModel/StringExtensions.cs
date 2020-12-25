using System;
using System.Net;

namespace MsGlossaryApp.DataModel
{
    public static class MarkdownExtensions
    {
        private const string H1Marker = "# ";
        private const string H2Marker = "## ";
        private const string H3Marker = "### ";
        private const string ListMarker = "-";
        private const string NoteMarker = ">";
        private const string LinkTextOpener = "[";
        private const string LinkSeparator = "](";
        private const string LinkUrlCloser = ")";

        public static string MakeLink(this string text, string url)
        {
            return $"{LinkTextOpener}{text.Trim()}{LinkSeparator}{url.Trim()}{LinkUrlCloser}";
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

            if (!parts[0].StartsWith(LinkTextOpener)
                || !parts[1].EndsWith(LinkUrlCloser))
            {
                return null;
            }

            parts[0] = parts[0].Substring(LinkTextOpener.Length).Trim();
            parts[1] = parts[1].Substring(0, parts[1].Length - LinkTextOpener.Length).Trim();

            return new Link
            {
                Text = parts[0],
                Url = parts[1]
            };
        }

        public static bool IsLink(this string text)
        {
            text = text.Trim();

            return text.StartsWith(LinkTextOpener)
                && text.Contains(LinkSeparator)
                && text.EndsWith(LinkUrlCloser);
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

        private static string Make(string line, string marker)
        {
            return $"{marker} {line.Trim()}";
        }

        public static string MakeListItem(this string line)
        {
            return Make(line, ListMarker);
        }

        public static string MakeNote(this string line)
        {
            return Make(line, NoteMarker);
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

        public static bool IsListItem(this string line)
        {
            return Is(line, ListMarker);
        }

        public static string ParseListItem(this string line)
        {
            return Parse(line, ListMarker);
        }

        public static bool IsNote(this string line)
        {
            return Is(line, NoteMarker);
        }

        public static string ParseNote(this string line)
        {
            return Parse(line, NoteMarker);
        }

        public static bool IsH1(this string line)
        {
            return Is(line, H1Marker);
        }

        public static string ParseH1(this string line)
        {
            return Parse(line, H1Marker);
        }

        public static bool IsH2(this string line)
        {
            return Is(line, H2Marker);
        }

        public static string ParseH2(this string line)
        {
            return Parse(line, H2Marker);
        }

        public static bool IsH3(this string line)
        {
            return Is(line, H3Marker);
        }

        public static string ParseH3(this string line)
        {
            return Parse(line, H3Marker);
        }

        private static bool Is(string line, string marker)
        {
            return line.StartsWith(marker);
        }

        private static string Parse(string line, string marker)
        {
            if (Is(line, marker))
            {
                line = line.Substring(marker.Length).Trim();
            }

            return line;
        }

        public static string MakeYouTubeVideo(this string code)
        {
            return $"[!VIDEO https://www.youtube.com/embed/{code}]";
        }
    }
}