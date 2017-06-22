using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            public Task FetchAsync(cMethodControl pMC, cMailboxId pMailboxId, iMessageHandle pHandle, cSection pSection, eDecodingRequired pDecoding, Stream pStream, cTrace.cContext pParentContext) => ZFetchSectionAsync(pMC, pMailboxId, pHandle.UID, pHandle, pSection, pDecoding, pStream, pParentContext);
            public Task UIDFetchAsync(cMethodControl pMC, cMailboxId pMailboxId, cUID pUID, cSection pSection, eDecodingRequired pDecoding, Stream pStream, cTrace.cContext pParentContext) => ZFetchSectionAsync(pMC, pMailboxId, pUID, null, pSection, pDecoding, pStream, pParentContext);

            private async Task ZFetchSectionAsync(cMethodControl pMC, cMailboxId pMailboxId, cUID pUID, iMessageHandle pHandle, cSection pSection, eDecodingRequired pDecoding, Stream pStream, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZFetchSectionAsync), pMC, pMailboxId, pUID, pHandle, pSection, pDecoding);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));

                if (pMailboxId.AccountId != _ConnectedAccountId) throw new cAccountNotConnectedException(lContext);

                // capture the capability
                var lCapability = _Capability;

                // work out if binary can/should be used or not
                bool lBinary = lCapability.Binary && pSection.TextPart == cSection.eTextPart.all && pDecoding != eDecodingRequired.none;

                cDecoder lDecoder;

                if (lBinary || pDecoding == eDecodingRequired.none) lDecoder = new cIdentityDecoder(pStream);
                else if (pDecoding == eDecodingRequired.base64) lDecoder = new cBase64Decoder(pStream);
                else if (pDecoding == eDecodingRequired.quotedprintable) lDecoder = new cQuotedPrintableDecoder(pStream);
                else throw new cContentTransferDecodingException("required decoding not supported", lContext);

                uint lOrigin = 0;

                while (true)
                {
                    int lLength = mFetchToStreamConfiguration.Current;

                    Stopwatch lStopwatch = Stopwatch.StartNew();

                    cBody lBody;
                    if (pUID == null) lBody = await ZFetchAsync(pMC, pMailboxId, pHandle, lCapability, lBinary, pSection, lOrigin, (uint)lLength, lContext).ConfigureAwait(false);
                    else lBody = await ZUIDFetchAsync(pMC, pMailboxId, pUID, lCapability, lBinary, pSection, lOrigin, (uint)lLength, lContext).ConfigureAwait(false);

                    lStopwatch.Stop();

                    // store the time taken so the next fetch is a better size
                    mFetchPropertiesConfiguration.AddSample(lBody.Bytes.Count, lStopwatch.ElapsedMilliseconds);

                    uint lBodyOrigin = lBody.Origin ?? 0;

                    // the body that we get may start before the place that we asked for
                    int lOffset = (int)(lOrigin - lBodyOrigin);

                    // write the bytes
                    await lDecoder.WriteAsync(pMC, lBody.Bytes, lOffset).ConfigureAwait(false);

                    // if the body we got was the whole body, we are done
                    if (lBody.Origin == null) break;

                    // if we got less bytes than asked for then we will assume that we are at the end
                    if (lBody.Bytes.Count - lOffset < lLength) break;

                    // set the start point for the next fetch
                    lOrigin = lBodyOrigin + (uint)lBody.Bytes.Count;
                }

                // if there are bytes left to decode then there is a problem because I think we have got all the bytes available
                if (lDecoder.HasUndecodedBytes()) throw new cContentTransferDecodingException("stream truncated", lContext);
            }
        }
    }
}