using System;
using System.Collections.Generic;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private class cBody
        {
            public readonly bool Binary; // note that if binary is true then the section (if present) can only have a part
            public readonly cSection Section;
            public readonly uint? Origin;
            public readonly cBytes Bytes; // may be null

            public cBody(bool pBinary, cSection pSection, uint? pOrigin, IList<byte> pBytes)
            {
                Binary = pBinary;
                Section = pSection ?? throw new ArgumentNullException(nameof(pSection));
                Origin = pOrigin;

                if (pBytes == null) Bytes = null;
                else Bytes = new cBytes(pBytes);
            }

            public override string ToString() => $"{nameof(cBody)}({Binary},{Section},{Origin},{Bytes})";
        }
    }
}