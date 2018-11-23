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

        public override Stream GetMessageDataStream(bool pDecodedIfRequired = true) => new cIMAPMessageDataStream(Client, MessageHandle, Part, pDecodedIfRequired);
        public cIMAPMessageDataStream GetIMAPMessageDataStream(bool pDecodedIfRequired = true) => new cIMAPMessageDataStream(Client, MessageHandle, Part, pDecodedIfRequired);

        public override void SaveAs(string pPath, cMethodConfiguration pConfiguration = null)
        {
            var lContext = Client.RootContext.NewMethod(nameof(cIMAPAttachment), nameof(SaveAs), pPath, pConfiguration);
            Client.Wait(ZSaveAsAsync(pPath, pConfiguration, lContext), lContext);
        }

        public override Task SaveAsAsync(string pPath, cMethodConfiguration pConfiguration = null)
        {
            var lContext = Client.RootContext.NewMethod(nameof(cIMAPAttachment), nameof(SaveAsAsync), pPath, pConfiguration);
            return ZSaveAsAsync(pPath, pConfiguration, lContext);
        }

        private Task ZSaveAsAsync(string pPath, cMethodConfiguration pConfiguration, cTrace.cContext pParentContext)
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
            else return ZZSaveAsAsync(pConfiguration.MC, pPath, pConfiguration.SetMaximum1, pConfiguration.Increment1, lContext);
        }

        private async Task ZZSaveAsAsync(cMethodControl pMC, string pPath, Action<long> pSetMaximum, Action<int> pIncrement, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPAttachment), nameof(ZZSaveAsAsync), pMC, pPath);

            using (var lMessageDataStream = new cIMAPMessageDataStream(Client, MessageHandle, Part, true))
            {
                cIMAPMessageDataStream.cScale lScale;
                if (pSetMaximum == null) lScale = null;
                else lScale = await lMessageDataStream.GetScaleAsync(pMC, lContext).ConfigureAwait(false);

                if (lScale != null) Client.InvokeActionLong(pSetMaximum, lScale.Value, lContext);

                using (var lFileStream = new FileStream(pPath, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var lIncrementer = Client.GetNewIncrementer(pIncrement, lContext))
                {
                    var lBuffer = new byte[cMailClient.BufferSize];

                    long lLastProgressPosition = 0;

                    while (true)
                    {
                        lMessageDataStream.ReadTimeout = pMC.Timeout;
                        var lBytesRead = await lMessageDataStream.ReadAsync(lBuffer, 0, lBuffer.Length, pMC.CancellationToken).ConfigureAwait(false);

                        if (lBytesRead != 0) await lFileStream.WriteAsync(lBuffer, 0, lBytesRead, pMC.CancellationToken).ConfigureAwait(false);

                        if (pIncrement != null)
                        {
                            if (lScale == null) lIncrementer.Increment(lBytesRead);
                            else
                            {
                                var lThisProgressPosition = lMessageDataStream.GetPositionOnScale(lScale);

                                int lIncrement = (int)(lThisProgressPosition - lLastProgressPosition);

                                if (lIncrement > 0)
                                {
                                    lIncrementer.Increment(lIncrement);
                                    lLastProgressPosition = lThisProgressPosition;
                                }
                            }
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
            var lReferenceEquals = cTools.EqualsReferenceEquals(pA, pB);
            if (lReferenceEquals != null) return lReferenceEquals.Value;
            return pA.Client.Equals(pB.Client) && pA.MessageHandle.Equals(pB.MessageHandle) && pA.Part.Equals(pB.Part);
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality(cAPIDocumentationTemplate, cAPIDocumentationTemplate)"/>
        public static bool operator !=(cIMAPAttachment pA, cIMAPAttachment pB) => !(pA == pB);
    }
}