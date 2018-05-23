﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.imapclient;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    internal class cBase64EncodingStream : Stream
    {
        private readonly Stream mStream;
        private readonly cBase64Encoder mEncoder;
        private readonly byte[] mInputBytesBuffer = new byte[cMailClient.BufferSize];
        private readonly Queue<byte> mOutputBytesBuffer = new Queue<byte>();
        private bool mStreamReadToEnd = false;

        public cBase64EncodingStream(Stream pStream)
        {
            mStream = pStream ?? throw new ArgumentNullException(nameof(pStream));
            if (!mStream.CanRead) throw new ArgumentOutOfRangeException(nameof(pStream));
            mEncoder = new cBase64Encoder();
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanTimeout => true;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override int ReadTimeout
        {
            ;?; // nope ... what if the stream can't timeout?
            get => mStream.ReadTimeout;
            set => mStream.ReadTimeout = value;
        }

        public override void Flush() => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override int Read(byte[] pBuffer, int pOffset, int pCount)
        {
            if (pCount == 0) return 0;

            ZReadInit(pBuffer, pOffset, pCount);

            var lMC = new cMethodControl();

            int lBytesRead = 0;

            while (true)
            {
                lBytesRead += ZReadFromOutputBytesBuffer(pBuffer, ref pOffset, ref pCount);

                if (pCount == 0 || mStreamReadToEnd) return lBytesRead;

                if (mStream.CanTimeout) mStream.ReadTimeout = lMC.Timeout;
                else _ = lMC.Timeout;
                    
                var lBytesReadIntoInputBuffer = mStream.Read(mInputBytesBuffer, 0, cMailClient.BufferSize);

                mEncoder.Encode(lMC, mInputBytesBuffer, lBytesReadIntoInputBuffer, lcontext);

                if (lBytesReadIntoInputBuffer == 0) mStreamReadToEnd = true;
            }
        }

        public override async Task<int> ReadAsync(byte[] pBuffer, int pOffset, int pCount, CancellationToken pCancellationToken)
        {
            if (pCount == 0) return 0;

            ZReadInit(pBuffer, pOffset, pCount);

            int lBytesRead = 0;

            while (true)
            {
                lBytesRead += ZReadFromOutputBytesBuffer(pBuffer, ref pOffset, ref pCount);
                if (pCount == 0) return lBytesRead;

                while (true)
                {
                    if (ZGenerateSomeOutputBytes()) break;
                    if (mReadStreamToEnd) return lBytesRead;
                    mBytesReadIntoReadBuffer = await mStream.ReadAsync(mReadBuffer, 0, kReadBufferSize, pCancellationToken).ConfigureAwait(false);
                    ZReadBytesIntoReadBuffer();
                }
            }
        }

        private void ZReadInit(byte[] pBuffer, int pOffset, int pCount)
        {
            if (pBuffer == null) throw new ArgumentNullException(nameof(pBuffer));
            if (pOffset < 0) throw new ArgumentOutOfRangeException(nameof(pOffset));
            if (pCount < 0) throw new ArgumentOutOfRangeException(nameof(pCount));
            if (pOffset + pCount > pBuffer.Length) throw new ArgumentException();
        }

        private int ZReadFromOutputBytesBuffer(byte[] pBuffer, ref int pOffset, ref int pCount)
        {
            int lBytesRead = 0;

            while (pCount > 0 && mOutputBytesBuffer.Count > 0)
            {
                pBuffer[pOffset++] = mOutputBytesBuffer.Dequeue();
                pCount--;
                lBytesRead++;
            }

            return lBytesRead;
        }

        private void ZReadBytesIntoReadBuffer()
        {
            mBytesReadOutOfReadBuffer = 0;

            if (mBytesReadIntoReadBuffer == 0)
            {
                mReadStreamToEnd = true;
                return;
            }
        }

        private bool ZGenerateSomeOutputBytesx()
        {
            bool lGeneratedSomeOutputBytes = false;

            // transfer bytes from the current read buffer to the input buffer until the input buffer has 57 bytes or we run out of data
            while (mInputBytesBuffer.Count < 57 && mBytesReadOutOfReadBuffer < mBytesReadIntoReadBuffer) mInputBytesBuffer.Enqueue(mReadBuffer[mBytesReadOutOfReadBuffer++]);

            // check if this is the end of the stream
            bool lAllBytesAvailableAreInInputBuffer = (mReadStreamToEnd && mBytesReadOutOfReadBuffer == mBytesReadIntoReadBuffer);

            // transfer 
            while (true)
            {
                mBytesToEncode.Clear();

                // move bytes from the input buffer to the encoding buffer in groups of 3 or to the end
                //
                while (true)
                {
                    if (mInputBytesBuffer.Count == 0) break;
                    if (mInputBytesBuffer.Count < 3 && !lAllBytesAvailableAreInInputBuffer) break;
                    mBytesToEncode.Add(mInputBytesBuffer.Dequeue());
                    mInputBytesOnCurrentOutputLine++;
                    if (mInputBytesBuffer.Count == 0) break;
                    mBytesToEncode.Add(mInputBytesBuffer.Dequeue());
                    mInputBytesOnCurrentOutputLine++;
                    if (mInputBytesBuffer.Count == 0) break;
                    mBytesToEncode.Add(mInputBytesBuffer.Dequeue());
                    mInputBytesOnCurrentOutputLine++;
                    if (mInputBytesOnCurrentOutputLine == 57) break;
                }

                if (mBytesToEncode.Count > 0)
                {
                    var lEncodedBytes = cBase64.Encode(mBytesToEncode);
                    foreach (var lByte in lEncodedBytes) mOutputBytesBuffer.Enqueue(lByte);
                    lGeneratedSomeOutputBytes = true;
                }

                if (mInputBytesOnCurrentOutputLine == 57 || (lAllBytesAvailableAreInInputBuffer && mInputBytesBuffer.Count == 0 && mInputBytesOnCurrentOutputLine > 0))
                {
                    mOutputBytesBuffer.Enqueue(cASCII.CR);
                    mOutputBytesBuffer.Enqueue(cASCII.LF);
                    lGeneratedSomeOutputBytes = true;

                    mInputBytesOnCurrentOutputLine = 0;
                }

                if (mBytesToEncode.Count == 0) return lGeneratedSomeOutputBytes;
            }
        }

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();



























        internal static void _Tests(cTrace.cContext pParentContext)
        {
            ZTest("1", "", pParentContext);
            ZTest("2", "this is a test", pParentContext);

            for (int i = 0; i < 1000; i++) ZRandomTest(i, pParentContext);

            ZRandomTest(99999, pParentContext);
        }

        private static void ZTest(string pTestName, string pInputString, cTrace.cContext pParentContext) => ZZTest(pTestName, pInputString, pParentContext).Wait();


        private static void ZRandomTest(int pSize, cTrace.cContext pParentContext)
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

            ZZTest("random", lBuilder.ToString(), pParentContext).Wait();
        }

        private static async Task ZZTest(string pTestName, string pInputString, cTrace.cContext pParentContext)
        {
            string lIntermediateString;
            string lFinalString;

            using (MemoryStream lInput = new MemoryStream(Encoding.UTF8.GetBytes(pInputString)))
            using (cBase64EncodingStream lEncoder = new cBase64EncodingStream(lInput))
            using (MemoryStream lIntermediate = new MemoryStream())
            {
                lEncoder.CopyTo(lIntermediate);

                if (lIntermediate.Length != EncodedLength(lInput.Length)) throw new cTestsException($"{nameof(cBase64EncodingStream)}({pTestName}.l)");

                lIntermediateString = new string(Encoding.UTF8.GetChars(lIntermediate.ToArray()));

                using (cDecoder._Tester lFinal = new cDecoder._Tester())
                {
                    cBase64Decoder lDecoder = new cBase64Decoder(lFinal);
                    await lDecoder.WriteAsync(lIntermediate.ToArray(), 0, CancellationToken.None, pParentContext).ConfigureAwait(false);
                    await lDecoder.FlushAsync(CancellationToken.None, pParentContext).ConfigureAwait(false);
                    lFinalString = new string(Encoding.UTF8.GetChars(lFinal.GetBuffer()));
                }
            }

            if (lFinalString != pInputString) throw new cTestsException($"{nameof(cBase64EncodingStream)}({pTestName}.f)");

            // check the lines are no longer than 76 chars and that they all end with crlf and there are no blank lines

            int lLineStart = 0;

            while (true)
            {
                if (lLineStart == lIntermediateString.Length) break;
                int lLineEnd = lIntermediateString.IndexOf("\r\n", lLineStart);
                if (lLineEnd == -1) throw new cTestsException($"{nameof(cBase64EncodingStream)}({pTestName}.e)");
                int lLineLength = lLineEnd - lLineStart;
                if (lLineLength < 1 || lLineLength > 76) throw new cTestsException($"{nameof(cBase64EncodingStream)}({pTestName}.ll)");
                lLineStart = lLineEnd + 2;
            }
        }
    }
}