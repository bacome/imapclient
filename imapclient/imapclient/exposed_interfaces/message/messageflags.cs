using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

    public class cSettableFlags : iSettableFlags
    {
        // immutable (for passing in)

        private readonly cSettableFlagsList mFlags;

        public cSettableFlags(params string[] pFlags) => mFlags = new cSettableFlagsList(pFlags); // validates, duplicates, removes duplicates
        public cSettableFlags(IEnumerable<string> pFlags) => mFlags = new cSettableFlagsList(pFlags); // validates, duplicates, removes duplicates
        public cSettableFlags(cSettableFlagsList pFlags) => mFlags = new cSettableFlagsList(pFlags); // duplicates

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

        public static implicit operator cSettableFlags(cSettableFlagsList pFlags) => new cSettableFlags(pFlags);
    }

    public class cMessageFlags : iMessageFlags
    {
        // immutable (for passing in and out)

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

        private readonly cMessageFlagsList mFlags;

        public cMessageFlags(params string[] pFlags) => mFlags = new cMessageFlagsList(pFlags); // validates, duplicates, removes duplicates
        public cMessageFlags(IEnumerable<string> pFlags) => mFlags = new cMessageFlagsList(pFlags); // validates, duplicates, removes duplicates
        public cMessageFlags(cMessageFlagsList pFlags) => mFlags = new cMessageFlagsList(pFlags); // duplicates
        private cMessageFlags(cMessageFlagsList pFlags, bool pWrap) => mFlags = pFlags; // wraps

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

        public static implicit operator cMessageFlags(cMessageFlagsList pFlags) => new cMessageFlagsList(pFlags);

        public static bool TryConstruct(IEnumerable<string> pFlags, out cMessageFlags rFlags)
        {
            if (!cMessageFlagsList.TryConstruct(pFlags, out var lFlags)) { rFlags = null; return false; }
            rFlags = new cMessageFlags(lFlags, true);
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

    public class cPermanentFlags : iMessageFlags
    {
        // read only wrapper (for passing out)
    
        private readonly cPermanentFlagsList mFlags;

        private cPermanentFlags(cPermanentFlagsList pFlags) => mFlags = pFlags;

        public bool CreateNewIsPossible => mFlags.Contains(cMessageFlags.Asterisk);

        public bool ContainsRecent => mFlags.Contains(cMessageFlags.Recent);

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
            if (!cPermanentFlagsList.TryConstruct(pFlags, out var lFlags)) { rFlags = null; return false; }
            rFlags = new cPermanentFlags(lFlags);
            return true;
        }
    }

    public abstract class cMessageFlagsListBase : iSettableFlags
    {
        // implements case insensitivity
        //  implements uniqueness (via mutation, not via construct)
        //  implements validity (via mutation, not via construct)

        // add more as they become known
        private static readonly string[] kCaseInsensitiveFlags = new string[] { cMessageFlags.MDNSent };

        private readonly List<string> mFlags;

        public cMessageFlagsListBase(List<string> pFlags)
        {
            mFlags = pFlags ?? throw new ArgumentNullException(nameof(pFlags));
        }

        public bool Contains(string pFlag) => YContains(mFlags, pFlag);
        public bool Contains(params string[] pFlags) => ZContains(pFlags);
        public bool Contains(IEnumerable<string> pFlags) => ZContains(pFlags);

        private bool ZContains(IEnumerable<string> pFlags)
        {
            if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));
            foreach (var lFlag in pFlags) if (!YContains(mFlags, lFlag)) return false;
            return true;
        }

        public bool ContainsAnswered
        {
            get => Contains(cMessageFlags.Answered);

            set
            {
                if (value) Add(cMessageFlags.Answered);
                else Remove(cMessageFlags.Answered);
            }
        }

        public bool ContainsFlagged
        {
            get => Contains(cMessageFlags.Flagged);

            set
            {
                if (value) Add(cMessageFlags.Flagged);
                else Remove(cMessageFlags.Flagged);
            }
        }

        public bool ContainsDeleted
        {
            get => Contains(cMessageFlags.Deleted);

            set
            {
                if (value) Add(cMessageFlags.Deleted);
                else Remove(cMessageFlags.Deleted);
            }
        }

        public bool ContainsSeen
        {
            get => Contains(cMessageFlags.Seen);

            set
            {
                if (value) Add(cMessageFlags.Seen);
                else Remove(cMessageFlags.Seen);
            }
        }

        public bool ContainsDraft
        {
            get => Contains(cMessageFlags.Draft);

            set
            {
                if (value) Add(cMessageFlags.Draft);
                else Remove(cMessageFlags.Draft);
            }
        }

        public bool ContainsMDNSent
        {
            get => Contains(cMessageFlags.MDNSent);

            set
            {
                if (value) Add(cMessageFlags.MDNSent);
                else Remove(cMessageFlags.MDNSent);
            }
        }

        public bool ContainsForwarded
        {
            get => Contains(cMessageFlags.Forwarded);

            set
            {
                if (value) Add(cMessageFlags.Forwarded);
                else Remove(cMessageFlags.Forwarded);
            }
        }

        public bool ContainsSubmitPending
        {
            get => Contains(cMessageFlags.SubmitPending);

            set
            {
                if (value) Add(cMessageFlags.SubmitPending);
                else Remove(cMessageFlags.SubmitPending);
            }
        }

        public bool ContainsSubmitted
        {
            get => Contains(cMessageFlags.Submitted);

            set
            {
                if (value) Add(cMessageFlags.Submitted);
                else Remove(cMessageFlags.Submitted);
            }
        }

        public void Add(string pFlag)
        {
            if (!YIsValidFlag(pFlag)) throw new ArgumentOutOfRangeException(nameof(pFlag));
            if (!YContains(mFlags, pFlag)) mFlags.Add(pFlag);
        }

        public void Add(params string[] pFlags) => ZAdd(pFlags);
        public void Add(IEnumerable<string> pFlags) => ZAdd(pFlags);

        private void ZAdd(IEnumerable<string> pFlags)
        {
            if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));
            foreach (var lFlag in pFlags) if (!YIsValidFlag(lFlag)) throw new ArgumentOutOfRangeException(nameof(pFlags));
            foreach (var lFlag in pFlags) if (!YContains(mFlags, lFlag)) mFlags.Add(lFlag);
        }

        public void Remove(string pFlag)
        {
            if (pFlag != null && pFlag.Length != 0 && (pFlag[0] == '\\' || kCaseInsensitiveFlags.Contains(pFlag, StringComparer.InvariantCultureIgnoreCase))) mFlags.RemoveAll(f => f.Equals(pFlag, StringComparison.InvariantCultureIgnoreCase));
            mFlags.Remove(pFlag);
        }

        public void Remove(params string[] pFlags) => ZRemove(pFlags);
        public void Remove(IEnumerable<string> pFlags) => ZRemove(pFlags);

        private void ZRemove(IEnumerable<string> pFlags)
        {
            if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));
            foreach (var lFlag in pFlags) Remove(lFlag);
        }

        public int Count => mFlags.Count;
        public IEnumerator<string> GetEnumerator() => mFlags.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => mFlags.GetEnumerator();

        protected abstract bool YIsValidFlag(string pFlag);

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cMessageFlagsListBase));
            foreach (var lFlag in mFlags) lBuilder.Append(lFlag);
            return lBuilder.ToString();
        }

        protected static bool YContains(List<string> pFlags, string pFlag)
        {
            if (pFlag != null && pFlag.Length != 0 && (pFlag[0] == '\\' || kCaseInsensitiveFlags.Contains(pFlag, StringComparer.InvariantCultureIgnoreCase))) return pFlags.Contains(pFlag, StringComparer.InvariantCultureIgnoreCase);
            return pFlags.Contains(pFlag);
        }

        protected static List<string> YDistinctFlagList(IEnumerable<string> pFlags)
        {
            var lFlags = new List<string>();
            foreach (string lFlag in pFlags) if (!YContains(lFlags, lFlag)) lFlags.Add(lFlag);
            return lFlags;
        }
    }

    public class cSettableFlagsList : cMessageFlagsListBase
    {
        public cSettableFlagsList() : base(new List<string>()) { }
        public cSettableFlagsList(params string[] pFlags) : base(ZCtor(pFlags)) { } // validates, duplicates, removes duplicates
        public cSettableFlagsList(IEnumerable<string> pFlags) : base(ZCtor(pFlags)) { } // validates, duplicates, removes duplicates
        public cSettableFlagsList(cSettableFlagsList pFlags) : base(new List<string>(pFlags)) { } // duplicates

        protected override bool YIsValidFlag(string pFlag) => ZIsValidFlag(pFlag);

        private static bool ZIsValidFlag(string pFlag)
        {
            if (pFlag == null) return false;
            if (pFlag.Length == 0) return false;

            if (pFlag.Equals(cMessageFlags.Recent, StringComparison.InvariantCultureIgnoreCase)) return false;

            string lFlag;
            if (pFlag[0] == '\\') lFlag = pFlag.Remove(0, 1);
            else lFlag = pFlag;

            return cCommandPartFactory.TryAsAtom(lFlag, out _);
        }

        private static List<string> ZCtor(IEnumerable<string> pFlags)
        {
            if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));
            foreach (var lFlag in pFlags) if (!ZIsValidFlag(lFlag)) throw new ArgumentOutOfRangeException(nameof(pFlags));
            return YDistinctFlagList(pFlags);
        }
    }

    public class cMessageFlagsList : cMessageFlagsListBase
    {
        public cMessageFlagsList() : base(new List<string>()) { }
        public cMessageFlagsList(params string[] pFlags) : base(ZCtor(pFlags)) { } // validates, duplicates, removes duplicates
        public cMessageFlagsList(IEnumerable<string> pFlags) : base(ZCtor(pFlags)) { } // validates, duplicates, removes duplicates
        public cMessageFlagsList(cMessageFlagsList pFlags) : base(new List<string>(pFlags)) { } // duplicates
        private cMessageFlagsList(List<string> pFlags) : base(pFlags) { } // wraps

        public bool ContainsRecent
        {
            get => Contains(cMessageFlags.Recent);

            set
            {
                if (value) Add(cMessageFlags.Recent);
                else Remove(cMessageFlags.Recent);
            }
        }

        protected override bool YIsValidFlag(string pFlag) => ZIsValidFlag(pFlag);

        private static bool ZIsValidFlag(string pFlag)
        {
            if (pFlag == null) return false;
            if (pFlag.Length == 0) return false;

            string lFlag;
            if (pFlag[0] == '\\') lFlag = pFlag.Remove(0, 1);
            else lFlag = pFlag;

            return cCommandPartFactory.TryAsAtom(lFlag, out _);
        }

        private static List<string> ZCtor(IEnumerable<string> pFlags)
        {
            if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));
            foreach (var lFlag in pFlags) if (!ZIsValidFlag(lFlag)) throw new ArgumentOutOfRangeException(nameof(pFlags));
            return YDistinctFlagList(pFlags);
        }

        public static bool TryConstruct(IEnumerable<string> pFlags, out cMessageFlagsList rFlags)
        {
            if (pFlags == null) { rFlags = null; return false; }
            foreach (var lFlag in pFlags) if (!ZIsValidFlag(lFlag)) { rFlags = null; return false; }
            rFlags = new cMessageFlagsList(YDistinctFlagList(pFlags));
            return true;
        }
    }

    public class cPermanentFlagsList : cMessageFlagsListBase
    {
        private cPermanentFlagsList(List<string> pFlags) : base(pFlags) { } // wraps

        protected override bool YIsValidFlag(string pFlag) => ZIsValidFlag(pFlag);

        private static bool ZIsValidFlag(string pFlag)
        {
            if (pFlag == null) return false;
            if (pFlag.Length == 0) return false;

            if (pFlag == cMessageFlags.Asterisk) return true;

            string lFlag;
            if (pFlag[0] == '\\') lFlag = pFlag.Remove(0, 1);
            else lFlag = pFlag;

            return cCommandPartFactory.TryAsAtom(lFlag, out _);
        }

        public static bool TryConstruct(IEnumerable<string> pFlags, out cPermanentFlagsList rFlags)
        {
            if (pFlags == null) { rFlags = null; return false; }
            foreach (var lFlag in pFlags) if (!ZIsValidFlag(lFlag)) { rFlags = null; return false; }
            rFlags = new cPermanentFlagsList(YDistinctFlagList(pFlags));
            return true;
        }
    }
}
