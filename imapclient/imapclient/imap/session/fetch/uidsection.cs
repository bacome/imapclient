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
            public async Task UIDFetchSectionAsync(iMailboxHandle pMailboxHandle, cUID pUID, cSection pSection, eDecodingRequired pDecoding, iFetchSectionTarget pTarget, CancellationToken pCancellationToken, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(UIDFetchSectionAsync), pMailboxHandle, pUID, pSection, pDecoding);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eIMAPConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);

                if (pUID == null) throw new ArgumentNullException(nameof(pUID));

                mMailboxCache.CheckIsSelectedMailbox(pMailboxHandle, pUID.UIDValidity); // to be repeated inside the select lock

                if (pSection == null) throw new ArgumentNullException(nameof(pSection));
                if (pTarget == null) throw new ArgumentNullException(nameof(pTarget));

                bool lBinary = _Capabilities.Binary && pSection.TextPart == eSectionTextPart.all && pDecoding != eDecodingRequired.none;
                cFetchSectionTargetWriter lWriter = new cFetchSectionTargetWriter(lBinary, pDecoding, pTarget);

                uint lOrigin = 0;
                Stopwatch lStopwatch = new Stopwatch();

                while (true)
                {
                    int lLength = mFetchBodySizer.Current;

                    lStopwatch.Restart();
                    var lBody = await ZUIDFetchBodyAsync(pMailboxHandle, pUID, lBinary, pSection, lOrigin, (uint)lLength, pCancellationToken, lContext).ConfigureAwait(false);
                    lStopwatch.Stop();

                    // store the time taken so the next fetch is a better size
                    mFetchBodySizer.AddSample(lBody.Bytes.Count, lStopwatch.ElapsedMilliseconds);

                    uint lBodyOrigin = lBody.Origin ?? 0;

                    // the body that we get may start before the place that we asked for
                    int lOffset = (int)(lOrigin - lBodyOrigin);

                    // write the bytes
                    await lWriter.WriteAsync(lBody.Bytes, lOffset, pCancellationToken, lContext).ConfigureAwait(false);

                    // if the body we got was the whole body, we are done
                    if (lBody.Origin == null) break;

                    // if we got less bytes than asked for then we will assume that we are at the end
                    if (lBody.Bytes.Count - lOffset < lLength) break;

                    // set the start point for the next fetch
                    lOrigin = lBodyOrigin + (uint)lBody.Bytes.Count;
                }

                // finish the write
                await lWriter.WriteAsync(cBytes.Empty, 0, pCancellationToken, lContext).ConfigureAwait(false);
            }
        }
    }
}