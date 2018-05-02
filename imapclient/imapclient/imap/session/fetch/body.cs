using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            public Task FetchBodyAsync(cMethodControl pMC, iMessageHandle pMessageHandle, cSection pSection, eDecodingRequired pDecoding, cSectionCache.cItem.cReaderWriter pReaderWriter, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(FetchBodyAsync), pMC, pMessageHandle, pSection, pDecoding);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eIMAPConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);

                mMailboxCache.CheckInSelectedMailbox(pMessageHandle); // to be repeated inside the select lock

                if (pMessageHandle.UID == null) return ZFetchBodyAsync(pMC, null, null, pMessageHandle, pSection, pDecoding, pReaderWriter, lContext);
                else return ZFetchBodyAsync(pMC, pMessageHandle.MessageCache.MailboxHandle, pMessageHandle.UID, null, pSection, pDecoding, pReaderWriter, lContext);
            }

            public Task UIDFetchBodyAsync(cMethodControl pMC, iMailboxHandle pMailboxHandle, cUID pUID, cSection pSection, eDecodingRequired pDecoding, cSectionCache.cItem.cReaderWriter pReaderWriter, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(UIDFetchBodyAsync), pMC, pMailboxHandle, pUID, pSection, pDecoding);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eIMAPConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);

                mMailboxCache.CheckIsSelectedMailbox(pMailboxHandle, pUID.UIDValidity); // to be repeated inside the select lock

                return ZFetchBodyAsync(pMC, pMailboxHandle, pUID, null, pSection, pDecoding, pReaderWriter, lContext);
            }

            private async Task ZFetchBodyAsync(cMethodControl pMC, iMailboxHandle pMailboxHandle, cUID pUID, iMessageHandle pMessageHandle, cSection pSection, eDecodingRequired pDecoding, cSectionCache.cItem.cReaderWriter pReaderWriter, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZFetchBodyAsync), pMC, pMailboxHandle, pUID, pMessageHandle, pSection, pDecoding);

                if (pSection == null) throw new ArgumentNullException(nameof(pSection));
                if (pReaderWriter == null) throw new ArgumentNullException(nameof(pReaderWriter));

                // work out if binary can/should be used or not
                bool lBinary = _Capabilities.Binary && pSection.TextPart == eSectionTextPart.all && pDecoding != eDecodingRequired.none;

                cDecoder lDecoder;

                if (lBinary || pDecoding == eDecodingRequired.none) lDecoder = new cIdentityDecoder(pReaderWriter);
                else if (pDecoding == eDecodingRequired.base64) lDecoder = new cBase64Decoder(pReaderWriter);
                else if (pDecoding == eDecodingRequired.quotedprintable) lDecoder = new cQuotedPrintableDecoder(pReaderWriter);
                else throw new cContentTransferDecodingException("required decoding not supported", lContext);

                uint lOrigin = 0;
                Stopwatch lStopwatch = new Stopwatch();

                while (true)
                {
                    int lLength = mFetchBodySizer.Current;

                    lStopwatch.Restart();

                    cBody lBody;
                    if (pUID == null) lBody = await ZFetchBodyAsync(pMC, pMessageHandle, lBinary, pSection, lOrigin, (uint)lLength, lContext).ConfigureAwait(false);
                    else lBody = await ZUIDFetchBodyAsync(pMC, pMailboxHandle, pUID, lBinary, pSection, lOrigin, (uint)lLength, lContext).ConfigureAwait(false);

                    lStopwatch.Stop();

                    // store the time taken so the next fetch is a better size
                    mFetchBodySizer.AddSample(lBody.Bytes.Count, lStopwatch.ElapsedMilliseconds);

                    uint lBodyOrigin = lBody.Origin ?? 0;

                    // the body that we get may start before the place that we asked for
                    int lOffset = (int)(lOrigin - lBodyOrigin);

                    // write the bytes
                    await lDecoder.WriteAsync(pMC, lBody.Bytes, lOffset,, lContext).ConfigureAwait(false);

                    // if the body we got was the whole body, we are done
                    if (lBody.Origin == null) break;

                    // if we got less bytes than asked for then we will assume that we are at the end
                    if (lBody.Bytes.Count - lOffset < lLength) break;

                    // set the start point for the next fetch
                    lOrigin = lBodyOrigin + (uint)lBody.Bytes.Count;
                }

                // end the write
                await lDecoder.FlushAsync(pMC, lContext).ConfigureAwait(false);
            }
        }
    }
}