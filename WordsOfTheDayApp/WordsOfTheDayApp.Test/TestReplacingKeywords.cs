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
            var expected = "This is a piece of text with the word [app service](/topic/app-service) in it once";

            var replacer = new KeywordReplacer();

            var result = replacer.ReplaceInMarkdown(
                markdown,
                list);

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestOneOccurenceOfSubtopic()
        {
            var list = MakeList1();

            var markdown = "This is a piece of text with the word web server in it once";
            var expected = "This is a piece of text with the word [web server](/topic/app-service/web-server) in it once";

            var replacer = new KeywordReplacer();

            var result = replacer.ReplaceInMarkdown(
                markdown,
                list);

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestOneOccurenceWithCaps()
        {
            var list = MakeList1();

            var markdown = "This is a piece of text with the word App Service in it once";
            var expected = "This is a piece of text with the word [App Service](/topic/app-service) in it once";

            var replacer = new KeywordReplacer();

            var result = replacer.ReplaceInMarkdown(
                markdown,
                list);

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestTwoOccurence()
        {
            var list = MakeList1();

            var markdown = "This is a piece of text with the word App Service in it twice because of app service.";
            var expected = "This is a piece of text with the word [App Service](/topic/app-service) in it twice because of app service.";

            var replacer = new KeywordReplacer();

            var result = replacer.ReplaceInMarkdown(
                markdown,
                list);

            Assert.AreEqual(expected, result);
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
            var expectedMarkdown = "This is a piece of text with the word [App Service](https://wordsoftheday.azurewebsites.net/topic/app-serv) in it once.";

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
            var expected = "This is a piece of text with the word [app service](/topic/app-service) in it once and also the word [asp.net](/topic/app-service/asp.net) to check it.";

            var replacer = new KeywordReplacer();

            var result = replacer.ReplaceInMarkdown(
                markdown,
                list);

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestKeywordInLink()
        {
            var list = MakeList1();

            var markdown = "This is a piece of text with the word App Service and [a link to the AAD topic](http://abcd.com/aad) in it once and also the word asp.net to check it.";
            var expectedMarkdown = "This is a piece of text with the word [App Service](/topic/app-service) and [a link to the AAD topic](http://abcd.com/aad) in it once and also the word [asp.net](/topic/app-service/asp.net) to check it.";

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
            var expectedMarkdown = "This is a piece of text with the word ([App Service](/topic/app-service)) and [a link to the AAD topic](http://abcd.com/aad) in it once and also the word [asp.net](/topic/app-service/asp.net) to check it.";

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
                + "and the words [App Service](/topic/app-service) in the transcript";

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
            var expected = "[This is AAD](http://test.com/aad/hello) and this is also [AAD](/topic/aad) to be encoded";
            var result = DoTestCase(markdown);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestCase03()
        {
            var markdown = "This is AAD and this is also AAD to be ignored";
            var expected = "This is [AAD](/topic/aad) and this is also AAD to be ignored";
            var result = DoTestCase(markdown);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestCase04()
        {
            var markdown = "[This is AAD](http://test.com/aad/hello) and this is also AAD to be encoded and this is also AAD to be ignored";
            var expected = "[This is AAD](http://test.com/aad/hello) and this is also [AAD](/topic/aad) to be encoded and this is also AAD to be ignored";
            var result = DoTestCase(markdown);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestCase05()
        {
            var markdown = "[This is AAD and nothing else";
            var expected = "[This is [AAD](/topic/aad) and nothing else";
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
            var expected = "[Some link](http://test.com/hello) some text (this is some [AAD](/topic/aad) in parenthesis) and hello";
            var result = DoTestCase(markdown);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestCase08()
        {
            var markdown = "Hello [a link](http://xxxx) and (my AAD is here) and hello";
            var expected = "Hello [a link](http://xxxx) and (my [AAD](/topic/aad) is here) and hello";
            var result = DoTestCase(markdown);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestCase09()
        {
            var markdown = "Hello [a link](http://xxxx) and my AAD is here and hello";
            var expected = "Hello [a link](http://xxxx) and my [AAD](/topic/aad) is here and hello";
            var result = DoTestCase(markdown);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestCase10()
        {
            var markdown = "Hello [a link](http://xxxx) and [my AAD is here] and hello";
            var expected = "Hello [a link](http://xxxx) and [my [AAD](/topic/aad) is here] and hello";
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
            var expected = "Hello [a link](http://xxxx) and (my [AAD](/topic/aad) is here)";
            var result = DoTestCase(markdown);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestCase13()
        {
            var markdown = "Hello [a link](http://xxxx) and my AAD is here";
            var expected = "Hello [a link](http://xxxx) and my [AAD](/topic/aad) is here";
            var result = DoTestCase(markdown);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestCase14()
        {
            var markdown = "Hello [a link](http://xxxx) and [my AAD is here]";
            var expected = "Hello [a link](http://xxxx) and [my [AAD](/topic/aad) is here]";
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
            var expected = "Hello [a link](http://xxxx) and )my [AAD](/topic/aad) is here)";
            var result = DoTestCase(markdown);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestCase17()
        {
            var markdown = "Hello my [AAD](/topic/aad2/aad2) is here)";
            var expected = "Hello my [AAD](/topic/aad) is here)";
            var result = DoTestCase(markdown);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestCase18()
        {
            var markdown = "Hello my [AAD](http://xxxx) is here)";
            var expected = "Hello my [AAD](http://xxxx) is here)";
            var result = DoTestCase(markdown);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestCase18A()
        {
            var markdown = "Hello my [AAD](http://xxxx) is here and another AAD here";
            var expected = "Hello my [AAD](http://xxxx) is here and another [AAD](/topic/aad) here";
            var result = DoTestCase(markdown);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestCase19()
        {
            var markdown = "Hello [a link](http://xxxx) and my [AAD](/topic/aad2/aad2) is here)";
            var expected = "Hello [a link](http://xxxx) and my [AAD](/topic/aad) is here)";
            var result = DoTestCase(markdown);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestCase20()
        {
            var markdown = "Hello my [something else AAD](http://xxxx)";
            var expected = "Hello my [something else AAD](http://xxxx)";
            var result = DoTestCase(markdown);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestCase20A()
        {
            var markdown = "Hello my [something else AAD](/topic/aad2)";
            var expected = "Hello my [something else AAD](/topic/aad)";
            var result = DoTestCase(markdown);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestCase21()
        {
            var markdown = "Hello my [something AAD else](http://xxxx) is here)";
            var expected = "Hello my [something AAD else](http://xxxx) is here)";
            var result = DoTestCase(markdown);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestCase21A()
        {
            var markdown = "Hello my [something AAD else](/topic/aad2) is here)";
            var expected = "Hello my [something AAD else](/topic/aad) is here)";
            var result = DoTestCase(markdown);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestCase22()
        {
            var markdown = "[This is MAADAM](http://test.com/aad/hello)";
            var expected = "[This is MAADAM](http://test.com/aad/hello)";
            var result = DoTestCase(markdown);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestCase23()
        {
            var markdown = "[This is MAADAM](http://test.com/aad/hello) and this is also MAADAM to be ignored";
            var expected = "[This is MAADAM](http://test.com/aad/hello) and this is also MAADAM to be ignored";
            var result = DoTestCase(markdown);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestCase24()
        {
            var markdown = "This is MAADAM and this is also AAD to be encoded";
            var expected = "This is MAADAM and this is also [AAD](/topic/aad) to be encoded";
            var result = DoTestCase(markdown);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestCase25()
        {
            var markdown = "[This is MAADAM](http://test.com/aad/hello) and this is also MAADAM to be ignored and this is also AAD to be encoded";
            var expected = "[This is MAADAM](http://test.com/aad/hello) and this is also MAADAM to be ignored and this is also [AAD](/topic/aad) to be encoded";
            var result = DoTestCase(markdown);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestCase26()
        {
            var markdown = "This is MAADAM and nothing else";
            var expected = "This is MAADAM and nothing else";
            var result = DoTestCase(markdown);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestCase26A()
        {
            var markdown = "This is AADAM and nothing else";
            var expected = "This is AADAM and nothing else";
            var result = DoTestCase(markdown);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestCase26B()
        {
            var markdown = "This is MAAD and nothing else";
            var expected = "This is MAAD and nothing else";
            var result = DoTestCase(markdown);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestCase26C()
        {
            var markdown = "AAD and nothing else";
            var expected = "[AAD](/topic/aad) and nothing else";
            var result = DoTestCase(markdown);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestCase26D()
        {
            var markdown = "Nothing else but AAD";
            var expected = "Nothing else but [AAD](/topic/aad)";
            var result = DoTestCase(markdown);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestCase27()
        {
            var markdown = "Hello my [AAD](/topic/aad2/aad2) is here and another AAD to be ignored";
            var expected = "Hello my [AAD](/topic/aad) is here and another AAD to be ignored";
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
