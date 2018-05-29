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
            ZQuotedPrintableTests(pParentContext);
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
            ZBasicTestQP(pTestName, pInputString, eQuotedPrintableEncodingType.Binary, eQuotedPrintableEncodingRule.EBCDIC, out _, out _, out _, pParentContext);
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
                if (lIntermediate.Length != cBase64EncodingStream.GetEncodedLength(lInput.Length)) throw new cTestsException($"{nameof(ZBasicTestB64)}({pTestName}.l1)");

                var lIntermediateString = new string(Encoding.UTF8.GetChars(lIntermediate.ToArray()));

                var lExpectedDecodedLength = cBase64DecodingStream.GetDecodedLength(lIntermediate);

                lDecoder.CopyTo(lFinal);
                if (lDecoder.GetBufferedBytes() != 0) throw new cTestsException($"{nameof(ZBasicTestB64)}({pTestName}.bb.2)");
                if (lFinal.Length != lExpectedDecodedLength) throw new cTestsException($"{nameof(ZBasicTestB64)}({pTestName}.l2)");

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

                var lExpectedEncodedLength = cQuotedPrintableEncodingStream.GetEncodedLength(lInput, pType, pRule);

                while (true)
                {
                    var lBytesRead = lEncoder.Read(lBuffer, 0, 1 + lRandom.Next(99));
                    lTotalRead += lBytesRead;
                    long lThisInputPosition = lInput.Position - lEncoder.GetBufferedBytes();
                    long lIncrement = lThisInputPosition - lLastInputPosition;
                    if (lIncrement < 0) throw new cTestsException($"{nameof(ZBasicTestQP)}({pTestName}.i1)");
                    lTotalIncrement += lIncrement;
                    lLastInputPosition = lThisInputPosition;
                    if (lBytesRead == 0) break;
                    lIntermediate.Write(lBuffer, 0, lBytesRead);
                }

                if (lEncoder.GetBufferedBytes() != 0) throw new cTestsException($"{nameof(ZBasicTestQP)}({pTestName}.bb.1)");

                if (lTotalIncrement != lInput.Length) throw new cTestsException($"{nameof(ZBasicTestQP)}({pTestName}.l.1)");
                if (lLastInputPosition != lInput.Length) throw new cTestsException($"{nameof(ZBasicTestQP)}({pTestName}.l.2)");
                if (lIntermediate.Length != lExpectedEncodedLength) throw new cTestsException($"{nameof(ZBasicTestQP)}({pTestName}.l.3)");

                rIntermediateString = new string(Encoding.UTF8.GetChars(lIntermediate.ToArray()));

                if (rIntermediateString.EndsWith("=")) throw new cTestsException($"{nameof(ZBasicTestQP)}({pTestName}.e.1)");
                if (rIntermediateString.Length > 1 && rIntermediateString[rIntermediateString.Length - 2] == '=') throw new cTestsException($"{nameof(ZBasicTestQP)}({pTestName}.e.2)");
                if (rIntermediateString.EndsWith("=\r\n")) throw new cTestsException($"{nameof(ZBasicTestQP)}({pTestName}.e.3)");
                if (rIntermediateString.Length > 3 && rIntermediateString[rIntermediateString.Length - 4] == '=' && rIntermediateString.EndsWith("\r\n")) throw new cTestsException($"{nameof(ZBasicTestQP)}({pTestName}.e.4)");

                lTotalRead = 0;
                lLastInputPosition = 0;
                lTotalIncrement = 0;

                var lExpectedDecodedLength = cQuotedPrintableDecodingStream.GetDecodedLength(lIntermediate);

                while (true)
                {
                    var lBytesRead = lDecoder.Read(lBuffer, 0, 1 + lRandom.Next(99));
                    lTotalRead += lBytesRead;
                    long lThisInputPosition = lIntermediate.Position - lEncoder.GetBufferedBytes();
                    long lIncrement = lThisInputPosition - lLastInputPosition;
                    if (lIncrement < 0) throw new cTestsException($"{nameof(ZBasicTestQP)}({pTestName}.i2)");
                    lTotalIncrement += lIncrement;
                    lLastInputPosition = lThisInputPosition;
                    if (lBytesRead == 0) break;
                    lFinal.Write(lBuffer, 0, lBytesRead);
                }

                if (lDecoder.GetBufferedBytes() != 0) throw new cTestsException($"{nameof(ZBasicTestB64)}({pTestName}.bb.2)");

                if (lTotalIncrement != lIntermediate.Length) throw new cTestsException($"{nameof(ZBasicTestQP)}({pTestName}.l.4)");
                if (lLastInputPosition != lIntermediate.Length) throw new cTestsException($"{nameof(ZBasicTestQP)}({pTestName}.l.5)");
                if (lFinal.Length != lExpectedDecodedLength) throw new cTestsException($"{nameof(ZBasicTestQP)}({pTestName}.l.6)");

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

        private static void ZQuotedPrintableTests(cTrace.cContext pParentContext)
        {
            ZQuotedPrintableTest(
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

            ZQuotedPrintableTest(
                "2",
                new string[]
                {
                //        1         2         3         4         5          6         7
                // 345678901234567890123456789012345678901234567890123 4567890123456789012345
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \r@bcdefghijklmnopqrstu",
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \n@bcdefghijklmnopqrstuv",
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrstuv\r",
                },
                eQuotedPrintableEncodingType.CRLFTerminatedLines,
                false,
                eQuotedPrintableEncodingRule.Minimal,
                pParentContext);

            ZQuotedPrintableTests(
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
                pParentContext);

            ZQuotedPrintableTests(
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
                pParentContext);

            ZQuotedPrintableTest(
                "4",
                new string[]
                {
                "@",
                "12345678901234567890123456789012345678901234567890123456789012345678901234 "
                },
                pParentContext);

            ZQuotedPrintableTest(
                "5",
                new string[]
                {
                "@",
                "1234567890123456789012345678901234567890123456789012345678901234567890123  "
                },
                pParentContext);

            ZQuotedPrintableTest(
                "6",
                new string[]
                {
                "@",
                "123456789012345678901234567890123456789012345678901234567890123456789012   "
                },
                pParentContext);




            ZQuotedPrintableTestRandomLines(pParentContext);
        }

        private static void ZQuotedPrintableTestRandomLines(cTrace.cContext pParentContext)
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

                ZQuotedPrintableTest(
                    "random",
                    lLines,
                    pParentContext);
            }
        }

        private static void ZQuotedPrintableTest(string pTestName, string[] pLines, cTrace.cContext pParentContext)
        {
            var lLFFalseMinimal = ZQuotedPrintableTest(pTestName + ".lf.false.Mininal", pLines, eQuotedPrintableEncodingType.LFTerminatedLines, false, eQuotedPrintableEncodingRule.Minimal, pParentContext);
            var lLFTrueMinimal = ZQuotedPrintableTest(pTestName + ".lf.true.Mininal", pLines, eQuotedPrintableEncodingType.LFTerminatedLines, true, eQuotedPrintableEncodingRule.Minimal, pParentContext);

            if (lLFFalseMinimal >= lLFTrueMinimal) throw new cTestsException(pTestName + ".compare.1");

            var lCRLFFalseMinimal = ZQuotedPrintableTest(pTestName + ".crlf.false.Mininal", pLines, eQuotedPrintableEncodingType.CRLFTerminatedLines, false, eQuotedPrintableEncodingRule.Minimal, pParentContext);

            if (lLFFalseMinimal != lCRLFFalseMinimal) throw new cTestsException(pTestName + ".compare.2");

            var lCRLFTrueMinimal = ZQuotedPrintableTest(pTestName + ".crlf.true.Mininal", pLines, eQuotedPrintableEncodingType.CRLFTerminatedLines, true, eQuotedPrintableEncodingRule.Minimal, pParentContext);

            if (lCRLFFalseMinimal >= lCRLFTrueMinimal) throw new cTestsException(pTestName + ".compare.3");

            var lBinaryFalseMinimal = ZQuotedPrintableTest(pTestName + ".binary.false.Mininal", pLines, eQuotedPrintableEncodingType.Binary, false, eQuotedPrintableEncodingRule.Minimal, pParentContext);

            if (lCRLFFalseMinimal >= lBinaryFalseMinimal) throw new cTestsException(pTestName + ".compare.4");

            var lBinaryTrueMinimal = ZQuotedPrintableTest(pTestName + ".binary.true.Mininal", pLines, eQuotedPrintableEncodingType.Binary, true, eQuotedPrintableEncodingRule.Minimal, pParentContext);

            if (lBinaryFalseMinimal >= lBinaryTrueMinimal) throw new cTestsException(pTestName + ".compare.5");

            var lLFFalseEBCDIC = ZQuotedPrintableTest(pTestName + ".lf.false.EBCDIC", pLines, eQuotedPrintableEncodingType.LFTerminatedLines, false, eQuotedPrintableEncodingRule.EBCDIC, pParentContext);

            if (lLFFalseMinimal >= lLFFalseEBCDIC) throw new cTestsException(pTestName + ".compare.6");
        }

        private static void ZQuotedPrintableTests(string pTestName, string[] pLines, cTrace.cContext pParentContext)
        {
            var lLFTrueMinimal = ZQuotedPrintableTest(pTestName + ".lf.true.Mininal", pLines, eQuotedPrintableEncodingType.LFTerminatedLines, true, eQuotedPrintableEncodingRule.Minimal, pParentContext);
            var lCRLFTrueMinimal = ZQuotedPrintableTest(pTestName + ".crlf.true.Mininal", pLines, eQuotedPrintableEncodingType.CRLFTerminatedLines, true, eQuotedPrintableEncodingRule.Minimal, pParentContext);
            if (lLFTrueMinimal != lCRLFTrueMinimal) throw new cTestsException(pTestName + ".compare.1");
            var lBinaryTrueMinimal = ZQuotedPrintableTest(pTestName + ".binary.true.Mininal", pLines, eQuotedPrintableEncodingType.Binary, true, eQuotedPrintableEncodingRule.Minimal, pParentContext);
            if (lCRLFTrueMinimal >= lBinaryTrueMinimal) throw new cTestsException(pTestName + ".compare.2");
            var lLFTrueEBCDIC = ZQuotedPrintableTest(pTestName + ".lf.true.EBCDIC", pLines, eQuotedPrintableEncodingType.LFTerminatedLines, true, eQuotedPrintableEncodingRule.EBCDIC, pParentContext);
        }

        private static int ZQuotedPrintableTest(string pTestName, string[] pLines, eQuotedPrintableEncodingType pType, bool pTerminateLastLine, eQuotedPrintableEncodingRule pRule, cTrace.cContext pParentContext)
        {
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
            else if (pTerminateLastLine) lExpectedHardLineBreaks = pLines.Length;
            else lExpectedHardLineBreaks = pLines.Length - 1;

            if (lHardLineBreaks != lExpectedHardLineBreaks) throw new cTestsException($"{nameof(ZQuotedPrintableTest)}({pTestName}.hlb)");

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

                pParentContext.TraceError("{0} {1}: {2} roundtrip vs {3} input", nameof(ZQuotedPrintableTest), pTestName, lLines.Count, pLines.Length);

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

                throw new cTestsException($"{nameof(ZQuotedPrintableTest)}({pTestName}.r)");
            }

            return lFinalString.Length;
        }
    }
}
