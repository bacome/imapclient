using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents an IMAP message.
    /// </summary>
    /// <remarks>
    /// Instances of this class are only valid whilst the <see cref="cMailbox"/> remains selected. 
    /// Re-selecting the mailbox will not bring message instances back to life.
    /// Instances of this class are only valid whilst the containing mailbox has the same UIDValidity.
    /// </remarks>
    /// <seealso cref="cMailbox.Messages(cFilter, cSort, cMessageCacheItems, cMessageFetchConfiguration)"/>
    /// <seealso cref="cMailbox.Messages(IEnumerable{iMessageHandle}, cMessageCacheItems, cCacheItemFetchConfiguration)"/>
    /// <seealso cref="cMailbox.Message(cUID, cMessageCacheItems)"/>
    /// <seealso cref="cMailbox.Messages(IEnumerable{cUID}, cMessageCacheItems, cCacheItemFetchConfiguration)"/>
    /// <seealso cref="cSort"/>
    public class cMessage
    {
        private enum eOperationType { fetch, store }

        private static readonly cMessageCacheItems kEnvelope = fMessageCacheAttributes.envelope;
        private static readonly cMessageCacheItems kFlags = fMessageCacheAttributes.flags;
        private static readonly cMessageCacheItems kReceived = fMessageCacheAttributes.received;
        private static readonly cMessageCacheItems kSize = fMessageCacheAttributes.size;
        private static readonly cMessageCacheItems kUID = fMessageCacheAttributes.uid;
        private static readonly cMessageCacheItems kModSeq = fMessageCacheAttributes.modseq;
        private static readonly cMessageCacheItems kBodyStructure = fMessageCacheAttributes.bodystructure;
        private static readonly cMessageCacheItems kReferences = cHeaderFieldNames.References;
        private static readonly cMessageCacheItems kImportance = cHeaderFieldNames.Importance;

        private PropertyChangedEventHandler mPropertyChanged;
        private object mPropertyChangedLock = new object();

        /**<summary>The client that this instance was created by.</summary>*/
        public readonly cIMAPClient Client;
        /**<summary>The message that this instance represents.</summary>*/
        public readonly iMessageHandle Handle;

        // re-instate if threading is ever done
        //public readonly int Indent; // Indicates the indent of the message. This only means something when compared to the indents of surrounding items in a threaded list of messages. It is a bit of a hack having it in this class.

        internal cMessage(cIMAPClient pClient, iMessageHandle pHandle) // , int pIndent = -1 // re-instate if threading is ever done
        {
            Client = pClient ?? throw new ArgumentNullException(nameof(pClient));
            Handle = pHandle ?? throw new ArgumentNullException(nameof(pHandle));
            //Indent = pIndent; // re-instate if threading is ever done
        }

        /// <summary>
        /// Fired when the server notifies the client of a change that affects a property value of this instance.
        /// </summary>
        /// <remarks>
        /// If <see cref="cIMAPClient.SynchronizationContext"/> is not <see langword="null"/>, events are invoked on the specified <see cref="System.Threading.SynchronizationContext"/>.
        /// If an exception is raised in an event handler the <see cref="cIMAPClient.CallbackException"/> event is raised, but otherwise the exception is ignored.
        /// </remarks>
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
            if (ReferenceEquals(pArgs.Handle, Handle)) mPropertyChanged?.Invoke(this, pArgs);
        }

        /// <summary>
        /// Indicates whether the message exists on the server.
        /// </summary>
        public bool Expunged => Handle.Expunged;

        private void ZThrowFailure(eOperationType pType)
        {
            if (Handle.Expunged) throw new cMessageExpungedException(Handle);

            switch (pType)
            {
                case eOperationType.fetch:

                    throw new cUnexpectedServerActionException("fetch data not returned");

                case eOperationType.store:

                    throw new cSingleMessageStoreException();

                default:

                    throw new cInternalErrorException();
            }
        }

        /// <summary>
        /// Gets the IMAP envelope data of the message.
        /// </summary>
        /// <remarks>
        /// If the message cache does not contain the <see cref="fMessageCacheAttributes.envelope"/> of the message, it will be fetched from the server.
        /// </remarks>
        public cEnvelope Envelope
        {
            get
            {
                if (!Client.Fetch(Handle, kEnvelope)) ZThrowFailure(eOperationType.fetch);
                return Handle.Envelope;
            }
        }

        /// <summary>
        /// Gets the sent date of the message from the <see cref="Envelope"/>. May be <see langword="null"/>.
        /// </summary>
        /// <inheritdoc cref="Envelope" select="remarks"/>
        public DateTime? Sent
        {
            get
            {
                if (!Client.Fetch(Handle, kEnvelope)) ZThrowFailure(eOperationType.fetch);
                return Handle.Envelope.Sent;
            }
        }

        /// <summary>
        /// Gets the subject of the message from the <see cref="Envelope"/>. May be <see langword="null"/>.
        /// </summary>
        /// <inheritdoc cref="Envelope" select="remarks"/>
        public cCulturedString Subject
        {
            get
            {
                if (!Client.Fetch(Handle, kEnvelope)) ZThrowFailure(eOperationType.fetch);
                return Handle.Envelope.Subject;
            }
        }

        /// <summary>
        /// Gets the base subject of the message from the <see cref="Envelope"/>. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// The base subject is defined RFC 5256 and is the subject with the RE: FW: etc artifacts removed.
        /// If the message cache does not contain the <see cref="fMessageCacheAttributes.envelope"/> of the message, it will be fetched from the server.
        /// </remarks>
        public string BaseSubject
        {
            get
            {
                if (!Client.Fetch(Handle, kEnvelope)) ZThrowFailure(eOperationType.fetch);
                return Handle.Envelope.BaseSubject;
            }
        }

        /// <summary>
        /// Gets the 'from' addresses of the message from the <see cref="Envelope"/>. May be <see langword="null"/>.
        /// </summary>
        /// <inheritdoc cref="Envelope" select="remarks"/>
        public cAddresses From
        {
            get
            {
                if (!Client.Fetch(Handle, kEnvelope)) ZThrowFailure(eOperationType.fetch);
                return Handle.Envelope.From;
            }
        }

        /// <summary>
        /// Gets the 'sender' addresses of the message from the <see cref="Envelope"/>. May be <see langword="null"/>.
        /// </summary>
        /// <inheritdoc cref="Envelope" select="remarks"/>
        public cAddresses Sender
        {
            get
            {
                if (!Client.Fetch(Handle, kEnvelope)) ZThrowFailure(eOperationType.fetch);
                return Handle.Envelope.Sender;
            }
        }

        /// <summary>
        /// Gets the 'reply-to' addresses of the message from the <see cref="Envelope"/>. May be <see langword="null"/>.
        /// </summary>
        /// <inheritdoc cref="Envelope" select="remarks"/>
        public cAddresses ReplyTo
        {
            get
            {
                if (!Client.Fetch(Handle, kEnvelope)) ZThrowFailure(eOperationType.fetch);
                return Handle.Envelope.ReplyTo;
            }
        }

        /// <summary>
        /// Gets the 'to' addresses of the message from the <see cref="Envelope"/>. May be <see langword="null"/>.
        /// </summary>
        /// <inheritdoc cref="Envelope" select="remarks"/>
        public cAddresses To
        {
            get
            {
                if (!Client.Fetch(Handle, kEnvelope)) ZThrowFailure(eOperationType.fetch);
                return Handle.Envelope.To;
            }
        }

        /// <summary>
        /// Gets the 'CC' addresses of the message from the <see cref="Envelope"/>. May be <see langword="null"/>.
        /// </summary>
        /// <inheritdoc cref="Envelope" select="remarks"/>
        public cAddresses CC
        {
            get
            {
                if (!Client.Fetch(Handle, kEnvelope)) ZThrowFailure(eOperationType.fetch);
                return Handle.Envelope.CC;
            }
        }

        /// <summary>
        /// Gets the 'BCC' addresses of the message from the <see cref="Envelope"/>. May be <see langword="null"/>.
        /// </summary>
        /// <inheritdoc cref="Envelope" select="remarks"/>
        public cAddresses BCC
        {
            get
            {
                if (!Client.Fetch(Handle, kEnvelope)) ZThrowFailure(eOperationType.fetch);
                return Handle.Envelope.BCC;
            }
        }

        /// <summary>
        /// Gets the normalised 'in-reply-to' message-ids of the message from the <see cref="Envelope"/>. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// Normalised message-ids have the delimiters, quoting, comments and white space removed.
        /// If the message cache does not contain the <see cref="fMessageCacheAttributes.envelope"/> of the message, it will be fetched from the server.
        /// </remarks>
        public cStrings InReplyTo
        {
            get
            {
                if (!Client.Fetch(Handle, kEnvelope)) ZThrowFailure(eOperationType.fetch);
                return Handle.Envelope.InReplyTo?.MsgIds;
            }
        }

        /// <summary>
        /// Gets the normalised message-id of the message from the <see cref="Envelope"/>. May be <see langword="null"/>.
        /// </summary>
        /// <inheritdoc cref="InReplyTo" select="remarks"/>
        public string MessageId
        {
            get
            {
                if (!Client.Fetch(Handle, kEnvelope)) ZThrowFailure(eOperationType.fetch);
                return Handle.Envelope.MessageId?.MsgId;
            }
        }

        /// <summary>
        /// Gets the flags set for the message.
        /// </summary>
        /// <remarks>
        /// If the message cache does not contain the <see cref="fMessageCacheAttributes.flags"/> of the message, they will be fetched from the server.
        /// </remarks>
        public cFetchableFlags Flags
        {
            get
            {
                if (!Client.Fetch(Handle, kFlags)) ZThrowFailure(eOperationType.fetch);
                return Handle.Flags;
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

        // see comments elsewhere to see why these are commented out
        //public bool MDNSent => ZFlagsContain(kMessageFlagName.MDNSent);
        //public void SetMDNSent() { ZFlagSet(cSettableFlags.MDNSent, true); }

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
        public bool Submitted  => ZFlagsContain(kMessageFlag.Submitted);

        private bool ZFlagsContain(string pFlag)
        {
            if (!Client.Fetch(Handle, kFlags)) ZThrowFailure(eOperationType.fetch);
            return Handle.Flags.Contains(pFlag);
        }

        private void ZFlagSet(cStorableFlags pFlags, bool pValue)
        {
            cStoreFeedback lFeedback;
            if (pValue) lFeedback = Client.Store(Handle, eStoreOperation.add, pFlags, null);
            else lFeedback = Client.Store(Handle, eStoreOperation.remove, pFlags, null);
            if (lFeedback.Summary().LikelyFailedCount != 0) ZThrowFailure(eOperationType.store); 
        }

        /// <summary>
        /// Gets the IMAP INTERNALDATE of the message.
        /// </summary>
        /// <remarks>
        /// If the message cache does not contain the <see cref="fMessageCacheAttributes.received"/> date of the message, it will be fetched from the server.
        /// </remarks>
        public DateTime Received
        {
            get
            {
                if (!Client.Fetch(Handle, kReceived)) ZThrowFailure(eOperationType.fetch);
                return Handle.Received.Value;
            }
        }

        /// <summary>
        /// Gets the size of the entire message in bytes.
        /// </summary>
        /// <remarks>
        /// If the message cache does not contain the <see cref="fMessageCacheAttributes.size"/> of the message, it will be fetched from the server.
        /// </remarks>
        public int Size
        {
            get
            {
                if (!Client.Fetch(Handle, kSize)) ZThrowFailure(eOperationType.fetch);
                return (int)Handle.Size.Value;
            }
        }

        /// <summary>
        /// Gets the UID of the message. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// If the message cache does not contain the <see cref="fMessageCacheAttributes.uid"/> of the message, it will be fetched from the server.
        /// Will be <see langword="null"/> if the mailbox does not support unique identifiers.
        /// </remarks>
        public cUID UID
        {
            get
            {
                if (!Client.Fetch(Handle, kUID)) ZThrowFailure(eOperationType.fetch);
                return Handle.UID;
            }
        }

        /// <summary>
        /// Gets the modification sequence number of the message. May be zero.
        /// </summary>
        /// <remarks>
        /// If the message cache does not contain the <see cref="fMessageCacheAttributes.modseq"/> of the message, it will be fetched from the server.
        /// Will be zero if <see cref="cCapabilities.CondStore"/> is not in use or if the mailbox does not support the persistent storage of mod-sequences.
        /// </remarks>
        public ulong ModSeq
        {
            get
            {
                if (!Client.Fetch(Handle, kModSeq)) ZThrowFailure(eOperationType.fetch);
                return Handle.ModSeq.Value;
            }
        }

        /// <summary>
        /// Gets the IMAP BODYSTRUCTURE of the message.
        /// </summary>
        /// <remarks>
        /// If the message cache does not contain the <see cref="fMessageCacheAttributes.bodystructure"/> of the message, it will be fetched from the server.
        /// </remarks>
        public cBodyPart BodyStructure
        {
            get
            {
                if (!Client.Fetch(Handle, kBodyStructure)) ZThrowFailure(eOperationType.fetch);
                return Handle.BodyStructure;
            }
        }

        /// <summary>
        /// Gets a list of message attachments. The list may be empty.
        /// </summary>
        /// <remarks>
        /// If the message cache does not contain the <see cref="fMessageCacheAttributes.bodystructure"/> of the message, it will be fetched from the server.
        /// The library defines an attachment as a message body-part with a disposition of ‘attachment’.
        /// If there are alternate versions of an attachment only one of the alternates is included in the list (the first one).
        /// </remarks>
        public List<cAttachment> Attachments
        {
            get
            {
                if (!Client.Fetch(Handle, kBodyStructure)) ZThrowFailure(eOperationType.fetch);
                return ZAttachmentParts(Handle.BodyStructure);
            }
        }

        private List<cAttachment> ZAttachmentParts(cBodyPart pPart)
        {
            // TODO: when we know what languages the user is interested in (on implementation of languages) choose from multipart/alternative options based on language tag

            List<cAttachment> lResult = new List<cAttachment>();

            if (pPart is cSinglePartBody lSinglePart)
            {
                if (lSinglePart.Disposition?.TypeCode == eDispositionTypeCode.attachment) lResult.Add(new cAttachment(Client, Handle, lSinglePart));
            }
            else if (pPart.Disposition?.TypeCode != eDispositionTypeCode.attachment && pPart is cMultiPartBody lMultiPart)
            {
                foreach (var lPart in lMultiPart.Parts)
                {
                    var lAttachments = ZAttachmentParts(lPart);
                    lResult.AddRange(lAttachments);
                    if (lAttachments.Count > 0 && lMultiPart.SubTypeCode == eMultiPartBodySubTypeCode.alternative) break;
                }
            }

            return lResult;
        }
        /// <summary>
        /// Gets the size in bytes of the plain text of the message. May be zero.
        /// </summary>
        /// <remarks>
        /// If the message cache does not contain the <see cref="fMessageCacheAttributes.bodystructure"/> of the message, it will be fetched from the server.
        /// The library defines plain text as being contained in message body-parts with a MIME type of text/plain and without a disposition of 'attachment'.
        /// If there are alternate versions of a body-part only one of the alternates is considered to be part of the plain text (the first one).
        /// The size returned is the encoded size of the body-parts.
        /// </remarks>
        public int PlainTextSizeInBytes
        {
            get
            {
                if (!Client.Fetch(Handle, kBodyStructure)) ZThrowFailure(eOperationType.fetch);
                int lSize = 0;
                foreach (var lPart in ZPlainTextParts(Handle.BodyStructure)) lSize += (int)lPart.SizeInBytes;
                return lSize;
            }
        }

        private List<cTextBodyPart> ZPlainTextParts(cBodyPart pPart)
        {
            // TODO: when we know what languages the user is interested in (on implementation of languages) choose from multipart/alternative options based on language tag

            List<cTextBodyPart> lResult = new List<cTextBodyPart>();

            if (pPart.Disposition?.TypeCode == eDispositionTypeCode.attachment) return lResult;

            if (pPart is cTextBodyPart lTextPart)
            {
                if (lTextPart.SubTypeCode == eTextBodyPartSubTypeCode.plain) lResult.Add(lTextPart);
            }
            else if (pPart is cMultiPartBody lMultiPart)
            {
                foreach (var lPart in lMultiPart.Parts)
                {
                    var lParts = ZPlainTextParts(lPart);
                    lResult.AddRange(lParts);
                    if (lParts.Count > 0 && lMultiPart.SubTypeCode == eMultiPartBodySubTypeCode.alternative) break;
                }
            }

            return lResult;
        }


        /// <summary>
        /// Gets the normalised message-ids from the references header field. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// If the message cache does not contain the <see cref="kHeaderFieldName.References"/> header field of the message, it will be fetched from the server.
        /// Normalised message-ids have the delimiters, quoting, comments and white space removed.
        /// Will be <see langword="null"/> if there is no references header field or if the references header field can not be parsed.
        /// </remarks>
        public cStrings References
        {
            get
            {
                if (!Client.Fetch(Handle, kReferences)) ZThrowFailure(eOperationType.fetch);
                return Handle.HeaderFields.References;
            }
        }

        /// <summary>
        /// Gets the importance value from the importance header field. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// If the message cache does not contain the <see cref="kHeaderFieldName.Importance"/> header field of the message, it will be fetched from the server.
        /// Will be <see langword="null"/> if there is no importance header field or if the importance header field can not be parsed.
        /// </remarks>
        public eImportance? Importance
        {
            get
            {
                if (!Client.Fetch(Handle, kImportance)) ZThrowFailure(eOperationType.fetch);
                return Handle.HeaderFields.Importance;
            }
        }

        /// <summary>
        /// Ensures that the message cache contains the specified items for this message.
        /// </summary>
        /// <param name="pItems"></param>
        /// <returns>
        /// <see langword="true"/> if the fetch populated the cache with the requested items, <see langword="false"/> otherwise.
        /// <see langword="false"/> indicates that the message has been expunged.
        /// </returns>
        /// <remarks>
        /// The items that aren't currently cached will be fetched from the server.
        /// <note type="note"><see cref="cMessageCacheItems"/> has implicit conversions from other types including <see cref="fMessageProperties"/>. This means that you can use values of those types as parameters to this method.</note>
        /// </remarks>
        public bool Fetch(cMessageCacheItems pItems) => Client.Fetch(Handle, pItems);

        /// <summary>
        /// Ansynchronously ensures that the message cache contains the specified items for this message.
        /// </summary>
        /// <param name="pItems"></param>
        /// <inheritdoc cref="Fetch(cMessageCacheItems)" select="returns|remarks"/>
        public Task<bool> FetchAsync(cMessageCacheItems pItems) => Client.FetchAsync(Handle, pItems);

        private bool ZContainsPart(cBodyPart pPart, cSinglePartBody pSinglePart)
        {
            if (ReferenceEquals(pPart, pSinglePart)) return true;
            if (pPart is cMultiPartBody lMultiPart) foreach (var lPart in lMultiPart.Parts) if (ZContainsPart(lPart, pSinglePart)) return true;
            return false;
        }

        /// <summary>
        /// Gets the fetch size of the specified <see cref="cSinglePartBody"/>.
        /// </summary>
        /// <param name="pPart"></param>
        /// <returns></returns>
        /// <remarks>
        /// Will throw if <paramref name="pPart"/> is not in <see cref="BodyStructure"/>.
        /// The result may be smaller than <see cref="cSinglePartBody.SizeInBytes"/> if <see cref="cSinglePartBody.DecodingRequired"/> isn't <see cref="eDecodingRequired.none"/> and <see cref="cCapabilities.Binary"/> is in use.
        /// The size may have to be fetched from the server, but once fetched it will be cached.
        /// </remarks>
        public int FetchSizeInBytes(cSinglePartBody pPart)
        {
            if (Handle.BodyStructure == null) throw new InvalidOperationException(kInvalidOperationExceptionMessage.BodyStructureHasNotBeenFetched);
            if (!ZContainsPart(Handle.BodyStructure, pPart)) throw new ArgumentOutOfRangeException(nameof(pPart));
            return Client.FetchSizeInBytes(Handle, pPart);
        }

        /// <summary>
        /// Asynchronously gets the fetch size of the specified <see cref="cSinglePartBody"/>.
        /// </summary>
        /// <param name="pPart"></param>
        /// <inheritdoc cref="Fetch(cMessageCacheItems)" select="returns|remarks"/>
        public Task<int> FetchSizeInBytesAsync(cSinglePartBody pPart)
        {
            if (Handle.BodyStructure == null) throw new InvalidOperationException(kInvalidOperationExceptionMessage.BodyStructureHasNotBeenFetched);
            if (!ZContainsPart(Handle.BodyStructure, pPart)) throw new ArgumentOutOfRangeException(nameof(pPart));
            return Client.FetchSizeInBytesAsync(Handle, pPart);
        }

        /// <summary>
        /// Returns a message sequence number offset for use in message filtering. See <see cref="cFilter.MSN"/>.
        /// </summary>
        /// <param name="pOffset">The offset from this message's sequence number.</param>
        /// <returns></returns>
        public cFilterMSNOffset MSNOffset(int pOffset) => new cFilterMSNOffset(Handle, pOffset);

        /// <summary>
        /// Returns the plain text of the message.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// If the message cache does not contain the <see cref="fMessageCacheAttributes.bodystructure"/> of the message, it will be fetched from the server.
        /// The library defines plain text as being contained in message body-parts with a MIME type of text/plain and without a disposition of 'attachment'.
        /// If there are alternate versions of a body-part only one of the alternates is considered to be part of the plain text (the first one).
        /// The required body-parts are fetched from the server and concatented to yield the result.
        /// The text returned is the decoded text.
        /// </remarks>
        public string PlainText()
        {
            if (!Client.Fetch(Handle, kBodyStructure)) ZThrowFailure(eOperationType.fetch);
            StringBuilder lBuilder = new StringBuilder();
            foreach (var lPart in ZPlainTextParts(Handle.BodyStructure)) lBuilder.Append(Fetch(lPart));
            return lBuilder.ToString();
        }

        /// <summary>
        /// Ansynchronously returns the plain text of the message.
        /// </summary>
        /// <inheritdoc cref="PlainText" select="returns|remarks"/>
        public async Task<string> PlainTextAsync()
        {
            if (!await Client.FetchAsync(Handle, kBodyStructure).ConfigureAwait(false)) ZThrowFailure(eOperationType.fetch);

            List<Task<string>> lTasks = new List<Task<string>>();
            foreach (var lPart in ZPlainTextParts(Handle.BodyStructure)) lTasks.Add(FetchAsync(lPart));
            await Task.WhenAll(lTasks).ConfigureAwait(false);

            StringBuilder lBuilder = new StringBuilder();
            foreach (var lTask in lTasks) lBuilder.Append(lTask.Result);
            return lBuilder.ToString();
        }

        /// <summary>
        /// Returns the content of the specified <see cref="cTextBodyPart"/>.
        /// </summary>
        /// <param name="pPart"></param>
        /// <returns></returns>
        /// <remarks>
        /// Will throw if <paramref name="pPart"/> is not in <see cref="BodyStructure"/>.
        /// The content is decoded if required.
        /// </remarks>
        public string Fetch(cTextBodyPart pPart)
        {
            if (Handle.BodyStructure == null) throw new InvalidOperationException(kInvalidOperationExceptionMessage.BodyStructureHasNotBeenFetched);
            if (!ZContainsPart(Handle.BodyStructure, pPart)) throw new ArgumentOutOfRangeException(nameof(pPart));

            using (var lStream = new MemoryStream())
            {
                Client.Fetch(Handle, pPart.Section, pPart.DecodingRequired, lStream, null);
                Encoding lEncoding = Encoding.GetEncoding(pPart.Charset);
                return new string(lEncoding.GetChars(lStream.GetBuffer(), 0, (int)lStream.Length));
            }
        }

        /// <summary>
        /// Asynchronously returns the content of the specified <see cref="cTextBodyPart"/>.
        /// </summary>
        /// <param name="pPart"></param>
        /// <inheritdoc cref="Fetch(cTextBodyPart)" select="returns|remarks"/>
        public async Task<string> FetchAsync(cTextBodyPart pPart)
        {
            if (Handle.BodyStructure == null) throw new InvalidOperationException(kInvalidOperationExceptionMessage.BodyStructureHasNotBeenFetched);
            if (!ZContainsPart(Handle.BodyStructure, pPart)) throw new ArgumentOutOfRangeException(nameof(pPart));

            using (var lStream = new MemoryStream())
            {
                await Client.FetchAsync(Handle, pPart.Section, pPart.DecodingRequired, lStream, null).ConfigureAwait(false);
                Encoding lEncoding = Encoding.GetEncoding(pPart.Charset);
                return new string(lEncoding.GetChars(lStream.GetBuffer(), 0, (int)lStream.Length));
            }
        }

        /// <summary>
        /// Returns the content of the specified <see cref="cSection"/>.
        /// </summary>
        /// <param name="pSection"></param>
        /// <returns></returns>
        /// <remarks>
        /// The result is not decoded.
        /// </remarks>
        public string Fetch(cSection pSection)
        {
            using (var lStream = new MemoryStream())
            {
                Client.Fetch(Handle, pSection, eDecodingRequired.none, lStream, null);
                return new string(Encoding.UTF8.GetChars(lStream.GetBuffer(), 0, (int)lStream.Length));
            }
        }

        /// <summary>
        /// Asynchronously returns the content of the specified <see cref="cSection"/>.
        /// </summary>
        /// <param name="pSection"></param>
        /// <inheritdoc cref="Fetch(cSection)" select="returns|remarks"/>
        public async Task<string> FetchAsync(cSection pSection)
        {
            using (var lStream = new MemoryStream())
            {
                await Client.FetchAsync(Handle, pSection, eDecodingRequired.none, lStream, null).ConfigureAwait(false);
                return new string(Encoding.UTF8.GetChars(lStream.GetBuffer(), 0, (int)lStream.Length));
            }
        }

        /// <summary>
        /// Fetches the content of the specified <see cref="cSinglePartBody"/> into the specified <see cref="Stream"/>.
        /// </summary>
        /// <param name="pPart"></param>
        /// <param name="pStream"></param>
        /// <param name="pConfiguration">An operation specific timeout, cancellation token, progress callback and write size controller.</param>
        /// <remarks>
        /// Will throw if <paramref name="pPart"/> is not in <see cref="BodyStructure"/>.
        /// The content is decoded if required.
        /// To calculate the number of bytes that have to be fetched, use <see cref="FetchSizeInBytes(cSinglePartBody)"/>. 
        /// (This may be useful if you are intending to display a progress bar.)
        /// </remarks>
        public void Fetch(cSinglePartBody pPart, Stream pStream, cBodyFetchConfiguration pConfiguration = null)
        {
            if (Handle.BodyStructure == null) throw new InvalidOperationException(kInvalidOperationExceptionMessage.BodyStructureHasNotBeenFetched);
            if (!ZContainsPart(Handle.BodyStructure, pPart)) throw new ArgumentOutOfRangeException(nameof(pPart));
            Client.Fetch(Handle, pPart.Section, pPart.DecodingRequired, pStream, pConfiguration);
        }

        /// <summary>
        /// Asynchronously fetches the content of the specified <see cref="cSinglePartBody"/> into the specified <see cref="Stream"/>.
        /// </summary>
        /// <param name="pPart"></param>
        /// <param name="pStream"></param>
        /// <param name="pConfiguration">An operation specific timeout, cancellation token, progress callback and write size controller.</param>
        /// <returns></returns>
        /// <inheritdoc cref="Fetch(cSinglePartBody, Stream, cBodyFetchConfiguration)" select="remarks"/>
        public Task FetchAsync(cSinglePartBody pPart, Stream pStream, cBodyFetchConfiguration pConfiguration = null)
        {
            if (Handle.BodyStructure == null) throw new InvalidOperationException(kInvalidOperationExceptionMessage.BodyStructureHasNotBeenFetched);
            if (!ZContainsPart(Handle.BodyStructure, pPart)) throw new ArgumentOutOfRangeException(nameof(pPart));
            return Client.FetchAsync(Handle, pPart.Section, pPart.DecodingRequired, pStream, pConfiguration);
        }

        /// <summary>
        /// Fetches the content of the specified <see cref="cSection"/> into the specified <see cref="Stream"/>.
        /// </summary>
        /// <param name="pSection"></param>
        /// <param name="pDecoding">The decoding that should be applied.</param>
        /// <param name="pStream"></param>
        /// <param name="pConfiguration">An operation specific timeout, cancellation token, progress callback and write size controller.</param>
        /// <remarks>
        /// If <see cref="cCapabilities.Binary"/> is in use and the entire body-part (<see cref="cSection.TextPart"/> is <see cref="eSectionTextPart.all"/>) is being fetched then
        /// unless <paramref name="pDecoding"/> is <see cref="eDecodingRequired.none"/> the server will do the decoding that it determines is required (i.e. the decoding specified is ignored).
        /// </remarks>
        public void Fetch(cSection pSection, eDecodingRequired pDecoding, Stream pStream, cBodyFetchConfiguration pConfiguration = null) => Client.Fetch(Handle, pSection, pDecoding, pStream, pConfiguration);

        /// <summary>
        /// Asynchronously fetches the content of the specified <see cref="cSection"/> into the specified <see cref="Stream"/>.
        /// </summary>
        /// <param name="pSection"></param>
        /// <param name="pDecoding"></param>
        /// <param name="pStream"></param>
        /// <param name="pConfiguration">An operation specific timeout, cancellation token, progress callback and write size controller.</param>
        /// <returns></returns>
        /// <inheritdoc cref="Fetch(cSection, eDecodingRequired, Stream, cBodyFetchConfiguration)" select="remarks"/>
        public Task FetchAsync(cSection pSection, eDecodingRequired pDecoding, Stream pStream, cBodyFetchConfiguration pConfiguration = null) => Client.FetchAsync(Handle, pSection, pDecoding, pStream, pConfiguration);

        /// <summary>
        /// Stores flags for the message. 
        /// </summary>
        /// <param name="pOperation">The type of store operation.</param>
        /// <param name="pFlags"></param>
        /// <param name="pIfUnchangedSinceModSeq"></param>
        /// <remarks>
        /// The <paramref name="pIfUnchangedSinceModSeq"/> can only be specified if the containing mailbox's <see cref="cMailbox.HighestModSeq"/> is not zero. 
        /// (i.e. <see cref="cCapabilities.CondStore"/> is in use and the mailbox supports the persistent storage of mod-sequences.)
        /// If the message has been modified since the specified value then the server will fail the store.
        /// This method will throw if it detects that the store is likely to have failed.
        /// </remarks>
        public void Store(eStoreOperation pOperation, cStorableFlags pFlags, ulong? pIfUnchangedSinceModSeq = null)
        {
            var lFeedback = Client.Store(Handle, pOperation, pFlags, pIfUnchangedSinceModSeq);
            if (lFeedback.Summary().LikelyFailedCount != 0) ZThrowFailure(eOperationType.store);
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
            var lFeedback = await Client.StoreAsync(Handle, pOperation, pFlags, pIfUnchangedSinceModSeq);
            if (lFeedback.Summary().LikelyFailedCount != 0) ZThrowFailure(eOperationType.store); 
        }

        /// <summary>
        /// Copies the message to the specified mailbox.
        /// </summary>
        /// <param name="pDestination"></param>
        /// <returns>If the server provides an RFC 4315 UIDCOPY response, the UID of the message in the destination mailbox, otherwise <see langword="null"/>.</returns>
        public cUID Copy(cMailbox pDestination)
        {
            var lFeedback = Client.Copy(Handle, pDestination.Handle);
            if (lFeedback?.Count == 1) return lFeedback[0].CreatedUID;
            return null;
        }

        /// <summary>
        /// Asynchronously copies the message to the specified mailbox.
        /// </summary>
        /// <param name="pDestination"></param>
        /// <inheritdoc cref="Copy(cMailbox)" select="returns|remarks"/>
        public async Task<cUID> CopyAsync(cMailbox pDestination)
        {
            var lFeedback = await Client.CopyAsync(Handle, pDestination.Handle).ConfigureAwait(false);
            if (lFeedback?.Count == 1) return lFeedback[0].CreatedUID;
            return null;
        }

        /*
        // for sending via SMTP (i.e. a draft)

        public MailMessage ToMailMessage(fToMailMessageOptions pOptions)
        {
            ;?; // TODO
        } */

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cMessage)}({Handle})"; // ,{Indent} // re-instate if threading is ever done
    }
}