using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            public Task FetchBodyAsync(cMethodControl pMC, iMessageHandle pHandle, cSection pSection, eDecodingRequired pDecoding, Stream pStream, cProgress pProgress, cBatchSizer pWriteSizer, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(FetchBodyAsync), pMC, pHandle, pSection, pDecoding);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);

                mMailboxCache.CheckInSelectedMailbox(pHandle); // to be repeated inside the select lock

                if (pHandle.UID == null) return ZFetchBodyAsync(pMC, null, null, pHandle, pSection, pDecoding, pStream, pProgress, pWriteSizer, lContext);
                else return ZFetchBodyAsync(pMC, pHandle.Cache.MailboxHandle, pHandle.UID, null, pSection, pDecoding, pStream, pProgress, pWriteSizer, lContext);
            }

            public Task UIDFetchBodyAsync(cMethodControl pMC, iMailboxHandle pHandle, cUID pUID, cSection pSection, eDecodingRequired pDecoding, Stream pStream, cProgress pProgress, cBatchSizer pWriteSizer, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(UIDFetchBodyAsync), pMC, pHandle, pUID, pSection, pDecoding);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);

                mMailboxCache.CheckIsSelectedMailbox(pHandle, pUID.UIDValidity); // to be repeated inside the select lock

                return ZFetchBodyAsync(pMC, pHandle, pUID, null, pSection, pDecoding, pStream, pProgress, pWriteSizer, lContext);
            }

            private async Task ZFetchBodyAsync(cMethodControl pMC, iMailboxHandle pMailboxHandle, cUID pUID, iMessageHandle pMessageHandle, cSection pSection, eDecodingRequired pDecoding, Stream pStream, cProgress pProgress, cBatchSizer pWriteSizer, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZFetchBodyAsync), pMC, pMailboxHandle, pUID, pMessageHandle, pSection, pDecoding);

                if (pSection == null) throw new ArgumentNullException(nameof(pSection));
                if (pStream == null) throw new ArgumentNullException(nameof(pStream));
                if (pProgress == null) throw new ArgumentNullException(nameof(pProgress));
                if (pWriteSizer == null) throw new ArgumentNullException(nameof(pWriteSizer));

                if (!pStream.CanWrite) throw new ArgumentOutOfRangeException(nameof(pStream));

                // work out if binary can/should be used or not
                bool lBinary = _Capabilities.Binary && pSection.TextPart == eSectionTextPart.all && pDecoding != eDecodingRequired.none;

                cDecoder lDecoder;

                if (lBinary || pDecoding == eDecodingRequired.none) lDecoder = new cIdentityDecoder(pStream);
                else if (pDecoding == eDecodingRequired.base64) lDecoder = new cBase64Decoder(pStream);
                else if (pDecoding == eDecodingRequired.quotedprintable) lDecoder = new cQuotedPrintableDecoder(pStream);
                else throw new cContentTransferDecodingException("required decoding not supported", lContext);

                uint lOrigin = 0;

                Stopwatch lStopwatch = new Stopwatch();

                while (true)
                {
                    int lLength = mFetchBodyReadSizer.Current;

                    lStopwatch.Restart();

                    cBody lBody;
                    if (pUID == null) lBody = await ZFetchBodyAsync(pMC, pMessageHandle, lBinary, pSection, lOrigin, (uint)lLength, lContext).ConfigureAwait(false);
                    else lBody = await ZUIDFetchBodyAsync(pMC, pMailboxHandle, pUID, lBinary, pSection, lOrigin, (uint)lLength, lContext).ConfigureAwait(false);

                    lStopwatch.Stop();

                    // store the time taken so the next fetch is a better size
                    mFetchBodyReadSizer.AddSample(lBody.Bytes.Count, lStopwatch.ElapsedMilliseconds);

                    uint lBodyOrigin = lBody.Origin ?? 0;

                    // the body that we get may start before the place that we asked for
                    int lOffset = (int)(lOrigin - lBodyOrigin);

                    // write the bytes
                    await lDecoder.WriteAsync(pMC, lBody.Bytes, lOffset, pWriteSizer, lContext).ConfigureAwait(false);

                    // update progress
                    pProgress.Increment(lBody.Bytes.Count - lOffset, lContext);

                    // if the body we got was the whole body, we are done
                    if (lBody.Origin == null) break;

                    // if we got less bytes than asked for then we will assume that we are at the end
                    if (lBody.Bytes.Count - lOffset < lLength) break;

                    // set the start point for the next fetch
                    lOrigin = lBodyOrigin + (uint)lBody.Bytes.Count;
                }

                await lDecoder.FlushAsync(pMC, pWriteSizer, lContext).ConfigureAwait(false);
            }
        }
    }
}