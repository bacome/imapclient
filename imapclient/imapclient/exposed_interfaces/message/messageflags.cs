using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public interface iSettableFlags : IReadOnlyCollection<string>
    {
        bool ContainsAnswered { get; }
        bool ContainsFlagged { get; }
        bool ContainsDeleted { get; }
        bool ContainsSeen { get; }
        bool ContainsDraft { get; }

        bool ContainsMDNSent { get; }
        bool ContainsForwarded { get; }
        bool ContainsSubmitPending { get; }
        bool ContainsSubmitted { get; }

        bool Contains(string pFlag);
        bool Contains(params string[] pFlags);
        bool Contains(IEnumerable<string> pFlags);
    }

    public interface iMessageFlags : iSettableFlags
    {
        bool ContainsRecent { get; }
    }

    public abstract class cMessageFlagConstants
    {
        public const string Asterisk = @"\*";
        public const string Recent = @"\ReCeNt";

        public const string Answered = @"\AnSwErEd";
        public const string Flagged = @"\FlAgGeD";
        public const string Deleted = @"\DeLeTeD";
        public const string Seen = @"\SeEn";
        public const string Draft = @"\DrAfT";

        // rfc 5788/ 5550
        public const string Forwarded = "$Forwarded";
        public const string SubmitPending = "$SubmitPending";
        public const string Submitted = "$Submitted";

        // rfc 3503/ 5550
        public const string MDNSent = "$MdNsEnT";
    }

    public class cSettableFlags : cMessageFlagConstants, iSettableFlags
    {
        // immutable (for passing in)

        private readonly cSettableFlagList mFlags;

        public cSettableFlags(params string[] pFlags) => mFlags = new cSettableFlagList(pFlags);
        public cSettableFlags(IEnumerable<string> pFlags) => mFlags = new cSettableFlagList(pFlags);
        public cSettableFlags(cSettableFlagList pFlags) => mFlags = new cSettableFlagList(pFlags);

        public bool ContainsAnswered => mFlags.ContainsAnswered;
        public bool ContainsFlagged => mFlags.ContainsFlagged;
        public bool ContainsDeleted => mFlags.ContainsDeleted;
        public bool ContainsSeen => mFlags.ContainsSeen;
        public bool ContainsDraft => mFlags.ContainsDraft;

        public bool ContainsMDNSent => mFlags.ContainsMDNSent;
        public bool ContainsForwarded => mFlags.ContainsForwarded;
        public bool ContainsSubmitPending => mFlags.ContainsSubmitPending;
        public bool ContainsSubmitted => mFlags.ContainsSubmitted;

        public bool Contains(string pFlag) => mFlags.Contains(pFlag);
        public bool Contains(params string[] pFlags) => mFlags.Contains(pFlags);
        public bool Contains(IEnumerable<string> pFlags) => mFlags.Contains(pFlags);

        public int Count => mFlags.Count;
        public IEnumerator<string> GetEnumerator() => mFlags.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => mFlags.GetEnumerator();

        public override string ToString() => mFlags.ToString();

        public static implicit operator cSettableFlags(cSettableFlagList pFlags) => new cSettableFlags(pFlags);
    }

    public class cMessageFlags : cMessageFlagConstants, iMessageFlags
    {
        // immutable (for passing in and out)

        private readonly cMessageFlagList mFlags;

        public cMessageFlags(params string[] pFlags) => mFlags = new cMessageFlagList(pFlags);
        public cMessageFlags(IEnumerable<string> pFlags) => mFlags = new cMessageFlagList(pFlags);
        public cMessageFlags(cMessageFlagList pFlags) => mFlags = new cMessageFlagList(pFlags);

        public bool ContainsRecent => mFlags.ContainsRecent;

        public bool ContainsAnswered => mFlags.ContainsAnswered;
        public bool ContainsFlagged => mFlags.ContainsFlagged;
        public bool ContainsDeleted => mFlags.ContainsDeleted;
        public bool ContainsSeen => mFlags.ContainsSeen;
        public bool ContainsDraft => mFlags.ContainsDraft;

        public bool ContainsMDNSent => mFlags.ContainsMDNSent;
        public bool ContainsForwarded => mFlags.ContainsForwarded;
        public bool ContainsSubmitPending => mFlags.ContainsSubmitPending;
        public bool ContainsSubmitted => mFlags.ContainsSubmitted;

        public bool Contains(string pFlag) => mFlags.Contains(pFlag);
        public bool Contains(params string[] pFlags) => mFlags.Contains(pFlags);
        public bool Contains(IEnumerable<string> pFlags) => mFlags.Contains(pFlags);

        public int Count => mFlags.Count;
        public IEnumerator<string> GetEnumerator() => mFlags.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => mFlags.GetEnumerator();

        public override string ToString() => mFlags.ToString();

        public static bool TryConstruct(IEnumerable<string> pFlags, out cMessageFlags rFlags)
        {
            if (!cBaseFlagList.TryConstruct(false, true, pFlags, out var lFlags)) { rFlags = null; return false; }
            ;?;
            rFlags = new cMessageFlags(lFlags);
            return true;
        }


        /*

        public static fMessageProperties Differences(cMessageFlags pOld, cMessageFlags pNew)
        {
            if (pNew == null) throw new ArgumentNullException(nameof(pNew));
            if (pOld == null) return 0;

            fMessageProperties lProperties = 0;

            if (pOld.Count != pNew.Count || !pOld.Contains(pNew)) lProperties |= fMessageProperties.flags;

            lProperties |= ZPropertyIfDifferent(pOld, pNew, Answered, fMessageProperties.isanswered);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, Flagged, fMessageProperties.isflagged);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, Deleted, fMessageProperties.isdeleted);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, Seen, fMessageProperties.isseen);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, Draft, fMessageProperties.isdraft);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, Recent, fMessageProperties.isrecent);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, MDNSent, fMessageProperties.ismdnsent);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, Forwarded, fMessageProperties.isforwarded);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, SubmitPending, fMessageProperties.issubmitpending);
            lProperties |= ZPropertyIfDifferent(pOld, pNew, Submitted, fMessageProperties.issubmitted);

            return lProperties;
        }

        private static fMessageProperties ZPropertyIfDifferent(cMessageFlags pA, cMessageFlags pB, string pFlag, fMessageProperties pProperty)
        {
            if (pA.Contains(pFlag) == pB.Contains(pFlag)) return 0;
            return pProperty;
        } */




    }

    public class cPermanentFlags : cMessageFlagConstants, iMessageFlags
    {
        // read only wrapper (for passing out)
    
            ;?;
        private readonly cMessageFlagList mFlags;

        private cPermanentFlags(cMessageFlagList pFlags) { mFlags = pFlags; }

        public bool CreateNewIsPossible => mFlags.Contains(Asterisk);

        public bool ContainsRecent => mFlags.ContainsRecent;

        public bool ContainsAnswered => mFlags.ContainsAnswered;
        public bool ContainsFlagged => mFlags.ContainsFlagged;
        public bool ContainsDeleted => mFlags.ContainsDeleted;
        public bool ContainsSeen => mFlags.ContainsSeen;
        public bool ContainsDraft => mFlags.ContainsDraft;

        public bool ContainsMDNSent => mFlags.ContainsMDNSent;
        public bool ContainsForwarded => mFlags.ContainsForwarded;
        public bool ContainsSubmitPending => mFlags.ContainsSubmitPending;
        public bool ContainsSubmitted => mFlags.ContainsSubmitted;

        public bool Contains(string pFlag) => mFlags.Contains(pFlag);
        public bool Contains(params string[] pFlags) => mFlags.Contains(pFlags);
        public bool Contains(IEnumerable<string> pFlags) => mFlags.Contains(pFlags);

        public int Count => mFlags.Count;
        public IEnumerator<string> GetEnumerator() => mFlags.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => mFlags.GetEnumerator();

        public override string ToString() => mFlags.ToString();

        public static bool TryConstruct(IEnumerable<string> pFlags, out cPermanentFlags rFlags)
        {
            if (!cBaseFlagList.TryConstruct(true, true, pFlags, out var lFlags)) { rFlags = null; return false; }
            rFlags = new cPermanentFlags(lFlags);
            return true;
        }
    }

    public class cMessageFlagList : cMessageFlagConstants, IReadOnlyCollection<string>
    {
        // implements the fact that asterisk and recent are only allowed in some types of flag list
        //  implements case insensitivity
        //  implements only one copy of each flag
        //  implements the grammar for flag and keyword names

        // add more as they become known
        private static readonly string[] kCaseInsensitiveFlags = new string[] { MDNSent };

        private readonly bool mAllowAsterisk;
        private readonly bool mAllowRecent;
        private readonly List<string> mFlags;

        public cMessageFlagList(bool pAllowAsterisk, bool pAllowRecent, IEnumerable<string> pFlags = null)
        {
            mAllowAsterisk = pAllowAsterisk;
            mAllowRecent = pAllowRecent;
            mFlags = new List<string>();
            if (pFlags == null) return;
            foreach (var lFlag in pFlags) if (!ZIsValidFlag(lFlag, pAllowAsterisk, pAllowRecent)) throw new ArgumentOutOfRangeException(nameof(pFlags));
            foreach (var lFlag in pFlags) if (!Contains(lFlag)) mFlags.Add(lFlag);
        }

        public cMessageFlagList(cMessageFlagList pFlags)
        {
            if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));
            mAllowAsterisk = pFlags.mAllowAsterisk;
            mAllowRecent = pFlags.mAllowRecent;
            mFlags = new List<string>(pFlags.mFlags);
        }

        private cMessageFlagList(bool pAllowAsterisk, bool pAllowRecent, IEnumerable<string> pFlags, bool pValid)
        {
            mAllowAsterisk = pAllowAsterisk;
            mAllowRecent = pAllowRecent;
            mFlags = new List<string>();
            foreach (var lFlag in pFlags) if (!Contains(lFlag)) mFlags.Add(lFlag);
        }

        public bool Contains(string pFlag)
        {
            if (pFlag == null || pFlag.Length == 0) return false;
            if (pFlag[0] == '\\' || kCaseInsensitiveFlags.Contains(pFlag, StringComparer.InvariantCultureIgnoreCase)) return mFlags.Contains(pFlag, StringComparer.InvariantCultureIgnoreCase);
            return mFlags.Contains(pFlag);
        }

        public bool Contains(IEnumerable<string> pFlags)
        {
            if (pFlags == null) return false;
            foreach (var lFlag in pFlags) if (!Contains(lFlag)) return false;
            return true;
        }

        public void Add(string pFlag)
        {
            if (pFlag == null) throw new ArgumentNullException(nameof(pFlag));
            if (Contains(pFlag)) return;
            if (!ZIsValidFlag(pFlag, mAllowAsterisk, mAllowRecent)) throw new ArgumentOutOfRangeException(nameof(pFlag));
            mFlags.Add(pFlag);
        }

        public void Add(IEnumerable<string> pFlags)
        {
            if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));
            foreach (var lFlag in pFlags) if (!ZIsValidFlag(lFlag, mAllowAsterisk, mAllowRecent)) throw new ArgumentOutOfRangeException(nameof(pFlags));
            foreach (var lFlag in pFlags) if (!Contains(lFlag)) mFlags.Add(lFlag);
        }

        public void Remove(string pFlag)
        {
            if (pFlag == null || pFlag.Length == 0) return;
            if (pFlag[0] == '\\' || kCaseInsensitiveFlags.Contains(pFlag, StringComparer.InvariantCultureIgnoreCase)) mFlags.RemoveAll(f => f.Equals(pFlag, StringComparison.InvariantCultureIgnoreCase));
            mFlags.Remove(pFlag);
        }

        public void Remove(IEnumerable<string> pFlags)
        {
            if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));
            foreach (var lFlag in pFlags) Remove(lFlag);
        }

        public int Count => mFlags.Count;
        public IEnumerator<string> GetEnumerator() => mFlags.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => mFlags.GetEnumerator();

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cMessageFlagList));
            foreach (var lFlag in mFlags) lBuilder.Append(lFlag);
            return lBuilder.ToString();
        }

        public static bool TryConstruct(bool pAllowAsterisk, bool pAllowRecent, IEnumerable<string> pFlags, out cMessageFlagList rFlags)
        {
            if (pFlags == null) { rFlags = null; return false; }
            foreach (var lFlag in pFlags) if (!ZIsValidFlag(lFlag, pAllowAsterisk, pAllowRecent)) { rFlags = null; return false; }
            rFlags = new cMessageFlagList(pAllowAsterisk, pAllowRecent, pFlags, true);
            return true;
        }

        private static bool ZIsValidFlag(string pFlag, bool pAllowAsterisk, bool pAllowRecent)
        {
            if (pFlag == null) return false;
            if (pFlag.Length == 0) return false;

            if (pFlag == Asterisk) return pAllowAsterisk;
            if (pFlag.Equals(Recent, StringComparison.InvariantCultureIgnoreCase)) return pAllowRecent;

            string lFlag;
            if (pFlag[0] == '\\') lFlag = pFlag.Remove(0, 1);
            else lFlag = pFlag;

            return cCommandPartFactory.TryAsAtom(lFlag, out _);
        }
    }

    public abstract class cMessageFlagsBase : cMessageFlagConstants, iSettableFlags
    {
        protected readonly cMessageFlagList mFlags;

        public cMessageFlagsBase(cMessageFlagList pFlags) { mFlags = pFlags; }

        public bool Contains(string pFlag) => mFlags.Contains(pFlag);
        public bool Contains(params string[] pFlags) => mFlags.Contains(pFlags);
        public bool Contains(IEnumerable<string> pFlags) => mFlags.Contains(pFlags);

        public void Add(string pFlag) => mFlags.Add(pFlag);
        public void Add(params string[] pFlags) => mFlags.Add(pFlags);
        public void Add(IEnumerable<string> pFlags) => mFlags.Add(pFlags);

        public void Remove(string pFlag) => mFlags.Remove(pFlag);
        public void Remove(params string[] pFlags) => mFlags.Remove(pFlags);
        public void Remove(IEnumerable<string> pFlags) => mFlags.Remove(pFlags);

        public bool ContainsAnswered
        {
            get => mFlags.Contains(Answered);

            set
            {
                if (value) mFlags.Add(Answered);
                else mFlags.Remove(Answered);
            }
        }

        public bool ContainsFlagged
        {
            get => mFlags.Contains(Flagged);

            set
            {
                if (value) mFlags.Add(Flagged);
                else mFlags.Remove(Flagged);
            }
        }

        public bool ContainsDeleted
        {
            get => mFlags.Contains(Deleted);

            set
            {
                if (value) mFlags.Add(Deleted);
                else mFlags.Remove(Deleted);
            }
        }

        public bool ContainsSeen
        {
            get => mFlags.Contains(Seen);

            set
            {
                if (value) mFlags.Add(Seen);
                else mFlags.Remove(Seen);
            }
        }

        public bool ContainsDraft
        {
            get => mFlags.Contains(Draft);

            set
            {
                if (value) mFlags.Add(Draft);
                else mFlags.Remove(Draft);
            }
        }

        public bool ContainsMDNSent
        {
            get => mFlags.Contains(MDNSent);

            set
            {
                if (value) mFlags.Add(MDNSent);
                else mFlags.Remove(MDNSent);
            }
        }

        public bool ContainsForwarded
        {
            get => mFlags.Contains(Forwarded);

            set
            {
                if (value) mFlags.Add(Forwarded);
                else mFlags.Remove(Forwarded);
            }
        }

        public bool ContainsSubmitPending
        {
            get => mFlags.Contains(SubmitPending);

            set
            {
                if (value) mFlags.Add(SubmitPending);
                else mFlags.Remove(SubmitPending);
            }
        }

        public bool ContainsSubmitted
        {
            get => mFlags.Contains(Submitted);

            set
            {
                if (value) mFlags.Add(Submitted);
                else mFlags.Remove(Submitted);
            }
        }

        public int Count => mFlags.Count;
        public IEnumerator<string> GetEnumerator() => mFlags.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => mFlags.GetEnumerator();
    }

    public class cSettableFlagsList : cRootFlagList, iSettableFlags
    {
        public cSettableFlagList(params string[] pFlags) => mFlags = new cBaseFlagList(false, false, pFlags);
        public cSettableFlagList(IEnumerable<string> pFlags) => mFlags = new cBaseFlagList(false, false, pFlags);
        private cSettableFlagList(cBaseFlagList pFlags) => mFlags = pFlags;
    }

    public class cMessageFlagsList : cBaseFlagList, iMessageFlags
    {
        public cMessageFlagList(params string[] pFlags) : base(false, true, pFlags) { }
        public cMessageFlagList(IEnumerable<string> pFlags = null) : base(false, true, pFlags) { }
        public cMessageFlagList(cMessageFlagList pFlags) : base(pFlags) { }
        private cMessageFlagList(cBaseFlagList pFlags) : base(pFlags) { }

        public bool ContainsRecent
        {
            get => Contains(Recent);

            set
            {
                if (value) Add(Recent);
                else Remove(Recent);
            }
        }


        ;?; // try construct
    }
}
