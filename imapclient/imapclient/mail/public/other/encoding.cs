using System;
using System.IO;
using System.Threading.Tasks;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{

    public class cBase64DecodingStream : cTransformingStream
    {
        public cBase64DecodingStream(Stream pStream) : base(pStream, new cBase64Decoder()) { }
        public static long GetDecodedLength(Stream pStream) => GetTransformedLength(pStream, new cBase64Decoder());
        public static Task<long> GetDecodedLengthAsync(Stream pStream) => GetTransformedLengthAsync(pStream, new cBase64Decoder());
    }

    public class cQuotedPrintableDecodingStream : cTransformingStream
    {
        public cQuotedPrintableDecodingStream(Stream pStream) : base(pStream, new cQuotedPrintableDecoder()) { }
        public static long GetDecodedLength(Stream pStream) => GetTransformedLength(pStream, new cQuotedPrintableDecoder());
        public static Task<long> GetDecodedLengthAsync(Stream pStream) => GetTransformedLengthAsync(pStream, new cQuotedPrintableDecoder());
    }

    public class cBase64EncodingStream : cTransformingStream
    {
        public cBase64EncodingStream(Stream pStream) : base(pStream, new cBase64Encoder()) { }
        public static long GetEncodedLength(long pUnencodedLength) => cBase64Encoder.GetEncodedLength(pUnencodedLength);
    }

    public class cQuotedPrintableEncodingStream : cTransformingStream
    {
        private static readonly eQuotedPrintableEncodingType kDefaultType = Environment.NewLine == "\n" ? eQuotedPrintableEncodingType.LFTerminatedLines : eQuotedPrintableEncodingType.CRLFTerminatedLines;

        public cQuotedPrintableEncodingStream(Stream pStream) : base(pStream, new cQuotedPrintableEncoder(kDefaultType, eQuotedPrintableEncodingRule.EBCDIC)) { }
        public cQuotedPrintableEncodingStream(Stream pStream, eQuotedPrintableEncodingType pType, eQuotedPrintableEncodingRule pRule) : base(pStream, new cQuotedPrintableEncoder(pType, pRule)) { }

        public static long GetEncodedLength(Stream pStream) => GetTransformedLength(pStream, new cQuotedPrintableEncoder(kDefaultType, eQuotedPrintableEncodingRule.EBCDIC));
        public static long GetEncodedLength(Stream pStream, eQuotedPrintableEncodingType pType, eQuotedPrintableEncodingRule pRule) => GetTransformedLength(pStream, new cQuotedPrintableEncoder(pType, pRule));
        public static Task<long> GetEncodedLengthAsync(Stream pStream) => GetTransformedLengthAsync(pStream, new cQuotedPrintableEncoder(kDefaultType, eQuotedPrintableEncodingRule.EBCDIC));
        public static Task<long> GetEncodedLengthAsync(Stream pStream, eQuotedPrintableEncodingType pType, eQuotedPrintableEncodingRule pRule) => GetTransformedLengthAsync(pStream, new cQuotedPrintableEncoder(pType, pRule));
    }
}