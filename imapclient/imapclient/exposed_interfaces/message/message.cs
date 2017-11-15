using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using work.bacome.apidocumentation;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Provides an API that allows interaction with an IMAP message.
    /// </summary>
    /// <remarks>
    /// Instances are only valid whilst the containing mailbox remains selected. 
    /// Re-selecting a mailbox will not bring the message instances back to life.
    /// Instances are only valid whilst the containing mailbox has the same UIDValidity.
    /// </remarks>
    /// <seealso cref="cMailbox.Message(cUID, cMessageCacheItems)"/>
    /// <seealso cref="cMailbox.Messages(cFilter, cSort, cMessageCacheItems, cMessageFetchConfiguration)"/>
    /// <seealso cref="cMailbox.Messages(IEnumerable{cUID}, cMessageCacheItems, cCacheItemFetchConfiguration)"/>
    /// <seealso cref="cMailbox.Messages(IEnumerable{iMessageHandle}, cMessageCacheItems, cCacheItemFetchConfiguration)"/>
    public class cMessage
    {
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
        /**<summary>The internal message cache item that this instance is attached to.</summary>*/
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
        /// Fired when the server notifies the client of a property value change that affects the message associated with this instance.
        /// Most properties of an IMAP message can never change.
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
        /// Indicates whether the server has told us that the message has been expunged.
        /// </summary>
        public bool Expunged => Handle.Expunged;

        /// <summary>
        /// Gets the IMAP envelope data of the message.
        /// </summary>
        /// <remarks>
        /// If the internal message cache does not contain the <see cref="fMessageCacheAttributes.envelope"/> data of the message, it will be fetched from the server.
        /// </remarks>
        public cEnvelope Envelope
        {
            get
            {
                if (!Client.Fetch(Handle, kEnvelope)) throw new InvalidOperationException();
                return Handle.Envelope;
            }
        }

        /// <summary>
        /// Gets the sent date of the message from the <see cref="Envelope"/>.
        /// May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// If the internal message cache does not contain the <see cref="fMessageCacheAttributes.envelope"/> data of the message, it will be fetched from the server.
        /// </remarks>
        public DateTime? Sent
        {
            get
            {
                if (!Client.Fetch(Handle, kEnvelope)) throw new InvalidOperationException();
                return Handle.Envelope.Sent;
            }
        }

        /// <summary>
        /// Gets the subject of the message from the <see cref="Envelope"/>.
        /// May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// If the internal message cache does not contain the <see cref="fMessageCacheAttributes.envelope"/> data of the message, it will be fetched from the server.
        /// </remarks>
        public cCulturedString Subject
        {
            get
            {
                if (!Client.Fetch(Handle, kEnvelope)) throw new InvalidOperationException();
                return Handle.Envelope.Subject;
            }
        }

        /// <summary>
        /// Gets the base subject of the message from the <see cref="Envelope"/>.
        /// May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// The base subject is defined RFC 5256 and is the subject with the RE: FW: etc artifacts removed.
        /// If the internal message cache does not contain the <see cref="fMessageCacheAttributes.envelope"/> data of the message, it will be fetched from the server.
        /// </remarks>
        public string BaseSubject
        {
            get
            {
                if (!Client.Fetch(Handle, kEnvelope)) throw new InvalidOperationException();
                return Handle.Envelope.BaseSubject;
            }
        }

        /// <summary>
        /// Gets the 'from' addresses of the message from the <see cref="Envelope"/>.
        /// May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// If the internal message cache does not contain the <see cref="fMessageCacheAttributes.envelope"/> data of the message, it will be fetched from the server.
        /// </remarks>
        public cAddresses From
        {
            get
            {
                if (!Client.Fetch(Handle, kEnvelope)) throw new InvalidOperationException();
                return Handle.Envelope.From;
            }
        }

        /// <summary>
        /// Gets the 'sender' addresses of the message from the <see cref="Envelope"/>.
        /// May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// If the internal message cache does not contain the <see cref="fMessageCacheAttributes.envelope"/> data of the message, it will be fetched from the server.
        /// </remarks>
        public cAddresses Sender
        {
            get
            {
                if (!Client.Fetch(Handle, kEnvelope)) throw new InvalidOperationException();
                return Handle.Envelope.Sender;
            }
        }

        /// <summary>
        /// Gets the 'reply-to' addresses of the message from the <see cref="Envelope"/>.
        /// May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// If the internal message cache does not contain the <see cref="fMessageCacheAttributes.envelope"/> data of the message, it will be fetched from the server.
        /// </remarks>
        public cAddresses ReplyTo
        {
            get
            {
                if (!Client.Fetch(Handle, kEnvelope)) throw new InvalidOperationException();
                return Handle.Envelope.ReplyTo;
            }
        }

        /// <summary>
        /// Gets the 'to' addresses of the message from the <see cref="Envelope"/>.
        /// May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// If the internal message cache does not contain the <see cref="fMessageCacheAttributes.envelope"/> data of the message, it will be fetched from the server.
        /// </remarks>
        public cAddresses To
        {
            get
            {
                if (!Client.Fetch(Handle, kEnvelope)) throw new InvalidOperationException();
                return Handle.Envelope.To;
            }
        }

        /// <summary>
        /// Gets the 'CC' addresses of the message from the <see cref="Envelope"/>.
        /// May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// If the internal message cache does not contain the <see cref="fMessageCacheAttributes.envelope"/> data of the message, it will be fetched from the server.
        /// </remarks>
        public cAddresses CC
        {
            get
            {
                if (!Client.Fetch(Handle, kEnvelope)) throw new InvalidOperationException();
                return Handle.Envelope.CC;
            }
        }

        /// <summary>
        /// Gets the 'BCC' addresses of the message from the <see cref="Envelope"/>.
        /// May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// If the internal message cache does not contain the <see cref="fMessageCacheAttributes.envelope"/> data of the message, it will be fetched from the server.
        /// </remarks>
        public cAddresses BCC
        {
            get
            {
                if (!Client.Fetch(Handle, kEnvelope)) throw new InvalidOperationException();
                return Handle.Envelope.BCC;
            }
        }

        /// <summary>
        /// Gets the normalised 'in-reply-to' message-ids of the message from the <see cref="Envelope"/>.
        /// May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// Normalised message-ids have the delimiters, quoting, comments and white space removed.
        /// If the internal message cache does not contain the <see cref="fMessageCacheAttributes.envelope"/> data of the message, it will be fetched from the server.
        /// </remarks>
        public cStrings InReplyTo
        {
            get
            {
                if (!Client.Fetch(Handle, kEnvelope)) throw new InvalidOperationException();
                return Handle.Envelope.InReplyTo?.MsgIds;
            }
        }

        /// <summary>
        /// Gets the normalised message-id of the message from the <see cref="Envelope"/>.
        /// May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// Normalised message-ids have the delimiters, quoting, comments and white space removed.
        /// If the internal message cache does not contain the <see cref="fMessageCacheAttributes.envelope"/> data of the message, it will be fetched from the server.
        /// </remarks>
        public string MessageId
        {
            get
            {
                if (!Client.Fetch(Handle, kEnvelope)) throw new InvalidOperationException();
                return Handle.Envelope.MessageId?.MsgId;
            }
        }

        /// <summary>
        /// Gets the flags set for the message.
        /// </summary>
        /// <remarks>
        /// If the internal message cache does not contain the <see cref="fMessageCacheAttributes.flags"/> of the message, they will be fetched from the server.
        /// </remarks>
        public cFetchableFlags Flags
        {
            get
            {
                if (!Client.Fetch(Handle, kFlags)) throw new InvalidOperationException();
                return Handle.Flags;
            }
        }

        /// <summary>
        /// Determines if <see cref="Flags"/> contains <see cref="kMessageFlagName.Answered"/>.
        /// </summary>
        /// <remarks>
        /// If the internal message cache does not contain the <see cref="fMessageCacheAttributes.flags"/> of the message, they will be fetched from the server.
        /// </remarks>
        public bool Answered => ZFlagsContain(kMessageFlagName.Answered);
        /**<summary>Adds <see cref="kMessageFlagName.Answered"/> to the message's flags.</summary>*/
        public void SetAnswered() { ZFlagSet(cStorableFlags.Answered, true); }

        /// <summary>
        /// Gets and sets the <see cref="kMessageFlagName.Flagged"/> flag of the message.
        /// </summary>
        /// <remarks>
        /// When getting the value, if the internal message cache does not contain the <see cref="fMessageCacheAttributes.flags"/> of the message, they will be fetched from the server.
        /// </remarks>
        public bool Flagged
        {
            get => ZFlagsContain(kMessageFlagName.Flagged);
            set => ZFlagSet(cStorableFlags.Flagged, value);
        }

        /// <summary>
        /// Gets and sets the <see cref="kMessageFlagName.Deleted"/> flag of the message.
        /// </summary>
        /// <remarks>
        /// When getting the value, if the internal message cache does not contain the <see cref="fMessageCacheAttributes.flags"/> of the message, they will be fetched from the server.
        /// </remarks>
        public bool Deleted
        {
            get => ZFlagsContain(kMessageFlagName.Deleted);
            set => ZFlagSet(cStorableFlags.Deleted, value);
        }

        /// <summary>
        /// Gets and sets the <see cref="kMessageFlagName.Seen"/> flag of the message.
        /// </summary>
        /// <remarks>
        /// When getting the value, if the internal message cache does not contain the <see cref="fMessageCacheAttributes.flags"/> of the message, they will be fetched from the server.
        /// </remarks>
        public bool Seen
        {
            get => ZFlagsContain(kMessageFlagName.Seen);
            set => ZFlagSet(cStorableFlags.Seen, value);
        }

        /// <summary>
        /// Gets and sets the <see cref="kMessageFlagName.Draft"/> flag of the message.
        /// </summary>
        /// <remarks>
        /// When getting the value, if the internal message cache does not contain the <see cref="fMessageCacheAttributes.flags"/> of the message, they will be fetched from the server.
        /// </remarks>
        public bool Draft
        {
            get => ZFlagsContain(kMessageFlagName.Draft);
            set => ZFlagSet(cStorableFlags.Draft, value);
        }

        /// <summary>
        /// Determines if <see cref="Flags"/> contains <see cref="kMessageFlagName.Recent"/>.
        /// </summary>
        /// <remarks>
        /// If the internal message cache does not contain the <see cref="fMessageCacheAttributes.flags"/> of the message, they will be fetched from the server.
        /// </remarks>
        public bool Recent => ZFlagsContain(kMessageFlagName.Recent);

        // see comments elsewhere to see why these are commented out
        //public bool MDNSent => ZFlagsContain(kMessageFlagName.MDNSent);
        //public void SetMDNSent() { ZFlagSet(cSettableFlags.MDNSent, true); }

        /// <summary>
        /// Determines if <see cref="Flags"/> contains <see cref="kMessageFlagName.Forwarded"/>.
        /// </summary>
        /// <remarks>
        /// If the internal message cache does not contain the <see cref="fMessageCacheAttributes.flags"/> of the message, they will be fetched from the server.
        /// </remarks>
        public bool Forwarded => ZFlagsContain(kMessageFlagName.Forwarded);
        /**<summary>Adds the <see cref="kMessageFlagName.Forwarded"/> flag to the message's flags.</summary>*/
        public void SetForwarded() { ZFlagSet(cStorableFlags.Forwarded, true); }

        /// <summary>
        /// Determines if <see cref="Flags"/> contains <see cref="kMessageFlagName.SubmitPending"/>.
        /// </summary>
        /// <remarks>
        /// If the internal message cache does not contain the <see cref="fMessageCacheAttributes.flags"/> of the message, they will be fetched from the server.
        /// </remarks>
        public bool SubmitPending => ZFlagsContain(kMessageFlagName.SubmitPending);
        /**<summary>Adds the <see cref="kMessageFlagName.SubmitPending"/> flag to the message's flags.</summary>*/
        public void SetSubmitPending() { ZFlagSet(cStorableFlags.SubmitPending, true); }

        /// <summary>
        /// Determines if <see cref="Flags"/> contains <see cref="kMessageFlagName.Submitted"/>.
        /// </summary>
        /// <remarks>
        /// If the internal message cache does not contain the <see cref="fMessageCacheAttributes.flags"/> of the message, they will be fetched from the server.
        /// </remarks>
        public bool Submitted  => ZFlagsContain(kMessageFlagName.Submitted);

        private bool ZFlagsContain(string pFlag)
        {
            if (!Client.Fetch(Handle, kFlags)) throw new InvalidOperationException();
            return Handle.Flags.Contains(pFlag);
        }

        private void ZFlagSet(cStorableFlags pFlags, bool pValue)
        {
            cStoreFeedback lFeedback;
            if (pValue) lFeedback = Client.Store(Handle, eStoreOperation.add, pFlags, null);
            else lFeedback = Client.Store(Handle, eStoreOperation.remove, pFlags, null);
            if (lFeedback.Summary().LikelyFailedCount != 0) throw new InvalidOperationException(); // the assumption here is that the message has been deleted
        }

        /// <summary>
        /// Gets the IMAP INTERNALDATE of the message.
        /// </summary>
        /// <remarks>
        /// If the internal message cache does not contain the <see cref="fMessageCacheAttributes.received"/> date of the message, it will be fetched from the server.
        /// </remarks>
        public DateTime Received
        {
            get
            {
                if (!Client.Fetch(Handle, kReceived)) throw new InvalidOperationException();
                return Handle.Received.Value;
            }
        }

        /// <summary>
        /// Gets the size of the entire message in bytes.
        /// </summary>
        /// <remarks>
        /// If the internal message cache does not contain the <see cref="fMessageCacheAttributes.size"/> of the message, it will be fetched from the server.
        /// </remarks>
        public int Size
        {
            get
            {
                if (!Client.Fetch(Handle, kSize)) throw new InvalidOperationException();
                return (int)Handle.Size.Value;
            }
        }

        /// <summary>
        /// Gets the IMAP UID of the message. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// If the internal message cache does not contain the <see cref="fMessageCacheAttributes.uid"/> of the message, it will be fetched from the server.
        /// May be <see langword="null"/> if the mailbox does not support unique identifiers.
        /// </remarks>
        public cUID UID
        {
            get
            {
                if (!Client.Fetch(Handle, kUID)) throw new InvalidOperationException();
                return Handle.UID;
            }
        }

        /// <summary>
        /// Gets the modification sequence number of the message. May be zero.
        /// </summary>
        /// <remarks>
        /// If the internal message cache does not contain the <see cref="fMessageCacheAttributes.modseq"/> for the message, it will be fetched from the server.
        /// Will be zero if <see cref="cCapabilities.CondStore"/> is not in use or if the mailbox does not support the persistent storage of mod-sequences.
        /// </remarks>
        public ulong ModSeq
        {
            get
            {
                if (!Client.Fetch(Handle, kModSeq)) throw new InvalidOperationException();
                return Handle.ModSeq.Value;
            }
        }

        /// <summary>
        /// Gets the IMAP BODYSTRUCTURE of the message.
        /// </summary>
        /// <remarks>
        /// If the internal message cache does not contain the <see cref="fMessageCacheAttributes.bodystructure"/> of the message, it will be fetched from the server.
        /// </remarks>
        public cBodyPart BodyStructure
        {
            get
            {
                if (!Client.Fetch(Handle, kBodyStructure)) throw new InvalidOperationException();
                return Handle.BodyStructure;
            }
        }

        /// <summary>
        /// Gets the list of message attachments. The list may be empty.
        /// </summary>
        /// <remarks>
        /// If the internal message cache does not contain the <see cref="fMessageCacheAttributes.bodystructure"/> of the message, it will be fetched from the server.
        /// The library defines an attachment as a message part with a disposition of ‘attachment’.
        /// If there are alternate versions of an attachment only one of the alternates is included in the list (the first one).
        /// </remarks>
        public List<cAttachment> Attachments
        {
            get
            {
                if (!Client.Fetch(Handle, kBodyStructure)) throw new InvalidOperationException();
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
        /// Gets the size in bytes of the plain text parts of the message. May be zero.
        /// </summary>
        /// <remarks>
        /// If the internal message cache does not contain the <see cref="fMessageCacheAttributes.bodystructure"/> of the message, it will be fetched from the server.
        /// The library defines plain text parts as parts with a MIME type of text/plain and without a disposition of 'attachment'.
        /// If there are alternate versions of a part only one of the alternates is used in calculating the size (the first one).
        /// </remarks>
        public int PlainTextSizeInBytes
        {
            get
            {
                if (!Client.Fetch(Handle, kBodyStructure)) throw new InvalidOperationException();
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
        /// If the internal message cache does not contain the <see cref="kHeaderFieldName.References"/> header field of the message, it will be fetched from the server.
        /// Normalised message-ids have the delimiters, quoting, comments and white space removed.
        /// May be <see langword="null"/> if there is no references header field or if the references header field can not be parsed by the library.
        /// </remarks>
        public cStrings References
        {
            get
            {
                if (!Client.Fetch(Handle, kReferences)) throw new InvalidOperationException();
                return Handle.HeaderFields.References;
            }
        }

        /// <summary>
        /// Gets the importance value from the importance header field. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// If the internal message cache does not contain the <see cref="kHeaderFieldName.Importance"/> header field of the message, it will be fetched from the server.
        /// May be <see langword="null"/> if there is no importance header field or if the importance header field can not be parsed by the library.
        /// </remarks>
        public eImportance? Importance
        {
            get
            {
                if (!Client.Fetch(Handle, kImportance)) throw new InvalidOperationException();
                return Handle.HeaderFields.Importance;
            }
        }

        /// <summary>
        /// Ensures that the internal message cache contains the specified items for this message instance.
        /// </summary>
        /// <param name="pItems"></param>
        /// <returns>
        /// <see langword="true"/> if the fetch populated the cache with the requested items, <see langword="false"/> otherwise.
        /// <see langword="false"/> indicates that the message is expunged.
        /// </returns>
        /// <remarks>
        /// The missing items will be fetched from the server.
        /// Note that <see cref="cMessageCacheItems"/> has implicit conversions from other types including <see cref="fMessageProperties"/> (so you can use values of those types as parameters to this method).
        /// </remarks>
        public bool Fetch(cMessageCacheItems pItems) => Client.Fetch(Handle, pItems);

        /// <summary>
        /// Ansynchronously ensures that the internal message cache contains the specified items for this message instance.
        /// </summary>
        /// <param name="pItems"></param>
        /// <inheritdoc cref="Fetch(cMessageCacheItems)" select="returns|remarks"/>
        public Task<bool> FetchAsync(cMessageCacheItems pItems) => Client.FetchAsync(Handle, pItems);

        /// <summary>
        /// Gets the fetch size in bytes of a <see cref="cSinglePartBody"/> part of this message.
        /// </summary>
        /// <param name="pPart"></param>
        /// <returns></returns>
        /// <remarks>
        /// This may be smaller than the <see cref="cSinglePartBody.SizeInBytes"/> if the part needs decoding (see <see cref="cSinglePartBody.DecodingRequired"/>) and <see cref="cCapabilities.Binary"/> is in use.
        /// The size may have to be fetched from the server. 
        /// Once fetched the size will be cached in the internal message cache.
        /// </remarks>
        public int FetchSizeInBytes(cSinglePartBody pPart) => Client.FetchSizeInBytes(Handle, pPart);

        /// <summary>
        /// Asynchronously gets the fetch size in bytes of a <see cref="cSinglePartBody"/> part of this message.
        /// </summary>
        /// <param name="pPart"></param>
        /// <inheritdoc cref="Fetch(cMessageCacheItems)" select="returns|remarks"/>
        public Task<int> FetchSizeInBytesAsync(cSinglePartBody pPart) => Client.FetchSizeInBytesAsync(Handle, pPart);

        /// <summary>
        /// Returns a message sequence number offset for use in message filtering. See <see cref="cFilter.MSN"/>.
        /// </summary>
        /// <param name="pOffset">The offset from this message's sequence number.</param>
        /// <returns></returns>
        public cFilterMSNOffset MSNOffset(int pOffset) => new cFilterMSNOffset(Handle, pOffset);

        /// <summary>
        /// Fetches the message's plain text parts from the server, decoding if required, and concatenates them yielding the returned value.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// If the internal message cache does not contain the <see cref="fMessageCacheAttributes.bodystructure"/> of the message, it will be fetched from the server.
        /// The library defines plain text parts as parts with a MIME type of text/plain and without a disposition of 'attachment'.
        /// If there are alternate versions of a part only one of the alternates is used in generating the plain text (the first one).
        /// </remarks>
        public string PlainText()
        {
            if (!Client.Fetch(Handle, kBodyStructure)) throw new InvalidOperationException();
            StringBuilder lBuilder = new StringBuilder();
            foreach (var lPart in ZPlainTextParts(Handle.BodyStructure)) lBuilder.Append(Fetch(lPart));
            return lBuilder.ToString();
        }

        /// <summary>
        /// Ansynchronously fetches the message's plain text parts from the server, decoding if required, and concatenates them yielding the returned value.
        /// </summary>
        /// <inheritdoc cref="PlainText" select="returns|remarks"/>
        public async Task<string> PlainTextAsync()
        {
            if (!await Client.FetchAsync(Handle, kBodyStructure).ConfigureAwait(false)) throw new InvalidOperationException();

            List<Task<string>> lTasks = new List<Task<string>>();
            foreach (var lPart in ZPlainTextParts(Handle.BodyStructure)) lTasks.Add(FetchAsync(lPart));
            await Task.WhenAll(lTasks).ConfigureAwait(false);

            StringBuilder lBuilder = new StringBuilder();
            foreach (var lTask in lTasks) lBuilder.Append(lTask.Result);
            return lBuilder.ToString();
        }

        /// <summary>
        /// Fetches the specified message part from the server, decoding if required, and returns the data in a string.
        /// </summary>
        /// <param name="pPart"></param>
        /// <returns></returns>
        public string Fetch(cTextBodyPart pPart)
        {
            using (var lStream = new MemoryStream())
            {
                Client.Fetch(Handle, pPart.Section, pPart.DecodingRequired, lStream, null);
                Encoding lEncoding = Encoding.GetEncoding(pPart.Charset);
                return new string(lEncoding.GetChars(lStream.GetBuffer(), 0, (int)lStream.Length));
            }
        }

        /// <summary>
        /// Asynchronously fetches the specified message part from the server, decoding if required, and returns the data in a string.
        /// </summary>
        /// <param name="pPart"></param>
        /// <inheritdoc cref="Fetch(cTextBodyPart)" select="returns|remarks"/>
        public async Task<string> FetchAsync(cTextBodyPart pPart)
        {
            using (var lStream = new MemoryStream())
            {
                await Client.FetchAsync(Handle, pPart.Section, pPart.DecodingRequired, lStream, null).ConfigureAwait(false);
                Encoding lEncoding = Encoding.GetEncoding(pPart.Charset);
                return new string(lEncoding.GetChars(lStream.GetBuffer(), 0, (int)lStream.Length));
            }
        }

        /// <summary>
        /// Fetches the specified message section from the server as text (without any decoding) and attempts to return the data as a string.
        /// </summary>
        /// <param name="pSection"></param>
        /// <returns></returns>
        public string Fetch(cSection pSection)
        {
            using (var lStream = new MemoryStream())
            {
                Client.Fetch(Handle, pSection, eDecodingRequired.none, lStream, null);
                return new string(Encoding.UTF8.GetChars(lStream.GetBuffer(), 0, (int)lStream.Length));
            }
        }

        /// <summary>
        /// Asynchronously fetches the specified message section from the server as text (without any decoding) and attempts to return the data as a string.
        /// </summary>
        /// <param name="pSection"></param>
        /// <inheritdoc cref="FetchAsync(cSection)" select="returns|remarks"/>
        public async Task<string> FetchAsync(cSection pSection)
        {
            using (var lStream = new MemoryStream())
            {
                await Client.FetchAsync(Handle, pSection, eDecodingRequired.none, lStream, null).ConfigureAwait(false);
                return new string(Encoding.UTF8.GetChars(lStream.GetBuffer(), 0, (int)lStream.Length));
            }
        }

        /// <summary>
        /// Fetches the specified message part from the server (decoding if required), and writes the part data into the provided stream.
        /// </summary>
        /// <param name="pPart"></param>
        /// <param name="pStream"></param>
        /// <param name="pConfiguration">An operation specific timeout, cancellation token, progress callback and write size controller.</param>
        /// <remarks>
        /// Any decoding required may be done client-side or server-side.
        /// To calculate the number of bytes that have to be fetched, use the <see cref="FetchSizeInBytes(cSinglePartBody)"/> method. 
        /// (This is useful if you are intending to display a progress bar.)
        /// </remarks>
        public void Fetch(cSinglePartBody pPart, Stream pStream, cBodyFetchConfiguration pConfiguration = null) => Client.Fetch(Handle, pPart.Section, pPart.DecodingRequired, pStream, pConfiguration);

        /// <summary>
        /// Asynchronously fetches the specified message part from the server (decoding if required), and writes the part data into the provided stream.
        /// </summary>
        /// <param name="pPart"></param>
        /// <param name="pStream"></param>
        /// <param name="pConfiguration">An operation specific timeout, cancellation token, progress callback and write size controller.</param>
        /// <returns></returns>
        /// <inheritdoc cref="Fetch(cSinglePartBody, Stream, cBodyFetchConfiguration)" select="remarks"/>
        public Task FetchAsync(cSinglePartBody pPart, Stream pStream, cBodyFetchConfiguration pConfiguration = null) => Client.FetchAsync(Handle, pPart.Section, pPart.DecodingRequired, pStream, pConfiguration);

        /// <summary>
        /// Fetches the specified message section from the server and writes the section data into the provided stream.
        /// </summary>
        /// <param name="pSection"></param>
        /// <param name="pDecoding"></param>
        /// <param name="pStream"></param>
        /// <param name="pConfiguration">An operation specific timeout, cancellation token, progress callback and write size controller.</param>
        /// <remarks>
        /// If <see cref="cCapabilities.Binary"/> is in use and the entire part (<see cref="eSectionTextPart.all"/>) is being fetched then unless <paramref name="pDecoding"/> is <see cref="eDecodingRequired.none"/> the server will do the decoding that it determines is required
        /// (i.e. the decoding specified is ignored).
        /// </remarks>
        public void Fetch(cSection pSection, eDecodingRequired pDecoding, Stream pStream, cBodyFetchConfiguration pConfiguration = null) => Client.Fetch(Handle, pSection, pDecoding, pStream, pConfiguration);

        /// <summary>
        /// Asynchronously fetches the specified message section from the server and writes the section data into the provided stream.
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
            if (lFeedback.Summary().LikelyFailedCount != 0) throw new InvalidOperationException(); // the assumption here is that the message has been deleted
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
            if (lFeedback.Summary().LikelyFailedCount != 0) throw new InvalidOperationException(); // the assumption here is that the message has been deleted
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

        /// <inheritdoc cref="cAPIDocumentationTemplate.ToString"/>
        public override string ToString() => $"{nameof(cMessage)}({Handle})"; // ,{Indent} // re-instate if threading is ever done
    }
}