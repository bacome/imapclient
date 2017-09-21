using System;
using System.Collections.Generic;

namespace work.bacome.imapclient.support
{
    public partial class cBytesCursor
    {
        private const string kGetFlagsSlashAsteriskString = @"\*";
        private static readonly cBytes kGetFlagsSlashAsteriskBytes = new cBytes(kGetFlagsSlashAsteriskString);

        public bool GetFlags(out List<string> rFlags)
        {
            var lBookmark = Position;

            if (!SkipByte(cASCII.LPAREN)) { rFlags = null; return false; }

            rFlags = new List<string>();

            while (true)
            {
                if (SkipBytes(kGetFlagsSlashAsteriskBytes)) rFlags.Add(kGetFlagsSlashAsteriskString);
                else
                {
                    if (SkipByte(cASCII.BACKSL))
                    {
                        if (!GetToken(cCharset.Atom, null, null, out cByteList lAtom))
                        {
                            Position = lBookmark;
                            rFlags = null;
                            return false;
                        }

                        rFlags.Add(cTools.ASCIIBytesToString('\\', lAtom));
                    }
                    else
                    {
                        if (!GetToken(cCharset.Atom, null, null, out cByteList lAtom)) break;
                        rFlags.Add(cTools.ASCIIBytesToString(lAtom));
                    }
                }

                if (!SkipByte(cASCII.SPACE)) break;
            }

            if (!SkipByte(cASCII.RPAREN))
            {
                Position = lBookmark;
                rFlags = null;
                return false;
            }

            return true;
        }
    }
}