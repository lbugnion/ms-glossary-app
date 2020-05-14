using System.Collections.Generic;

namespace WordsOfTheDayApp.Model
{
    public class TopicLanguageInfo
    {
        public string LanguageCode
        {
            get;
            set;
        }

        public IList<TopicInformation> Topics
        {
            get;
            set;
        }
    }
}