using System;
using System.Collections.Generic;
using work.bacome.imapclient;

namespace work.bacome.imapclient
{
    public class cBase64Decoder : cDecoder
    {
        private readonly List<byte> mInputBytes = new List<byte>(76);

        public cBase64Decoder() { }

        protected sealed override void YDecode(bool pCRLF)
        {
            foreach (var lByte in mBufferedInputBytes) if (cBase64.IsInAlphabet(lByte)) mInputBytes.Add(lByte);
            if (!cBase64.TryDecode(mInputBytes, out var lDecodedBytes, out var lError)) throw new cDecodingException(lError);
            mDecodedBytes.AddRange(lDecodedBytes);
            mInputBytes.Clear();
        }
    }
}
