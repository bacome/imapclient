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
                if (!Client.Fetch(Handle, fMessageProperties.envelope)) throw new InvalidOperationException();
                return Handle.Envelope;
            }
        }

        public DateTime? Sent
        {
            get
            {
                if (!Client.Fetch(Handle, fMessageProperties.sent)) throw new InvalidOperationException();
                return Handle.Envelope.Sent;
            }
        }

        public cCulturedString Subject
        {
            get
            {
                if (!Client.Fetch(Handle, fMessageProperties.subject)) throw new InvalidOperationException();
                return Handle.Envelope.Subject;
            }
        }

        public string BaseSubject
        {
            get
            {
                if (!Client.Fetch(Handle, fMessageProperties.basesubject)) throw new InvalidOperationException();
                return Handle.Envelope.BaseSubject;
            }
        }

        public cAddresses From
        {
            get
            {
                if (!Client.Fetch(Handle, fMessageProperties.from)) throw new InvalidOperationException();
                return Handle.Envelope.From;
            }
        }

        public cAddresses Sender
        {
            get
            {
                if (!Client.Fetch(Handle, fMessageProperties.sender)) throw new InvalidOperationException();
                return Handle.Envelope.Sender;
            }
        }

        public cAddresses ReplyTo
        {
            get
            {
                if (!Client.Fetch(Handle, fMessageProperties.replyto)) throw new InvalidOperationException();
                return Handle.Envelope.ReplyTo;
            }
        }

        public cAddresses To
        {
            get
            {
                if (!Client.Fetch(Handle, fMessageProperties.to)) throw new InvalidOperationException();
                return Handle.Envelope.To;
            }
        }

        public cAddresses CC
        {
            get
            {
                if (!Client.Fetch(Handle, fMessageProperties.cc)) throw new InvalidOperationException();
                return Handle.Envelope.CC;
            }
        }

        public cAddresses BCC
        {
            get
            {
                if (!Client.Fetch(Handle, fMessageProperties.bcc)) throw new InvalidOperationException();
                return Handle.Envelope.BCC;
            }
        }

        public string InReplyTo
        {
            get
            {
                if (!Client.Fetch(Handle, fMessageProperties.inreplyto)) throw new InvalidOperationException();
                return Handle.Envelope.InReplyTo;
            }
        }

        public string MessageId
        {
            get
            {
                if (!Client.Fetch(Handle, fMessageProperties.messageid)) throw new InvalidOperationException();
                return Handle.Envelope.MessageId;
            }
        }

        public cMessageFlags Flags
        {
            get
            {
                if (!Client.Fetch(Handle, fMessageProperties.flags)) throw new InvalidOperationException();
                return Handle.Flags;
            }
        }

        public bool FlagsContain(params string[] pFlags) => ZFlagsContain(pFlags);
        public bool FlagsContain(IEnumerable<string> pFlags) => ZFlagsContain(pFlags);

        private bool ZFlagsContain(IEnumerable<string> pFlags)
        {
            if (!Client.Fetch(Handle, fMessageProperties.flags)) throw new InvalidOperationException();
            return Handle.Flags.Contain(pFlags);
        }

        public bool IsAnswered => ZFlagsContain(fKnownMessageFlags.answered);
        public bool IsFlagged => ZFlagsContain(fKnownMessageFlags.flagged);
        public bool IsDeleted => ZFlagsContain(fKnownMessageFlags.deleted);
        public bool IsSeen => ZFlagsContain(fKnownMessageFlags.seen);
        public bool IsDraft => ZFlagsContain(fKnownMessageFlags.draft);
        public bool IsRecent => ZFlagsContain(fKnownMessageFlags.recent);

        public bool IsMDNSent => ZFlagsContain(fKnownMessageFlags.mdnsent);
        public bool IsForwarded => ZFlagsContain(fKnownMessageFlags.forwarded);
        public bool IsSubmitPending => ZFlagsContain(fKnownMessageFlags.submitpending);
        public bool IsSubmitted => ZFlagsContain(fKnownMessageFlags.submitted);

        private bool ZFlagsContain(fKnownMessageFlags pFlag)
        {
            if (!Client.Fetch(Handle, fMessageProperties.flags)) throw new InvalidOperationException();
            return (Handle.Flags.KnownMessageFlags & pFlag) != 0;
        }

        public DateTime Received
        {
            get
            {
                if (!Client.Fetch(Handle, fMessageProperties.received)) throw new InvalidOperationException();
                return Handle.Received.Value;
            }
        }

        public uint Size
        {
            get
            {
                if (!Client.Fetch(Handle, fMessageProperties.size)) throw new InvalidOperationException();
                return Handle.Size.Value;
            }
        }

        public cUID UID
        {
            get
            {
                if (!Client.Fetch(Handle, fMessageProperties.uid)) throw new InvalidOperationException();
                return Handle.UID;
            }
        }

        public cStrings References
        {
            get
            {
                if (!Client.Fetch(Handle, fMessageProperties.references)) throw new InvalidOperationException();
                return Handle.References;
            }
        }

        public ulong ModSeq
        {
            get
            {
                if (!Client.Fetch(Handle, fMessageProperties.modseq)) throw new InvalidOperationException();
                return Handle.ModSeq.Value;
            }
        }

        public cBodyPart BodyStructure
        {
            get
            {
                if (!Client.Fetch(Handle, fMessageProperties.bodystructure)) throw new InvalidOperationException();
                return Handle.BodyStructure;
            }
        }

        public List<cAttachment> Attachments
        {
            get
            {
                if (!Client.Fetch(Handle, fMessageProperties.attachments)) throw new InvalidOperationException();
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

        public uint PlainTextSizeInBytes
        {
            get
            {
                if (!Client.Fetch(Handle, fMessageProperties.plaintextsizeinbytes)) throw new InvalidOperationException();
                uint lSize = 0;
                foreach (var lPart in ZPlainTextParts(Handle.BodyStructure)) lSize += lPart.SizeInBytes;
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

        public bool Fetch(fMessageProperties pProperties) => Client.Fetch(Handle, pProperties);

        public Task<bool> FetchAsync(fMessageProperties pProperties) => Client.FetchAsync(Handle, pProperties);

        public cFilterMSNOffset MSNOffset(int pOffset) => new cFilterMSNOffset(Handle, pOffset);

        // get data

        public string PlainText()
        {
            if (!Client.Fetch(Handle, fMessageProperties.bodystructure)) throw new InvalidOperationException();
            StringBuilder lBuilder = new StringBuilder();
            foreach (var lPart in ZPlainTextParts(Handle.BodyStructure)) lBuilder.Append(Fetch(lPart));
            return lBuilder.ToString();
        }

        public async Task<string> PlainTextAsync()
        {
            if (!await Client.FetchAsync(Handle, fMessageProperties.bodystructure).ConfigureAwait(false)) throw new InvalidOperationException();

            List<Task<string>> lTasks = new List<Task<string>>();
            foreach (var lPart in ZPlainTextParts(Handle.BodyStructure)) lTasks.Add(FetchAsync(lPart));
            await Task.WhenAll(lTasks).ConfigureAwait(false);

            StringBuilder lBuilder = new StringBuilder();
            foreach (var lTask in lTasks) lBuilder.Append(lTask.Result);
            return lBuilder.ToString();
        }

        public string Fetch(cBodyPart pPart)
        {
            using (var lStream = new MemoryStream())
            {
                if (pPart is cTextBodyPart lPart)
                {
                    Client.Fetch(Handle, lPart.Section, lPart.DecodingRequired, lStream, null);
                    Encoding lEncoding = Encoding.GetEncoding(lPart.Charset);
                    return new string(lEncoding.GetChars(lStream.GetBuffer(), 0, (int)lStream.Length));
                }

                Client.Fetch(Handle, pPart.Section, eDecodingRequired.none, lStream, null);
                return new string(Encoding.UTF8.GetChars(lStream.GetBuffer(), 0, (int)lStream.Length));
            }
        }

        public async Task<string> FetchAsync(cBodyPart pPart)
        {
            using (var lStream = new MemoryStream())
            {
                if (pPart is cTextBodyPart lPart)
                {
                    await Client.FetchAsync(Handle, lPart.Section, lPart.DecodingRequired, lStream, null).ConfigureAwait(false);
                    Encoding lEncoding = Encoding.GetEncoding(lPart.Charset);
                    return new string(lEncoding.GetChars(lStream.GetBuffer(), 0, (int)lStream.Length));
                }

                await Client.FetchAsync(Handle, pPart.Section, eDecodingRequired.none, lStream, null).ConfigureAwait(false);
                return new string(Encoding.UTF8.GetChars(lStream.GetBuffer(), 0, (int)lStream.Length));
            }
        }

        public void Fetch(cBodyPart pPart, Stream pStream, cBodyFetchConfiguration pConfiguration = null)
        {
            if (pPart is cSinglePartBody lPart) Client.Fetch(Handle, lPart.Section, lPart.DecodingRequired, pStream, pConfiguration);
            else Client.Fetch(Handle, pPart.Section, eDecodingRequired.none, pStream, pConfiguration);
        }

        public Task FetchAsync(cBodyPart pPart, Stream pStream, cBodyFetchConfiguration pConfiguration = null)
        {
            if (pPart is cSinglePartBody lPart) return Client.FetchAsync(Handle, lPart.Section, lPart.DecodingRequired, pStream, pConfiguration);
            else return Client.FetchAsync(Handle, pPart.Section, eDecodingRequired.none, pStream, pConfiguration);
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

        public void Fetch(cSection pSection, eDecodingRequired pDecoding, Stream pStream, cBodyFetchConfiguration pConfiguration = null) => Client.Fetch(Handle, pSection, pDecoding, pStream, pConfiguration);

        public Task FetchAsync(cSection pSection, eDecodingRequired pDecoding, Stream pStream, cBodyFetchConfiguration pConfiguration = null) => Client.FetchAsync(Handle, pSection, pDecoding, pStream, pConfiguration);

        // debugging
        public override string ToString() => $"{nameof(cMessage)}({Handle},{Indent})";
    }
}