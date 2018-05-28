using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    internal class cEncodingTests
    {
        internal static void _Tests(cTrace.cContext pParentContext)
        {
            ZBasicTests(pParentContext);
            ZTestProgress(pParentContext);
            ;?;
        }

        private static void ZBasicTests(cTrace.cContext pParentContext)
        {
            ZBasicTest("1", "", pParentContext);
            ZBasicTest("2", "this is a test", pParentContext);

            for (int i = 0; i < 1000; i++) ZRandomBasicTest(i, pParentContext);

            ZRandomBasicTest(99999, pParentContext);
        }

        private static void ZRandomBasicTest(int pSize, cTrace.cContext pParentContext)
        {
            Random lRandom = new Random();
            StringBuilder lBuilder = new StringBuilder();

            while (lBuilder.Length < pSize)
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

            ZBasicTest("random", lBuilder.ToString(), pParentContext);
        }

        private static void ZBasicTest(string pTestName, string pInputString, cTrace.cContext pParentContext)
        {
            ZBasicTestB64(pTestName, pInputString, pParentContext);
            ZBasicTestQP(pTestName, pInputString, pParentContext);
        }

        private static void ZBasicTestB64(string pTestName, string pInputString, cTrace.cContext pParentContext)
        {
            using (MemoryStream lInput = new MemoryStream(Encoding.UTF8.GetBytes(pInputString)))
            using (cBase64EncodingStream lEncoder = new cBase64EncodingStream(lInput))
            using (MemoryStream lIntermediate = new MemoryStream())
            using (cBase64DecodingStream lDecoder = new cBase64DecodingStream(lIntermediate))
            using (MemoryStream lFinal = new MemoryStream())
            {
                lEncoder.CopyTo(lIntermediate);
                if (lEncoder.GetBufferedBytes() != 0) throw new cTestsException($"{nameof(ZBasicTestB64)}({pTestName}.bb.1)");
                if (lIntermediate.Length != cBase64EncodingStream.EncodedLength(lInput.Length)) throw new cTestsException($"{nameof(ZBasicTestB64)}({pTestName}.l)");
                var lIntermediateString = new string(Encoding.UTF8.GetChars(lIntermediate.ToArray()));
                lIntermediate.Position = 0;
                lDecoder.CopyTo(lFinal);
                if (lDecoder.GetBufferedBytes() != 0) throw new cTestsException($"{nameof(ZBasicTestB64)}({pTestName}.bb.2)");

                var lFinalString = new string(Encoding.UTF8.GetChars(lFinal.ToArray()));
                if (lFinalString != pInputString) throw new cTestsException($"{nameof(ZBasicTestB64)}({pTestName}.f)");
                ZCheckEncodedFormat(pTestName, lIntermediateString);
            }
        }

        private static void ZBasicTestQP(string pTestName, string pInputString, eQuotedPrintableEncodingType pType, eQuotedPrintableEncodingRule pRule, out string rIntermediateString, out int rHardLineBreaks, out string rFinalString, cTrace.cContext pParentContext)
        {
            using (MemoryStream lInput = new MemoryStream(Encoding.UTF8.GetBytes(pInputString)))
            using (cQuotedPrintableEncodingStream lEncoder = new cQuotedPrintableEncodingStream(lInput, pType, pRule))
            using (MemoryStream lIntermediate = new MemoryStream())
            using (cQuotedPrintableDecodingStream lDecoder = new cQuotedPrintableDecodingStream(lIntermediate))
            using (MemoryStream lFinal = new MemoryStream())
            {
                var lBuffer = new byte[100];
                var lRandom = new Random();
                long lTotalRead = 0;
                long lLastInputPosition = 0;
                long lTotalIncrement = 0;

                while (true)
                {
                    var lBytesRead = lEncoder.Read(lBuffer, 0, 1 + lRandom.Next(99));
                    lTotalRead += lBytesRead;
                    long lThisInputPosition = lInput.Position - lEncoder.GetBufferedBytes();
                    long lIncrement = lThisInputPosition - lLastInputPosition;
                    if (lIncrement < 0) throw new cTestsException($"{nameof(ZBasicTestQP)}({pTestName}.i)");
                    lTotalIncrement += lIncrement;
                    lLastInputPosition = lThisInputPosition;
                    if (lBytesRead == 0) break;
                    lIntermediate.Write(lBuffer, 0, lBytesRead);
                }

                if (lTotalIncrement != lInput.Length) throw new cTestsException($"{nameof(ZBasicTestQP)}({pTestName}.l.1)");
                if (lLastInputPosition != lInput.Length) throw new cTestsException($"{nameof(ZBasicTestQP)}({pTestName}.l.2)");

                if (lEncoder.GetBufferedBytes() != 0) throw new cTestsException($"{nameof(ZBasicTestQP)}({pTestName}.bb.1)");

                rIntermediateString = new string(Encoding.UTF8.GetChars(lIntermediate.ToArray()));

                if (rIntermediateString.Length > 0 && rIntermediateString[rIntermediateString.Length - 1] == '=') throw new cTestsException($"{nameof(ZBasicTestQP)}({pTestName}.e.1)");
                if (rIntermediateString.Length > 1 && rIntermediateString[rIntermediateString.Length - 2] == '=') throw new cTestsException($"{nameof(ZBasicTestQP)}({pTestName}.e.1)");

                lIntermediate.Position = 0;
                lDecoder.CopyTo(lFinal);
                if (lDecoder.GetBufferedBytes() != 0) throw new cTestsException($"{nameof(ZBasicTestB64)}({pTestName}.bb.2)");

                rFinalString = new string(Encoding.UTF8.GetChars(lFinal.ToArray()));
                if (rFinalString != pInputString) throw new cTestsException($"{nameof(ZBasicTestQP)}({pTestName}.f)");

                rHardLineBreaks = ZCheckEncodedFormat(pTestName, rIntermediateString);
            }
        }

        private static int ZCheckEncodedFormat(string pTestName, string pEncoded)
        {
            // check the lines are no longer than 76 chars and that they all end with crlf and there are no blank lines

            int lLineStart = 0;
            int lLines = 0;
            int lLinesEndingWithEquals = 0;

            while (true)
            {
                if (lLineStart == pEncoded.Length) break;
                int lLineEnd = pEncoded.IndexOf("\r\n", lLineStart, StringComparison.Ordinal);
                if (lLineEnd == -1) throw new cTestsException($"{nameof(ZCheckEncodedFormat)}({pTestName}.e)");
                int lLineLength = lLineEnd - lLineStart;
                if (lLineLength < 1 || lLineLength > 76) throw new cTestsException($"{nameof(ZCheckEncodedFormat)}({pTestName}.ll)");
                lLines++;
                if (pEncoded[lLineEnd - 1] == '=') lLinesEndingWithEquals++;
                lLineStart = lLineEnd + 2;
            }

            return lLines - lLinesEndingWithEquals;
        }

        private static void ZTestProgress(cTrace.cContext pParentContext)
        {
            ZTestProgress(

                //        1         2         3         4         5          6         7
                // 345678901234567890123456789012345678901234567890123 4567890123456789012345
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrstu\r\n" +
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrstuv\r\n" +
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrstuvw\r\n" +
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrs=\r\n" +
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrst=\r\n" +
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrstu=\r\n" +
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqr \r\n" +
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrs \r\n" +
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrst \r\n" +
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrstu \r\n" +
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrstuv \r\n" +
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqr\t\r\n" +
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrs\t\r\n" +
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrst\t\r\n" +
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrstu\t\r\n" +
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrstuv\t\r\n" +
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopq \t\r\n" +
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqr \t\r\n" +
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrs \t\r\n" +
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrst \t\r\n" +
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrstu \t\r\n" +
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrstu ",
                pParentContext)
                ;
        }

        private static void ZTestProgress(string pInput, cTrace.cContext pParentContext)
        {
            ZTestProgressB64("progress", pInput, pParentContext);
            ZTestProgressQP("progress", pInput, pParentContext);
        }

        private static void ZTestProgressB64(string pTestName, string pInputString, cTrace.cContext pParentContext)
        {
            using (MemoryStream lInput = new MemoryStream(Encoding.UTF8.GetBytes(pInputString)))
            using (cBase64EncodingStream lEncoder = new cBase64EncodingStream(lInput))
            using (MemoryStream lIntermediate = new MemoryStream())
            using (cBase64DecodingStream lDecoder = new cBase64DecodingStream(lIntermediate))
            using (MemoryStream lFinal = new MemoryStream())
            {
                ZTestProgress(pTestName, pInputString, lInput, lEncoder, lIntermediate, lDecoder, lFinal);
            }
        }

        private static void ZTestProgressQP(string pTestName, string pInputString, cTrace.cContext pParentContext)
        {
            using (MemoryStream lInput = new MemoryStream(Encoding.UTF8.GetBytes(pInputString)))
            using (cQuotedPrintableEncodingStream lEncoder = new cQuotedPrintableEncodingStream(lInput))
            using (MemoryStream lIntermediate = new MemoryStream())
            using (cBase64DecodingStream lDecoder = new cBase64DecodingStream(lIntermediate))
            using (MemoryStream lFinal = new MemoryStream())
            {
                ZTestProgress(pTestName, pInputString, lInput, lEncoder, lIntermediate, lDecoder, lFinal);
            }
        }

        private static void ZTestProgress(string pTestName, string pInputString, Stream pInput, cEncodingStream pEncoder, MemoryStream pIntermediate, cDecodingStream pDecoder, MemoryStream pFinal)
        {
            long lLastPosition = 0;

            while (true)
            {
                var lByte = pEncoder.ReadByte();
                if (lByte == -1) break;
                var lThisPosition = pInput.Position - pEncoder.GetBufferedBytes();
                if (lThisPosition <= lLastPosition) throw new cTestsException("progress base 64.1");
                lLastPosition = lThisPosition;
                pIntermediate.WriteByte((byte)lByte);
            }

            if (lLastPosition != pInput.Length) throw new cTestsException("progress base 64.2");

            var lIntermediateString = new string(Encoding.UTF8.GetChars(pIntermediate.ToArray()));
            pIntermediate.Position = 0;

            lLastPosition = 0;

            while (true)
            {
                var lByte = pDecoder.ReadByte();
                if (lByte == -1) break;
                var lThisPosition = pIntermediate.Position - pDecoder.GetBufferedBytes();
                if (lThisPosition <= lLastPosition) throw new cTestsException("progress base 64.3");
                lLastPosition = lThisPosition;
                pFinal.WriteByte((byte)lByte);
            }

            if (lLastPosition != pIntermediate.Length) throw new cTestsException("progress base 64.4");

            var lFinalString = new string(Encoding.UTF8.GetChars(pFinal.ToArray()));
            if (lFinalString != pInputString) throw new cTestsException($"{nameof(cBase64EncodingStream)}({pTestName}.f)");
            ZCheckEncodedFormat(pTestName, lIntermediateString);

        }

        private static void ZQuotedPrintableEncodeTests(cTrace.cContext pParentContext)
        {
            ZQuotedPrintableEncodeTest(
                "1",
                new string[]
                {
                //        1         2         3         4         5          6         7
                // 345678901234567890123456789012345678901234567890123 4567890123456789012345
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrstu",
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrstuv",
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrstuvw",
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrs=",
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrst=",
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrstu=",
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqr ",
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrs ",
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrst ",
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrstu ",
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrstuv ",
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqr\t",
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrs\t",
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrst\t",
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrstu\t",
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrstuv\t",
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrstu "
                    //        1         2         3         4         5          6         7
                    // 345678901234567890123456789012345678901234567890123 45678901234567890123456
                },
                pParentContext);

            ZQuotedPrintableEncodeTest(
                "2",
                new string[]
                {
                //        1         2         3         4         5          6         7
                // 345678901234567890123456789012345678901234567890123 4567890123456789012345
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \r@bcdefghijklmnopqrstu",
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \n@bcdefghijklmnopqrstuv",
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrstuv\r",
                },
                eQuotedPrintablet.CRLFTerminatedLines,
                false,
                eQuotedPrintableEncodeQuotingRule.Minimal,
                pParentContext);

            ZQuotedPrintableEncodeTest(
                "3",
                new string[]
                {
                //        1         2         3         4         5          6         7
                // 345678901234567890123456789012345678901234567890123 4567890123456789012345
                " \t\t   \t\t\t\t     \t\t\t\t\t\t       \t\t\t\t\t\t\t\t         \t\t\t\t\t\t\t\t\t\t           \t\t\t\t\t\t\t\t\t\t\t\t             \t\t\t\t\t\t\t\t\t\t\t\t\t\t               ",
                "",
                " \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t",
                ""
                },
                true,
                pParentContext);

            ZQuotedPrintableEncodeTest(
                "3.1",
                new string[]
                {
                //        1         2         3         4         5          6         7
                // 345678901234567890123456789012345678901234567890123 4567890123456789012345
                "",
                " \t\t   \t\t\t\t     \t\t\t\t\t\t       \t\t\t\t\t\t\t\t         \t\t\t\t\t\t\t\t\t\t           \t\t\t\t\t\t\t\t\t\t\t\t             \t\t\t\t\t\t\t\t\t\t\t\t\t\t               ",
                " \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t",
                ""
                },
                true,
                pParentContext);

            ZQuotedPrintableEncodeTest(
                "4",
                new string[]
                {
                "@",
                "12345678901234567890123456789012345678901234567890123456789012345678901234 "
                },
                pParentContext);

            ZQuotedPrintableEncodeTest(
                "5",
                new string[]
                {
                "@",
                "1234567890123456789012345678901234567890123456789012345678901234567890123  "
                },
                pParentContext);

            ZQuotedPrintableEncodeTest(
                "6",
                new string[]
                {
                "@",
                "123456789012345678901234567890123456789012345678901234567890123456789012   "
                },
                pParentContext);




            ZQuotedPrintableEncodeTestRandomLines(pParentContext);


            long lExpectedLength =
                ZQuotedPrintableEncodeTest(
                    "poem",
                    new string[]
                    {
                                "All doggies go to heaven (or so I've been told).",
                                "They run and play along the streets of Gold.",
                                "Why is heaven such a doggie-delight?",
                                "Why, because there's not a single cat in sight!"
                    },
                    eQuotedPrintableEncodeSourceType.CRLFTerminatedLines,
                    false,
                    eQuotedPrintableEncodeQuotingRule.EBCDIC, pParentContext);

            using (var lClient = new cIMAPClient())
            using (var lInput = new MemoryStream(Encoding.UTF8.GetBytes("All doggies go to heaven (or so I've been told).\r\nThey run and play along the streets of Gold.\r\nWhy is heaven such a doggie-delight?\r\nWhy, because there's not a single cat in sight!")))
            {
                long lLength = lClient.QuotedPrintableEncode(lInput);
                if (lLength != lExpectedLength) throw new cTestsException($"dev/null: {lLength} vs {lExpectedLength}");
            }
        }

        private static void ZQuotedPrintableEncodeTestRandomLines(cTrace.cContext pParentContext)
        {
            Random lRandom = new Random();

            for (int s = 0; s < 10; s++)
            {
                int lLineCount = lRandom.Next(100) + 1;

                string[] lLines = new string[lLineCount];

                for (int i = 0; i < lLineCount; i++)
                {
                    int lLength = lRandom.Next(160);

                    StringBuilder lBuilder = new StringBuilder();
                    if (i == 0) lBuilder.Append('@');

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

                    lLines[i] = lBuilder.ToString();
                }

                ZQuotedPrintableEncodeTest(
                    "ZTestQuotedPrintableRandomLines",
                    lLines,
                    pParentContext);
            }
        }

        private static void ZQuotedPrintableEncodeTest(string pTestName, string[] pLines, cTrace.cContext pParentContext)
        {
            var lLFFalseMinimal = ZQuotedPrintableEncodeTest(pTestName + ".lf.false.Mininal", pLines, eQuotedPrintableEncodeSourceType.LFTerminatedLines, false, eQuotedPrintableEncodeQuotingRule.Minimal, pParentContext);
            var lLFTrueMinimal = ZQuotedPrintableEncodeTest(pTestName + ".lf.true.Mininal", pLines, eQuotedPrintableEncodeSourceType.LFTerminatedLines, true, eQuotedPrintableEncodeQuotingRule.Minimal, pParentContext);

            if (lLFFalseMinimal >= lLFTrueMinimal) throw new cTestsException(pTestName + ".compare.1");

            var lCRLFFalseMinimal = ZQuotedPrintableEncodeTest(pTestName + ".crlf.false.Mininal", pLines, eQuotedPrintableEncodeSourceType.CRLFTerminatedLines, false, eQuotedPrintableEncodeQuotingRule.Minimal, pParentContext);

            if (lLFFalseMinimal != lCRLFFalseMinimal) throw new cTestsException(pTestName + ".compare.2");

            var lCRLFTrueMinimal = ZQuotedPrintableEncodeTest(pTestName + ".crlf.true.Mininal", pLines, eQuotedPrintableEncodeSourceType.CRLFTerminatedLines, true, eQuotedPrintableEncodeQuotingRule.Minimal, pParentContext);

            if (lCRLFFalseMinimal >= lCRLFTrueMinimal) throw new cTestsException(pTestName + ".compare.3");

            var lBinaryFalseMinimal = ZQuotedPrintableEncodeTest(pTestName + ".binary.false.Mininal", pLines, eQuotedPrintableEncodeSourceType.Binary, false, eQuotedPrintableEncodeQuotingRule.Minimal, pParentContext);

            if (lCRLFFalseMinimal >= lBinaryFalseMinimal) throw new cTestsException(pTestName + ".compare.4");

            var lBinaryTrueMinimal = ZQuotedPrintableEncodeTest(pTestName + ".binary.true.Mininal", pLines, eQuotedPrintableEncodeSourceType.Binary, true, eQuotedPrintableEncodeQuotingRule.Minimal, pParentContext);

            if (lBinaryFalseMinimal >= lBinaryTrueMinimal) throw new cTestsException(pTestName + ".compare.5");

            var lLFFalseEBCDIC = ZQuotedPrintableEncodeTest(pTestName + ".lf.false.EBCDIC", pLines, eQuotedPrintableEncodeSourceType.LFTerminatedLines, false, eQuotedPrintableEncodeQuotingRule.EBCDIC, pParentContext);

            if (lLFFalseMinimal >= lLFFalseEBCDIC) throw new cTestsException(pTestName + ".compare.6");
        }

        private static void ZQuotedPrintableEncodeTestx(string pTestName, string[] pLines, bool pNoNo, cTrace.cContext pParentContext)
        {
            ;?; // who calles this?
            var lLFTrueMinimal = ZQuotedPrintableEncodeTest(pTestName + ".lf.true.Mininal", pLines, eQuotedPrintableEncodeSourceType.LFTerminatedLines, true, eQuotedPrintableEncodeQuotingRule.Minimal, pParentContext);
            var lCRLFTrueMinimal = ZQuotedPrintableEncodeTest(pTestName + ".crlf.true.Mininal", pLines, eQuotedPrintableEncodeSourceType.CRLFTerminatedLines, true, eQuotedPrintableEncodeQuotingRule.Minimal, pParentContext);
            if (lLFTrueMinimal != lCRLFTrueMinimal) throw new cTestsException(pTestName + ".compare.1");
            var lBinaryTrueMinimal = ZQuotedPrintableEncodeTest(pTestName + ".binary.true.Mininal", pLines, eQuotedPrintableEncodeSourceType.Binary, true, eQuotedPrintableEncodeQuotingRule.Minimal, pParentContext);
            if (lCRLFTrueMinimal >= lBinaryTrueMinimal) throw new cTestsException(pTestName + ".compare.2");
            var lLFTrueEBCDIC = ZQuotedPrintableEncodeTest(pTestName + ".lf.true.EBCDIC", pLines, eQuotedPrintableEncodeSourceType.LFTerminatedLines, true, eQuotedPrintableEncodeQuotingRule.EBCDIC, pParentContext);
        }

        private static long ZQuotedPrintableEncodeTest(string pTestName, string[] pLines, eQuotedPrintableEncodingType pType, bool pTerminateLastLine, eQuotedPrintableEncodingRule pRule, cTrace.cContext pParentContext)
        {
            long lBytesWritten;

            StringBuilder lBuilder = new StringBuilder();

            for (int i = 0; i < pLines.Length; i++)
            {
                lBuilder.Append(pLines[i]);

                if (i < pLines.Length - 1 || pTerminateLastLine)
                {
                    if (pType == eQuotedPrintableEncodingType.LFTerminatedLines) lBuilder.Append('\n');
                    else lBuilder.Append("\r\n");
                }
            }

            ZBasicTestQP(pTestName, lBuilder.ToString(), pType, pRule, out var lIntermediateString, out var lHardLineBreaks, out var lFinalString, pParentContext);

            int lExpectedHardLineBreaks;
            if (pType == eQuotedPrintableEncodingType.Binary) lExpectedHardLineBreaks = 0;

            ;?;
            if (lHardLineBreaks != pLines.Length) throw new cTestsException($"{nameof(ZQuotedPrintableEncodeTest)}({pTestName}.hlb)");

            var lLines = new List<string>();

            int lLineStart = 0;

            while (lLineStart < lFinalString.Length)
            {
                int lLineEnd = lFinalString.IndexOf("\r\n", lLineStart, StringComparison.Ordinal);
                if (lLineEnd == -1) lLineEnd = lFinalString.Length;
                lLines.Add(lFinalString.Substring(lLineStart, lLineEnd - lLineStart));
                lLineStart = lLineEnd + 2;
            }

            bool lDump = false;
            if (lLines.Count != pLines.Length) lDump = true;
            else for (int i = 0; i < lLines.Count; i++) if (lLines[i] != pLines[i]) lDump = true;

            if (lDump)
            {
                // note: this is the error that lead to the inclusion of ordinal string searches ... the occasional \r\n was missed without it

                pParentContext.TraceError("TestQuotedPrintable {0}: {1} roundtrip vs {2} input", pTestName, lLines.Count, pLines.Length);

                for (int i = 0; i < Math.Max(pLines.Length, lLines.Count); i++)
                {
                    if (i >= lLines.Count || i >= pLines.Length || lLines[i] != pLines[i])
                    {
                        for (int j = i; j < Math.Max(pLines.Length, lLines.Count) && j < i + 3; j++)
                        {
                            if (j < pLines.Length) pParentContext.TraceError(pLines[j]);
                            if (j < lLines.Count) pParentContext.TraceWarning(lLines[j]);
                        }

                        break;
                    }

                    pParentContext.TraceInformation(pLines[i]);
                }

                throw new cTestsException($"TestQuotedPrintable.{pTestName}.r.1");
            }





            using (var lInput = new MemoryStream(Encoding.UTF8.GetBytes(lBuilder.ToString())))
            using (var lEncoded = new MemoryStream())
            {
                var lIncrement = new cTestActionInt();
                cIncrementConfiguration lConfig = new cIncrementConfiguration(CancellationToken.None, lIncrement.ActionInt);

                lBytesWritten = lClient.QuotedPrintableEncode(lInput, pType, pRule, lEncoded, lConfig);

                string lEncodedString = new string(Encoding.UTF8.GetChars(lEncoded.GetBuffer(), 0, (int)lEncoded.Length));
                if (lBytesWritten > 0 && lEncodedString[lEncodedString.Length - 1] == '=') throw new cTestsException($"TestQuotedPrintable.{pTestName}.e.1");
                if (lBytesWritten > 1 && lEncodedString[lEncodedString.Length - 2] == '=') throw new cTestsException($"TestQuotedPrintable.{pTestName}.e.2");

                // check the length outputs
                if (lBytesWritten != lEncoded.Length) throw new cTestsException($"TestQuotedPrintable.{pTestName}.l.1");
                if (lIncrement.Total != lInput.Length) throw new cTestsException($"TestQuotedPrintable.{pTestName}.l.2");

                // round trip test

                lEncoded.Position = 0;

                using (var lDecoded = new cDecoder._Tester())
                {
                    var lDecoder = new cQuotedPrintableDecoder(lDecoded);

                    var lReadBuffer = new byte[10000];

                    while (true)
                    {
                        int lBytesRead = lEncoded.Read(lReadBuffer, 0, lReadBuffer.Length);
                        if (lBytesRead == 0) break;
                        var lWriteBuffer = new byte[lBytesRead];
                        Array.Copy(lReadBuffer, lWriteBuffer, lBytesRead);
                        lDecoder.WriteAsync(lWriteBuffer, 0, CancellationToken.None, pParentContext).Wait();
                    }

                    lDecoder.FlushAsync(CancellationToken.None, pParentContext).Wait();

                    var lTemp1 = new string(Encoding.UTF8.GetChars(lDecoded.GetBuffer(), 0, lDecoded.Length));

                    var lLines = new List<string>();

                    int lStartIndex = 0;

                    while (lStartIndex < lTemp1.Length)
                    {
                        int lEOL = lTemp1.IndexOf("\r\n", lStartIndex, StringComparison.Ordinal);
                        if (lEOL == -1) lEOL = lTemp1.Length;
                        lLines.Add(lTemp1.Substring(lStartIndex, lEOL - lStartIndex));
                        lStartIndex = lEOL + 2;
                    }

                    bool lDump = false;
                    if (lLines.Count != pLines.Length) lDump = true;
                    for (int i = 0; i < lLines.Count; i++) if (lLines[i] != pLines[i]) lDump = true;

                    if (lDump)
                    {
                        // note: this is the error that lead to the inclusion of ordinal string searches ... the occasional \r\n was missed without it

                        pParentContext.TraceError("TestQuotedPrintable {0}: {1} roundtrip vs {2} input", pTestName, lLines.Count, pLines.Length);

                        for (int i = 0; i < Math.Max(pLines.Length, lLines.Count); i++)
                        {
                            if (i >= lLines.Count || i >= pLines.Length || lLines[i] != pLines[i])
                            {
                                for (int j = i; j < Math.Max(pLines.Length, lLines.Count) && j < i + 3; j++)
                                {
                                    if (j < pLines.Length) pParentContext.TraceError(pLines[j]);
                                    if (j < lLines.Count) pParentContext.TraceWarning(lLines[j]);
                                }

                                break;
                            }

                            pParentContext.TraceInformation(pLines[i]);
                        }

                        throw new cTestsException($"TestQuotedPrintable.{pTestName}.r.1");
                    }
                }

                // check lines are no longer than 76 chars
                //  every line in a binary file should end with an =
                //  the number of lines not ending with = should be the same as the number of input lines

                lEncoded.Position = 0;

                int lLinesNotEndingWithEquals = 0;

                using (var lReader = new StreamReader(lEncoded))
                {
                    while (!lReader.EndOfStream)
                    {
                        var lLine = lReader.ReadLine();
                        if (lLine.Length > 76) throw new cTestsException($"TestQuotedPrintable.{pTestName}.l.4");
                        if (lLine.Length == 0 || lLine[lLine.Length - 1] != '=') lLinesNotEndingWithEquals++;
                    }
                }

                if (pType == eQuotedPrintableEncodeSourceType.Binary)
                {
                    if (lLinesNotEndingWithEquals != 1) throw new cTestsException($"TestQuotedPrintable.{pTestName}.i.1");
                }
                else
                {
                    if (lLinesNotEndingWithEquals != pLines.Length) throw new cTestsException($"TestQuotedPrintable.{pTestName}.i.2");
                }
            }

            return lBytesWritten;
        }
    }
}
