using System;
using System.Collections;
using System.Collections.Generic;

namespace work.bacome.imapclient.support
{
    public class cFlags : IEnumerable<string>
    {
        private readonly Dictionary<string, bool> mDictionary = new Dictionary<string, bool>(StringComparer.InvariantCultureIgnoreCase);

        public cFlags() { }

        public bool Has(string pFlag) => mDictionary.ContainsKey(pFlag);

        public void Set(string pFlag)
        {
            if (!mDictionary.ContainsKey(pFlag)) mDictionary.Add(pFlag, true);
        }

        public IEnumerator<string> GetEnumerator() => mDictionary.Keys.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => mDictionary.Keys.GetEnumerator();

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cFlags));
            foreach (var lFlag in mDictionary.Keys) lBuilder.Append(lFlag);
            return lBuilder.ToString();
        }
    }

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