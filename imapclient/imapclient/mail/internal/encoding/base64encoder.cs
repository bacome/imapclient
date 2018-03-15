using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    internal class cBase64Encoder : Stream
    {
        private readonly Stream mStream;
        private readonly cBatchSizer mReadSizer;

        private readonly Queue<byte> mOutputBytesBuffer = new Queue<byte>();
        private readonly Queue<byte> mInputBytesBuffer = new Queue<byte>();

        private readonly Stopwatch mStopwatch = new Stopwatch();

        private bool mReadStreamToEnd = false;
        private int mInputBytesOnCurrentOutputLine = 0;

        private byte[] mReadBuffer = null;
        private int mBytesReadIntoReadBuffer = 0;
        private int mBytesReadOutOfReadBuffer = 0;

        private readonly List<byte> mBytesToEncode = new List<byte>(57);

        public cBase64Encoder(Stream pStream, cBatchSizerConfiguration pConfiguration)
        {
            mStream = pStream ?? throw new ArgumentNullException(nameof(pStream));
            if (!mStream.CanRead) throw new ArgumentOutOfRangeException(nameof(pStream));
            mReadSizer = new cBatchSizer(pConfiguration);
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanTimeout => true;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override int ReadTimeout
        {
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

            int lBytesRead = 0;

            while (true)
            {
                lBytesRead += ZReadFromOutputBytesBuffer(pBuffer, ref pOffset, ref pCount);
                if (pCount == 0) return lBytesRead;

                while (true)
                {
                    if (ZGenerateSomeOutputBytes()) break;
                    if (mReadStreamToEnd) return lBytesRead;
                    ZPrepareForReadIntoReadBuffer();
                    mBytesReadIntoReadBuffer = mStream.Read(mReadBuffer, 0, mReadBuffer.Length);
                    ZReadBytesIntoReadBuffer();
                }
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
                    ZPrepareForReadIntoReadBuffer();
                    mBytesReadIntoReadBuffer = await mStream.ReadAsync(mReadBuffer, 0, mReadBuffer.Length, pCancellationToken);
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

        private void ZPrepareForReadIntoReadBuffer()
        {
            int lCurrent = mReadSizer.Current;
            if (mReadBuffer == null || lCurrent > mReadBuffer.Length) mReadBuffer = new byte[lCurrent];
            mStopwatch.Restart();
        }

        private void ZReadBytesIntoReadBuffer()
        {
            mStopwatch.Stop();

            mBytesReadOutOfReadBuffer = 0;

            if (mBytesReadIntoReadBuffer == 0)
            {
                mReadStreamToEnd = true;
                return;
            }

            mReadSizer.AddSample(mBytesReadIntoReadBuffer, mStopwatch.ElapsedMilliseconds);
        }

        private bool ZGenerateSomeOutputBytes()
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

        public static long EncodedLength(long pUnencodedLength)
        {
            if (pUnencodedLength < 0) throw new ArgumentOutOfRangeException(nameof(pUnencodedLength));

            long l3s = pUnencodedLength / 3;
            if (pUnencodedLength % 3 != 0) l3s++;

            long l57s = pUnencodedLength / 57;
            if (pUnencodedLength % 57 != 0) l57s++;

            return l3s * 4 + l57s * 2;
        }


























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
            cMethodControl lMC = new cMethodControl(-1, CancellationToken.None);
            cBatchSizer lWS = new cBatchSizer(new cBatchSizerConfiguration(10, 10, 10000, 10));
            string lIntermediateString;
            string lFinalString;

            using (MemoryStream lInput = new MemoryStream(Encoding.UTF8.GetBytes(pInputString)))
            using (cBase64Encoder lEncoder = new cBase64Encoder(lInput, new cBatchSizerConfiguration(10, 10, 10000, 10)))
            using (MemoryStream lIntermediate = new MemoryStream())
            {
                lEncoder.CopyTo(lIntermediate);

                if (lIntermediate.Length != EncodedLength(lInput.Length)) throw new cTestsException($"{nameof(cBase64Encoder)}({pTestName}.l)");

                lIntermediateString = new string(Encoding.UTF8.GetChars(lIntermediate.ToArray()));

                using (MemoryStream lFinal = new MemoryStream())
                {
                    cBase64Decoder lDecoder = new cBase64Decoder(lMC, lFinal, lWS);
                    await lDecoder.WriteAsync(lIntermediate.ToArray(), 0, pParentContext);
                    await lDecoder.FlushAsync(pParentContext);
                    lFinalString = new string(Encoding.UTF8.GetChars(lFinal.ToArray()));
                }
            }

            if (lFinalString != pInputString) throw new cTestsException($"{nameof(cBase64Encoder)}({pTestName}.f)");

            // check the lines are no longer than 76 chars and that they all end with crlf and there are no blank lines

            int lLineStart = 0;

            while (true)
            {
                if (lLineStart == lIntermediateString.Length) break;
                int lLineEnd = lIntermediateString.IndexOf("\r\n", lLineStart);
                if (lLineEnd == -1) throw new cTestsException($"{nameof(cBase64Encoder)}({pTestName}.e)");
                int lLineLength = lLineEnd - lLineStart;
                if (lLineLength < 1 || lLineLength > 76) throw new cTestsException($"{nameof(cBase64Encoder)}({pTestName}.ll)");
                lLineStart = lLineEnd + 2;
            }
        }
    }
}