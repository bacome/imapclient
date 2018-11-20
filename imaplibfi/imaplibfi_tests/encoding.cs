using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using work.bacome.imapinternals;
using work.bacome.imapsupport;

namespace work.bacome.imapclient_tests
{
    [TestClass]
    public class Test_Encoding
    {
        private enum eEncoding { base64, quotedprintable }
        private enum eCopyStrategy { readbyte, readrandom, copyto }

        private readonly cBytes kCRLF = new cBytes("\r\n");
        private readonly cBytes kLF = new cBytes("\n");

        [TestMethod]
        public void Encoding_Base64_Empty() => ZEncoding_Base64_RoundTrip_Tests("");

        [TestMethod]
        public void Encoding_Base64_ShortLine() => ZEncoding_Base64_RoundTrip_Tests("this is a test");

        [TestMethod]
        public void Encoding_Base64_LongerLine() => ZEncoding_Base64_RoundTrip_Tests("this is a test line that is longer than 76 characters to make sure that the lines are split");

        [TestMethod]
        public void Encoding_Base64_Random()
        {
            for (int i = 1; i < 100; i++) ZEncoding_Base64_RoundTrip_Tests(ZRandomLines(i, string.Empty, false));
        }

        [TestMethod]
        public void Encoding_QuotedPrintable_Empty() => ZEncoding_QuotedPrintable_RoundTrip_Tests("");

        [TestMethod]
        public void Encoding_QuotedPrintable_EBCDIC() => ZEncoding_QuotedPrintable_RoundTrip_Tests("this is a test!", new cQuotedPrintableEncoder(eQuotedPrintableEncoderInputType.environmentterminated, eQuotedPrintableEncodingRule.ebcdic), "this is a test=21=\r\n");

        [TestMethod]
        public void Encoding_QuotedPrintable_Minimal() => ZEncoding_QuotedPrintable_RoundTrip_Tests("this is a test!", new cQuotedPrintableEncoder(eQuotedPrintableEncoderInputType.environmentterminated, eQuotedPrintableEncodingRule.minimal), "this is a test!=\r\n");

        [TestMethod]
        public void Encoding_QuotedPrintable_Split1() =>
            ZEncoding_QuotedPrintable_RoundTrip_Tests(
                "this   test   is   to   ensure   that   longer   lines   are   split   in a   suitable   place" + Environment.NewLine,
                new cQuotedPrintableEncoder(eQuotedPrintableEncoderInputType.environmentterminated, eQuotedPrintableEncodingRule.minimal),
                "this   test   is   to   ensure   that   longer   lines   are   split   in a=\r\n   suitable   place\r\n");

        [TestMethod]
        public void Encoding_QuotedPrintable_Split2() =>
            ZEncoding_QuotedPrintable_RoundTrip_Tests(
                "this   test   is   to   ensure   that   longer   lines   are   split   in  a   suitable   place" + Environment.NewLine,
                new cQuotedPrintableEncoder(eQuotedPrintableEncoderInputType.environmentterminated, eQuotedPrintableEncodingRule.minimal),
                "this   test   is   to   ensure   that   longer   lines   are   split   in  =\r\na   suitable   place\r\n");

        [TestMethod]
        public void Encoding_QuotedPrintable_Split3() =>
            ZEncoding_QuotedPrintable_RoundTrip_Tests(
                "this   test   is   to   ensure   that   longer   lines   are   split   in   a   suitable   place" + Environment.NewLine,
                new cQuotedPrintableEncoder(eQuotedPrintableEncoderInputType.environmentterminated, eQuotedPrintableEncodingRule.minimal),
                "this   test   is   to   ensure   that   longer   lines   are   split   in  =\r\n a   suitable   place\r\n");

        [TestMethod]
        public void Encoding_QuotedPrintable_Split4() =>
            ZEncoding_QuotedPrintable_RoundTrip_Tests(
                "this   test   is   to   ensure   that   longer   lines   are   split   in a" + Environment.NewLine + "   suitable   place" + Environment.NewLine,
                new cQuotedPrintableEncoder(eQuotedPrintableEncoderInputType.environmentterminated, eQuotedPrintableEncodingRule.minimal),
                "this   test   is   to   ensure   that   longer   lines   are   split   in a\r\n   suitable   place\r\n");

        [TestMethod]
        public void Encoding_QuotedPrintable_Split5() =>
            ZEncoding_QuotedPrintable_RoundTrip_Tests(
                "this   test   is   to   ensure   that   longer   lines   are   split   in  a" + Environment.NewLine + "   suitable   place" + Environment.NewLine,
                new cQuotedPrintableEncoder(eQuotedPrintableEncoderInputType.environmentterminated, eQuotedPrintableEncodingRule.minimal),
                "this   test   is   to   ensure   that   longer   lines   are   split   in  a\r\n   suitable   place\r\n");

        [TestMethod]
        public void Encoding_QuotedPrintable_Split6() =>
            ZEncoding_QuotedPrintable_RoundTrip_Tests(
                "this   test   is   to   ensure   that   longer   lines   are   split   in   a" + Environment.NewLine + "   suitable   place" + Environment.NewLine,
                new cQuotedPrintableEncoder(eQuotedPrintableEncoderInputType.environmentterminated, eQuotedPrintableEncodingRule.minimal),
                "this   test   is   to   ensure   that   longer   lines   are   split   in  =\r\n a\r\n   suitable   place\r\n");

        [TestMethod]
        public void Encoding_QuotedPrintable_LineEndings() =>
            ZEncoding_QuotedPrintable_RoundTrip_Tests(
                //        1         2         3         4         5          6         7
                // 345678901234567890123456789012345678901234567890123 4567890123456789012345
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrst" + Environment.NewLine +
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrstu" + Environment.NewLine +
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrstuv" + Environment.NewLine +
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrstuvw" + Environment.NewLine +
                Environment.NewLine +
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrs=" + Environment.NewLine +
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrst=" + Environment.NewLine +
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrstu=" + Environment.NewLine +
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrstuv=" + Environment.NewLine +
                Environment.NewLine +
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrs " + Environment.NewLine +
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrst " + Environment.NewLine +
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrstu " + Environment.NewLine +
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrstuv " + Environment.NewLine +
                Environment.NewLine +
                Environment.NewLine +
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrs\t" + Environment.NewLine +
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrst\t" + Environment.NewLine +
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrstu\t" + Environment.NewLine +
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrstuv\t" + Environment.NewLine +
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqr \t" + Environment.NewLine +
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrs \t" + Environment.NewLine +
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrst \t" + Environment.NewLine +
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrstu \t" + Environment.NewLine +
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrstu ", new cQuotedPrintableEncoder(eQuotedPrintableEncoderInputType.environmentterminated, eQuotedPrintableEncodingRule.ebcdic));
        //        1         2         3         4         5          6         7
        // 345678901234567890123456789012345678901234567890123 4567890123456789012345=


        [TestMethod]
        public void Encoding_QuotedPrintable_Random()
        {
            for (int i = 1; i < 50; i++) ZEncoding_QuotedPrintable_RoundTrip_Tests(ZRandomLines(i, Environment.NewLine, false));
            for (int i = 1; i < 50; i++) ZEncoding_QuotedPrintable_RoundTrip_Tests(ZRandomLines(i, Environment.NewLine, true));
            for (int i = 1; i < 50; i++) ZEncoding_QuotedPrintable_RoundTrip_Tests(ZRandomLines(i, string.Empty, true));
            for (int i = 1; i < 50; i++) ZEncoding_QuotedPrintable_RoundTrip_Tests(ZRandomLines(i));
        }

        [TestMethod]
        public void Encoding_QuotedPrintable_LineTerminators()
        {
            ZEncoding_QuotedPrintable_RoundTrip_Tests(new string[] { "to be", "or not to be", "that is the question" }, false, false, "to be\r\nor not to be\r\nthat is the question=\r\n");
            ZEncoding_QuotedPrintable_RoundTrip_Tests(new string[] { "to be", "or not to be", "that is the question" }, false, true, "to be\r\nor not to be\r\nthat is the question\r\n");
            ZEncoding_QuotedPrintable_RoundTrip_Tests(new string[] { "to be", "or not to be", "that is the question" }, true, false, "to be\r\nor not to be\r\nthat is the question=\r\n");
            ZEncoding_QuotedPrintable_RoundTrip_Tests(new string[] { "to be", "or not to be", "that is the question" }, true, true, "to be\r\nor not to be\r\nthat is the question\r\n");
        }

        [TestMethod]
        public void Encoding_QuotedPrintable_EndsInSpaces()
        {
            ZEncoding_QuotedPrintable_RoundTrip_Tests(
                "a line that ends in white space \t \t",
                new cQuotedPrintableEncoder(eQuotedPrintableEncoderInputType.environmentterminated, eQuotedPrintableEncodingRule.minimal),
                "a line that ends in white space \t \t=\r\n");

            ZEncoding_QuotedPrintable_RoundTrip_Tests(
                "a line that ends in white space \t \t" + Environment.NewLine,
                new cQuotedPrintableEncoder(eQuotedPrintableEncoderInputType.environmentterminated, eQuotedPrintableEncodingRule.minimal),
                "a line that ends in white space \t =09\r\n");

            ZEncoding_QuotedPrintable_RoundTrip_Tests(
                "a line that ends in white space ",
                new cQuotedPrintableEncoder(eQuotedPrintableEncoderInputType.environmentterminated, eQuotedPrintableEncodingRule.minimal),
                "a line that ends in white space =\r\n");

            //        1         2         3         4         5          6         7
            //   123456789012345678901234567890123456789012345678901234567890123456789012345=
            ZEncoding_QuotedPrintable_RoundTrip_Tests(
                "a line that ends in white                                            space ",
                new cQuotedPrintableEncoder(eQuotedPrintableEncoderInputType.environmentterminated, eQuotedPrintableEncodingRule.minimal),
                "a line that ends in white                                            space =\r\n");

            //        1         2         3         4         5          6         7
            //   123456789012345678901234567890123456789012345678901234567890123456789012345=
            ZEncoding_QuotedPrintable_RoundTrip_Tests(
                "a line that ends in white                                             space ",
                new cQuotedPrintableEncoder(eQuotedPrintableEncoderInputType.environmentterminated, eQuotedPrintableEncodingRule.minimal),
                "a line that ends in white                                             space=\r\n =\r\n");

            ZEncoding_QuotedPrintable_RoundTrip_Tests(
                "a line that ends in white space " + Environment.NewLine,
                new cQuotedPrintableEncoder(eQuotedPrintableEncoderInputType.environmentterminated, eQuotedPrintableEncodingRule.minimal),
                "a line that ends in white space=20\r\n");

            //        1         2         3         4         5          6         7
            //   123456789012345678901234567890123456789012345678901234567890123456789012345=
            ZEncoding_QuotedPrintable_RoundTrip_Tests(
                "a line that ends in white                                           space " + Environment.NewLine,
                new cQuotedPrintableEncoder(eQuotedPrintableEncoderInputType.environmentterminated, eQuotedPrintableEncodingRule.minimal),
                "a line that ends in white                                           space=20\r\n");

            //        1         2         3         4         5          6         7
            //   123456789012345678901234567890123456789012345678901234567890123456789012345=
            ZEncoding_QuotedPrintable_RoundTrip_Tests(
                "a line that ends in white                                            space " + Environment.NewLine,
                new cQuotedPrintableEncoder(eQuotedPrintableEncoderInputType.environmentterminated, eQuotedPrintableEncodingRule.minimal),
                "a line that ends in white                                            space=\r\n=20\r\n");

        }

        [TestMethod]
        public void Encoding_QuotedPrintable_NakedCR()
        {
            ZEncoding_QuotedPrintable_RoundTrip_Tests(new string[] { "naked \r in a line", "at the end of a line \r", "at the end of file \r" }, false, false);
            ZEncoding_QuotedPrintable_RoundTrip_Tests(new string[] { "naked \r in a line", "at the end of a line \r", "at the end of file \r" }, false, true);
            ZEncoding_QuotedPrintable_RoundTrip_Tests(new string[] { "naked \r in a line", "at the end of a line \r", "at the end of file \r" }, true, false);
            ZEncoding_QuotedPrintable_RoundTrip_Tests(new string[] { "naked \r in a line", "at the end of a line \r", "at the end of file \r" }, true, true);
        }

        private string ZRandomLines(int pLines, string pNewLine, bool pTerminateLastLine)
        {
            bool lFirst = true;
            StringBuilder lBuilder = new StringBuilder();

            foreach (var lLine in ZRandomLines(pLines))
            {
                if (lFirst) lFirst = false;
                else lBuilder.Append(pNewLine);
                lBuilder.Append(lLine);
            }

            if (pTerminateLastLine) lBuilder.Append(pNewLine);

            return lBuilder.ToString();
        }

        private string[] ZRandomLines(int pLines)
        {
            Random lRandom = new Random();
            var lLines = new List<string>();
            for (int i = 0; i < pLines; i++) lLines.Add(ZRandomLine(lRandom.Next(100)));
            return lLines.ToArray();
        }

        private string ZRandomLine(int lLength)
        {
            Random lRandom = new Random();
            StringBuilder lBuilder = new StringBuilder();

            while (lBuilder.Length < lLength)
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
                    ) lBuilder.Append(lChar);
            }

            return lBuilder.ToString();
        }

        private void ZEncoding_Base64_RoundTrip_Tests(string pInputString)
        {
            var lEncoder = new cBase64Encoder();
            var lDecoder = new cBase64Decoder();
            var lExpectedEncodedLength = cBase64Encoder.GetEncodedLength(Encoding.UTF8.GetByteCount(pInputString));

            ZEncoding_RoundTrip_Test(pInputString, eCopyStrategy.copyto, lEncoder, lExpectedEncodedLength, lDecoder);
            ZEncoding_RoundTrip_Test(pInputString, eCopyStrategy.readbyte, lEncoder, lExpectedEncodedLength, lDecoder);
            ZEncoding_RoundTrip_Test(pInputString, eCopyStrategy.readrandom, lEncoder, lExpectedEncodedLength, lDecoder);
        }

        private void ZEncoding_QuotedPrintable_RoundTrip_Tests(string pInputString)
        {
            ZEncoding_QuotedPrintable_RoundTrip_Tests(pInputString, new cQuotedPrintableEncoder(eQuotedPrintableEncoderInputType.binary, eQuotedPrintableEncodingRule.minimal));
            ZEncoding_QuotedPrintable_RoundTrip_Tests(pInputString, new cQuotedPrintableEncoder(eQuotedPrintableEncoderInputType.binary, eQuotedPrintableEncodingRule.ebcdic));
            ZEncoding_QuotedPrintable_RoundTrip_Tests(pInputString, new cQuotedPrintableEncoder(eQuotedPrintableEncoderInputType.environmentterminated, eQuotedPrintableEncodingRule.minimal));
            ZEncoding_QuotedPrintable_RoundTrip_Tests(pInputString, new cQuotedPrintableEncoder(eQuotedPrintableEncoderInputType.environmentterminated, eQuotedPrintableEncodingRule.ebcdic));
        }

        private void ZEncoding_QuotedPrintable_RoundTrip_Tests(string pInputString, cQuotedPrintableEncoder pEncoder, string pExpectedEncodedString = null)
        {
            var lDecoder = new cQuotedPrintableDecoder();

            ZEncoding_RoundTrip_Test(pInputString, eCopyStrategy.copyto, pEncoder, null, lDecoder, pExpectedEncodedString);
            ZEncoding_RoundTrip_Test(pInputString, eCopyStrategy.readbyte, pEncoder, null, lDecoder, pExpectedEncodedString);
            ZEncoding_RoundTrip_Test(pInputString, eCopyStrategy.readrandom, pEncoder, null, lDecoder, pExpectedEncodedString);
        }

        private void ZEncoding_RoundTrip_Test(string pInputString, eCopyStrategy pCopyStrategy, iTransformer pEncoder, long? pExpectedEncodedLength, iTransformer pDecoder, string pExpectedEncodedString = null)
        {
            var lBuffer = new byte[100];
            var lRandom = new Random();

            using (MemoryStream lInput = new MemoryStream(Encoding.UTF8.GetBytes(pInputString)))
            using (cTransformingStream lEncoder = new cTransformingStream(lInput, pEncoder))
            using (MemoryStream lEncoded = new MemoryStream())
            using (cTransformingStream lDecoder = new cTransformingStream(lEncoded, pDecoder))
            using (MemoryStream lDecoded = new MemoryStream())
            {
                long lExpectedEncodedLength = cTransformingStream.GetTransformedLength(lInput, pEncoder);
                if (pExpectedEncodedLength != null) Assert.AreEqual(pExpectedEncodedLength.Value, lExpectedEncodedLength, "externally calculated encoded length");

                lInput.Position = 0;
                ZEncoding_Copy(lEncoder, lEncoded, pCopyStrategy);

                Assert.AreEqual(0, pEncoder.BufferedInputByteCount, "buffered bytes in encoder");
                Assert.AreEqual(lExpectedEncodedLength, lEncoded.Length, "encoded length");
                var lEncodedString = new string(Encoding.UTF8.GetChars(lEncoded.ToArray()));
                if (pExpectedEncodedString != null) Assert.IsTrue(lEncodedString == pExpectedEncodedString, "encoded string");
                ZCheckEncodedFormat(lEncodedString);

                lEncoded.Position = 0;
                var lExpectedDecodedLength = cTransformingStream.GetTransformedLength(lEncoded, pDecoder);

                lEncoded.Position = 0;
                ZEncoding_Copy(lDecoder, lDecoded, pCopyStrategy);

                Assert.AreEqual(0, pDecoder.BufferedInputByteCount, "buffered bytes in decoder");
                Assert.AreEqual(lExpectedDecodedLength, lDecoded.Length, "final length compared to calculated length");

                var lDecodedString = new string(Encoding.UTF8.GetChars(lDecoded.ToArray()));
                Assert.AreEqual(pInputString, lDecodedString, false, "round trip");
            }
        }

        private void ZEncoding_QuotedPrintable_RoundTrip_Tests(string[] pInputLines)
        {
            ZEncoding_QuotedPrintable_RoundTrip_Tests(pInputLines, false, false);
            ZEncoding_QuotedPrintable_RoundTrip_Tests(pInputLines, false, true);
            ZEncoding_QuotedPrintable_RoundTrip_Tests(pInputLines, true, false);
            ZEncoding_QuotedPrintable_RoundTrip_Tests(pInputLines, true, true);
        }

        private void ZEncoding_QuotedPrintable_RoundTrip_Tests(string[] pInputLines, bool pCRLF, bool pTerminateLastLine, string pExpectedEncodedString = null)
        {
            var lInputBytes = new List<byte>();

            bool lFirst = true;

            cBytes lBytesNewLine;
            iTransformer lQPEncoder;

            if (pCRLF)
            {
                lBytesNewLine = kCRLF;
                lQPEncoder = new cQuotedPrintableEncoder(eQuotedPrintableEncoderInputType.crlfterminated, eQuotedPrintableEncodingRule.minimal);
            }
            else
            {
                lBytesNewLine = kLF;
                lQPEncoder = new cQuotedPrintableEncoder(eQuotedPrintableEncoderInputType.lfterminated, eQuotedPrintableEncodingRule.minimal);
            }

            foreach (var lLine in pInputLines)
            {
                if (lFirst) lFirst = false;
                else lInputBytes.AddRange(lBytesNewLine);
                lInputBytes.AddRange(Encoding.UTF8.GetBytes(lLine));
            }

            if (pTerminateLastLine || pInputLines[pInputLines.Length - 1].Length == 0) lInputBytes.AddRange(lBytesNewLine);

            using (MemoryStream lInput = new MemoryStream(lInputBytes.ToArray()))
            using (cTransformingStream lEncoder = new cTransformingStream(lInput, lQPEncoder))
            using (MemoryStream lEncoded = new MemoryStream())
            using (cTransformingStream lDecoder = new cTransformingStream(lEncoded, new cQuotedPrintableDecoder()))
            using (MemoryStream lDecoded = new MemoryStream())
            {
                ZEncoding_Copy(lEncoder, lEncoded, eCopyStrategy.copyto);

                Assert.AreEqual(0, lQPEncoder.BufferedInputByteCount, "buffered bytes in encoder");

                var lEncodedString = new string(Encoding.UTF8.GetChars(lEncoded.ToArray()));
                if (pExpectedEncodedString != null) Assert.IsTrue(lEncodedString == pExpectedEncodedString, "encoded string");
                ZCheckEncodedFormat(lEncodedString);

                lEncoded.Position = 0;
                ZEncoding_Copy(lDecoder, lDecoded, eCopyStrategy.copyto);

                var lDecodedString = new string(Encoding.UTF8.GetChars(lDecoded.ToArray()));

                int lLineStart = 0;
                int lLineIndex = 0;

                while (true)
                {
                    if (lLineStart == lDecodedString.Length) break;

                    int lLineEnd = lDecodedString.IndexOf(Environment.NewLine, lLineStart, StringComparison.Ordinal);
                    int lLineLength;

                    if (lLineEnd == -1) lLineLength = lDecodedString.Length - lLineStart;
                    else lLineLength = lLineEnd - lLineStart;

                    string lDecodedLine = lDecodedString.Substring(lLineStart, lLineLength);

                    Assert.AreEqual(pInputLines[lLineIndex], lDecodedLine, false, "line");

                    lLineIndex++;

                    if (lLineEnd == -1) break;
                    lLineStart = lLineEnd + Environment.NewLine.Length;
                }

                Assert.AreEqual(pInputLines.Length, lLineIndex, $"line count {pInputLines[pInputLines.Length - 1]}");
            }
        }

        private void ZEncoding_Copy(Stream pFrom, Stream pTo, eCopyStrategy pCopyStrategy)
        {
            switch (pCopyStrategy)
            {
                case eCopyStrategy.copyto:

                    pFrom.CopyTo(pTo);
                    return;

                case eCopyStrategy.readbyte:

                    while (true)
                    {
                        var lByte = pFrom.ReadByte();
                        if (lByte == -1) return;
                        pTo.WriteByte((byte)lByte);
                    }

                case eCopyStrategy.readrandom:

                    var lBuffer = new byte[100];
                    var lRandom = new Random();

                    while (true)
                    {
                        var lBytesRead = pFrom.Read(lBuffer, 0, 1 + lRandom.Next(99));
                        if (lBytesRead == 0) return;
                        pTo.Write(lBuffer, 0, lBytesRead);
                    }
            }
        }

        private void ZCheckEncodedFormat(string pEncoded)
        {
            // check the lines are no longer than 76 chars and that they all end with crlf

            int lLineStart = 0;

            while (true)
            {
                if (lLineStart == pEncoded.Length) break;
                int lLineEnd = pEncoded.IndexOf("\r\n", lLineStart, StringComparison.Ordinal);
                Assert.AreNotEqual(-1, lLineEnd, "encoded line not terminated with CRLF");
                Assert.IsTrue(lLineEnd - lLineStart < 77, "invalid encoded line length");
                lLineStart = lLineEnd + 2;
            }
        }
    }
}