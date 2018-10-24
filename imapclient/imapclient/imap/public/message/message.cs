using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents an IMAP message.
    /// </summary>
    /// <remarks>
    /// Instances of this class are only valid whilst the mailbox that they are in remains selected. 
    /// Re-selecting the mailbox will not bring instances back to life.
    /// Instances of this class are only valid whilst the containing mailbox has the same UIDValidity.
    /// </remarks>
    public class cIMAPMessage : cMailMessage, IEquatable<cIMAPMessage>
    {
        private static readonly cMessageCacheItems kUID = fMessageCacheAttributes.uid;
        private static readonly cMessageCacheItems kModSeqFlags = fMessageCacheAttributes.modseqflags;
        private static readonly cMessageCacheItems kEnvelope = fMessageCacheAttributes.envelope;
        private static readonly cMessageCacheItems kReceived = fMessageCacheAttributes.received;
        private static readonly cMessageCacheItems kBodyStructure = fMessageCacheAttributes.bodystructure;
        private static readonly cMessageCacheItems kReferences = cHeaderFieldNames.References;
        private static readonly cMessageCacheItems kImportance = cHeaderFieldNames.Importance;

        private PropertyChangedEventHandler mPropertyChanged;
        private object mPropertyChangedLock = new object();

        /**<summary>The client that this instance was created by.</summary>*/
        public readonly cIMAPClient Client;
        /**<summary>The message that this instance represents.</summary>*/
        public readonly iMessageHandle MessageHandle;

        // re-instate if threading is ever done
        //public readonly int Indent; // Indicates the indent of the message. This only means something when compared to the indents of surrounding items in a threaded list of messages. It is a bit of a hack having it in this class.

        internal cIMAPMessage(cIMAPClient pClient, iMessageHandle pMessageHandle) // , int pIndent = -1 // re-instate if threading is ever done
        {
            Client = pClient ?? throw new ArgumentNullException(nameof(pClient));
            MessageHandle = pMessageHandle ?? throw new ArgumentNullException(nameof(pMessageHandle));
            //Indent = pIndent; // re-instate if threading is ever done
        }

        /// <summary>
        /// Fired when the server notifies the client of a change that affects a property value of the instance.
        /// </summary>
        /// <inheritdoc cref="cAPIDocumentationTemplate.Event" select="remarks"/>
        public event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                lock (mPropertyChangedLock)
                {
                    if (mPropertyChanged == null) Client.MessagePropertyChanged += ZMessagePropertyChanged;
                    mPropertyChanged += value;
                }
            }

            remove
            {
                lock (mPropertyChangedLock)
                {
                    mPropertyChanged -= value;
                    if (mPropertyChanged == null) Client.MessagePropertyChanged -= ZMessagePropertyChanged;
                }
            }
        }

        private void ZMessagePropertyChanged(object pSender, cMessagePropertyChangedEventArgs pArgs)
        {
            if (ReferenceEquals(pArgs.MessageHandle, MessageHandle)) mPropertyChanged?.Invoke(this, pArgs);
        }

        /// <summary>
        /// Indicates whether the message exists on the server.
        /// </summary>
        public bool Expunged => MessageHandle.Expunged;

        /// <summary>
        /// Gets the MessageUID of the message. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// If the message cache does not contain the <see cref="fMessageCacheAttributes.uid"/> of the message, it will be fetched from the server.
        /// Will be <see langword="null"/> if the mailbox does not support unique identifiers.
        /// </remarks>
        public cMessageUID MessageUID
        {
            get
            {
                ZFetch(kUID, true);
                return MessageHandle.MessageUID;
            }
        }

        /// <summary>
        /// Gets the UID of the message. May be <see langword="null"/>.
        /// </summary>
        /// <inheritdoc cref="MessageUID" select="remarks"/>
        public cUID UID
        {
            get
            {
                ZFetch(kUID, true);
                return MessageHandle.MessageUID?.UID;
            }
        }

        /// <summary>
        /// Gets the mod-sequence and flags of the message. May be zero.
        /// </summary>
        /// <remarks>
        /// If the message cache does not contain the <see cref="fMessageCacheAttributes.modseqflags"/> of the message, they will be fetched from the server.
        /// </remarks>
        public cModSeqFlags ModSeqFlags
        {
            get
            {
                ZFetch(kModSeqFlags, true);
                return MessageHandle.ModSeqFlags;
            }
        }

        /// <summary>
        /// Gets the flags set for the message.
        /// </summary>
        /// <inheritdoc cref="Flags" select="remarks"/>
        public cFetchableFlags Flags
        {
            get
            {
                ZFetch(kModSeqFlags, true);
                return MessageHandle.ModSeqFlags.Flags;
            }
        }

        /// <summary>
        /// Indicates whether <see cref="Flags"/> contains <see cref="kMessageFlag.Answered"/>.
        /// </summary>
        /// <inheritdoc cref="Flags" select="remarks"/>
        public bool Answered => ZFlagsContain(kMessageFlag.Answered);

        /// <summary>
        /// Adds <see cref="kMessageFlag.Answered"/> to the message's flags.
        /// </summary>
        /// <remarks>
        /// This method will throw if it detects that the underlying <see cref="Store(eStoreOperation, cStorableFlags, ulong?)"/> is likely to have failed.
        /// </remarks>
        public void SetAnswered() { ZFlagSet(cStorableFlags.Answered, true); }

        /// <summary>
        /// Gets and sets the <see cref="kMessageFlag.Flagged"/> flag of the message.
        /// </summary>
        /// <remarks>
        /// When getting the value, if the message cache does not contain the <see cref="fMessageCacheAttributes.flags"/> of the message, they will be fetched from the server.
        /// When setting the value, an exception will be raised if the underlying <see cref="Store(eStoreOperation, cStorableFlags, ulong?)"/> is suspected of failing.
        /// </remarks>
        public bool Flagged
        {
            get => ZFlagsContain(kMessageFlag.Flagged);
            set => ZFlagSet(cStorableFlags.Flagged, value);
        }

        /// <summary>
        /// Gets and sets the <see cref="kMessageFlag.Deleted"/> flag of the message.
        /// </summary>
        /// <inheritdoc cref="Flagged" select="remarks"/>
        public bool Deleted
        {
            get => ZFlagsContain(kMessageFlag.Deleted);
            set => ZFlagSet(cStorableFlags.Deleted, value);
        }

        /// <summary>
        /// Gets and sets the <see cref="kMessageFlag.Seen"/> flag of the message.
        /// </summary>
        /// <inheritdoc cref="Flagged" select="remarks"/>
        public bool Seen
        {
            get => ZFlagsContain(kMessageFlag.Seen);
            set => ZFlagSet(cStorableFlags.Seen, value);
        }

        /// <summary>
        /// Gets and sets the <see cref="kMessageFlag.Draft"/> flag of the message.
        /// </summary>
        /// <inheritdoc cref="Flagged" select="remarks"/>
        public bool Draft
        {
            get => ZFlagsContain(kMessageFlag.Draft);
            set => ZFlagSet(cStorableFlags.Draft, value);
        }

        /// <summary>
        /// Indicates whether <see cref="Flags"/> contains <see cref="kMessageFlag.Recent"/>.
        /// </summary>
        /// <remarks>
        /// See RFC 3501 for a definition of recent.
        /// If the message cache does not contain the <see cref="fMessageCacheAttributes.flags"/> of the message, they will be fetched from the server.
        /// </remarks>
        public bool Recent => ZFlagsContain(kMessageFlag.Recent);

        /// <summary>
        /// Indicates whether <see cref="Flags"/> contains <see cref="kMessageFlag.Forwarded"/>.
        /// </summary>
        /// <inheritdoc cref="Flags" select="remarks"/>
        public bool Forwarded => ZFlagsContain(kMessageFlag.Forwarded);

        /// <summary>
        /// Adds the <see cref="kMessageFlag.Forwarded"/> flag to the message's flags.
        /// </summary>
        /// <inheritdoc cref="SetAnswered" select="remarks"/>
        public void SetForwarded() { ZFlagSet(cStorableFlags.Forwarded, true); }

        /// <summary>
        /// Indicates whether <see cref="Flags"/> contains <see cref="kMessageFlag.SubmitPending"/>.
        /// </summary>
        /// <inheritdoc cref="Flags" select="remarks"/>
        public bool SubmitPending => ZFlagsContain(kMessageFlag.SubmitPending);

        /// <summary>
        /// Adds the <see cref="kMessageFlag.SubmitPending"/> flag to the message's flags.
        /// </summary>
        /// <inheritdoc cref="SetAnswered" select="remarks"/>
        public void SetSubmitPending() { ZFlagSet(cStorableFlags.SubmitPending, true); }

        /// <summary>
        /// Indicates whether <see cref="Flags"/> contains <see cref="kMessageFlag.Submitted"/>.
        /// </summary>
        /// <inheritdoc cref="Flags" select="remarks"/>
        public bool Submitted => ZFlagsContain(kMessageFlag.Submitted);

        private bool ZFlagsContain(string pFlag)
        {
            ZFetch(kModSeqFlags, true);
            return MessageHandle.ModSeqFlags.Flags.Contains(pFlag);
        }

        private void ZFlagSet(cStorableFlags pFlags, bool pValue)
        {
            var lContext = Client.RootContext.NewMethod(nameof(cIMAPMessage), nameof(ZFlagSet), pFlags, pValue);

            cStoreFeedback lFeedback;
            if (pValue) lFeedback = new cStoreFeedback(MessageHandle, eStoreOperation.add, pFlags);
            else lFeedback = new cStoreFeedback(MessageHandle, eStoreOperation.remove, pFlags);

            Client.Wait(Client.StoreAsync(lFeedback, null, lContext), lContext);

            ZStoreProcessFeedback(lFeedback);
        }

        /// <inheritdoc select="summary"/>
        /// <remarks>
        /// If the message cache does not contain the <see cref="fMessageCacheAttributes.envelope"/> of the message, it will be fetched from the server.
        /// </remarks>
        public override cEnvelope Envelope
        {
            get
            {
                ZFetch(kEnvelope, true);
                return MessageHandle.Envelope;
            }
        }

        /// <summary>
        /// Gets the IMAP INTERNALDATE of the message.
        /// </summary>
        /// <remarks>
        /// If the message cache does not contain the <see cref="fMessageCacheAttributes.received"/> date of the message, it will be fetched from the server.
        /// </remarks>
        public cTimestamp Received
        {
            get
            {
                ZFetch(kReceived, true);
                return MessageHandle.Received;
            }
        }

        /// <summary>
        /// Gets the size of the entire message in bytes.
        /// </summary>
        /// <remarks>
        /// If the message cache does not contain the <see cref="fMessageCacheAttributes.size"/> of the message, it will be fetched from the server.
        /// </remarks>
        public override uint Size
        {
            get
            {
                ZFetch(cMessageCacheItems.Size, true);
                return MessageHandle.Size.Value;
            }
        }

        /// <summary>
        /// Gets the MIME body structure of the message.
        /// </summary>
        /// <remarks>
        /// If the message cache does not contain the <see cref="fMessageCacheAttributes.bodystructure"/> of the message, it will be fetched from the server.
        /// </remarks>
        public override cBodyPart BodyStructure
        {
            get
            {
                ZFetch(kBodyStructure, true);
                return MessageHandle.BodyStructure;
            }
        }

        public override fMessageDataFormat Format
        {
            get
            {
                ZFetch(kBodyStructure, true);
                return MessageHandle.BodyStructure.Format | (Client.SupportedFormats & fMessageDataFormat.utf8headers);
            }
        }

        public override IEnumerable<cMailAttachment> GetAttachments()
        {
            ZFetch(kBodyStructure, true);
            return from lPart in YGetAttachmentParts(MessageHandle.BodyStructure) select new cIMAPAttachment(Client, MessageHandle, lPart);
        }

        /// <summary>
        /// Gets a list of message attachments. The list may be empty.
        /// </summary>
        /// <remarks>
        /// If the message cache does not contain the <see cref="fMessageCacheAttributes.bodystructure"/> of the message, it will be fetched from the server.
        /// The library defines an attachment as a single-part message body-part with a disposition of ‘attachment’.
        /// If there are alternate versions of an attachment only one of the alternates is included in the list (the first one).
        /// </remarks>
        public IEnumerable<cIMAPAttachment> GetIMAPAttachments()
        {
            ZFetch(kBodyStructure, true);
            return from lPart in YGetAttachmentParts(MessageHandle.BodyStructure) select new cIMAPAttachment(Client, MessageHandle, lPart);
        }

        /// <summary>
        /// Gets the normalised message-ids from the references header field. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// If the message cache does not contain the <see cref="kHeaderFieldName.References"/> header field of the message, it will be fetched from the server.
        /// Normalised message-ids have the delimiters, quoting, comments and white space removed.
        /// Will be <see langword="null"/> if there is no references header field or if the references header field can not be parsed.
        /// </remarks>
        public override cStrings References
        {
            get
            {
                ZFetch(kReferences, true);
                return MessageHandle.HeaderFields.References;
            }
        }

        /// <summary>
        /// Gets the importance value from the importance header field. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// If the message cache does not contain the <see cref="kHeaderFieldName.Importance"/> header field of the message, it will be fetched from the server.
        /// Will be <see langword="null"/> if there is no importance header field or if the importance header field can not be parsed.
        /// </remarks>
        public override eImportance? Importance
        {
            get
            {
                ZFetch(kImportance, true);
                return MessageHandle.HeaderFields.Importance;
            }
        }

        public bool IsInvalid => MessageHandle.MessageCache.IsInvalid;

        /// <summary>
        /// Ensures that the message cache contains the specified items for the message.
        /// </summary>
        /// <param name="pItems"></param>
        /// <returns>
        /// <see langword="true"/> if the fetch populated the cache with the requested items, <see langword="false"/> otherwise.
        /// <see langword="false"/> indicates that the message has been expunged.
        /// </returns>
        /// <remarks>
        /// The items that aren't currently cached will be fetched from the server.
        /// <note type="note"><see cref="cMessageCacheItems"/> has implicit conversions from other types including <see cref="fIMAPMessageProperties"/>. This means that you can use values of those types as arguments to this method.</note>
        /// </remarks>
        public bool Fetch(cMessageCacheItems pItems) => ZFetch(pItems, false);

        private bool ZFetch(cMessageCacheItems pItems, bool pThrowOnFailure)
        {
            if (MessageHandle.Contains(pItems)) return true;

            var lContext = Client.RootContext.NewMethod(nameof(cIMAPMessage), nameof(ZFetch), pItems, pThrowOnFailure);

            Client.Wait(Client.FetchCacheItemsAsync(cMessageHandleList.FromMessageHandle(MessageHandle), pItems, null, lContext), lContext);

            if (MessageHandle.Contains(pItems)) return true;

            if (pThrowOnFailure)
            {
                if (MessageHandle.Expunged) throw new cMessageExpungedException(MessageHandle);
                throw new cRequestedIMAPDataNotReturnedException(MessageHandle);
            }

            return false;
        }

        /// <summary>
        /// Ansynchronously ensures that the message cache contains the specified items for the message.
        /// </summary>
        /// <param name="pItems"></param>
        /// <inheritdoc cref="Fetch(cMessageCacheItems)" select="returns|remarks"/>
        public Task<bool> FetchAsync(cMessageCacheItems pItems) => ZFetchAsync(pItems, false);

        private async Task<bool> ZFetchAsync(cMessageCacheItems pItems, bool pThrowOnFailure)
        {
            if (MessageHandle.Contains(pItems)) return true;

            var lContext = Client.RootContext.NewMethod(nameof(cIMAPMessage), nameof(ZFetchAsync), pItems, pThrowOnFailure);

            await Client.FetchCacheItemsAsync(cMessageHandleList.FromMessageHandle(MessageHandle), pItems, null, lContext).ConfigureAwait(false);

            if (MessageHandle.Contains(pItems)) return true;

            if (pThrowOnFailure)
            {
                if (MessageHandle.Expunged) throw new cMessageExpungedException(MessageHandle);
                throw new cRequestedIMAPDataNotReturnedException(MessageHandle);
            }

            return false;
        }

        /// <summary>
        /// Returns a message sequence number offset for use in message filtering. See <see cref="cFilter.MSN"/>.
        /// </summary>
        /// <param name="pOffset"></param>
        /// <returns></returns>
        public cFilterMSNOffset GetMSNOffset(int pOffset) => new cFilterMSNOffset(MessageHandle, pOffset);

        public override async Task<string> GetPlainTextAsync()
        {
            await ZFetchAsync(kBodyStructure, true).ConfigureAwait(false);
            return await base.GetPlainTextAsync().ConfigureAwait(false);
        }

        public override string Fetch(cTextBodyPart pPart)
        {
            CheckPart(pPart);

            using (var lDataStream = new cIMAPMessageDataStream(Client, MessageHandle, pPart, true))
            using (var lMemoryStream = new MemoryStream())
            {
                lDataStream.CopyTo(lMemoryStream);
                Encoding lEncoding = Encoding.GetEncoding(pPart.Charset);
                return new string(lEncoding.GetChars(lMemoryStream.GetBuffer(), 0, (int)lMemoryStream.Length));
            }
        }

        public override async Task<string> FetchAsync(cTextBodyPart pPart)
        {
            CheckPart(pPart);

            using (var lDataStream = new cIMAPMessageDataStream(Client, MessageHandle, pPart, true))
            using (var lMemoryStream = new MemoryStream())
            {
                await lDataStream.CopyToAsync(lMemoryStream).ConfigureAwait(false);
                Encoding lEncoding = Encoding.GetEncoding(pPart.Charset);
                return new string(lEncoding.GetChars(lMemoryStream.GetBuffer(), 0, (int)lMemoryStream.Length));
            }
        }

        public override string Fetch(cSection pSection)
        {
            using (var lDataStream = new cIMAPMessageDataStream(Client, MessageHandle, pSection))
            using (var lMemoryStream = new MemoryStream())
            {
                lDataStream.CopyTo(lMemoryStream);
                return new string(Encoding.UTF8.GetChars(lMemoryStream.GetBuffer(), 0, (int)lMemoryStream.Length));
            }
        }

        public override async Task<string> FetchAsync(cSection pSection)
        {
            using (var lDataStream = new cIMAPMessageDataStream(Client, MessageHandle, pSection))
            using (var lMemoryStream = new MemoryStream())
            {
                await lDataStream.CopyToAsync(lMemoryStream).ConfigureAwait(false);
                return new string(Encoding.UTF8.GetChars(lMemoryStream.GetBuffer(), 0, (int)lMemoryStream.Length));
            }
        }

        public override Stream GetMessageDataStream() => new cIMAPMessageDataStream(Client, MessageHandle, cSection.All);

        public override Stream GetMessageDataStream(cSinglePartBody pPart, bool pDecodedIfRequired = true)
        {
            CheckPart(pPart);
            return new cIMAPMessageDataStream(Client, MessageHandle, pPart, pDecodedIfRequired);
        }

        public override Stream GetMessageDataStream(cSection pSection) => new cIMAPMessageDataStream(Client, MessageHandle, pSection);

        public cIMAPMessageDataStream GetIMAPMessageDataStream() => new cIMAPMessageDataStream(Client, MessageHandle, cSection.All);

        public cIMAPMessageDataStream GetIMAPMessageDataStream(cSinglePartBody pPart, bool pDecodedIfRequired = true)
        {
            CheckPart(pPart);
            return new cIMAPMessageDataStream(Client, MessageHandle, pPart, pDecodedIfRequired);
        }

        public cIMAPMessageDataStream GetIMAPMessageDataStream(cSection pSection) => new cIMAPMessageDataStream(Client, MessageHandle, pSection);

        /// <summary>
        /// Stores flags for the message. 
        /// </summary>
        /// <param name="pOperation">The type of store operation.</param>
        /// <param name="pFlags"></param>
        /// <param name="pIfUnchangedSinceModSeq"></param>
        /// <remarks>
        /// <paramref name="pIfUnchangedSinceModSeq"/> may only be specified if the containing mailbox's <see cref="cMailbox.HighestModSeq"/> is not zero. 
        /// (i.e. <see cref="cIMAPCapabilities.CondStore"/> is in use and the mailbox supports the persistent storage of mod-sequences.)
        /// If the message has been modified since the specified value then the server will fail the store.
        /// This method will throw if it detects that the store is likely to have failed.
        /// </remarks>
        public void Store(eStoreOperation pOperation, cStorableFlags pFlags, ulong? pIfUnchangedSinceModSeq = null)
        {
            var lContext = Client.RootContext.NewMethod(nameof(cIMAPMessage), nameof(Store), pOperation, pFlags, pIfUnchangedSinceModSeq);
            var lFeedback = new cStoreFeedback(MessageHandle, pOperation, pFlags);
            Client.Wait(Client.StoreAsync(lFeedback, pIfUnchangedSinceModSeq, lContext), lContext);
            ZStoreProcessFeedback(lFeedback);
        }

        /// <summary>
        /// Asynchronously stores flags for the message.
        /// </summary>
        /// <param name="pOperation">The type of store operation.</param>
        /// <param name="pFlags"></param>
        /// <param name="pIfUnchangedSinceModSeq"></param>
        /// <returns></returns>
        /// <inheritdoc cref="Store(eStoreOperation, cStorableFlags, ulong?)" select="remarks"/>
        public async Task StoreAsync(eStoreOperation pOperation, cStorableFlags pFlags, ulong? pIfUnchangedSinceModSeq = null)
        {
            var lContext = Client.RootContext.NewMethod(nameof(cIMAPMessage), nameof(StoreAsync), pOperation, pFlags, pIfUnchangedSinceModSeq);
            var lFeedback = new cStoreFeedback(MessageHandle, pOperation, pFlags);
            await Client.StoreAsync(lFeedback, pIfUnchangedSinceModSeq, lContext).ConfigureAwait(false);
            ZStoreProcessFeedback(lFeedback);
        }

        private void ZStoreProcessFeedback(cStoreFeedback pFeedback)
        {
            if (pFeedback.Summary().LikelyFailedCount != 0)
            {
                if (MessageHandle.Expunged) throw new cMessageExpungedException(MessageHandle);
                throw new cSingleMessageStoreException(MessageHandle);
            }
        }

        /// <summary>
        /// Copies the message to the specified mailbox.
        /// </summary>
        /// <param name="pDestination"></param>
        /// <returns>If the server provides an RFC 4315 UIDCOPY response, the UID of the message in the destination mailbox, otherwise <see langword="null"/>.</returns>
        public cUID Copy(cMailbox pDestination)
        {
            var lContext = Client.RootContext.NewMethod(nameof(cIMAPMessage), nameof(Copy), pDestination);
            if (pDestination == null) throw new ArgumentNullException(nameof(pDestination));
            if (!ReferenceEquals(pDestination.Client, Client)) throw new ArgumentOutOfRangeException(nameof(pDestination));
            var lTask = Client.CopyAsync(cMessageHandleList.FromMessageHandle(MessageHandle), pDestination.MailboxHandle, lContext);
            Client.Wait(lTask, lContext);
            var lFeedback = lTask.Result;
            if (lFeedback?.Count == 1) return lFeedback[0].CreatedMessageUID;
            return null;
        }

        /// <summary>
        /// Asynchronously copies the message to the specified mailbox.
        /// </summary>
        /// <param name="pDestination"></param>
        /// <inheritdoc cref="Copy(cMailbox)" select="returns|remarks"/>
        public async Task<cUID> CopyAsync(cMailbox pDestination)
        {
            var lContext = Client.RootContext.NewMethod(nameof(cIMAPMessage), nameof(CopyAsync), pDestination);
            if (pDestination == null) throw new ArgumentNullException(nameof(pDestination));
            if (!ReferenceEquals(pDestination.Client, Client)) throw new ArgumentOutOfRangeException(nameof(pDestination));
            var lFeedback = await Client.CopyAsync(cMessageHandleList.FromMessageHandle(MessageHandle), pDestination.MailboxHandle, lContext).ConfigureAwait(false);
            if (lFeedback?.Count == 1) return lFeedback[0].CreatedMessageUID;
            return null;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cIMAPMessage pObject) => this == pObject;

        /// <inheritdoc />
        public override bool Equals(object pObject) => this == pObject as cIMAPMessage;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetHashCode"/>
        public override int GetHashCode() => MessageHandle.GetHashCode();

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cIMAPMessage)}({MessageHandle})"; // ,{Indent} // re-instate if threading is ever done

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality(cAPIDocumentationTemplate, cAPIDocumentationTemplate)"/>
        public static bool operator ==(cIMAPMessage pA, cIMAPMessage pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return pA.MessageHandle.Equals(pB.MessageHandle);
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality(cAPIDocumentationTemplate, cAPIDocumentationTemplate)"/>
        public static bool operator !=(cIMAPMessage pA, cIMAPMessage pB) => !(pA == pB);
    }
}