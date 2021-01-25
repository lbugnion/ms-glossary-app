using System;

namespace MsGlossaryApp.DataModel
{
    public static class MarkdownExtensions
    {
        private const string H1Marker = "# ";
        private const string H2Marker = "## ";
        private const string H3Marker = "### ";
        private const string ImageTextOpener = "![";
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

        public static bool IsImage(this string text)
        {
            text = text.Trim();

            return text.StartsWith(ImageTextOpener)
                && text.Contains(LinkSeparator);
        }

        public static bool IsLink(this string text)
        {
            text = text.Trim();

            return text.StartsWith(LinkTextOpener)
                && text.Contains(LinkSeparator)
                && text.Contains(LinkUrlCloser);
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

        public static string MakeImage(this string title, string url)
        {
            return $"{ImageTextOpener}{title.Trim()}{LinkSeparator}{url.Trim()}{LinkUrlCloser}";
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
                .Replace('.', '-')
                .Replace('\'', '-')
                .Replace(',', '-')
                .Replace("---", "-");
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

        public static Image ParseImage(this string text)
        {
            text = text.Trim();

            if (!text.IsImage())
            {
                return null;
            }

            var parts = text.Split(new[]
                {
                    LinkSeparator
                },
                StringSplitOptions.RemoveEmptyEntries
            );

            if (!parts[0].StartsWith(ImageTextOpener)
                || !parts[1].EndsWith(LinkUrlCloser))
            {
                return null;
            }

            parts[0] = parts[0].Substring(ImageTextOpener.Length).Trim();
            parts[1] = parts[1].Substring(0, parts[1].Length - LinkUrlCloser.Length).Trim();

            var image = new Image
            {
                Title = parts[0],
                Url = parts[1]
            };

            return image;
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

            string url;
            string note = null;

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

            var link = new Link
            {
                Url = url,
                Note = note
            };

            link.Text = parts[0]; // Assign last to avoid overwriting Text property
            return link;
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