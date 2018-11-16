using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using work.bacome.imapclient.support;

namespace work.bacome.mailclient
{
        /* TEMP comment out for cachefile work
    public sealed class cStreamMessage : cMailMessage, IDisposable
    {
        // note: use exclusive access to the stream in the fetch APIs => disposable

        private bool mDisposed = false;
        private readonly Stream mStream;
        private readonly cEnvelope mEnvelope;
        ;?; // headers
        private readonly cBodyPart mBodyStructure;
        ;?; // parts
        private readonly uint mSize;
        private readonly eMessageDataFormat mFormat;

        private cStreamMessage(Stream pStream, cEnvelope pEnvelope, cBodyPart pBodyStructure, uint pSize, eMessageDataFormat pFormat)
        {
            mStream = pStream ?? throw new ArgumentNullException(nameof(pStream));
            mEnvelope = pEnvelope ?? throw new ArgumentNullException(nameof(pEnvelope));
            mBodyStructure = pBodyStructure ?? throw new ArgumentNullException(nameof(pBodyStructure));
            mSize = pSize;
            mFormat = pFormat;
        }

        public override cEnvelope Envelope => mEnvelope;
        public override uint Size => mSize;
        public override cBodyPart BodyStructure => mBodyStructure;
        public override eMessageDataFormat Format => mFormat;

        public override List<cMailAttachment> Attachments
        {
            get
            {
                var lAttachments = new List<cLocalAttachment>();
                foreach (var lPart in YAttachmentParts(mBodyStructure)) lAttachments.Add(new cLocalAttachment(this, lPart));
                return lAttachments;
            }
        }







        internal static async Task<cStreamMessage> ParseAsync(cMethodControl pMC, Stream pStream)
        {
            if (pStream == null) throw new ArgumentNullException(nameof(pStream));
            if (!pStream.CanSeek) throw new ArgumentOutOfRangeException(nameof(pStream));

            pStream.Position = 0;

            ;?;



        }


        public static async Task<cStreamMessage> ParseAsync(Stream pStream, int pTimeout = Timeout.infinite, cancellatio)
        {
            if (pStream == null) throw new ArgumentNullException(nameof(pStream));
            if (!pStream.CanSeek) throw new ArgumentOutOfRangeException(nameof(pStream));

            pStream.Position = 0;

            ;?;



        }
    } */
}
