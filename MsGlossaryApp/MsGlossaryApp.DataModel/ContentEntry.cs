﻿using System;

namespace MsGlossaryApp.DataModel
{
    public class ContentEntry
    {
        public string Content
        {
            get;
            set;
        }

        public ContentEntry(string content)
        {
            Content = content;
        }

        public ContentEntry()
        {
            Content = "Please fill the entry";
        }

        public override bool Equals(object obj)
        {
            return obj is ContentEntry entry
                && Content == entry.Content;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Content);
        }
    }
}