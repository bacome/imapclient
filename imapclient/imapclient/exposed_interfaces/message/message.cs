using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace work.bacome.imapclient
{
    public class cMessage
    {
        private PropertyChangedEventHandler mPropertyChanged;
        private object mPropertyChangedLock = new object();

        public readonly cIMAPClient Client;
        public readonly cMailboxId MailboxId;
        public readonly iMessageHandle Handle;
        public readonly int Indent; // Indicates the indent of the message. This only means something when compared to the indents of surrounding items in a threaded list of messages. It is a bit of a hack having it in this class.

        public cMessage(cIMAPClient pClient, cMailboxId pMailboxId, iMessageHandle pHandle, int pIndent = -1)
        {
            Client = pClient ?? throw new ArgumentNullException(nameof(pClient));
            MailboxId = pMailboxId ?? throw new ArgumentNullException(nameof(pMailboxId));
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
            if (pArgs.MailboxId == MailboxId && ReferenceEquals(pArgs.Handle, Handle)) mPropertyChanged?.Invoke(this, pArgs);
        }

        public bool IsExpunged => Handle.Expunged;

        public cBodyPart BodyStructure
        {
            get
            {
                Client.Fetch(MailboxId, Handle, fMessageProperties.bodystructure);
                return Handle.BodyStructure;
            }
        }

        public DateTime? Sent
        {
            get
            {



                if ((Handle.Attributes & fFetchAttributes.envelope) == 0) Client.Fetch(MailboxId, Handle, fFetchAttributes.envelope);
                if ((Handle.Attributes & fFetchAttributes.envelope) == 0) throw new cFetchAttributeException();
                return Handle.Envelope.Sent;
            }
        }

        public cCulturedString Subject
        {
            get
            {
                if ((Handle.Attributes & fFetchAttributes.envelope) == 0) Client.Fetch(MailboxId, Handle, fFetchAttributes.envelope);
                if ((Handle.Attributes & fFetchAttributes.envelope) == 0) throw new cFetchAttributeException();
                return Handle.Envelope.Subject;
            }
        }

        public cAddresses From
        {
            get
            {
                if ((Handle.Attributes & fFetchAttributes.envelope) == 0) Client.Fetch(MailboxId, Handle, fFetchAttributes.envelope);
                if ((Handle.Attributes & fFetchAttributes.envelope) == 0) throw new cFetchAttributeException();
                return Handle.Envelope.From;
            }
        }

        public cAddresses Sender
        {
            get
            {
                if ((Handle.Attributes & fFetchAttributes.envelope) == 0) Client.Fetch(MailboxId, Handle, fFetchAttributes.envelope);
                if ((Handle.Attributes & fFetchAttributes.envelope) == 0) throw new cFetchAttributeException();
                return Handle.Envelope.Sender;
            }
        }

        public cAddresses ReplyTo
        {
            get
            {
                if ((Handle.Attributes & fFetchAttributes.envelope) == 0) Client.Fetch(MailboxId, Handle, fFetchAttributes.envelope);
                if ((Handle.Attributes & fFetchAttributes.envelope) == 0) throw new cFetchAttributeException();
                return Handle.Envelope.ReplyTo;
            }
        }

        public cAddresses To
        {
            get
            {
                if ((Handle.Attributes & fFetchAttributes.envelope) == 0) Client.Fetch(MailboxId, Handle, fFetchAttributes.envelope);
                if ((Handle.Attributes & fFetchAttributes.envelope) == 0) throw new cFetchAttributeException();
                return Handle.Envelope.To;
            }
        }

        public cAddresses CC
        {
            get
            {
                if ((Handle.Attributes & fFetchAttributes.envelope) == 0) Client.Fetch(MailboxId, Handle, fFetchAttributes.envelope);
                if ((Handle.Attributes & fFetchAttributes.envelope) == 0) throw new cFetchAttributeException();
                return Handle.Envelope.CC;
            }
        }

        public cAddresses BCC
        {
            get
            {
                if ((Handle.Attributes & fFetchAttributes.envelope) == 0) Client.Fetch(MailboxId, Handle, fFetchAttributes.envelope);
                if ((Handle.Attributes & fFetchAttributes.envelope) == 0) throw new cFetchAttributeException();
                return Handle.Envelope.BCC;
            }
        }

        public string InReplyTo
        {
            get
            {
                if ((Handle.Attributes & fFetchAttributes.envelope) == 0) Client.Fetch(MailboxId, Handle, fFetchAttributes.envelope);
                if ((Handle.Attributes & fFetchAttributes.envelope) == 0) throw new cFetchAttributeException();
                return Handle.Envelope.InReplyTo;
            }
        }

        public string MessageId
        {
            get
            {
                if ((Handle.Attributes & fFetchAttributes.envelope) == 0) Client.Fetch(MailboxId, Handle, fFetchAttributes.envelope);
                if ((Handle.Attributes & fFetchAttributes.envelope) == 0) throw new cFetchAttributeException();
                return Handle.Envelope.MessageId;
            }
        }

        public cMessageFlags Flags
        {
            get
            {
                if ((Handle.Attributes & fFetchAttributes.flags) == 0) Client.Fetch(MailboxId, Handle, fFetchAttributes.flags);
                if ((Handle.Attributes & fFetchAttributes.flags) == 0) throw new cFetchAttributeException();
                return Handle.Flags;
            }
        }

        public bool FlagsContain(params string[] pFlags) => ZFlagsContain(pFlags);
        public bool FlagsContain(IEnumerable<string> pFlags) => ZFlagsContain(pFlags);

        private bool ZFlagsContain(IEnumerable<string> pFlags)
        {
            if ((Handle.Attributes & fFetchAttributes.flags) == 0) Client.Fetch(MailboxId, Handle, fFetchAttributes.flags);
            if ((Handle.Attributes & fFetchAttributes.flags) == 0) throw new cFetchAttributeException();
            return Handle.Flags.Contain(pFlags);
        }

        public bool IsAnswered => ZFlagsContain(fKnownMessageFlags.answered);
        public bool IsFlagged => ZFlagsContain(fKnownFlags.flagged);
        public bool IsDeleted => ZFlagsContain(fKnownFlags.deleted);
        public bool IsSeen => ZFlagsContain(fKnownFlags.seen);
        public bool IsDraft => ZFlagsContain(fKnownFlags.draft);
        public bool IsRecent => ZFlagsContain(fKnownFlags.recent);

        public bool IsMDNSent => ZFlagsContain(fKnownFlags.mdnsent);
        public bool IsForwarded => ZFlagsContain(fKnownFlags.forwarded);
        public bool IsSubmitPending => ZFlagsContain(fKnownFlags.submitpending);
        public bool IsSubmitted => ZFlagsContain(fKnownFlags.submitted);

        private bool ZFlagsContain(fKnownMessageFlags pFlag)
        {
            if ((Handle.Attributes & fFetchAttributes.flags) == 0) Client.Fetch(MailboxId, Handle, fFetchAttributes.flags);
            if ((Handle.Attributes & fFetchAttributes.flags) == 0) throw new cFetchAttributeException();
            return (Handle.Flags.KnownFlags & pFlag) != 0;
        }

        public DateTime Received
        {
            get
            {
                if ((Handle.Attributes & fFetchAttributes.received) == 0) Client.Fetch(MailboxId, Handle, fFetchAttributes.received);
                if ((Handle.Attributes & fFetchAttributes.received) == 0) throw new cFetchAttributeException();
                return Handle.Received.Value;
            }
        }

        public uint Size
        {
            get
            {
                if ((Handle.Attributes & fFetchAttributes.size) == 0) Client.Fetch(MailboxId, Handle, fFetchAttributes.size);
                if ((Handle.Attributes & fFetchAttributes.size) == 0) throw new cFetchAttributeException();
                return Handle.Size.Value;
            }
        }

        public cUID UID
        {
            get
            {
                if ((Handle.Attributes & fFetchAttributes.uid) == 0) Client.Fetch(MailboxId, Handle, fFetchAttributes.uid);
                if ((Handle.Attributes & fFetchAttributes.uid) == 0) throw new cFetchAttributeException();
                return Handle.UID;
            }
        }

        public cStrings References
        {
            get
            {
                if ((Handle.Attributes & fFetchAttributes.references) == 0) Client.Fetch(MailboxId, Handle, fFetchAttributes.references);
                if ((Handle.Attributes & fFetchAttributes.references) == 0) throw new cFetchAttributeException();
                return Handle.References;
            }
        }

        public ulong ModSeq
        {
            get
            {
                if ((Handle.Attributes & fFetchAttributes.modseq) == 0) Client.Fetch(MailboxId, Handle, fFetchAttributes.modseq);
                if ((Handle.Attributes & fFetchAttributes.modseq) == 0) throw new cFetchAttributeException();
                return Handle.ModSeq.Value;
            }
        }

        public string PlainText
        {
            get
            {
                if ((Handle.Attributes & fFetchAttributes.bodystructure) == 0) Client.Fetch(MailboxId, Handle, fFetchAttributes.bodystructure);
                if ((Handle.Attributes & fFetchAttributes.bodystructure) == 0) throw new cFetchAttributeException();
                StringBuilder lBuilder = new StringBuilder();
                foreach (var lPart in ZPlainText(Handle.BodyStructure)) lBuilder.Append(Fetch(lPart));
                return lBuilder.ToString();
            }
        }

        public async Task<string> GetPlainTextAsync()
        {
            if ((Handle.Attributes & fFetchAttributes.bodystructure) == 0) await Client.FetchAsync(MailboxId, Handle, fFetchAttributes.bodystructure).ConfigureAwait(false);
            if ((Handle.Attributes & fFetchAttributes.bodystructure) == 0) throw new cFetchAttributeException();
            StringBuilder lBuilder = new StringBuilder();
            foreach (var lPart in ZPlainText(Handle.BodyStructure)) lBuilder.Append(await FetchAsync(lPart).ConfigureAwait(false));
            return lBuilder.ToString();
        }

        private List<cBodyPart> ZPlainText(cBodyPart pPart)
        {
            // TODO: when we know what languages the user is interested in (on implementation of languages) choose from multipart/alternative options based on language tag

            List<cBodyPart> lResult = new List<cBodyPart>();

            if (pPart.Disposition?.TypeCode == eDispositionTypeCode.attachment) return lResult;

            if (pPart is cTextBodyPart lTextPart)
            {
                if (lTextPart.SubTypeCode == eTextBodyPartSubTypeCode.plain) lResult.Add(pPart);
            }
            else if (pPart is cMultiPartBody lMultiPart)
            {
                foreach (var lPart in lMultiPart.Parts)
                {
                    var lParts = ZPlainText(lPart);
                    lResult.AddRange(lParts);
                    if (lParts.Count > 0 && lMultiPart.SubTypeCode == eMultiPartBodySubTypeCode.alternative) break;
                }
            }

            return lResult;
        }

        public List<cAttachment> Attachments
        {
            get
            {
                if ((Handle.Attributes & fFetchAttributes.bodystructure) == 0) Client.Fetch(MailboxId, Handle, fFetchAttributes.bodystructure);
                if ((Handle.Attributes & fFetchAttributes.bodystructure) == 0) throw new cFetchAttributeException();
                return ZAttachments(Handle.BodyStructure);
            }
        }

        public async Task<List<cAttachment>> GetAttachmentsAsync()
        {
            if ((Handle.Attributes & fFetchAttributes.bodystructure) == 0) await Client.FetchAsync(MailboxId, Handle, fFetchAttributes.bodystructure).ConfigureAwait(false);
            if ((Handle.Attributes & fFetchAttributes.bodystructure) == 0) throw new cFetchAttributeException();
            return ZAttachments(Handle.BodyStructure);
        }

        private List<cAttachment> ZAttachments(cBodyPart pPart)
        {
            // TODO: when we know what languages the user is interested in (on implementation of languages) choose from multipart/alternative options based on language tag

            List<cAttachment> lResult = new List<cAttachment>();

            if (pPart is cSinglePartBody lSinglePart)
            {
                if (lSinglePart.Disposition?.TypeCode == eDispositionTypeCode.attachment) lResult.Add(new cAttachment(Client, MailboxId, Handle, lSinglePart));
            }
            else if (pPart.Disposition?.TypeCode != eDispositionTypeCode.attachment && pPart is cMultiPartBody lMultiPart)
            {
                foreach (var lPart in lMultiPart.Parts)
                {
                    var lAttachments = ZAttachments(lPart);
                    lResult.AddRange(lAttachments);
                    if (lAttachments.Count > 0 && lMultiPart.SubTypeCode == eMultiPartBodySubTypeCode.alternative) break;
                }
            }

            return lResult;
        }

        // get data

        public void Fetch(fMessageProperties pProperties) => Client.Fetch(MailboxId, Handle, pProperties);

        public Task FetchAsync(fMessageProperties pProperties) => Client.FetchAsync(MailboxId, Handle, pProperties);

        public string Fetch(cBodyPart pPart)
        {
            using (var lStream = new MemoryStream())
            {
                if (pPart is cTextBodyPart lPart)
                {
                    Client.Fetch(MailboxId, Handle, lPart.Section, lPart.DecodingRequired, lStream, null);
                    Encoding lEncoding = Encoding.GetEncoding(lPart.Charset);
                    return new string(lEncoding.GetChars(lStream.GetBuffer(), 0, (int)lStream.Length));
                }

                Client.Fetch(MailboxId, Handle, pPart.Section, eDecodingRequired.none, lStream, null);
                return new string(Encoding.UTF8.GetChars(lStream.GetBuffer(), 0, (int)lStream.Length));
            }
        }

        public async Task<string> FetchAsync(cBodyPart pPart)
        {
            using (var lStream = new MemoryStream())
            {
                if (pPart is cTextBodyPart lPart)
                {
                    await Client.FetchAsync(MailboxId, Handle, lPart.Section, lPart.DecodingRequired, lStream, null).ConfigureAwait(false);
                    Encoding lEncoding = Encoding.GetEncoding(lPart.Charset);
                    return new string(lEncoding.GetChars(lStream.GetBuffer(), 0, (int)lStream.Length));
                }

                await Client.FetchAsync(MailboxId, Handle, pPart.Section, eDecodingRequired.none, lStream, null).ConfigureAwait(false);
                return new string(Encoding.UTF8.GetChars(lStream.GetBuffer(), 0, (int)lStream.Length));
            }
        }

        public void Fetch(cBodyPart pPart, Stream pStream, cFetchControl pFC = null)
        {
            if (pPart is cSinglePartBody lPart) Client.Fetch(MailboxId, Handle, lPart.Section, lPart.DecodingRequired, pStream, pFC);
            else Client.Fetch(MailboxId, Handle, pPart.Section, eDecodingRequired.none, pStream, pFC);
        }

        public Task FetchAsync(cBodyPart pPart, Stream pStream, cFetchControl pFC = null)
        {
            if (pPart is cSinglePartBody lPart) return Client.FetchAsync(MailboxId, Handle, lPart.Section, lPart.DecodingRequired, pStream, pFC);
            else return Client.FetchAsync(MailboxId, Handle, pPart.Section, eDecodingRequired.none, pStream, pFC);
        }

        public string Fetch(cSection pSection)
        {
            using (var lStream = new MemoryStream())
            {
                Client.Fetch(MailboxId, Handle, pSection, eDecodingRequired.none, lStream, null);
                return new string(Encoding.UTF8.GetChars(lStream.GetBuffer(), 0, (int)lStream.Length));
            }
        }

        public async Task<string> FetchAsync(cSection pSection)
        {
            using (var lStream = new MemoryStream())
            {
                await Client.FetchAsync(MailboxId, Handle, pSection, eDecodingRequired.none, lStream, null).ConfigureAwait(false);
                return new string(Encoding.UTF8.GetChars(lStream.GetBuffer(), 0, (int)lStream.Length));
            }
        }

        public void Fetch(cSection pSection, eDecodingRequired pDecoding, Stream pStream, cFetchControl pFC = null) => Client.Fetch(MailboxId, Handle, pSection, pDecoding, pStream, pFC);

        public Task FetchAsync(cSection pSection, eDecodingRequired pDecoding, Stream pStream, cFetchControl pFC = null) => Client.FetchAsync(MailboxId, Handle, pSection, pDecoding, pStream, pFC);

        // debugging
        public override string ToString() => $"{nameof(cMessage)}({MailboxId},{Handle},{Indent})";
    }
}