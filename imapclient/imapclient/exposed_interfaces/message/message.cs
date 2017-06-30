using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace work.bacome.imapclient
{
    public class cMessage
    {
        private EventHandler mExpunged;
        private object mExpungedLock = new object();

        private EventHandler<cPropertiesSetEventArgs> mPropertiesSet;
        private object mPropertiesSetLock = new object();

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

        public event EventHandler Expunged
        {
            add
            {
                lock (mExpungedLock)
                {
                    if (mExpunged == null) Client.MessageExpunged += ZMessageExpunged;
                    mExpunged += value;
                }
            }

            remove
            {
                lock (mExpungedLock)
                {
                    mExpunged -= value;
                    if (mExpunged == null) Client.MessageExpunged -= ZMessageExpunged;
                }
            }
        }

        private void ZMessageExpunged(object pSender, cMessageExpungedEventArgs pArgs)
        {
            if (pArgs.MailboxId == MailboxId && ReferenceEquals(pArgs.Handle, Handle)) mExpunged?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler<cPropertiesSetEventArgs> PropertiesSet
        {
            add
            {
                lock (mPropertiesSetLock)
                {
                    if (mPropertiesSet == null) Client.MessagePropertiesSet += ZMessagePropertiesSet;
                    mPropertiesSet += value;
                }
            }

            remove
            {
                lock (mPropertiesSetLock)
                {
                    mPropertiesSet -= value;
                    if (mPropertiesSet == null) Client.MessagePropertiesSet -= ZMessagePropertiesSet;
                }
            }
        }

        private void ZMessagePropertiesSet(object pSender, cMessagePropertiesSetEventArgs pArgs)
        {
            if (pArgs.MailboxId == MailboxId && ReferenceEquals(pArgs.Handle, Handle)) mPropertiesSet?.Invoke(this, pArgs);
        }

        public bool IsExpunged => Handle.Expunged;

        public cBodyPart BodyStructure
        {
            get
            {
                if ((Handle.Properties & fMessageProperties.bodystructure) == 0) Client.Fetch(MailboxId, Handle, fMessageProperties.bodystructure);
                return Handle.BodyStructure;
            }
        }

        public DateTime? Sent
        {
            get
            {
                if ((Handle.Properties & fMessageProperties.envelope) == 0) Client.Fetch(MailboxId, Handle, fMessageProperties.envelope);
                return Handle.Envelope?.Sent; // note that if the message has been deleted the envelope still might not be there
            }
        }

        public cCulturedString Subject
        {
            get
            {
                if ((Handle.Properties & fMessageProperties.envelope) == 0) Client.Fetch(MailboxId, Handle, fMessageProperties.envelope);
                return Handle.Envelope?.Subject; // note that if the message has been deleted the envelope still might not be there
            }
        }

        public cAddresses From
        {
            get
            {
                if ((Handle.Properties & fMessageProperties.envelope) == 0) Client.Fetch(MailboxId, Handle, fMessageProperties.envelope);
                return Handle.Envelope?.From; // note that if the message has been deleted the envelope still might not be there
            }
        }

        public cAddresses Sender
        {
            get
            {
                if ((Handle.Properties & fMessageProperties.envelope) == 0) Client.Fetch(MailboxId, Handle, fMessageProperties.envelope);
                return Handle.Envelope?.Sender; // note that if the message has been deleted the envelope still might not be there
            }
        }

        public cAddresses ReplyTo
        {
            get
            {
                if ((Handle.Properties & fMessageProperties.envelope) == 0) Client.Fetch(MailboxId, Handle, fMessageProperties.envelope);
                return Handle.Envelope?.ReplyTo; // note that if the message has been deleted the envelope still might not be there
            }
        }

        public cAddresses To
        {
            get
            {
                if ((Handle.Properties & fMessageProperties.envelope) == 0) Client.Fetch(MailboxId, Handle, fMessageProperties.envelope);
                return Handle.Envelope?.To; // note that if the message has been deleted the envelope still might not be there
            }
        }

        public cAddresses CC
        {
            get
            {
                if ((Handle.Properties & fMessageProperties.envelope) == 0) Client.Fetch(MailboxId, Handle, fMessageProperties.envelope);
                return Handle.Envelope?.CC; // note that if the message has been deleted the envelope still might not be there
            }
        }

        public cAddresses BCC
        {
            get
            {
                if ((Handle.Properties & fMessageProperties.envelope) == 0) Client.Fetch(MailboxId, Handle, fMessageProperties.envelope);
                return Handle.Envelope?.BCC; // note that if the message has been deleted the envelope still might not be there
            }
        }

        public string InReplyTo
        {
            get
            {
                if ((Handle.Properties & fMessageProperties.envelope) == 0) Client.Fetch(MailboxId, Handle, fMessageProperties.envelope);
                return Handle.Envelope?.InReplyTo; // note that if the message has been deleted the envelope still might not be there
            }
        }

        public string MessageId
        {
            get
            {
                if ((Handle.Properties & fMessageProperties.envelope) == 0) Client.Fetch(MailboxId, Handle, fMessageProperties.envelope);
                return Handle.Envelope?.MessageId; // note that if the message has been deleted the envelope still might not be there
            }
        }

        public fMessageFlags? Flags
        {
            get
            {
                if ((Handle.Properties & fMessageProperties.flags) == 0) Client.Fetch(MailboxId, Handle, fMessageProperties.flags);
                return Handle.Flags?.KnownFlags; // note that if the message has been deleted the flags still might not be there
            }
        }

        public cStrings AllFlags
        {
            get
            {
                if ((Handle.Properties & fMessageProperties.flags) == 0) Client.Fetch(MailboxId, Handle, fMessageProperties.flags);
                return Handle.Flags?.AllFlags; // note that if the message has been deleted the flags still might not be there
            }
        }

        public bool? IsFlagged(fMessageFlags pFlags)
        {
            if ((Handle.Properties & fMessageProperties.flags) == 0) Client.Fetch(MailboxId, Handle, fMessageProperties.flags);
            if (Handle.Flags == null) return null;
            return (Handle.Flags.KnownFlags & pFlags) == pFlags;
        }

        public bool? IsNotFlagged(fMessageFlags pFlags)
        {
            if ((Handle.Properties & fMessageProperties.flags) == 0) Client.Fetch(MailboxId, Handle, fMessageProperties.flags);
            if (Handle.Flags == null) return null;
            return (Handle.Flags.KnownFlags & pFlags) == 0;
        }

        public bool? IsFlagged(string pKeyword)
        {
            if ((Handle.Properties & fMessageProperties.flags) == 0) Client.Fetch(MailboxId, Handle, fMessageProperties.flags);
            if (Handle.Flags == null) return null;
            return Handle.Flags.Has(pKeyword);
        }

        public bool? IsNotFlagged(string pKeyword)
        {
            if ((Handle.Properties & fMessageProperties.flags) == 0) Client.Fetch(MailboxId, Handle, fMessageProperties.flags);
            if (Handle.Flags == null) return null;
            return !Handle.Flags.Has(pKeyword);
        }

        public DateTime? Received
        {
            get
            {
                if ((Handle.Properties & fMessageProperties.received) == 0) Client.Fetch(MailboxId, Handle, fMessageProperties.received);
                return Handle.Received;
            }
        }

        public uint? Size
        {
            get
            {
                if ((Handle.Properties & fMessageProperties.size) == 0) Client.Fetch(MailboxId, Handle, fMessageProperties.size);
                return Handle.Size;
            }
        }

        public cUID UID
        {
            get
            {
                if ((Handle.Properties & fMessageProperties.uid) == 0) Client.Fetch(MailboxId, Handle, fMessageProperties.uid);
                return Handle.UID;
            }
        }

        public cStrings References
        {
            get
            {
                if ((Handle.Properties & fMessageProperties.references) == 0) Client.Fetch(MailboxId, Handle, fMessageProperties.references);
                return Handle.References;
            }
        }

        public string PlainText
        {
            get
            {
                if ((Handle.Properties & fMessageProperties.bodystructure) == 0) Client.Fetch(MailboxId, Handle, fMessageProperties.bodystructure);
                if (Handle.BodyStructure == null) return null;
                StringBuilder lBuilder = new StringBuilder();
                foreach (var lPart in ZPlainText(Handle.BodyStructure)) lBuilder.Append(Fetch(lPart));
                return lBuilder.ToString();
            }
        }

        public async Task<string> GetPlainTextAsync()
        {
            if ((Handle.Properties & fMessageProperties.bodystructure) == 0) await Client.FetchAsync(MailboxId, Handle, fMessageProperties.bodystructure).ConfigureAwait(false);
            if (Handle.BodyStructure == null) return null;
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
                if ((Handle.Properties & fMessageProperties.bodystructure) == 0) Client.Fetch(MailboxId, Handle, fMessageProperties.bodystructure);
                if (Handle.BodyStructure == null) return null;
                return ZAttachments(Handle.BodyStructure);
            }
        }

        public async Task<List<cAttachment>> GetAttachmentsAsync()
        {
            if ((Handle.Properties & fMessageProperties.bodystructure) == 0) await Client.FetchAsync(MailboxId, Handle, fMessageProperties.bodystructure).ConfigureAwait(false);
            if (Handle.BodyStructure == null) return null;
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