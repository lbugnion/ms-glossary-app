using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
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
                new KeywordPair("app-service", "app-service", "App Service"),
                new KeywordPair("app-service", "web-server", "Web Server"),
                new KeywordPair("app-service", "asp.net", "ASP.NET"),
                new KeywordPair("app-service", "web-api", "Web Api"),
                new KeywordPair("aad", "aad", "AAD")
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
                string.Format(KeywordReplacer.KeywordLinkTemplate, "app service", "app-service", "app-service"));

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestOneOccurenceOfSubtopic()
        {
            var list = MakeList1();

            var markdown = "This is a piece of text with the word web server in it once";

            var replacer = new KeywordReplacer();

            var result = replacer.ReplaceInMarkdown(
                markdown,
                list);

            var expectedResult = markdown.Replace(
                "web server",
                string.Format(KeywordReplacer.KeywordLinkTemplate, "web server", "app-service", "web-server"));

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
                string.Format(KeywordReplacer.KeywordLinkTemplate, "App Service", "app-service", "app-service"));

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
                + string.Format(KeywordReplacer.KeywordLinkTemplate, "App Service", "app-service", "app-service")
                + markdown.Substring(markdown.IndexOf("App Service") + "App Service".Length);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestOneOccurenceWhichAlreadyHadTheSameLink()
        {
            var list = MakeList1();

            var markdown = "This is a piece of text with the word [App Service](/topic/app-service) in it once.";

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
        public void TestTwoKeywordsOccurence()
        {
            var list = MakeList1();

            var markdown = "This is a piece of text with the word app service in it once and also the word asp.net to check it.";

            var replacer = new KeywordReplacer();

            var result = replacer.ReplaceInMarkdown(
                markdown,
                list);

            var expectedResult = markdown
                .Replace(
                    "app service",
                    string.Format(KeywordReplacer.KeywordLinkTemplate, "app service", "app-service", "app-service"))
                .Replace(
                    "asp.net",
                    string.Format(KeywordReplacer.KeywordLinkTemplate, "asp.net", "app-service", "asp.net"));

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestKeywordInLink()
        {
            var list = MakeList1();

            var markdown = "This is a piece of text with the word App Service and [a link to the AAD topic](http://abcd.com/aad) in it once and also the word asp.net to check it.";
            var expectedMarkdown = "This is a piece of text with the word [App Service](/topic/app-service/app-service) and [a link to the AAD topic](http://abcd.com/aad) in it once and also the word [asp.net](/topic/app-service/asp.net) to check it.";

            var replacer = new KeywordReplacer();

            var result = replacer.ReplaceInMarkdown(
                markdown,
                list);

            Assert.AreEqual(expectedMarkdown, result);
        }


        [TestMethod]
        public void TestKeywordInParenthesis()
        {
            var list = MakeList1();

            var markdown = "This is a piece of text with the word (App Service) and [a link to the AAD topic](http://abcd.com/aad) in it once and also the word asp.net to check it.";
            var expectedMarkdown = "This is a piece of text with the word ([App Service](/topic/app-service/app-service)) and [a link to the AAD topic](http://abcd.com/aad) in it once and also the word [asp.net](/topic/app-service/asp.net) to check it.";

            var replacer = new KeywordReplacer();

            var result = replacer.ReplaceInMarkdown(
                markdown,
                list);

            Assert.AreEqual(expectedMarkdown, result);
        }

        [TestMethod]
        public void TestKeywordBeforeAndInTranscript()
        {
            var list = MakeList1();

            var markdown = "This is a piece of text with the word App Service in the intro"
                + Environment.NewLine
                + Environment.NewLine
                + "## Transcript"
                + Environment.NewLine
                + Environment.NewLine
                + "and the words App Service in the transcript";

            var expectedMarkdown = "This is a piece of text with the word App Service in the intro"
                + Environment.NewLine
                + Environment.NewLine
                + "## Transcript"
                + Environment.NewLine
                + Environment.NewLine
                + "and the words [App Service](/topic/app-service/app-service) in the transcript";

            var replacer = new KeywordReplacer();

            var result = replacer.ReplaceInMarkdown(
                markdown,
                list);

            Assert.AreEqual(expectedMarkdown, result);
        }

        [TestMethod]
        public void TestCase01()
        {
            var markdown = "[This is AAD](http://test.com/aad/hello)";
            var result = DoTestCase(markdown);
            Assert.AreEqual(markdown, result);
        }

        [TestMethod]
        public void TestCase02()
        {
            var markdown = "[This is AAD](http://test.com/aad/hello) and this is also AAD to be encoded";
            var expected = "[This is AAD](http://test.com/aad/hello) and this is also [AAD](/topic/aad/aad) to be encoded";
            var result = DoTestCase(markdown);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestCase03()
        {
            var markdown = "This is AAD and this is also AAD to be ignored";
            var expected = "This is [AAD](/topic/aad/aad) and this is also AAD to be ignored";
            var result = DoTestCase(markdown);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestCase04()
        {
            var markdown = "[This is AAD](http://test.com/aad/hello) and this is also AAD to be encoded and this is also AAD to be ignored";
            var expected = "[This is AAD](http://test.com/aad/hello) and this is also [AAD](/topic/aad/aad) to be encoded and this is also AAD to be ignored";
            var result = DoTestCase(markdown);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestCase05()
        {
            var markdown = "[This is AAD and nothing else";
            var expected = "[This is [AAD](/topic/aad/aad) and nothing else";
            var result = DoTestCase(markdown);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestCase06()
        {
            var markdown = "[Hello](http://test.com/aad/hello)";
            var expected = "[Hello](http://test.com/aad/hello)";
            var result = DoTestCase(markdown);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestCase07()
        {
            var markdown = "[Some link](http://test.com/hello) some text (this is some AAD in parenthesis) and hello";
            var expected = "[Some link](http://test.com/hello) some text (this is some [AAD](/topic/aad/aad) in parenthesis) and hello";
            var result = DoTestCase(markdown);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestCase08()
        {
            var markdown = "Hello [a link](http://xxxx) and (my AAD is here) and hello";
            var expected = "Hello [a link](http://xxxx) and (my [AAD](/topic/aad/aad) is here) and hello";
            var result = DoTestCase(markdown);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestCase09()
        {
            var markdown = "Hello [a link](http://xxxx) and my AAD is here and hello";
            var expected = "Hello [a link](http://xxxx) and my [AAD](/topic/aad/aad) is here and hello";
            var result = DoTestCase(markdown);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestCase10()
        {
            var markdown = "Hello [a link](http://xxxx) and [my AAD is here] and hello";
            var expected = "Hello [a link](http://xxxx) and [my [AAD](/topic/aad/aad) is here] and hello";
            var result = DoTestCase(markdown);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestCase11()
        {
            var markdown = "Hello [a link](http://xxxx) and [my AAD is here](http://xxxx) and hello";
            var expected = "Hello [a link](http://xxxx) and [my AAD is here](http://xxxx) and hello";
            var result = DoTestCase(markdown);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestCase12()
        {
            var markdown = "Hello [a link](http://xxxx) and (my AAD is here)";
            var expected = "Hello [a link](http://xxxx) and (my [AAD](/topic/aad/aad) is here)";
            var result = DoTestCase(markdown);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestCase13()
        {
            var markdown = "Hello [a link](http://xxxx) and my AAD is here";
            var expected = "Hello [a link](http://xxxx) and my [AAD](/topic/aad/aad) is here";
            var result = DoTestCase(markdown);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestCase14()
        {
            var markdown = "Hello [a link](http://xxxx) and [my AAD is here]";
            var expected = "Hello [a link](http://xxxx) and [my [AAD](/topic/aad/aad) is here]";
            var result = DoTestCase(markdown);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestCase15()
        {
            var markdown = "Hello [a link](http://xxxx) and [my AAD is here](http://xxxx)";
            var expected = "Hello [a link](http://xxxx) and [my AAD is here](http://xxxx)";
            var result = DoTestCase(markdown);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestCase16()
        {
            var markdown = "Hello [a link](http://xxxx) and )my AAD is here)";
            var expected = "Hello [a link](http://xxxx) and )my [AAD](/topic/aad/aad) is here)";
            var result = DoTestCase(markdown);
            Assert.AreEqual(expected, result);
        }

        private string DoTestCase(string markdown)
        {
            var list = new List<KeywordPair>
            {
                new KeywordPair("aad", "aad", "AAD")
            };

            var replacer = new KeywordReplacer();

            var result = replacer.ReplaceInMarkdown(
                markdown,
                list);

            return result;
        }
    }
}
