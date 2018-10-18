using System;
using System.IO;
using System.Threading.Tasks;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents an IMAP message attachment.
    /// </summary>
    /// <remarks>
    /// Instances of this class are only valid whilst the mailbox that they are in remains selected. 
    /// Re-selecting the mailbox will not bring instances back to life.
    /// Instances of this class are only valid whilst the containing mailbox has the same UIDValidity.
    /// </remarks>
    public class cIMAPAttachment : cMailAttachment, IEquatable<cIMAPAttachment>
    {
        /**<summary>The client that the instance was created by.</summary>*/
        public readonly cIMAPClient Client;
        /**<summary>The message that the attachment belongs to.</summary>*/
        public readonly iMessageHandle MessageHandle;

        internal cIMAPAttachment(cIMAPClient pClient, iMessageHandle pMessageHandle, cSinglePartBody pPart) : base(pPart)
        {
            Client = pClient ?? throw new ArgumentNullException(nameof(pClient));
            MessageHandle = pMessageHandle ?? throw new ArgumentNullException(nameof(pMessageHandle));
        }

        public bool IsInvalid => MessageHandle.MessageCache.IsInvalid;

        ;?; // decoded can't default to true
        public override Stream GetMessageDataStream(bool pDecoded = true) => new cIMAPMessageDataStream(Client, MessageHandle, Part, pDecoded);
        ;?; // decoded can't default to true
        public cIMAPMessageDataStream GetIMAPMessageDataStream(bool pDecoded = true) => new cIMAPMessageDataStream(Client, MessageHandle, Part, pDecoded);

        public override void SaveAs(string pPath, cSetMaximumConfiguration pConfiguration = null)
        {
            var lContext = Client.RootContext.NewMethod(nameof(cIMAPAttachment), nameof(SaveAs), pPath, pConfiguration);
            Client.Wait(ZSaveAsAsync(pPath, pConfiguration, lContext), lContext);
        }

        public override Task SaveAsAsync(string pPath, cSetMaximumConfiguration pConfiguration = null)
        {
            var lContext = Client.RootContext.NewMethod(nameof(cIMAPAttachment), nameof(SaveAsAsync), pPath, pConfiguration);
            return ZSaveAsAsync(pPath, pConfiguration, lContext);
        }

        private Task ZSaveAsAsync(string pPath, cSetMaximumConfiguration pConfiguration, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPAttachment), nameof(ZSaveAsAsync), pPath, pConfiguration);

            if (pPath == null) throw new ArgumentNullException(nameof(pPath));

            if (pConfiguration == null)
            {
                using (var lToken = Client.CancellationManager.GetToken(lContext))
                {
                    var lMC = new cMethodControl(Client.Timeout, lToken.CancellationToken);
                    return ZZSaveAsAsync(lMC, pPath, null, null, lContext);
                }
            }
            else return ZZSaveAsAsync(pConfiguration.MC, pPath, pConfiguration.SetMaximum, pConfiguration.Increment, lContext);
        }

        private async Task ZZSaveAsAsync(cMethodControl pMC, string pPath, Action<long> pSetMaximum, Action<int> pIncrement, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPAttachment), nameof(ZZSaveAsAsync), pMC, pPath);

            using (var lMessageDataStream = new cIMAPMessageDataStream(Client, MessageHandle, Part, Part.DecodingRequired != eDecodingRequired.none))
            {
                long? lProgressLength;
                if (pSetMaximum == null) lProgressLength = null;
                else lProgressLength = await lMessageDataStream.GetProgressLengthAsync(pMC, lContext).ConfigureAwait(false);

                if (lProgressLength != null) Client.InvokeActionLong(pSetMaximum, lProgressLength.Value, lContext);

                using (var lFileStream = new FileStream(pPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    var lBuffer = new byte[cMailClient.BufferSize];

                    long lLastProgressPosition = 0;

                    while (true)
                    {
                        lMessageDataStream.ReadTimeout = pMC.Timeout;
                        var lBytesRead = await lMessageDataStream.ReadAsync(lBuffer, 0, lBuffer.Length, pMC.CancellationToken).ConfigureAwait(false);

                        // filestreams can't timeout
                        if (lBytesRead != 0) await lFileStream.WriteAsync(lBuffer, 0, lBytesRead, pMC.CancellationToken).ConfigureAwait(false);

                        if (pIncrement != null)
                        {
                            long lThisProgressPosition = lMessageDataStream.GetProgressPosition();
                            int lIncrement = (int)(lThisProgressPosition - lLastProgressPosition);
                            if (lIncrement > 0) Client.InvokeActionInt(pIncrement, lIncrement, lContext);
                            lLastProgressPosition = lThisProgressPosition;
                        }

                        if (lBytesRead == 0) break;
                    }
                }
            }

            YSetFileTimes(pPath, lContext);
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cIMAPAttachment pObject) => this == pObject;

        /// <inheritdoc />
        public override bool Equals(object pObject) => this == pObject as cIMAPAttachment;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetHashCode"/>
        public override int GetHashCode() 
        {
            unchecked
            {
                int lHash = 17;
                lHash = lHash * 23 + MessageHandle.GetHashCode();
                lHash = lHash * 23 + Part.GetHashCode();
                return lHash;
            }
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cIMAPAttachment)}({MessageHandle},{Part.Section})";

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality(cAPIDocumentationTemplate, cAPIDocumentationTemplate)"/>
        public static bool operator ==(cIMAPAttachment pA, cIMAPAttachment pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return pA.Client.Equals(pB.Client) && pA.MessageHandle.Equals(pB.MessageHandle) && pA.Part.Equals(pB.Part);
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality(cAPIDocumentationTemplate, cAPIDocumentationTemplate)"/>
        public static bool operator !=(cIMAPAttachment pA, cIMAPAttachment pB) => !(pA == pB);
    }
}