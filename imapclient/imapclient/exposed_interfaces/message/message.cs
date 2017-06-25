using System;
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

        public fMessageProperties Properties => Handle.Properties;

        // may return null if the property hasn't been retrieved yet
        public cBodyPart BodyStructure => Handle.BodyStructure;
        public cBodyPart BodyStructureEx => Handle.BodyStructureEx;
        public cEnvelope Envelope => Handle.Envelope;
        public cMessageFlags Flags => Handle.Flags;
        public DateTime? Received => Handle.Received;
        public uint? Size => Handle.Size;
        public cUID UID => Handle.UID;
        public cStrings References => Handle.References;

        // get data
        public void Fetch(fMessageProperties pProperties) => Client.Fetch(MailboxId, Handle, pProperties);
        public Task FetchAsync(fMessageProperties pProperties) => Client.FetchAsync(MailboxId, Handle, pProperties);

        public string Fetch(cBodyPart pPart)
        {
            using (var lStream = new MemoryStream())
            {
                if (pPart is cTextBodyPart lPart)
                {
                    Client.Fetch(MailboxId, Handle, lPart.Section, lPart.DecodingRequired, lStream);
                    Encoding lEncoding = Encoding.GetEncoding(lPart.Charset);
                    return new string(lEncoding.GetChars(lStream.GetBuffer(), 0, (int)lStream.Length));
                }

                Client.Fetch(MailboxId, Handle, pPart.Section, eDecodingRequired.none, lStream);
                return new string(Encoding.UTF8.GetChars(lStream.GetBuffer(), 0, (int)lStream.Length));
            }
        }

        public async Task<string> FetchAsync(cBodyPart pPart)
        {
            using (var lStream = new MemoryStream())
            {
                if (pPart is cTextBodyPart lPart)
                {
                    await Client.FetchAsync(MailboxId, Handle, lPart.Section, lPart.DecodingRequired, lStream);
                    Encoding lEncoding = Encoding.GetEncoding(lPart.Charset);
                    return new string(lEncoding.GetChars(lStream.GetBuffer(), 0, (int)lStream.Length));
                }

                await Client.FetchAsync(MailboxId, Handle, pPart.Section, eDecodingRequired.none, lStream);
                return new string(Encoding.UTF8.GetChars(lStream.GetBuffer(), 0, (int)lStream.Length));
            }
        }

        public void Fetch(cBodyPart pPart, Stream pStream)
        {
            if (pPart is cSinglePartBody lPart) Client.Fetch(MailboxId, Handle, lPart.Section, lPart.DecodingRequired, pStream);
            else Client.Fetch(MailboxId, Handle, pPart.Section, eDecodingRequired.none, pStream);
        }

        public Task FetchAsync(cBodyPart pPart, Stream pStream)
        {
            if (pPart is cSinglePartBody lPart) return Client.FetchAsync(MailboxId, Handle, lPart.Section, lPart.DecodingRequired, pStream);
            else return Client.FetchAsync(MailboxId, Handle, pPart.Section, eDecodingRequired.none, pStream);
        }

        public string Fetch(cSection pSection)
        {
            using (var lStream = new MemoryStream())
            {
                Client.Fetch(MailboxId, Handle, pSection, eDecodingRequired.none, lStream);
                return new string(Encoding.UTF8.GetChars(lStream.GetBuffer(), 0, (int)lStream.Length));
            }
        }

        public async Task<string> FetchAsync(cSection pSection)
        {
            using (var lStream = new MemoryStream())
            {
                await Client.FetchAsync(MailboxId, Handle, pSection, eDecodingRequired.none, lStream);
                return new string(Encoding.UTF8.GetChars(lStream.GetBuffer(), 0, (int)lStream.Length));
            }
        }

        public void Fetch(cSection pSection, eDecodingRequired pDecoding, Stream pStream) => Client.Fetch(MailboxId, Handle, pSection, pDecoding, pStream);
        public Task FetchAsync(cSection pSection, eDecodingRequired pDecoding, Stream pStream) => Client.FetchAsync(MailboxId, Handle, pSection, pDecoding, pStream);

        public override string ToString() => $"{nameof(cMessage)}({MailboxId},{Handle},{Indent})";
    }
}