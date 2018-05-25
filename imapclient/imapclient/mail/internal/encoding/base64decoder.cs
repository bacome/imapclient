using System;
using System.Collections.Generic;

namespace work.bacome.mailclient
{
    internal class cBase64Decoder : cDecoder
    {
        private readonly List<byte> mInput = new List<byte>(76);

        public cBase64Decoder() { }

        protected sealed override void YDecode(bool pCRLF)
        {
            foreach (var lByte in mPendingInput) if (cBase64.IsInAlphabet(lByte)) mInput.Add(lByte);
            if (!cBase64.TryDecode(mInput, out var lOutput, out var lError)) throw new cDecodingException(lError);
            mOutput.AddRange(lOutput);
            mInput.Clear();
        }
    }
}
