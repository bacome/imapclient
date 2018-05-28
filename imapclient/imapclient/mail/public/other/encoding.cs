using System;
using System.IO;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    public enum eQuotedPrintableEncodingType { Binary, CRLFTerminatedLines, LFTerminatedLines }
    public enum eQuotedPrintableEncodingRule { Minimal, EBCDIC }

    public class cBase64DecodingStream : cDecodingStream
    {
        public cBase64DecodingStream(Stream pStream) : base(pStream, new cBase64Decoder()) { }
    }

    public class cQuotedPrintableDecodingStream : cDecodingStream
    {
        public cQuotedPrintableDecodingStream(Stream pStream) : base(pStream, new cQuotedPrintableDecoder()) { }
    }

    public class cBase64EncodingStream : cEncodingStream
    {
        public cBase64EncodingStream(Stream pStream) : base(pStream, new cBase64Encoder()) { }
        internal static long EncodedLength(long pUnencodedLength) => cBase64Encoder.EncodedLength(pUnencodedLength);
    }

    public class cQuotedPrintableEncodingStream : cEncodingStream
    {
        private static readonly eQuotedPrintableEncodingType kDefaultType = Environment.NewLine == "\n" ? eQuotedPrintableEncodingType.LFTerminatedLines : eQuotedPrintableEncodingType.CRLFTerminatedLines;
        public cQuotedPrintableEncodingStream(Stream pStream) : base(pStream, new cQuotedPrintableEncoder(kDefaultType, eQuotedPrintableEncodingRule.EBCDIC)) { }
        public cQuotedPrintableEncodingStream(Stream pStream, eQuotedPrintableEncodingType pType, eQuotedPrintableEncodingRule pRule) : base(pStream, new cQuotedPrintableEncoder(pType, pRule)) { }
    }

}