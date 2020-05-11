﻿using System;
using System.Collections.Generic;
using WordsOfTheDayApp.Model;

namespace WordsOfTheDayApp
{
    public class TopicInformation
    {
        public string Language { get; set; }
        public string TopicName { get; set; }
        public Uri Uri { get; set; }
        public string Keywords { get; set; }
        public string YouTubeCode { get; set; }
        public string Title { get; set; }
        public List<string> MustDisambiguate { get; set; }
        public string Blurb { get; set; }
        public IList<LanguageInfo> Captions { get; set; }
    }
}