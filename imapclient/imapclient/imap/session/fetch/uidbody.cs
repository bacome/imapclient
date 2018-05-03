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
            public async Task UIDFetchBodyAsync(iMailboxHandle pMailboxHandle, cSectionCachePersistentKey pKey, cSectionCache.cItem.cReaderWriter pReaderWriter, CancellationToken pCancellationToken, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(UIDFetchBodyAsync), pMailboxHandle, pKey);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eIMAPConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);
                if (pKey == null) throw new ArgumentNullException(nameof(pKey));
                if (_ConnectedAccountId != pKey.AccountId) throw new InvalidOperationException(kInvalidOperationExceptionMessage.WrongAccount);
                if (pReaderWriter == null) throw new ArgumentNullException(nameof(pReaderWriter));

                mMailboxCache.CheckIsSelectedMailbox(pMailboxHandle, pKey.UID.UIDValidity); // to be repeated inside the select lock

                pReaderWriter.WriteBegin(lContext);

                bool lBinary = _Capabilities.Binary && pKey.Section.TextPart == eSectionTextPart.all && pKey.Decoding != eDecodingRequired.none;
                cDecoder lDecoder = cDecoder.GetDecoder(lBinary, pKey.Decoding, pReaderWriter, lContext);

                uint lOrigin = 0;
                Stopwatch lStopwatch = new Stopwatch();

                while (true)
                {
                    int lLength = mFetchBodySizer.Current;

                    lStopwatch.Restart();
                    var lBody = await ZUIDFetchBodyAsync(pMailboxHandle, pKey.UID, lBinary, pKey.Section, lOrigin, (uint)lLength, pCancellationToken, lContext).ConfigureAwait(false);
                    lStopwatch.Stop();

                    // store the time taken so the next fetch is a better size
                    mFetchBodySizer.AddSample(lBody.Bytes.Count, lStopwatch.ElapsedMilliseconds);

                    uint lBodyOrigin = lBody.Origin ?? 0;

                    // the body that we get may start before the place that we asked for
                    int lOffset = (int)(lOrigin - lBodyOrigin);

                    // write the bytes
                    await lDecoder.WriteAsync(pMC, lBody.Bytes, lOffset, lContext).ConfigureAwait(false);

                    // if the body we got was the whole body, we are done
                    if (lBody.Origin == null) break;

                    // if we got less bytes than asked for then we will assume that we are at the end
                    if (lBody.Bytes.Count - lOffset < lLength) break;

                    // set the start point for the next fetch
                    lOrigin = lBodyOrigin + (uint)lBody.Bytes.Count;
                }

                // finish the write
                await lDecoder.FlushAsync(pMC, lContext).ConfigureAwait(false);

                // submit the item to cache
                await pReaderWriter.WriteEndAsync(pMC, pKey, lContext).ConfigureAwait(false);
            }
        }
    }
}