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
    /// Provides an API that allows interaction with an IMAP message.
    /// </summary>
    /// <remarks>
    /// Instances are only valid whilst the containing mailbox remains selected. 
    /// Re-selecting a mailbox will not bring the message instances back to life.
    /// Instances are only valid whilst the containing mailbox has the same UIDValidity.
    /// </remarks>
    public class cMessage
    {
        private static readonly cCacheItems kEnvelope = fCacheAttributes.envelope;
        private static readonly cCacheItems kFlags = fCacheAttributes.flags;
        private static readonly cCacheItems kReceived = fCacheAttributes.received;
        private static readonly cCacheItems kSize = fCacheAttributes.size;
        private static readonly cCacheItems kUID = fCacheAttributes.uid;
        private static readonly cCacheItems kModSeq = fCacheAttributes.modseq;
        private static readonly cCacheItems kBodyStructure = fCacheAttributes.bodystructure;
        private static readonly cCacheItems kReferences = cHeaderFieldNames.References;
        private static readonly cCacheItems kImportance = cHeaderFieldNames.Importance;

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
        /// Fired when the server notifies the client of a message property value change.
        /// Most properties of an IMAP message can never change.
        /// </summary>
        /// <remarks>
        /// <para>If <see cref="cIMAPClient.SynchronizationContext"/> is non-null, events are fired on the specified <see cref="System.Threading.SynchronizationContext"/>.</para>
        /// <para>If an exception is raised in an event handler the <see cref="cIMAPClient.CallbackException"/> event is raised, but otherwise the exception is ignored.</para>
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
        /// If the internal message cache does not contain the envelope data of the message, it will be fetched from the server.
        /// </summary>
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
        /// If the internal message cache does not contain the envelope data of the message, it will be fetched from the server.
        /// May be null.
        /// </summary>
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
        /// If the internal message cache does not contain the envelope data of the message, it will be fetched from the server.
        /// May be null.
        /// </summary>
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
        /// If the internal message cache does not contain the envelope data of the message, it will be fetched from the server.
        /// May be null.
        /// </summary>
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
        /// If the internal message cache does not contain the envelope data of the message, it will be fetched from the server.
        /// May be null.
        /// </summary>
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
        /// If the internal message cache does not contain the envelope data of the message, it will be fetched from the server.
        /// May be null.
        /// </summary>
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
        /// If the internal message cache does not contain the envelope data of the message, it will be fetched from the server.
        /// May be null.
        /// </summary>
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
        /// If the internal message cache does not contain the envelope data of the message, it will be fetched from the server.
        /// May be null.
        /// </summary>
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
        /// If the internal message cache does not contain the envelope data of the message, it will be fetched from the server.
        /// May be null.
        /// </summary>
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
        /// If the internal message cache does not contain the envelope data of the message, it will be fetched from the server.
        /// May be null.
        /// </summary>
        public cAddresses BCC
        {
            get
            {
                if (!Client.Fetch(Handle, kEnvelope)) throw new InvalidOperationException();
                return Handle.Envelope.BCC;
            }
        }

        /// <summary>
        /// Gets the normalised (delimiters, quoting, comments and white space removed) 'in-reply-to' message-ids of the message from the <see cref="Envelope"/>.
        /// If the internal message cache does not contain the envelope data of the message, it will be fetched from the server.
        /// May be null.
        /// </summary>
        public cStrings InReplyTo
        {
            get
            {
                if (!Client.Fetch(Handle, kEnvelope)) throw new InvalidOperationException();
                return Handle.Envelope.InReplyTo?.MsgIds;
            }
        }

        /// <summary>
        /// Gets the normalised (delimiters, quoting, comments and white space removed) message-id of the message from the <see cref="Envelope"/>.
        /// If the internal message cache does not contain the envelope data of the message, it will be fetched from the server.
        /// May be null.
        /// </summary>
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
        /// If the internal message cache does not contain the flags of the message, they will be fetched from the server.
        /// </summary>
        public cFetchableFlags Flags
        {
            get
            {
                if (!Client.Fetch(Handle, kFlags)) throw new InvalidOperationException();
                return Handle.Flags;
            }
        }

        /// <summary>
        /// Determines if the <see cref="Flags"/> contain the <see cref="kMessageFlagName.Answered"/> flag.
        /// If the internal message cache does not contain flags for the message, they will be fetched from the server.
        /// </summary>
        public bool Answered => ZFlagsContain(kMessageFlagName.Answered);
        /**<summary>Adds the <see cref="kMessageFlagName.Answered"/> flag to the message flags.</summary>*/
        public void SetAnswered() { ZFlagSet(cStorableFlags.Answered, true); }

        /// <summary>
        /// Gets and sets <see cref="kMessageFlagName.Flagged"/> flag of the message.
        /// When getting the value, if the internal message cache does not contain the flags of the message, they will be fetched from the server.
        /// </summary>
        public bool Flagged
        {
            get => ZFlagsContain(kMessageFlagName.Flagged);
            set => ZFlagSet(cStorableFlags.Flagged, value);
        }

        /// <summary>
        /// Gets and sets <see cref="kMessageFlagName.Deleted"/> flag of the message.
        /// When getting the value, if the internal message cache does not contain the flags of the message, they will be fetched from the server.
        /// </summary>
        public bool Deleted
        {
            get => ZFlagsContain(kMessageFlagName.Deleted);
            set => ZFlagSet(cStorableFlags.Deleted, value);
        }

        /// <summary>
        /// Gets and sets <see cref="kMessageFlagName.Seen"/> flag of the message.
        /// When getting the value, if the internal message cache does not contain the flags of the message, they will be fetched from the server.
        /// </summary>
        public bool Seen
        {
            get => ZFlagsContain(kMessageFlagName.Seen);
            set => ZFlagSet(cStorableFlags.Seen, value);
        }

        /// <summary>
        /// Gets and sets <see cref="kMessageFlagName.Draft"/> flag of the message.
        /// When getting the value, if the internal message cache does not contain the flags of the message, they will be fetched from the server.
        /// </summary>
        public bool Draft
        {
            get => ZFlagsContain(kMessageFlagName.Draft);
            set => ZFlagSet(cStorableFlags.Draft, value);
        }

        /// <summary>
        /// Determines if the <see cref="Flags"/> contain the <see cref="kMessageFlagName.Recent"/> flag.
        /// If the internal message cache does not contain flags for the message, they will be fetched from the server.
        /// </summary>
        public bool Recent => ZFlagsContain(kMessageFlagName.Recent);

        // see comments elsewhere to see why these are commented out
        //public bool MDNSent => ZFlagsContain(kMessageFlagName.MDNSent);
        //public void SetMDNSent() { ZFlagSet(cSettableFlags.MDNSent, true); }

        /// <summary>
        /// Determines if the <see cref="Flags"/> contain the <see cref="kMessageFlagName.Forwarded"/> flag.
        /// If the internal message cache does not contain flags for the message, they will be fetched from the server.
        /// </summary>
        public bool Forwarded => ZFlagsContain(kMessageFlagName.Forwarded);
        /**<summary>Adds the <see cref="kMessageFlagName.Forwarded"/> flag to the message flags.</summary>*/
        public void SetForwarded() { ZFlagSet(cStorableFlags.Forwarded, true); }

        /// <summary>
        /// Determines if the <see cref="Flags"/> contain the <see cref="kMessageFlagName.SubmitPending"/> flag.
        /// If the internal message cache does not contain flags for the message, they will be fetched from the server.
        /// </summary>
        public bool SubmitPending => ZFlagsContain(kMessageFlagName.SubmitPending);
        /**<summary>Adds the <see cref="kMessageFlagName.SubmitPending"/> flag to the message flags.</summary>*/
        public void SetSubmitPending() { ZFlagSet(cStorableFlags.SubmitPending, true); }

        /// <summary>
        /// Determines if the <see cref="Flags"/> contain the <see cref="kMessageFlagName.Submitted"/> flag.
        /// If the internal message cache does not contain flags for the message, they will be fetched from the server.
        /// </summary>
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
        /// If the internal message cache does not contain the internal date of the message, it will be fetched from the server.
        /// </summary>
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
        /// If the internal message cache does not contain the size of the message, it will be fetched from the server.
        /// </summary>
        public int Size
        {
            get
            {
                if (!Client.Fetch(Handle, kSize)) throw new InvalidOperationException();
                return (int)Handle.Size.Value;
            }
        }

        /// <summary>
        /// Gets the IMAP UID of the message.
        /// If the internal message cache does not contain the UID of the message, it will be fetched from the server.
        /// May be null if the server does not support unique identifiers.
        /// </summary>
        public cUID UID
        {
            get
            {
                if (!Client.Fetch(Handle, kUID)) throw new InvalidOperationException();
                return Handle.UID;
            }
        }

        /// <summary>
        /// Gets the modification sequence number of the message.
        /// If the internal message cache does not contain a modseq for the message, it will be fetched from the server.
        /// Will be 0 if the mailbox does not support CONDSTORE.
        /// </summary>
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
        /// If the internal message cache does not contain the body structure of the message, it will be fetched from the server.
        /// </summary>
        public cBodyPart BodyStructure
        {
            get
            {
                if (!Client.Fetch(Handle, kBodyStructure)) throw new InvalidOperationException();
                return Handle.BodyStructure;
            }
        }

        /// <summary>
        /// Gets the list of message attachments.
        /// If the internal message cache does not contain the body structure of the message, it will be fetched from the server.
        /// The returned list may be empty.
        /// </summary>
        /// <remarks>
        /// The library defines an attachment as a message part with a disposition of ‘attachment’.
        /// If there are alternate versions of an attachment only one of the alternates is included in the returned list (the first one).
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
        /// Gets the size in bytes of the plain text parts of the message.
        /// If the internal message cache does not contain the body structure of the message, it will be fetched from the server.
        /// May be zero.
        /// </summary>
        /// <remarks>
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
        /// Gets the normalised (delimiters, quoting, comments and white space removed) message-ids from the references header field.
        /// If the internal message cache does not contain the references header field of the message, it will be fetched from the server.
        /// May be null.
        /// </summary>
        /// <remarks>
        /// May be null if there is no references header field or if the references header field can not be parsed by the library.
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
        /// Gets the importance value from the importance header field.
        /// If the internal message cache does not contain the importance header field of the message, it will be fetched from the server.
        /// May be null.
        /// </summary>
        /// <remarks>
        /// May be null if there is no importance header field or if the importance header field can not be parsed by the library.
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
        /// The missing items will be fetched from the server.
        /// </summary>
        /// <param name="pItems">
        /// The items required in the cache.
        /// Note that <see cref="cCacheItems"/> has implicit conversions from other types including <see cref="fMessageProperties"/> (so you can use values of those types as parameters to this method).
        /// </param>
        /// <returns>
        /// True if the fetch populated the cache with the requested items, false otherwise.
        /// False indicates that the message is expunged.
        /// </returns>
        public bool Fetch(cCacheItems pItems) => Client.Fetch(Handle, pItems);

        /// <summary>
        /// Ansynchronously ensures that the internal message cache contains the specified items for this message instance.
        /// The missing items will be fetched from the server.
        /// </summary>
        /// <param name="pItems">
        /// The items required in the cache.
        /// Note that <see cref="cCacheItems"/> has implicit conversions from other types including <see cref="fMessageProperties"/> (so you can use values of those types as parameters to this method).
        /// </param>
        /// <returns>
        /// True if the fetch populated the cache with the requested items, false otherwise.
        /// False indicates that the message is expunged.
        /// </returns>
        public Task<bool> FetchAsync(cCacheItems pItems) => Client.FetchAsync(Handle, pItems);

        /// <summary>
        /// Gets the fetch size in bytes of a <see cref="cSinglePartBody"/> part of this message. This may be smaller than the <see cref="cSinglePartBody.SizeInBytes"/>.
        /// </summary>
        /// <param name="pPart"></param>
        /// <returns></returns>
        /// <remarks>
        /// This may be smaller than the <see cref="cSinglePartBody.SizeInBytes"/> if the part needs decoding (see <see cref="cSinglePartBody.DecodingRequired"/>) and the server supports <see cref="cCapabilities.Binary"/>.
        /// The size may have to be fetched from the server. 
        /// Once fetched the size will be cached in the internal message cache.
        /// </remarks>
        public int FetchSizeInBytes(cSinglePartBody pPart) => Client.FetchSizeInBytes(Handle, pPart);

        /// <summary>
        /// Asynchronously gets the fetch size in bytes of a <see cref="cSinglePartBody"/> part of this message. This may be smaller than the <see cref="cSinglePartBody.SizeInBytes"/>.
        /// </summary>
        /// <param name="pPart"></param>
        /// <returns></returns>
        /// <remarks>
        /// This may be smaller than the <see cref="cSinglePartBody.SizeInBytes"/> if the part needs decoding (see <see cref="cSinglePartBody.DecodingRequired"/>) and the server supports <see cref="cCapabilities.Binary"/>.
        /// The size may have to be fetched from the server. 
        /// Once fetched the size will be cached in the internal message cache.
        /// </remarks>
        public Task<int> FetchSizeInBytesAsync(cSinglePartBody pPart) => Client.FetchSizeInBytesAsync(Handle, pPart);

        /// <summary>
        /// Returns a message sequence number offset for use in message filtering. See <see cref="cFilter.MSN"/>.
        /// </summary>
        /// <param name="pOffset">The offset from this message's sequence number.</param>
        /// <returns></returns>
        public cFilterMSNOffset MSNOffset(int pOffset) => new cFilterMSNOffset(Handle, pOffset);

        /// <summary>
        /// Fetches the message's plain text parts from the server, decoding if required, and concatenates them yielding the returned value.
        /// If the internal message cache does not contain the bodystructure of the message, it will be fetched from the server.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
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
        /// If the internal message cache does not contain the bodystructure of the message, it will be fetched from the server.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// The library defines plain text parts as parts with a MIME type of text/plain and without a disposition of 'attachment'.
        /// If there are alternate versions of a part only one of the alternates is used in generating the plain text (the first one).
        /// </remarks>
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
        /// Fetches the specified part from the server, decoding if required, and returns the data in a string.
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
        /// Asynchronously fetches the specified part from the server, decoding if required, and returns the data in a string.
        /// </summary>
        /// <param name="pPart"></param>
        /// <returns></returns>
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
        /// <returns></returns>
        public async Task<string> FetchAsync(cSection pSection)
        {
            using (var lStream = new MemoryStream())
            {
                await Client.FetchAsync(Handle, pSection, eDecodingRequired.none, lStream, null).ConfigureAwait(false);
                return new string(Encoding.UTF8.GetChars(lStream.GetBuffer(), 0, (int)lStream.Length));
            }
        }

        /// <summary>
        /// Fetches the specified part from the server, decoding if required, and writes the part data into the provided stream.
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
        /// Asynchronously fetches the specified part from the server, decoding if required, and writes the part data into the provided stream.
        /// </summary>
        /// <param name="pPart"></param>
        /// <param name="pStream"></param>
        /// <param name="pConfiguration">An operation specific timeout, cancellation token, progress callback and write size controller.</param>
        /// <returns></returns>
        /// <remarks>
        /// Any decoding required may be done client-side or server-side.
        /// To calculate the number of bytes that have to be fetched, use the <see cref="FetchSizeInBytes(cSinglePartBody)"/> method. 
        /// (This is useful if you are intending to display a progress bar.)
        /// </remarks>
        public Task FetchAsync(cSinglePartBody pPart, Stream pStream, cBodyFetchConfiguration pConfiguration = null) => Client.FetchAsync(Handle, pPart.Section, pPart.DecodingRequired, pStream, pConfiguration);

        /// <summary>
        /// <para>Fetches the specified section from the server, applying the specified decoding, and writing the resulting bytes into the provided stream.</para>
        /// <para>Any decoding required may be done client-side or server-side (if the server supports RFC 3516).</para>
        /// <para>Optionally you may specify an operation specific timeout, cancellation token, progress callback and write size controller in the <paramref name="pConfiguration"/> parameter.</para>
        /// </summary>
        /// <param name="pSection">The section to fetch.</param>
        /// <param name="pDecoding">The content-transfer-decoding to apply.</param>
        /// <param name="pStream">The stream to write to</param>
        /// <param name="pConfiguration">Optionally use this parameter to specify an operation specific timeout, cancellation token, progress callback and write size controller.</param>
        public void Fetch(cSection pSection, eDecodingRequired pDecoding, Stream pStream, cBodyFetchConfiguration pConfiguration = null) => Client.Fetch(Handle, pSection, pDecoding, pStream, pConfiguration);
        /**<summary>The async version of <see cref="FetchAsync(cSection, eDecodingRequired, Stream, cBodyFetchConfiguration)"/>.</summary>*/
        public Task FetchAsync(cSection pSection, eDecodingRequired pDecoding, Stream pStream, cBodyFetchConfiguration pConfiguration = null) => Client.FetchAsync(Handle, pSection, pDecoding, pStream, pConfiguration);

        /// <summary>
        /// <para>Store flags for the message.</para>
        /// <para>This method will throw if it detects that the store is likely to have failed.</para>
        /// </summary>
        /// <param name="pOperation">The type of store operation.</param>
        /// <param name="pFlags">The flags to store.</param>
        /// <param name="pIfUnchangedSinceModSeq">
        /// <para>The modseq to use in the unchangedsince clause of a conditional store (RFC 7162).</para>
        /// <para>Can only be specified if the mailbox supports RFC 7162.</para>
        /// <para>If the message has been modified since the specified modseq the server should fail the update.</para>
        /// </param>
        public void Store(eStoreOperation pOperation, cStorableFlags pFlags, ulong? pIfUnchangedSinceModSeq = null)
        {
            var lFeedback = Client.Store(Handle, pOperation, pFlags, pIfUnchangedSinceModSeq);
            if (lFeedback.Summary().LikelyFailedCount != 0) throw new InvalidOperationException(); // the assumption here is that the message has been deleted
        }

        /**<summary>The async version of <see cref="Store(eStoreOperation, cStorableFlags, ulong?)"/>.</summary>*/
        public async Task StoreAsync(eStoreOperation pOperation, cStorableFlags pFlags, ulong? pIfUnchangedSinceModSeq = null)
        {
            var lFeedback = await Client.StoreAsync(Handle, pOperation, pFlags, pIfUnchangedSinceModSeq);
            if (lFeedback.Summary().LikelyFailedCount != 0) throw new InvalidOperationException(); // the assumption here is that the message has been deleted
        }

        /// <summary>
        /// Copy the message to the specified mailbox.
        /// </summary>
        /// <param name="pDestination">The mailbox to copy the message to.</param>
        /// <returns>If the server provides a UIDCOPY response: the UID of the message in the destination mailbox; otherwise null.</returns>
        public cUID Copy(cMailbox pDestination)
        {
            var lFeedback = Client.Copy(Handle, pDestination.Handle);
            if (lFeedback?.Count == 1) return lFeedback[0].Destination;
            return null;
        }

        /**<summary>The async version of <see cref="Copy(cMailbox)"/>.</summary>*/
        public async Task<cUID> CopyAsync(cMailbox pDestination)
        {
            var lFeedback = await Client.CopyAsync(Handle, pDestination.Handle).ConfigureAwait(false);
            if (lFeedback?.Count == 1) return lFeedback[0].Destination;
            return null;
        }

        /*
        // for sending via SMTP (i.e. a draft)

        public MailMessage ToMailMessage(fToMailMessageOptions pOptions)
        {
            ;?; // TODO
        } */

        /**<summary>Returns a string that represents the instance.</summary>*/
        public override string ToString() => $"{nameof(cMessage)}({Handle})"; // ,{Indent} // re-instate if threading is ever done
    }
}