using System;
using work.bacome.mailclient;
using work.bacome.mailclient.support;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    internal class cNoUIDHeaderCacheItem : iHeaderCacheItem
    {
        private cEnvelope mEnvelope = null;
        private cTimestamp mReceived = null;
        private uint? mSize = null;
        private cBodyPart mBodyStructure = null;
        private cHeaderFields mHeaderFields = null;
        private cBinarySizes mBinarySizes = null;

        public cEnvelope Envelope => mEnvelope;
        public cTimestamp Received => mReceived;
        public uint? Size => mSize;
        public cBodyPart BodyStructure => mBodyStructure;
        public cHeaderFields HeaderFields => mHeaderFields;
        public cBinarySizes BinarySizes => mBinarySizes;

        public void Update(iHeaderDataItem pHeaderDataItem, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cNoUIDHeaderCacheItem), nameof(Update), pHeaderDataItem);

            if (mEnvelope == null) mEnvelope = pHeaderDataItem.Envelope;
            if (mReceived == null) mReceived = pHeaderDataItem.Received;
            if (mSize == null) mSize = pHeaderDataItem.Size;
            if (mBodyStructure == null) mBodyStructure = pHeaderDataItem.BodyStructure;
            mHeaderFields += pHeaderDataItem.HeaderFields;
            mBinarySizes += pHeaderDataItem.BinarySizes;
        }
    }
}