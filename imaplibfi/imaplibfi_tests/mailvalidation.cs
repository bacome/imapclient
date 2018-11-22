using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using work.bacome.imapclient;
using work.bacome.imapinternals;

namespace work.bacome.imapclient_tests
{
    [TestClass]
    public class Test_cMailValidation
    {
        [TestMethod]
        public void cMailValidation_Tests()
        {
            Assert.IsTrue(cMailValidation.IsDotAtomText("fred.angus.mike"));
            Assert.IsTrue(cMailValidation.IsDotAtomText("fred"));
            Assert.IsFalse(cMailValidation.IsDotAtomText("fred..angus.mike"));
            Assert.IsFalse(cMailValidation.IsDotAtomText(""));
            Assert.IsFalse(cMailValidation.IsDotAtomText("fred,angus.mike"));

            Assert.IsTrue(cMailValidation.IsDomainLiteral("[192.168.1.1]"));
            Assert.IsTrue(cMailValidation.IsDomainLiteral("[ 192 . \t 168 . 1 . 1 ]"));
            Assert.IsTrue(cMailValidation.IsDomainLiteral("[]"));
            Assert.IsFalse(cMailValidation.IsDomainLiteral("["));
            Assert.IsFalse(cMailValidation.IsDomainLiteral("[[]"));
            Assert.IsFalse(cMailValidation.IsDomainLiteral("[192.168.1.1] "));

            Assert.IsTrue(cMailValidation.IsNoFoldLiteral("[192.168.1.1]"));
            Assert.IsFalse(cMailValidation.IsNoFoldLiteral("[ 192 . \t 168 . 1 . 1 ]"));
            Assert.IsTrue(cMailValidation.IsNoFoldLiteral("[]"));
            Assert.IsFalse(cMailValidation.IsNoFoldLiteral("["));
            Assert.IsFalse(cMailValidation.IsNoFoldLiteral("[[]"));
            Assert.IsFalse(cMailValidation.IsNoFoldLiteral("[192.168.1.1] "));

            Assert.IsFalse(cMailValidation.IsSectionPart(""));
            Assert.IsFalse(cMailValidation.IsSectionPart("0"));
            Assert.IsTrue(cMailValidation.IsSectionPart("1"));
            Assert.IsFalse(cMailValidation.IsSectionPart("1."));
            Assert.IsFalse(cMailValidation.IsSectionPart("1.0"));
            Assert.IsTrue(cMailValidation.IsSectionPart("1.1"));

            ZTestLocalPart(" fred   .   angus .   simon ", "fred.angus.simon");
            ZTestLocalPart(" \"fred\"   .   angus .   simon ", "fred.angus.simon");
            Assert.IsFalse(cMailValidation.TryParseLocalPart(" fred   .  .  angus .   simon ", out _));
            ZTestLocalPart(" \"fred..angus\" .   simon ", "fred..angus.simon");
            ZTestLocalPart(" \"fred.\\.angus\" .   simon ", "fred..angus.simon");
            Assert.IsFalse(cMailValidation.TryParseLocalPart(" \"fred.\\\angus\" .   simon ", out _));
            ZTestLocalPart("fred.angus.simon", "fred.angus.simon");
            ZTestLocalPart("\"fred.angus.simon\"", "fred.angus.simon");
            ZTestLocalPart("\"fred angus simon\"", "fred angus simon");
            Assert.IsFalse(cMailValidation.TryParseLocalPart("\"fred angus simon\"  .  ", out _));



            ZTestDomain(" fred   .   angus .   simon ", "fred.angus.simon");
            Assert.IsFalse(cMailValidation.TryParseDomain(" \"fred\"   .   angus .   simon ", out _));
            Assert.IsFalse(cMailValidation.TryParseDomain(" fred   .  .  angus .   simon ", out _));
            ZTestDomain("fred.angus.simon", "fred.angus.simon");
            ZTestDomain(" [] ", "[]");
            ZTestDomain(" [ 192   . 168   . 1 \t   . 1 ] ",  "[192 . 168 . 1 . 1]");
            ZTestDomain("[192.168.1.1]", "[192.168.1.1]");
            ZTestDomain("[192.168\\.1.1]", "[192.168.1.1]");
            Assert.IsFalse(cMailValidation.TryParseDomain("[192.168\\\a1.1]", out _));
            Assert.IsFalse(cMailValidation.TryParseDomain("[192.168.1.1]a", out _));


            ZTestMsgId("<fred.angus.miles@simon.john.lemar>", "<fred.angus.miles@simon.john.lemar>");
            ZTestMsgId("  \t   <   \"fred\"    .  \t angus  .\"miles\"  \r\n @ simon    .   john   \r\n .  lemar > \t     ", "<fred.angus.miles@simon.john.lemar>");
            ZTestMsgId("  \t   <   \"fred\"    .  \t angus  .\"miles\"  \r\n @ [simon.john.lemar] > \t     ", "<fred.angus.miles@[simon.john.lemar]>");
            Assert.IsFalse(cMailValidation.TryParseMsgId("  \t   <   \"fred\"    .  \t angus  .\"miles\"  \r\n @ [simon.john. lemar] > \t     ", out _));
            Assert.IsFalse(cMailValidation.TryParseMsgId("  \t   <   \"fred\"    .  \t angus  .\" miles\"  \r\n @ [simon.john.lemar] > \t     ", out _));
            ZTestMsgId("<this@one>", "<this@one>");
            Assert.IsFalse(cMailValidation.TryParseMsgId("<this@one><this@one>", out _));
            ZTestMsgId("<\"this\"@one>", "<this@one>");
            Assert.IsFalse(cMailValidation.TryParseMsgId("<\"th is\"@one>", out _));
            ZTestMsgId("<this@[on\\a]>", "<this@[ona]>");
            Assert.IsFalse(cMailValidation.TryParseMsgId("<this@[on\\\a]>", out _));


            ZTestMsgIds("<this@one><this@one>", new string[] { "<this@one>", "<this@one>" });
            ZTestMsgIds(" <  \r\n this @ one > \r\n <  \t this @  \t\t one > ", new string[] { "<this@one>", "<this@one>" });
            Assert.IsFalse(cMailValidation.TryParseMsgIds("<this@one><this@one", out _));
            Assert.IsFalse(cMailValidation.TryParseMsgIds("", out _));
            Assert.IsFalse(cMailValidation.TryParseMsgIds("       ", out _));
            ZTestMsgIds("<this@one><\"this\"@one>", new string[] { "<this@one>", "<this@one>" });
            Assert.IsFalse(cMailValidation.TryParseMsgIds("<this@one><\"this \"@one>", out _));
            ZTestMsgIds("<this@one><this@[one]>", new string[] { "<this@one>", "<this@[one]>" });
            ZTestMsgIds("<this@one><this@[ one.two ]>", new string[] { "<this@one>", "<this@[one.two]>" });
            Assert.IsFalse(cMailValidation.TryParseMsgIds("<this@one><this@[ one .two ]>", out _));


            Assert.IsFalse(cMailValidation.TryParsePhrase("", out _));
            Assert.IsFalse(cMailValidation.TryParsePhrase("     \t      ()    \t   (   \t stuff  \t  (more    \t stuff  ))    ", out _));
            ZTestPhrase("    \t      ()  x \t   (   \t stuff  \t  (more    \t stuff  ))    ", "x");
            ZTestPhrase("    \t      ()  x \t   (   \t stuff  \t  (more    \t stuff  ))  \"xxx\"   (extra \"comment\")  ", "x xxx");
            ZTestPhrase("    \t      ()  x \t   (   \t stuff  \t  (more    \t stuff  ))  \"x x\"   (extra \"comment\")  ", "x\"x x\"");
            ZTestPhrase("   Arthur     C.    Clarke  (Author)  ", "Arthur C. Clarke");
            Assert.IsFalse(cMailValidation.TryParsePhrase("   .Arthur     C.    Clarke(, Author)  ", out _));
            Assert.IsFalse(cMailValidation.TryParsePhrase("    Arthur     C.    Clarke,  Author   ", out _));
            ZTestPhrase("   Arthur     C.  \"Clarke,\"Author   ", "Arthur C. Clarke, Author");
            ZTestPhrase("   A.         C.    Clarke   \t\t\t   ", "A. C. Clarke");
            ZTestPhrase("  \"A.\"    \"C.\"  Clarke   \t\r\n   ", "A. C. Clarke");
            ZTestPhrase("   A.         C.    Clarke   \"\"     ", "A. C. Clarke\"\"");
            Assert.IsFalse(cMailValidation.TryParsePhrase(" \"\arthur\"   C.  \"Clarke,\"Author   ", out _));
            ZTestPhrase("\" Arthur\"   C.    Clarke (\author)  ", "\" Arthur\"C. Clarke");


            Assert.IsFalse(cMailValidation.TryParsePhrases("", out _));
            Assert.IsFalse(cMailValidation.TryParsePhrases("  ,   (),       (comment) ,    (  \t \r\n longer  comment   ) ,    ", out _));
            ZTestPhrases(" ,   (),   x   (comment) ,    (  \t \r\n longer  comment   ) ,    ", "x");
        }

        private void ZTestLocalPart(string pInput, string pExpectedLocalPart)
        {
            Assert.IsTrue(cMailValidation.TryParseLocalPart(pInput, out var lString));
            Assert.AreEqual(pExpectedLocalPart, lString);
        }

        private void ZTestDomain(string pInput, string pExpectedLocalPart)
        {
            Assert.IsTrue(cMailValidation.TryParseDomain(pInput, out var lString));
            Assert.AreEqual(pExpectedLocalPart, lString);
        }

        private void ZTestMsgId(string pInput, string pExpectedLocalPart)
        {
            Assert.IsTrue(cMailValidation.TryParseMsgId(pInput, out var lString));
            Assert.AreEqual(pExpectedLocalPart, lString);
        }

        private void ZTestMsgIds(string pInput, IList<string> pExpectedMsgIds)
        {
            Assert.IsTrue(cMailValidation.TryParseMsgIds(pInput, out var lStrings));
            Assert.AreEqual(pExpectedMsgIds.Count, lStrings.Count);
            for (int i = 0; i < pExpectedMsgIds.Count; i++) Assert.AreEqual(pExpectedMsgIds[i], lStrings[i]);
        }

        private void ZTestPhrase(string pInput, string pExpectedPhrase)
        {
            Assert.IsTrue(cMailValidation.TryParsePhrase(pInput, out var lPhrase));

            var lBuilder = new StringBuilder();

            foreach (var lPart in lPhrase.Parts)
            {
                switch (lPart)
                {
                    case cHeaderFieldTextValue lText:

                        lBuilder.Append(lText.Text);
                        break;

                    case cHeaderFieldQuotedStringValue lQuotedString:

                        lBuilder.Append(cTools.Enquote(lQuotedString.Text));
                        break;

                    default:

                        throw new AssertFailedException();
                }
            }

            Assert.AreEqual(pExpectedPhrase, lBuilder.ToString());
        }

        private void ZTestPhrases(string pInput, string pExpectedPhrases)
        {
            Assert.IsTrue(cMailValidation.TryParsePhrases(pInput, out var lPhrases));

            var lBuilder = new StringBuilder();

            foreach (var lPhrase in lPhrases)
            {
                if (lBuilder.Length > 0) lBuilder.Append(",");

                foreach (var lPart in lPhrase.Parts)
                {
                    switch (lPart)
                    {
                        case cHeaderFieldTextValue lText:

                            lBuilder.Append(lText.Text);
                            break;

                        case cHeaderFieldQuotedStringValue lQuotedString:

                            lBuilder.Append(cTools.Enquote(lQuotedString.Text));
                            break;

                        default:

                            throw new AssertFailedException();
                    }
                }
            }

            Assert.AreEqual(pExpectedPhrases, lBuilder.ToString());
        }




        /*

        internal static void _Tests()
        {


            string lString;


            ;?; // check only quoted strings 
            ;?; // check only comments
            ;?; // check only atoms


            cHeaderFieldPhraseValue lPhrase;
            if (TryParsePhrase("", out lPhrase)) throw new cTestsException($"{ nameof(cMailValidation)}.TryParsePhrase.1");
            if (TryParsePhrase("     \t      ()    \t   (   \t stuff  \t  (more    \t stuff  ))    ", out lPhrase)) throw new cTestsException($"{nameof(cMailValidation)}.TryParsePhrase.1");
            if (!TryParsePhrase("    \t      ()  x \t   (   \t stuff  \t  (more    \t stuff  ))    ", out lPhrase) || ZTestPhraseToString(lPhrase) != "x") throw new cTestsException($"{nameof(cMailValidation)}.TryParsePhrase.2");
            if (!TryParsePhrase("    \t      ()  x \t   (   \t stuff  \t  (more    \t stuff  ))  \"xxx\"   (extra \"comment\")  ", out lPhrase) || ZTestPhraseToString(lPhrase) != "x xxx") throw new cTestsException($"{nameof(cMailValidation)}.TryParsePhrase.3");
            if (!TryParsePhrase("    \t      ()  x \t   (   \t stuff  \t  (more    \t stuff  ))  \"x x\"   (extra \"comment\")  ", out lPhrase) || ZTestPhraseToString(lPhrase) != "x\"x x\"") throw new cTestsException($"{nameof(cMailValidation)}.TryParsePhrase.4");
            if (!TryParsePhrase("   Arthur     C.    Clarke  (Author)  ", out lPhrase) || ZTestPhraseToString(lPhrase) != "Arthur C. Clarke") throw new cTestsException($"{nameof(cMailValidation)}.TryParsePhrase.5");
            if (TryParsePhrase("   .Arthur     C.    Clarke(, Author)  ", out lPhrase)) throw new cTestsException($"{nameof(cMailValidation)}.TryParsePhrase.6");
            if (TryParsePhrase("    Arthur     C.    Clarke,  Author   ", out lPhrase)) throw new cTestsException($"{nameof(cMailValidation)}.TryParsePhrase.7");
            if (!TryParsePhrase("   Arthur     C.  \"Clarke,\"Author   ", out lPhrase) || ZTestPhraseToString(lPhrase) != "Arthur C. Clarke, Author") throw new cTestsException($"{nameof(cMailValidation)}.TryParsePhrase.8");
            if (!TryParsePhrase("   A.         C.    Clarke   \t\t\t   ", out lPhrase) || ZTestPhraseToString(lPhrase) != "A. C. Clarke") throw new cTestsException($"{nameof(cMailValidation)}.TryParsePhrase.9");
            if (!TryParsePhrase("  \"A.\"    \"C.\"  Clarke   \t\r\n   ", out lPhrase) || ZTestPhraseToString(lPhrase) != "A. C. Clarke") throw new cTestsException($"{nameof(cMailValidation)}.TryParsePhrase.10");
            if (!TryParsePhrase("   A.         C.    Clarke   \"\"     ", out lPhrase) || ZTestPhraseToString(lPhrase) != "A. C. Clarke\"\"") throw new cTestsException($"{nameof(cMailValidation)}.TryParsePhrase.11");
            if (TryParsePhrase(" \"\arthur\"   C.  \"Clarke,\"Author   ", out lPhrase)) throw new cTestsException($"{nameof(cMailValidation)}.TryParsePhrase.12");
            if (!TryParsePhrase("\" Arthur\"   C.    Clarke (\author)  ", out lPhrase) || ZTestPhraseToString(lPhrase) != "\" Arthur\"C. Clarke") throw new cTestsException($"{nameof(cMailValidation)}.TryParsePhrase.13");

            // phrases
            List<cHeaderFieldPhraseValue> lPhrases;
            if (TryParsePhrases("", out lPhrases)) throw new cTestsException($"{ nameof(cMailValidation)}.TryParsePhrases.1");
            if (TryParsePhrases("  ,   (),       (comment) ,    (  \t \r\n longer  comment   ) ,    ", out lPhrases)) throw new cTestsException($"{ nameof(cMailValidation)}.TryParsePhrases.2");
            if (!TryParsePhrases(" ,   (),   x   (comment) ,    (  \t \r\n longer  comment   ) ,    ", out lPhrases) || lPhrases.Count != 1 || ZTestPhraseToString(lPhrases[0]) != "x") throw new cTestsException($"{ nameof(cMailValidation)}.TryParsePhrases.3");
        }

        private static string ZTestPhraseToString(cHeaderFieldPhraseValue pPhrase)
        {
            var lBuilder = new StringBuilder();

            foreach (var lPart in pPhrase.Parts)
            {
                switch (lPart)
                {
                    case cHeaderFieldCommentValue lComment:

                        lBuilder.Append(ZTestCommentToString(lComment));
                        break;

                    case cHeaderFieldTextValue lText:

                        lBuilder.Append(lText.Text);
                        break;

                    case cHeaderFieldQuotedStringValue lQuotedString:

                        lBuilder.Append('"');
                        lBuilder.Append(lQuotedString.Text);
                        lBuilder.Append('"');
                        break;

                    default:

                        throw new cInternalErrorException(nameof(cMailValidation), nameof(ZTestPhraseToString));
                }
            }

            return lBuilder.ToString();
        }

        private static string ZTestCommentToString(cHeaderFieldCommentValue pComment)
        {
            var lBuilder = new StringBuilder();

            lBuilder.Append('(');

            foreach (var lPart in pComment.Parts)
            {
                switch (lPart)
                {
                    case cHeaderFieldCommentValue lComment:

                        lBuilder.Append(ZTestCommentToString(lComment));
                        break;

                    case cHeaderFieldTextValue lText:

                        lBuilder.Append(lText.Text);
                        break;

                    default:

                        throw new cInternalErrorException(nameof(cMailValidation), nameof(ZTestCommentToString));
                }
            }

            lBuilder.Append(')');

            return lBuilder.ToString();
        }

    */

    }
}