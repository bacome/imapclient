using System;

namespace work.bacome.imapclient.support
{
    public partial class cBytesCursor
    {
        private const string kGetFlagsSlashAsteriskString = @"\*";
        private static readonly cBytes kGetFlagsSlashAsteriskBytes = new cBytes(kGetFlagsSlashAsteriskString);

        public bool GetFlags(out cFlags rFlags)
        {
            var lBookmark = Position;

            if (!SkipByte(cASCII.LPAREN)) { rFlags = null; return false; }

            rFlags = new cFlags();

            while (true)
            {
                if (SkipBytes(kGetFlagsSlashAsteriskBytes)) rFlags.Set(kGetFlagsSlashAsteriskString);
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

                        rFlags.Set(cTools.ASCIIBytesToString(cASCII.BACKSL, lAtom));
                    }
                    else
                    {
                        if (!GetToken(cCharset.Atom, null, null, out cByteList lAtom)) break;
                        rFlags.Set(cTools.ASCIIBytesToString(lAtom));
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