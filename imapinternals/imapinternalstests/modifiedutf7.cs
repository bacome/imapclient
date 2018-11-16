using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using work.bacome.imapclient;
using work.bacome.imapsupport;

namespace work.bacome.imapinternalstests
{
    [TestClass]
    public class cModifiedUTF7Tests
    {
        [TestMethod]
        public void cModifiedUTF7_RFC_3501_1()
        {
            if (!cModifiedUTF7.TryDecode(new cBytes("~peter/mail/&U,BTFw-/&ZeVnLIqe-"), out var lDecodedValue, out var lError) || lDecodedValue != "~peter/mail/台北/日本語") Assert.Fail(lError);
        }

        [TestMethod]
        public void cModifiedUTF7_RFC_3501_2()
        {
            Assert.IsFalse(cModifiedUTF7.TryDecode(new cBytes("&Jjo!"), out _, out _));
        }

        [TestMethod]
        public void cModifiedUTF7_RFC_3501_3()
        {
            if (!cModifiedUTF7.TryDecode(new cBytes("&Jjo-!"), out var lDecodedValue, out var lError) || lDecodedValue != "☺!") Assert.Fail(lError);
        }

        [TestMethod]
        public void cModifiedUTF7_RFC_3501_4()
        {
            if (!cModifiedUTF7.TryDecode(new cBytes("&U,BTFw-&ZeVnLIqe-"), out var lDecodedValue, out var lError) || lDecodedValue != "台北日本語") Assert.Fail(lError);
        }

        [TestMethod]
        public void cModifiedUTF7_RFC_3501_5()
        {
            if (!cModifiedUTF7.TryDecode(new cBytes("&U,BTF2XlZyyKng-"), out var lDecodedValue, out var lError) || lDecodedValue != "台北日本語") Assert.Fail(lError);
        }

        [TestMethod]
        public void cModifiedUTF7_Random_Tests()
        {
            Random lRandom = new Random();

            // generate random tuples

            int lMade;
            const int kCount = 1000;

            var lTriples = new List<char[]>();
            var lTriplesToSet0 = new List<char[]>();
            var lTriplesToSet1 = new List<char[]>();
            var lTriplesToSet2 = new List<char[]>();

            var lPairs = new List<char[]>();
            var lPairsToSet0 = new List<char[]>();
            var lPairsToSet1 = new List<char[]>();

            var lChars = new List<char>();

            for (int i = 0; i < kCount; i++)
            {
                var lTriple = new char[3];
                lTriples.Add(lTriple);
                lTriplesToSet0.Add(lTriple);
                lTriplesToSet1.Add(lTriple);
                lTriplesToSet2.Add(lTriple);

                var lPair = new char[2];
                lPairs.Add(lPair);
                lPairsToSet0.Add(lPair);
                lPairsToSet1.Add(lPair);
            }

            lMade = 0;

            for (char c = '\u0001'; c < 256; c++)
            {
                LAddToTuple(lRandom, c, lTriplesToSet0, 0);
                LAddToTuple(lRandom, c, lTriplesToSet1, 1);
                LAddToTuple(lRandom, c, lTriplesToSet2, 2);

                LAddToTuple(lRandom, c, lPairsToSet0, 0);
                LAddToTuple(lRandom, c, lPairsToSet1, 1);

                lChars.Add(c);

                lMade++;
            }

            while (lMade < kCount)
            {
                char lChar = (char)lRandom.Next(0xFFFF);
                System.Globalization.UnicodeCategory lCat = char.GetUnicodeCategory(lChar);
                if (lCat == System.Globalization.UnicodeCategory.ClosePunctuation ||
                    lCat == System.Globalization.UnicodeCategory.ConnectorPunctuation ||
                    lCat == System.Globalization.UnicodeCategory.CurrencySymbol ||
                    lCat == System.Globalization.UnicodeCategory.DashPunctuation ||
                    lCat == System.Globalization.UnicodeCategory.DecimalDigitNumber ||
                    lCat == System.Globalization.UnicodeCategory.FinalQuotePunctuation ||
                    lCat == System.Globalization.UnicodeCategory.InitialQuotePunctuation ||
                    lCat == System.Globalization.UnicodeCategory.LowercaseLetter ||
                    lCat == System.Globalization.UnicodeCategory.MathSymbol ||
                    lCat == System.Globalization.UnicodeCategory.OpenPunctuation ||
                    lCat == System.Globalization.UnicodeCategory.OtherLetter ||
                    lCat == System.Globalization.UnicodeCategory.OtherNumber ||
                    lCat == System.Globalization.UnicodeCategory.OtherPunctuation ||
                    lCat == System.Globalization.UnicodeCategory.OtherSymbol ||
                    lCat == System.Globalization.UnicodeCategory.SpaceSeparator ||
                    lCat == System.Globalization.UnicodeCategory.TitlecaseLetter ||
                    lCat == System.Globalization.UnicodeCategory.UppercaseLetter
                    )
                {
                    LAddToTuple(lRandom, lChar, lTriplesToSet0, 0);
                    LAddToTuple(lRandom, lChar, lTriplesToSet1, 1);
                    LAddToTuple(lRandom, lChar, lTriplesToSet2, 2);

                    LAddToTuple(lRandom, lChar, lPairsToSet0, 0);
                    LAddToTuple(lRandom, lChar, lPairsToSet1, 1);

                    lChars.Add(lChar);

                    lMade++;
                }
            }

            // generate a set of random strings of random number of triples, adding a random pair and a random single 

            var lStrings = new List<string>();
            int lType = 0;

            while (lTriples.Count > 0)
            {
                var lString = new StringBuilder();

                int lLength = lRandom.Next(10);
                if (lLength > lTriples.Count) lLength = lTriples.Count;

                for (int i = 0; i < lLength; i++)
                {
                    int lTripleIndex = lRandom.Next(lTriples.Count);
                    lString.Append(lTriples[lTripleIndex]);
                    lTriples.RemoveAt(lTripleIndex);
                }

                if (lType == 0) lType++;
                else if (lType == 1)
                {
                    int lCharIndex = lRandom.Next(lChars.Count);
                    lString.Append(lChars[lCharIndex]);
                    lChars.RemoveAt(lCharIndex);
                    lType++;
                }
                else
                {
                    int lPairIndex = lRandom.Next(lPairs.Count);
                    lString.Append(lPairs[lPairIndex]);
                    lPairs.RemoveAt(lPairIndex);
                    lType = 0;
                }

                lStrings.Add(lString.ToString());
            }

            lStrings.Add("H&M");
            lStrings.Add("ST&M");
            lStrings.Add("HIM&M");
            lStrings.Add("HIM&M\u20AC");

            foreach (string lString in lStrings)
            {
                var lBytes = cModifiedUTF7.Encode(lString);
                var lEncodedString = cTools.ASCIIBytesToString(lBytes);
                if (!cModifiedUTF7.TryDecode(lBytes, out var lDecodedString, out var lError)) Assert.Fail($"failed to decode {lEncodedString} due to {lError}");
                var lResult = $"'{lString}'\t'{lEncodedString}'\t'{lDecodedString}'";
                if (lDecodedString != lString) Assert.Fail($"modifiedutf7 round trip failure {lString}->{lEncodedString}->{lDecodedString}");
            }

            void LAddToTuple(Random pRandom, char pChar, List<char[]> pTuples, int pIndex)
            {
                int lTupleIndex = pRandom.Next(pTuples.Count);
                var lTuple = pTuples[lTupleIndex];
                pTuples.RemoveAt(lTupleIndex);
                lTuple[pIndex] = pChar;
            }
        }
    }
}
