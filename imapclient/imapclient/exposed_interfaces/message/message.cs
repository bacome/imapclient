using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
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
        public readonly int Indent; // Indicates the indent of the message. This only means something when compared to the indents of surrounding items in a threaded list of messages. It is a bit of a hack having it in this class.

        public cMessage(cIMAPClient pClient, iMessageHandle pHandle, int pIndent = -1)
        {
            Client = pClient ?? throw new ArgumentNullException(nameof(pClient));
            Handle = pHandle ?? throw new ArgumentNullException(nameof(pHandle));
            Indent = pIndent;
        }

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

        public bool IsExpunged => Handle.Expunged;

        public cEnvelope Envelope
        {
            get
            {
                if (!Client.Fetch(Handle, kEnvelope)) throw new InvalidOperationException();
                return Handle.Envelope;
            }
        }

        public DateTime? Sent
        {
            get
            {
                if (!Client.Fetch(Handle, kEnvelope)) throw new InvalidOperationException();
                return Handle.Envelope.Sent;
            }
        }

        public cCulturedString Subject
        {
            get
            {
                if (!Client.Fetch(Handle, kEnvelope)) throw new InvalidOperationException();
                return Handle.Envelope.Subject;
            }
        }

        public string BaseSubject
        {
            get
            {
                if (!Client.Fetch(Handle, kEnvelope)) throw new InvalidOperationException();
                return Handle.Envelope.BaseSubject;
            }
        }

        public cAddresses From
        {
            get
            {
                if (!Client.Fetch(Handle, kEnvelope)) throw new InvalidOperationException();
                return Handle.Envelope.From;
            }
        }

        public cAddresses Sender
        {
            get
            {
                if (!Client.Fetch(Handle, kEnvelope)) throw new InvalidOperationException();
                return Handle.Envelope.Sender;
            }
        }

        public cAddresses ReplyTo
        {
            get
            {
                if (!Client.Fetch(Handle, kEnvelope)) throw new InvalidOperationException();
                return Handle.Envelope.ReplyTo;
            }
        }

        public cAddresses To
        {
            get
            {
                if (!Client.Fetch(Handle, kEnvelope)) throw new InvalidOperationException();
                return Handle.Envelope.To;
            }
        }

        public cAddresses CC
        {
            get
            {
                if (!Client.Fetch(Handle, kEnvelope)) throw new InvalidOperationException();
                return Handle.Envelope.CC;
            }
        }

        public cAddresses BCC
        {
            get
            {
                if (!Client.Fetch(Handle, kEnvelope)) throw new InvalidOperationException();
                return Handle.Envelope.BCC;
            }
        }

        public cStrings InReplyTo
        {
            get
            {
                if (!Client.Fetch(Handle, kEnvelope)) throw new InvalidOperationException();
                return Handle.Envelope.InReplyTo?.MsgIds;
            }
        }

        public string MessageId
        {
            get
            {
                if (!Client.Fetch(Handle, kEnvelope)) throw new InvalidOperationException();
                return Handle.Envelope.MessageId?.MsgId;
            }
        }

        public cFetchableFlags Flags
        {
            get
            {
                if (!Client.Fetch(Handle, kFlags)) throw new InvalidOperationException();
                return Handle.Flags;
            }
        }

        public bool IsAnswered
        {
            get => ZFlagsContain(kMessageFlagName.Answered);
            set => ZFlagSet(cSettableFlags.Answered, value);
        }

        public bool IsFlagged
        {
            get => ZFlagsContain(kMessageFlagName.Flagged);
            set => ZFlagSet(cSettableFlags.Flagged, value);
        }

        public bool IsDeleted
        {
            get => ZFlagsContain(kMessageFlagName.Deleted);
            set => ZFlagSet(cSettableFlags.Deleted, value);
        }

        public bool IsSeen
        {
            get => ZFlagsContain(kMessageFlagName.Seen);
            set => ZFlagSet(cSettableFlags.Seen, value);
        }

        public bool IsDraft
        {
            get => ZFlagsContain(kMessageFlagName.Draft);
            set => ZFlagSet(cSettableFlags.Draft, value);
        }

        public bool IsRecent => ZFlagsContain(kMessageFlagName.Recent);

        public bool IsMDNSent
        {
            get => ZFlagsContain(kMessageFlagName.MDNSent);
            set => ZFlagSet(cSettableFlags.MDNSent, value);
        }

        public bool IsForwarded
        {
            get => ZFlagsContain(kMessageFlagName.Forwarded);
            set => ZFlagSet(cSettableFlags.Forwarded, value);
        }

        public bool IsSubmitPending
        {
            get => ZFlagsContain(kMessageFlagName.SubmitPending);
            set => ZFlagSet(cSettableFlags.SubmitPending, value);
        }

        public bool IsSubmitted
        {
            get => ZFlagsContain(kMessageFlagName.Submitted);
            set => ZFlagSet(cSettableFlags.Submitted, value);
        }

        private bool ZFlagsContain(string pFlag)
        {
            if (!Client.Fetch(Handle, kFlags)) throw new InvalidOperationException();
            return Handle.Flags.Contains(pFlag);
        }

        private void ZFlagSet(cSettableFlags pFlags, bool pValue)
        {
            if (pValue) Client.Store(Handle, eStoreOperation.add, pFlags, null);
            else Client.Store(Handle, eStoreOperation.remove, pFlags, null);
        }

        public DateTime Received
        {
            get
            {
                if (!Client.Fetch(Handle, kReceived)) throw new InvalidOperationException();
                return Handle.Received.Value;
            }
        }

        public int Size
        {
            get
            {
                if (!Client.Fetch(Handle, kSize)) throw new InvalidOperationException();
                return (int)Handle.Size.Value;
            }
        }

        public cUID UID
        {
            get
            {
                if (!Client.Fetch(Handle, kUID)) throw new InvalidOperationException();
                return Handle.UID;
            }
        }

        public ulong ModSeq
        {
            get
            {
                if (!Client.Fetch(Handle, kModSeq)) throw new InvalidOperationException();
                return Handle.ModSeq.Value;
            }
        }

        public cBodyPart BodyStructure
        {
            get
            {
                if (!Client.Fetch(Handle, kBodyStructure)) throw new InvalidOperationException();
                return Handle.BodyStructure;
            }
        }

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

        public cStrings References
        {
            get
            {
                if (!Client.Fetch(Handle, kReferences)) throw new InvalidOperationException();
                return Handle.HeaderFields.References;
            }
        }

        public eImportance? Importance
        {
            get
            {
                if (!Client.Fetch(Handle, kImportance)) throw new InvalidOperationException();
                return Handle.HeaderFields.Importance;
            }
        }

        public bool Fetch(cCacheItems pItems) => Client.Fetch(Handle, pItems);
        public Task<bool> FetchAsync(cCacheItems pItems) => Client.FetchAsync(Handle, pItems);

        public int FetchSizeInBytes(cSinglePartBody pPart) => Client.FetchSizeInBytes(Handle, pPart);
        public Task<int> FetchSizeInBytesAsync(cSinglePartBody pPart) => Client.FetchSizeInBytesAsync(Handle, pPart);

        public cFilterMSNOffset MSNOffset(int pOffset) => new cFilterMSNOffset(Handle, pOffset);

        // get data

        public string PlainText()
        {
            if (!Client.Fetch(Handle, kBodyStructure)) throw new InvalidOperationException();
            StringBuilder lBuilder = new StringBuilder();
            foreach (var lPart in ZPlainTextParts(Handle.BodyStructure)) lBuilder.Append(Fetch(lPart));
            return lBuilder.ToString();
        }

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

        public string Fetch(cTextBodyPart pPart)
        {
            using (var lStream = new MemoryStream())
            {
                Client.Fetch(Handle, pPart.Section, pPart.DecodingRequired, lStream, null);
                Encoding lEncoding = Encoding.GetEncoding(pPart.Charset);
                return new string(lEncoding.GetChars(lStream.GetBuffer(), 0, (int)lStream.Length));
            }
        }

        public async Task<string> FetchAsync(cTextBodyPart pPart)
        {
            using (var lStream = new MemoryStream())
            {
                await Client.FetchAsync(Handle, pPart.Section, pPart.DecodingRequired, lStream, null).ConfigureAwait(false);
                Encoding lEncoding = Encoding.GetEncoding(pPart.Charset);
                return new string(lEncoding.GetChars(lStream.GetBuffer(), 0, (int)lStream.Length));
            }
        }

        public string Fetch(cSection pSection)
        {
            using (var lStream = new MemoryStream())
            {
                Client.Fetch(Handle, pSection, eDecodingRequired.none, lStream, null);
                return new string(Encoding.UTF8.GetChars(lStream.GetBuffer(), 0, (int)lStream.Length));
            }
        }

        public async Task<string> FetchAsync(cSection pSection)
        {
            using (var lStream = new MemoryStream())
            {
                await Client.FetchAsync(Handle, pSection, eDecodingRequired.none, lStream, null).ConfigureAwait(false);
                return new string(Encoding.UTF8.GetChars(lStream.GetBuffer(), 0, (int)lStream.Length));
            }
        }

        public void Fetch(cSinglePartBody pPart, Stream pStream, cBodyFetchConfiguration pConfiguration = null) => Client.Fetch(Handle, pPart.Section, pPart.DecodingRequired, pStream, pConfiguration);
        public Task FetchAsync(cSinglePartBody pPart, Stream pStream, cBodyFetchConfiguration pConfiguration = null) => Client.FetchAsync(Handle, pPart.Section, pPart.DecodingRequired, pStream, pConfiguration);
        public void Fetch(cSection pSection, eDecodingRequired pDecoding, Stream pStream, cBodyFetchConfiguration pConfiguration = null) => Client.Fetch(Handle, pSection, pDecoding, pStream, pConfiguration);
        public Task FetchAsync(cSection pSection, eDecodingRequired pDecoding, Stream pStream, cBodyFetchConfiguration pConfiguration = null) => Client.FetchAsync(Handle, pSection, pDecoding, pStream, pConfiguration);

        // set data

        public cStoreFeedbackItem Store(eStoreOperation pOperation, cSettableFlags pFlags, ulong? pIfUnchangedSinceModSeq = null) => Client.Store(Handle, pOperation, pFlags, pIfUnchangedSinceModSeq);
        public Task<cStoreFeedbackItem> StoreAsync(eStoreOperation pOperation, cSettableFlags pFlags, ulong? pIfUnchangedSinceModSeq = null) => Client.StoreAsync(Handle, pOperation, pFlags, pIfUnchangedSinceModSeq);

        // debugging
        public override string ToString() => $"{nameof(cMessage)}({Handle},{Indent})";
    }
}