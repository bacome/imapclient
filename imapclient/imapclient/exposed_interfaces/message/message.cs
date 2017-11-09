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
    /// <para>Provides an API that allows interaction with an IMAP message.</para>
    /// <para>Instances are only valid whilst the containing mailbox remains selected. Re-selecting a mailbox will not bring the message instances back to life.</para>
    /// <para>Instances are only valid whilst the containing mailbox has the same UIDValidity.</para>
    /// </summary>
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

        public readonly cIMAPClient Client;
        public readonly iMessageHandle Handle;

        // re-instate if threading is ever done
        //public readonly int Indent; // Indicates the indent of the message. This only means something when compared to the indents of surrounding items in a threaded list of messages. It is a bit of a hack having it in this class.

        public cMessage(cIMAPClient pClient, iMessageHandle pHandle) // , int pIndent = -1 // re-instate if threading is ever done
        {
            Client = pClient ?? throw new ArgumentNullException(nameof(pClient));
            Handle = pHandle ?? throw new ArgumentNullException(nameof(pHandle));
            //Indent = pIndent; // re-instate if threading is ever done
        }

        /// <summary>
        /// Fired when the server notifies the client of a message property value change.
        /// Most properties of an IMAP message can never change.
        /// </summary>
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
        /// True if the server has told us that the message has been expunged.
        /// </summary>
        public bool Expunged => Handle.Expunged;

        /// <summary>
        /// <para>The IMAP envelope data of the message.</para>
        /// <para>If the internal message cache does not contain the envelope data of the message, it will be fetched from the server.</para>
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
        /// <para>The sent date of the message (from the <see cref="Envelope"/>).</para>
        /// <para>If the internal message cache does not contain the envelope data of the message, it will be fetched from the server.</para>
        /// <para>May be null.</para>
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
        /// <para>The subject of the message (from the <see cref="Envelope"/>).</para>
        /// <para>If the internal message cache does not contain the envelope data of the message, it will be fetched from the server.</para>
        /// <para>May be null.</para>
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
        /// <para>The base subject (as defined in RFC 5256: with the RE: FWD: etc stripped off) of the message (from the <see cref="Envelope"/>).</para>
        /// <para>If the internal message cache does not contain the envelope data of the message, it will be fetched from the server.</para>
        /// <para>May be null.</para>
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
        /// <para>The 'from' addresses of the message (from the <see cref="Envelope"/>).</para>
        /// <para>If the internal message cache does not contain the envelope data of the message, it will be fetched from the server.</para>
        /// <para>May be null.</para>
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
        /// <para>The 'sender' addresses of the message (from the <see cref="Envelope"/>).</para>
        /// <para>If the internal message cache does not contain the envelope data of the message, it will be fetched from the server.</para>
        /// <para>May be null.</para>
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
        /// <para>The 'reply-to' addresses of the message (from the <see cref="Envelope"/>).</para>
        /// <para>If the internal message cache does not contain the envelope data of the message, it will be fetched from the server.</para>
        /// <para>May be null.</para>
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
        /// <para>The 'to' addresses of the message (from the <see cref="Envelope"/>).</para>
        /// <para>If the internal message cache does not contain the envelope data of the message, it will be fetched from the server.</para>
        /// <para>May be null.</para>
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
        /// <para>The 'CC' addresses of the message (from the <see cref="Envelope"/>).</para>
        /// <para>If the internal message cache does not contain the envelope data of the message, it will be fetched from the server.</para>
        /// <para>May be null.</para>
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
        /// <para>The 'BCC' addresses of the message (from the <see cref="Envelope"/>).</para>
        /// <para>If the internal message cache does not contain the envelope data of the message, it will be fetched from the server.</para>
        /// <para>May be null.</para>
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
        /// <para>The normalised (delimiters, quoting, comments and white space removed) 'in-reply-to' message-ids of the message (from the <see cref="Envelope"/>).</para>
        /// <para>If the internal message cache does not contain the envelope data of the message, it will be fetched from the server.</para>
        /// <para>May be null.</para>
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
        /// <para>The normalised (delimiters, quoting, comments and white space removed) message-id of the message (from the <see cref="Envelope"/>).</para>
        /// <para>If the internal message cache does not contain the envelope data of the message, it will be fetched from the server.</para>
        /// <para>May be null.</para>
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
        /// <para>The flags set for the message.</para>
        /// <para>If the internal message cache does not contain flags for the message, they will be fetched from the server.</para>
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
        /// <para>True if the flags contain the <see cref="kMessageFlagName.Answered"/> flag.</para>
        /// <para>If the internal message cache does not contain flags for the message, they will be fetched from the server.</para>
        /// </summary>
        public bool Answered => ZFlagsContain(kMessageFlagName.Answered);
        /**<summary>Add the <see cref="kMessageFlagName.Answered"/> flag to the message flags.</summary>*/
        public void SetAnswered() { ZFlagSet(cSettableFlags.Answered, true); }

        /// <summary>
        /// <para>Get and set the <see cref="kMessageFlagName.Flagged"/> flag on the message.</para>
        /// <para>When getting the value, if the internal message cache does not contain flags for the message, they will be fetched from the server.</para>
        /// </summary>
        public bool Flagged
        {
            get => ZFlagsContain(kMessageFlagName.Flagged);
            set => ZFlagSet(cSettableFlags.Flagged, value);
        }

        /// <summary>
        /// <para>Get and set the <see cref="kMessageFlagName.Deleted"/> flag on the message.</para>
        /// <para>When getting the value, if the internal message cache does not contain flags for the message, they will be fetched from the server.</para>
        /// </summary>
        public bool Deleted
        {
            get => ZFlagsContain(kMessageFlagName.Deleted);
            set => ZFlagSet(cSettableFlags.Deleted, value);
        }

        /// <summary>
        /// <para>Get and set the <see cref="kMessageFlagName.Seen"/> flag on the message.</para>
        /// <para>When getting the value, if the internal message cache does not contain flags for the message, they will be fetched from the server.</para>
        /// </summary>
        public bool Seen
        {
            get => ZFlagsContain(kMessageFlagName.Seen);
            set => ZFlagSet(cSettableFlags.Seen, value);
        }

        /// <summary>
        /// <para>Get and set the <see cref="kMessageFlagName.Draft"/> flag on the message.</para>
        /// <para>When getting the value, if the internal message cache does not contain flags for the message, they will be fetched from the server.</para>
        /// </summary>
        public bool Draft
        {
            get => ZFlagsContain(kMessageFlagName.Draft);
            set => ZFlagSet(cSettableFlags.Draft, value);
        }

        /// <summary>
        /// <para>True if the flags contain the <see cref="kMessageFlagName.Recent"/> flag.</para>
        /// <para>If the internal message cache does not contain flags for the message, they will be fetched from the server.</para>
        /// </summary>
        public bool Recent => ZFlagsContain(kMessageFlagName.Recent);

        // see comments elsewhere to see why these are commented out
        //public bool MDNSent => ZFlagsContain(kMessageFlagName.MDNSent);
        //public void SetMDNSent() { ZFlagSet(cSettableFlags.MDNSent, true); }

        /// <summary>
        /// <para>True if the flags contain the <see cref="kMessageFlagName.Forwarded"/> flag.</para>
        /// <para>If the internal message cache does not contain flags for the message, they will be fetched from the server.</para>
        /// </summary>
        public bool Forwarded => ZFlagsContain(kMessageFlagName.Forwarded);
        /**<summary>Add the <see cref="kMessageFlagName.Forwarded"/> flag to the message flags.</summary>*/
        public void SetForwarded() { ZFlagSet(cSettableFlags.Forwarded, true); }

        /// <summary>
        /// <para>True if the flags contain the <see cref="kMessageFlagName.SubmitPending"/> flag.</para>
        /// <para>If the internal message cache does not contain flags for the message, they will be fetched from the server.</para>
        /// </summary>
        public bool SubmitPending => ZFlagsContain(kMessageFlagName.SubmitPending);
        /**<summary>Add the <see cref="kMessageFlagName.SubmitPending"/> flag to the message flags.</summary>*/
        public void SetSubmitPending() { ZFlagSet(cSettableFlags.SubmitPending, true); }

        /// <summary>
        /// <para>True if the flags contain the <see cref="kMessageFlagName.Submitted"/> flag.</para>
        /// <para>If the internal message cache does not contain flags for the message, they will be fetched from the server.</para>
        /// </summary>
        public bool Submitted  => ZFlagsContain(kMessageFlagName.Submitted);

        private bool ZFlagsContain(string pFlag)
        {
            if (!Client.Fetch(Handle, kFlags)) throw new InvalidOperationException();
            return Handle.Flags.Contains(pFlag);
        }

        private void ZFlagSet(cSettableFlags pFlags, bool pValue)
        {
            cStoreFeedback lFeedback;
            if (pValue) lFeedback = Client.Store(Handle, eStoreOperation.add, pFlags, null);
            else lFeedback = Client.Store(Handle, eStoreOperation.remove, pFlags, null);
            if (lFeedback.Summary().LikelyFailedCount != 0) throw new InvalidOperationException(); // the assumption here is that the message has been deleted
        }

        /// <summary>
        /// <para>The IMAP internaldate for the message.</para>
        /// <para>If the internal message cache does not contain the internaldate of the message, it will be fetched from the server.</para>
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
        /// <para>The size of the entire message in bytes.</para>
        /// <para>If the internal message cache does not contain the size of the message, it will be fetched from the server.</para>
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
        /// <para>The IMAP UID of the message.</para>
        /// <para>If the internal message cache does not contain the UID of the message, it will be fetched from the server.</para>
        /// <para>May be null if the server does not support unique identifiers.</para>
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
        /// <para>The modification sequence number of the message.</para>
        /// <para>If the internal message cache does not contain a modseq for the message, it will be fetched from the server.</para>
        /// <para>Will be 0 if the mailbox does not support CONDSTORE.</para>
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
        /// <para>The IMAP bodystructure of the message.</para>
        /// <para>If the internal message cache does not contain the bodystructure of the message, it will be fetched from the server.</para>
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
        /// <para>Returns the list of message attachments.</para>
        /// <para>If the internal message cache does not contain the bodystructure of the message, it will be fetched from the server.</para>
        /// <para>The library defines an attachment as a message part with a disposition of ‘attachment’.</para>
        /// <para>If there are alternate versions of an attachment only one of the alternates is included in the returned list (the first one).</para>
        /// <para>The returned list may be empty.</para>
        /// </summary>
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
        /// <para>The size in bytes of the plain text parts of the message.</para>
        /// <para>If the internal message cache does not contain the bodystructure of the message, it will be fetched from the server.</para>
        /// <para>The library defines plain text parts as parts with a MIME type of text/plain and without a disposition of 'attachment'.</para>
        /// <para>If there are alternate versions of a part only one of the alternates is used in calculating the size (the first one).</para>
        /// </summary>
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
        /// <para>The normalised (delimiters, quoting, comments and white space removed) message-ids from the references header field, or null if there was no references header field or if the references header field could not be parsed.</para>
        /// <para>If the internal message cache does not contain the references header field of the message, it will be fetched from the server.</para>
        /// </summary>
        public cStrings References
        {
            get
            {
                if (!Client.Fetch(Handle, kReferences)) throw new InvalidOperationException();
                return Handle.HeaderFields.References;
            }
        }

        /// <summary>
        /// <para>The importance value from the importance header field, or null if there was no importance header field or if the importance header field could not be parsed.</para>
        /// <para>If the internal message cache does not contain the importance header field of the message, it will be fetched from the server.</para>
        /// </summary>
        public eImportance? Importance
        {
            get
            {
                if (!Client.Fetch(Handle, kImportance)) throw new InvalidOperationException();
                return Handle.HeaderFields.Importance;
            }
        }

        /// <summary>
        /// <para>Ensures that the internal message cache contains the specified items for this message instance.</para>
        /// <para>The missing items will be fetched from the server.</para>
        /// </summary>
        /// <param name="pItems">
        /// <para>The items required in the cache.</para>
        /// <para>Note that the <see cref="cCacheItems"/> has implicit conversions from other types including <see cref="fMessageProperties"/> (so you can use values of those types as parameters to this method).</para>
        /// </param>
        /// <returns>
        /// <para>True if the fetch populated the cache with the requested items, false otherwise.</para>
        /// <para>False indicates that the message is expunged.</para>
        /// </returns>
        public bool Fetch(cCacheItems pItems) => Client.Fetch(Handle, pItems);
        /**<summary>The async version of <see cref="Fetch(cCacheItems)"/>.</summary>*/
        public Task<bool> FetchAsync(cCacheItems pItems) => Client.FetchAsync(Handle, pItems);

        /// <summary>
        /// <para>Returns the fetch size in bytes of a <see cref="cSinglePartBody"/> part of this message.</para>
        /// <para>This may be smaller than the <see cref="cSinglePartBody.SizeInBytes"/> if the part needs decoding (<see cref="cSinglePartBody.DecodingRequired"/>) and the server supports RFC 3516.</para>
        /// <para>This method may have to fetch the size from the server. (The size will be cached in the internal message cache.)</para>
        /// <para></para>
        /// </summary>
        /// <param name="pPart">The part to get the size for.</param>
        /// <returns>The size in bytes.</returns>
        public int FetchSizeInBytes(cSinglePartBody pPart) => Client.FetchSizeInBytes(Handle, pPart);
        /**<summary>The async version of <see cref="FetchSizeInBytes(cSinglePartBody)"/>.</summary>*/
        public Task<int> FetchSizeInBytesAsync(cSinglePartBody pPart) => Client.FetchSizeInBytesAsync(Handle, pPart);

        /// <summary>
        /// <para>Generates message sequence number offset for use in message filtering.</para>
        /// <para>See <see cref="cFilter.MSN"/> - a static instance of <see cref="cFilterMSN"/>.</para>
        /// </summary>
        /// <param name="pOffset">The offset from this message's sequence number.</param>
        /// <returns>A message sequence number offset.</returns>
        public cFilterMSNOffset MSNOffset(int pOffset) => new cFilterMSNOffset(Handle, pOffset);

        /// <summary>
        /// <para>Fetches the message's plain text parts from the server, decodes them, and concatenates them yielding the returned value.</para>
        /// <para>If the internal message cache does not contain the bodystructure of the message, it will be fetched from the server.</para>
        /// <para>The library defines plain text parts as parts with a MIME type of text/plain and without a disposition of 'attachment'.</para>
        /// <para>If there are alternate versions of a part only one of the alternates is used in generating the plain text (the first one).</para>
        /// </summary>
        /// <returns></returns>
        public string PlainText()
        {
            if (!Client.Fetch(Handle, kBodyStructure)) throw new InvalidOperationException();
            StringBuilder lBuilder = new StringBuilder();
            foreach (var lPart in ZPlainTextParts(Handle.BodyStructure)) lBuilder.Append(Fetch(lPart));
            return lBuilder.ToString();
        }

        /**<summary>The async version of <see cref="PlainText"/>.</summary>*/
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
        /// <para>Fetches the specified part from the server, decodes, and returns the data in a string.</para>
        /// </summary>
        /// <param name="pPart">The part to fetch.</param>
        /// <returns>The decoded data of the message part.</returns>
        public string Fetch(cTextBodyPart pPart)
        {
            using (var lStream = new MemoryStream())
            {
                Client.Fetch(Handle, pPart.Section, pPart.DecodingRequired, lStream, null);
                Encoding lEncoding = Encoding.GetEncoding(pPart.Charset);
                return new string(lEncoding.GetChars(lStream.GetBuffer(), 0, (int)lStream.Length));
            }
        }

        /**<summary>The async version of <see cref="Fetch(cTextBodyPart)"/>.</summary>*/
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
        /// <para>Fetches the specified message section from the server as text (without any content-transfer-decoding) and attempts to return the data as a string.</para>
        /// </summary>
        /// <param name="pSection">The section to fetch.</param>
        /// <returns>The raw data of the section as a string.</returns>
        public string Fetch(cSection pSection)
        {
            using (var lStream = new MemoryStream())
            {
                Client.Fetch(Handle, pSection, eDecodingRequired.none, lStream, null);
                return new string(Encoding.UTF8.GetChars(lStream.GetBuffer(), 0, (int)lStream.Length));
            }
        }

        /**<summary>The async version of <see cref="Fetch(cSection)"/>.</summary>*/
        public async Task<string> FetchAsync(cSection pSection)
        {
            using (var lStream = new MemoryStream())
            {
                await Client.FetchAsync(Handle, pSection, eDecodingRequired.none, lStream, null).ConfigureAwait(false);
                return new string(Encoding.UTF8.GetChars(lStream.GetBuffer(), 0, (int)lStream.Length));
            }
        }

        /// <summary>
        /// <para>Fetches the specified part from the server and writes the (possibly decoded) bytes into the provided stream.</para>
        /// <para>Any decoding required may be done client-side or server-side (if the server supports RFC 3516).</para>
        /// <para>To calculate the number of bytes that have to be fetched, use the <see cref="FetchSizeInBytes(cSinglePartBody)"/> method. (This is useful if you are intending to display a progress bar.)</para>
        /// <para>Optionally you may specify an operation specific timeout, cancellation token, progress callback and write size controller in the <paramref name="pConfiguration"/> parameter.</para>
        /// </summary>
        /// <param name="pPart">The part to fetch.</param>
        /// <param name="pStream">The stream to write into.</param>
        /// <param name="pConfiguration">Optionally use this parameter to specify an operation specific timeout, cancellation token, progress callback and write size controller.</param>
        public void Fetch(cSinglePartBody pPart, Stream pStream, cBodyFetchConfiguration pConfiguration = null) => Client.Fetch(Handle, pPart.Section, pPart.DecodingRequired, pStream, pConfiguration);
        /**<summary>The async version of <see cref="Fetch(cSinglePartBody, Stream, cBodyFetchConfiguration)"/>.</summary>*/
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
        public void Store(eStoreOperation pOperation, cSettableFlags pFlags, ulong? pIfUnchangedSinceModSeq = null)
        {
            var lFeedback = Client.Store(Handle, pOperation, pFlags, pIfUnchangedSinceModSeq);
            if (lFeedback.Summary().LikelyFailedCount != 0) throw new InvalidOperationException(); // the assumption here is that the message has been deleted
        }

        /**<summary>The async version of <see cref="Store(eStoreOperation, cSettableFlags, ulong?)"/>.</summary>*/
        public async Task StoreAsync(eStoreOperation pOperation, cSettableFlags pFlags, ulong? pIfUnchangedSinceModSeq = null)
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

        // debugging
        public override string ToString() => $"{nameof(cMessage)}({Handle})"; // ,{Indent} // re-instate if threading is ever done
    }
}