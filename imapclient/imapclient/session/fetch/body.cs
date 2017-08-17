using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            public Task FetchBodyAsync(cFetchBodyMethodControl pMC, iMessageHandle pHandle, cSection pSection, eDecodingRequired pDecoding, Stream pStream, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(FetchBodyAsync), pMC, pHandle, pSection, pDecoding);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));

                mMailboxCache.CheckInSelectedMailbox(pHandle); // to be repeated inside the select lock

                if (pHandle.UID == null) return ZFetchBodyAsync(pMC, pHandle.Cache.MailboxHandle, pHandle.UID, null, pSection, pDecoding, pStream, lContext);
                else return ZFetchBodyAsync(pMC, null, null, pHandle, pSection, pDecoding, pStream, lContext);
            }

            public Task UIDFetchBodyAsync(cFetchBodyMethodControl pMC, iMailboxHandle pHandle, cUID pUID, cSection pSection, eDecodingRequired pDecoding, Stream pStream, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(UIDFetchBodyAsync), pMC, pHandle, pUID, pSection, pDecoding);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));

                mMailboxCache.CheckIsSelectedMailbox(pHandle, pUID.UIDValidity); // to be repeated inside the select lock

                return ZFetchBodyAsync(pMC, pHandle, pUID, null, pSection, pDecoding, pStream, lContext);
            }

            private async Task ZFetchBodyAsync(cFetchBodyMethodControl pMC, iMailboxHandle pMailboxHandle, cUID pUID, iMessageHandle pMessageHandle, cSection pSection, eDecodingRequired pDecoding, Stream pStream, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZFetchBodyAsync), pMC, pMailboxHandle, pUID, pMessageHandle, pSection, pDecoding);

                // work out if binary can/should be used or not
                bool lBinary = mCapabilities.Binary && pSection.TextPart == eSectionPart.all && pDecoding != eDecodingRequired.none;

                cDecoder lDecoder;

                if (lBinary || pDecoding == eDecodingRequired.none) lDecoder = new cIdentityDecoder(pStream);
                else if (pDecoding == eDecodingRequired.base64) lDecoder = new cBase64Decoder(pStream);
                else if (pDecoding == eDecodingRequired.quotedprintable) lDecoder = new cQuotedPrintableDecoder(pStream);
                else throw new cContentTransferDecodingException("required decoding not supported", lContext);

                uint lOrigin = 0;

                while (true)
                {
                    int lLength = mFetchBodyReadSizer.Current;

                    Stopwatch lStopwatch = Stopwatch.StartNew();

                    cBody lBody;
                    if (pUID == null) lBody = await ZFetchBodyAsync(pMC, pMessageHandle, lBinary, pSection, lOrigin, (uint)lLength, lContext).ConfigureAwait(false);
                    else lBody = await ZUIDFetchBodyAsync(pMC, pMailboxHandle, pUID, lBinary, pSection, lOrigin, (uint)lLength, lContext).ConfigureAwait(false);

                    lStopwatch.Stop();

                    // store the time taken so the next fetch is a better size
                    mFetchBodyReadSizer.AddSample(lBody.Bytes.Count, lStopwatch.ElapsedMilliseconds, lContext);

                    uint lBodyOrigin = lBody.Origin ?? 0;

                    // the body that we get may start before the place that we asked for
                    int lOffset = (int)(lOrigin - lBodyOrigin);

                    // write the bytes
                    await lDecoder.WriteAsync(pMC, lBody.Bytes, lOffset, lContext).ConfigureAwait(false);

                    // update progress
                    pMC.IncrementProgress(lBody.Bytes.Count - lOffset, lContext);

                    // if the body we got was the whole body, we are done
                    if (lBody.Origin == null) break;

                    // if we got less bytes than asked for then we will assume that we are at the end
                    if (lBody.Bytes.Count - lOffset < lLength) break;

                    // set the start point for the next fetch
                    lOrigin = lBodyOrigin + (uint)lBody.Bytes.Count;
                }

                await lDecoder.FlushAsync(pMC, lContext).ConfigureAwait(false);
            }
        }
    }
}