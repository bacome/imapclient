using System;
using System.IO;
using System.Threading.Tasks;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    public enum eQuotedPrintableEncodingType { Binary, CRLFTerminatedLines, LFTerminatedLines }
    public enum eQuotedPrintableEncodingRule { Minimal, EBCDIC }

    public class cBase64DecodingStream : cDecodingStream
    {
        public cBase64DecodingStream(Stream pStream) : base(pStream, new cBase64Decoder()) { }
        public static long GetDecodedLength(Stream pStream) => GetDecodedLength(pStream, new cBase64Decoder());
        public static Task<long> GetDecodedLengthAsync(Stream pStream) => GetDecodedLengthAsync(pStream, new cBase64Decoder());
    }

    public class cQuotedPrintableDecodingStream : cDecodingStream
    {
        public cQuotedPrintableDecodingStream(Stream pStream) : base(pStream, new cQuotedPrintableDecoder()) { }
        public static long GetDecodedLength(Stream pStream) => GetDecodedLength(pStream, new cQuotedPrintableDecoder());
        public static Task<long> GetDecodedLengthAsync(Stream pStream) => GetDecodedLengthAsync(pStream, new cQuotedPrintableDecoder());
    }

    public class cBase64EncodingStream : cEncodingStream
    {
        public cBase64EncodingStream(Stream pStream) : base(pStream, new cBase64Encoder()) { }
        public static long GetEncodedLength(long pUnencodedLength) => cBase64Encoder.GetEncodedLength(pUnencodedLength);
    }

    public class cQuotedPrintableEncodingStream : cEncodingStream
    {
        private static readonly eQuotedPrintableEncodingType kDefaultType = Environment.NewLine == "\n" ? eQuotedPrintableEncodingType.LFTerminatedLines : eQuotedPrintableEncodingType.CRLFTerminatedLines;

        public cQuotedPrintableEncodingStream(Stream pStream) : base(pStream, new cQuotedPrintableEncoder(kDefaultType, eQuotedPrintableEncodingRule.EBCDIC)) { }
        public cQuotedPrintableEncodingStream(Stream pStream, eQuotedPrintableEncodingType pType, eQuotedPrintableEncodingRule pRule) : base(pStream, new cQuotedPrintableEncoder(pType, pRule)) { }

        public static long GetEncodedLength(Stream pStream) => ZGetEncodedLength(pStream, kDefaultType, eQuotedPrintableEncodingRule.EBCDIC);
        public static long GetEncodedLength(Stream pStream, eQuotedPrintableEncodingType pType, eQuotedPrintableEncodingRule pRule) => ZGetEncodedLength(pStream, pType, pRule);
        public static Task<long> GetEncodedLengthAsync(Stream pStream) => ZGetEncodedLengthAsync(pStream, kDefaultType, eQuotedPrintableEncodingRule.EBCDIC);
        public static Task<long> GetEncodedLengthAsync(Stream pStream, eQuotedPrintableEncodingType pType, eQuotedPrintableEncodingRule pRule) => ZGetEncodedLengthAsync(pStream, pType, pRule);

        private static long ZGetEncodedLength(Stream pStream, eQuotedPrintableEncodingType pType, eQuotedPrintableEncodingRule pRule)
        {
            if (pStream == null) throw new ArgumentNullException(nameof(pStream));
            if (!pStream.CanRead) throw new ArgumentOutOfRangeException(nameof(pStream));
            if (pStream.CanSeek && pStream.Position != 0) pStream.Position = 0;

            cQuotedPrintableEncoder lEncoder = new cQuotedPrintableEncoder(pType, pRule);
            byte[] lUnencodedBuffer = new byte[cMailClient.BufferSize];
            long lEncodedLength = 0;

            while (true)
            {
                int lBytesRead = pStream.Read(lUnencodedBuffer, 0, cMailClient.BufferSize);
                lEncodedLength += lEncoder.GetEncodedLength(lUnencodedBuffer, lBytesRead);
                if (lBytesRead == 0) return lEncodedLength;
            }
        }

        private static async Task<long> ZGetEncodedLengthAsync(Stream pStream, eQuotedPrintableEncodingType pType, eQuotedPrintableEncodingRule pRule)
        {
            if (pStream == null) throw new ArgumentNullException(nameof(pStream));
            if (!pStream.CanRead) throw new ArgumentOutOfRangeException(nameof(pStream));
            if (pStream.CanSeek && pStream.Position != 0) pStream.Position = 0;

            cQuotedPrintableEncoder lEncoder = new cQuotedPrintableEncoder(pType, pRule);
            byte[] lUnencodedBuffer = new byte[cMailClient.BufferSize];
            long lEncodedLength = 0;

            while (true)
            {
                int lBytesRead = await pStream.ReadAsync(lUnencodedBuffer, 0, cMailClient.BufferSize).ConfigureAwait(false);
                lEncodedLength += lEncoder.GetEncodedLength(lUnencodedBuffer, lBytesRead);
                if (lBytesRead == 0) return lEncodedLength;
            }
        }
    }
}