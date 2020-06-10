using System;
using System.Collections.Generic;

namespace WordsOfTheDayApp.Model
{
    public class TopicInformation
    {
        public IList<LanguageInfo> Captions { get; set; }

        public IList<KeywordPair> Keywords { get; set; }

        public LanguageInfo Language { get; set; }

        public string Title { get; set; }

        public string TopicName { get; set; }

        public Uri Uri { get; set; }

        public string YouTubeCode { get; set; }
    }
}