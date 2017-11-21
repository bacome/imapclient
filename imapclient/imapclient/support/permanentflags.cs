using System;
using System.Collections.Generic;
using System.Linq;

namespace work.bacome.imapclient
{
    internal class cPermanentFlags : cMessageFlags
    {
        // read only wrapper (for passing out)

        private cPermanentFlags(cPermanentFlagList pFlags) : base(pFlags) { }

        public static bool TryConstruct(IEnumerable<string> pFlags, out cPermanentFlags rFlags)
        {
            if (!cPermanentFlagList.TryConstruct(pFlags, out var lFlags)) { rFlags = null; return false; }
            rFlags = new cPermanentFlags(lFlags);
            return true;
        }
    }

    internal class cPermanentFlagList : cMessageFlagList
    {
        private cPermanentFlagList(List<string> pFlags) : base(pFlags) { } // wraps

        protected override bool YIsValidFlag(string pFlag) => ZIsValidFlag(pFlag);

        private static bool ZIsValidFlag(string pFlag)
        {
            if (pFlag == null) return false;
            if (pFlag.Length == 0) return false;

            if (pFlag == kMessageFlag.CreateNewIsPossible) return true;

            string lFlag;
            if (pFlag[0] == '\\') lFlag = pFlag.Remove(0, 1);
            else lFlag = pFlag;

            return cCommandPartFactory.TryAsAtom(lFlag, out _);
        }

        public static bool TryConstruct(IEnumerable<string> pFlags, out cPermanentFlagList rFlags)
        {
            if (pFlags == null) { rFlags = null; return false; }
            foreach (var lFlag in pFlags) if (!ZIsValidFlag(lFlag)) { rFlags = null; return false; }
            rFlags = new cPermanentFlagList(new List<string>(pFlags.Distinct(StringComparer.InvariantCultureIgnoreCase)));
            return true;
        }
    }
}