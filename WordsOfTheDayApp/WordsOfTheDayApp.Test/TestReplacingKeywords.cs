using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using WordsOfTheDayApp.Model;

namespace WordsOfTheDayApp.Test
{
    [TestClass]
    public class TestReplacingLinks
    {
        public List<KeywordPair> MakeList1()
        {
            return new List<KeywordPair>
            {
                new KeywordPair("app-service", "app service"),
                new KeywordPair("app-service", "web server"),
                new KeywordPair("app-service", "asp.net"),
                new KeywordPair("app-service", "web api")
            };
        }

        public List<KeywordPair> MakeList2()
        {
            return new List<KeywordPair>
            {
                new KeywordPair("app-service", "app service"),
                new KeywordPair("app-service", "web server"),
                new KeywordPair("app-service", "asp.net"),
                new KeywordPair("app-service", "web api"),
                new KeywordPair("app-service2", "app service")
            };
        }

        [TestMethod]
        public void TestOneOccurence()
        {
            var list = MakeList1();

            var markdown = "This is a piece of text with the word app service in it once";

            var replacer = new KeywordReplacer();

            var result = replacer.ReplaceInMarkdown(
                markdown,
                list);

            var expectedResult = markdown.Replace(
                "app service",
                string.Format(KeywordReplacer.KeywordLinkTemplate, "app service", "app-service"));

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestOneOccurenceWithCaps()
        {
            var list = MakeList1();

            var markdown = "This is a piece of text with the word App Service in it once";

            var replacer = new KeywordReplacer();

            var result = replacer.ReplaceInMarkdown(
                markdown,
                list);

            var expectedResult = markdown.Replace(
                "App Service",
                string.Format(KeywordReplacer.KeywordLinkTemplate, "App Service", "app-service"));

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestTwoOccurence()
        {
            var list = MakeList1();

            var markdown = "This is a piece of text with the word App Service in it twice because of app service.";

            var replacer = new KeywordReplacer();

            var result = replacer.ReplaceInMarkdown(
                markdown,
                list);

            var expectedResult = markdown.Substring(0, markdown.IndexOf("App Service"))
                + string.Format(KeywordReplacer.KeywordLinkTemplate, "App Service", "app-service")
                + markdown.Substring(markdown.IndexOf("App Service") + "App Service".Length);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestOneOccurenceWhichAlreadyHadTheSameLink()
        {
            var list = MakeList1();

            var markdown = "This is a piece of text with the word [App Service](https://wordsoftheday.azurewebsites.net/topic/app-service) in it once.";

            var replacer = new KeywordReplacer();

            var result = replacer.ReplaceInMarkdown(
                markdown,
                list);

            var expectedResult = markdown;

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestOneOccurenceWhichAlreadyHadAnotherLink()
        {
            var list = MakeList1();

            var markdown = "This is a piece of text with the word [App Service](https://wordsoftheday.azurewebsites.net/topic/app-serv) in it once.";
            var expectedMarkdown = "This is a piece of text with the word [App Service](https://wordsoftheday.azurewebsites.net/topic/app-service) in it once.";

            var replacer = new KeywordReplacer();

            var result = replacer.ReplaceInMarkdown(
                markdown,
                list);

            Assert.AreEqual(expectedMarkdown, result);
        }

        [TestMethod]
        public void TestOneOccurenceWithTwoSameKeywords()
        {
            var list = MakeList2();

            var markdown = "This is a piece of text with the word App Service in it once.";

            // Last one wins
            var expectedMarkdown = "This is a piece of text with the word [App Service](https://wordsoftheday.azurewebsites.net/topic/app-service2) in it once.";

            var replacer = new KeywordReplacer();

            var result = replacer.ReplaceInMarkdown(
                markdown,
                list);

            Assert.AreEqual(expectedMarkdown, result);
        }

        [TestMethod]
        public void TestOneOccurenceWhichAlreadyHadAnotherLinkWithTwoSameKeywords()
        {
            var list = MakeList2();

            var markdown = "This is a piece of text with the word [App Service](https://wordsoftheday.azurewebsites.net/topic/app-serv) in it once.";

            // Last one wins
            var expectedMarkdown = "This is a piece of text with the word [App Service](https://wordsoftheday.azurewebsites.net/topic/app-service2) in it once.";

            var replacer = new KeywordReplacer();

            var result = replacer.ReplaceInMarkdown(
                markdown,
                list);

            Assert.AreEqual(expectedMarkdown, result);
        }


        [TestMethod]
        public void TestTwoKeywordsOccurence()
        {
            var list = MakeList1();

            var markdown = "This is a piece of text with the word app service in it onceand also the word asp.net to check it.";

            var replacer = new KeywordReplacer();

            var result = replacer.ReplaceInMarkdown(
                markdown,
                list);

            var expectedResult = markdown
                .Replace(
                    "app service",
                    string.Format(KeywordReplacer.KeywordLinkTemplate, "app service", "app-service"))
                .Replace(
                    "asp.net",
                    string.Format(KeywordReplacer.KeywordLinkTemplate, "asp.net", "app-service"));

            Assert.AreEqual(expectedResult, result);
        }
    }
}
