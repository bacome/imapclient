using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cFetchSectionTargetWriter
            {
                private readonly cDecoder mDecoder;
                private readonly iFetchSectionTarget mTarget;
                private int mLastBufferedInputBytes = 0;

                public cFetchSectionTargetWriter(bool pBinary, eDecodingRequired pDecoding, iFetchSectionTarget pTarget)
                {
                    if (pBinary || pDecoding == eDecodingRequired.none) mDecoder = null;
                    else if (pDecoding == eDecodingRequired.base64) mDecoder = new cBase64Decoder();
                    else if (pDecoding == eDecodingRequired.quotedprintable) mDecoder = new cQuotedPrintableDecoder();
                    else throw new cContentTransferDecodingNotSupportedException(pDecoding);

                    mTarget = pTarget ?? throw new ArgumentNullException(nameof(pTarget));
                }

                public Task WriteAsync(IList<byte> pBytes, int pOffset, CancellationToken pCancellationToken, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cFetchSectionTargetWriter), nameof(WriteAsync), pOffset);

                    if (pBytes == null) throw new ArgumentNullException(nameof(pBytes));
                    if (pOffset > pBytes.Count) throw new ArgumentOutOfRangeException(nameof(pOffset));
                    if (pOffset == pBytes.Count) return Task.WhenAll(); // TODO => Task.CompletedTask;

                    if (mDecoder == null)
                    {
                        var lBuffer = new byte[pBytes.Count - pOffset];
                        for (int i = 0; i < lBuffer.Length; i++) lBuffer[i] = pBytes[pOffset + i];
                        return mTarget.WriteAsync(lBuffer, lBuffer.Length, pCancellationToken, lContext);
                    }
                    else
                    {
                        var lCount = pBytes.Count - pOffset;

                        var lBuffer = mDecoder.Decode(pBytes, pOffset, lCount);

                        if (lBuffer == null)
                        {
                            mLastBufferedInputBytes = mDecoder.GetBufferedInputBytes();
                            return Task.WhenAll(); // TODO: convert to completed task
                        }

                        int lThisBufferedInputBytes = mDecoder.GetBufferedInputBytes();
                        int lBytesThatWereBuffered = lThisBufferedInputBytes - mLastBufferedInputBytes;
                        mLastBufferedInputBytes = mDecoder.GetBufferedInputBytes();

                        return mTarget.WriteAsync(lBuffer, lCount - lBytesThatWereBuffered, pCancellationToken, lContext);
                    }
                }
            }
        }
    }
}