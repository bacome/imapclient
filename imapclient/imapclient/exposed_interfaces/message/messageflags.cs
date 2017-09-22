using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public interface iMessageFlags : IReadOnlyCollection<string>
    {
        bool Contains(string pFlag);
        bool Contains(params string[] pFlags);
        bool Contains(IEnumerable<string> pFlags);
    }

    public class cMessageFlags : iMessageFlags
    {
        public const string Asterisk = @"\*";
        public const string Recent = @"\ReCeNt";

        public const string Answered = @"\AnSwErEd";
        public const string Flagged = @"\FlAgGeD";
        public const string Deleted = @"\DeLeTeD";
        public const string Seen = @"\SeEn";
        public const string Draft = @"\DrAfT";

        // rfc 3503/ 5550
        public const string MDNSent = "$MdNsEnT";

        // rfc 5788/ 5550
        public const string Forwarded = "$Forwarded";
        public const string SubmitPending = "$SubmitPending";
        public const string Submitted = "$Submitted";

        private readonly cMessageFlagList mFlags;

        private cMessageFlags(cMessageFlagList pFlags) { mFlags = pFlags; }

        public bool ContainsCreateNewPossible => Contains(cMessageFlags.Asterisk);

        public bool ContainsAnswered => Contains(cMessageFlags.Answered);
        public bool ContainsFlagged => Contains(cMessageFlags.Flagged);
        public bool ContainsDeleted => Contains(cMessageFlags.Deleted);
        public bool ContainsSeen => Contains(cMessageFlags.Seen);
        public bool ContainsDraft => Contains(cMessageFlags.Draft);

        public bool ContainsRecent => Contains(cMessageFlags.Recent);

        public bool ContainsMDNSent => Contains(cMessageFlags.MDNSent);
        public bool ContainsForwarded => Contains(cMessageFlags.Forwarded);
        public bool ContainsSubmitPending => Contains(cMessageFlags.SubmitPending);
        public bool ContainsSubmitted => Contains(cMessageFlags.Submitted);

        public bool Contains(string pFlag) => mFlags.Contains(pFlag);
        public bool Contains(params string[] pFlags) => mFlags.Contains(pFlags);
        public bool Contains(IEnumerable<string> pFlags) => mFlags.Contains(pFlags);

        public int Count => mFlags.Count;
        public IEnumerator<string> GetEnumerator() => mFlags.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => mFlags.GetEnumerator();

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cMessageFlags));
            foreach (var lFlag in mFlags) lBuilder.Append(lFlag);
            return lBuilder.ToString();
        }

        public static bool TryConstruct(IEnumerable<string> pFlags, cMessageFlags rFlags)
        {
            if (cMessageFlagList.TryConstruct(true, true, pFlags, out var lFlags))
            {
                rFlags = new cMessageFlags(lFlags);
                return true;
            }

            rFlags = null;
            return false;
        }

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
        }
    }

    public class cMessageFlagList : iMessageFlags
    {
        // add more as they become known
        private static readonly string[] kCaseInsensitiveFlags = new string[] { cMessageFlags.MDNSent };

        private readonly bool mAllowAsterisk;
        private readonly bool mAllowRecent;
        private readonly List<string> mFlags = new List<string>();

        public cMessageFlagList(bool pAllowAsterisk, bool pAllowRecent, IEnumerable<string> pFlags = null)
        {
            mAllowAsterisk = pAllowAsterisk;
            mAllowRecent = pAllowRecent;
            if (pFlags == null) return;
            foreach (var lFlag in pFlags) if (!IsValidFlag(lFlag, pAllowAsterisk, pAllowRecent)) throw new ArgumentOutOfRangeException(nameof(pFlags));
            foreach (var lFlag in pFlags) if (!Contains(lFlag)) mFlags.Add(lFlag);
        }

        private cMessageFlagList(bool pAllowAsterisk, bool pAllowRecent, IEnumerable<string> pFlags, bool pValid)
        {
            mAllowAsterisk = pAllowAsterisk;
            mAllowRecent = pAllowRecent;
            foreach (var lFlag in pFlags) if (!Contains(lFlag)) mFlags.Add(lFlag);
        }

        public bool Contains(string pFlag)
        {
            if (pFlag == null || pFlag.Length == 0) return false;
            if (pFlag[0] == '\\' || kCaseInsensitiveFlags.Contains(pFlag, StringComparer.InvariantCultureIgnoreCase)) return mFlags.Contains(pFlag, StringComparer.InvariantCultureIgnoreCase);
            return mFlags.Contains(pFlag);
        }

        public bool Contains(params string[] pFlags) => ZContains(pFlags);
        public bool Contains(IEnumerable<string> pFlags) => ZContains(pFlags);

        private bool ZContains(IEnumerable<string> pFlags)
        {
            if (pFlags == null) return false;
            foreach (var lFlag in pFlags) if (!Contains(lFlag)) return false;
            return true;
        }

        public void Add(string pFlag)
        {
            if (pFlag == null) throw new ArgumentNullException(nameof(pFlag));
            if (Contains(pFlag)) return;
            if (!IsValidFlag(pFlag, mAllowAsterisk, mAllowRecent)) throw new ArgumentOutOfRangeException(nameof(pFlag));
            mFlags.Add(pFlag);
        }

        public void Add(params string[] pFlags) => ZAdd(pFlags);
        public void Add(IEnumerable<string> pFlags) => ZAdd(pFlags);

        private void ZAdd(IEnumerable<string> pFlags)
        {
            if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));
            foreach (var lFlag in pFlags) if (!IsValidFlag(lFlag, mAllowAsterisk, mAllowRecent)) throw new ArgumentOutOfRangeException(nameof(pFlags));
            foreach (var lFlag in pFlags) if (!Contains(lFlag)) mFlags.Add(lFlag);
        }

        public void Remove(string pFlag)
        {
            if (pFlag == null || pFlag.Length == 0) return;
            if (pFlag[0] == '\\' || kCaseInsensitiveFlags.Contains(pFlag, StringComparer.InvariantCultureIgnoreCase)) mFlags.RemoveAll(f => f.Equals(pFlag, StringComparison.InvariantCultureIgnoreCase));
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

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cMessageFlagList));
            foreach (var lFlag in this) lBuilder.Append(lFlag);
            return lBuilder.ToString();
        }

        public static bool TryConstruct(bool pAllowAsterisk, bool pAllowRecent, IEnumerable<string> pFlags, out cMessageFlagList rFlags)
        {
            if (pFlags == null) { rFlags = null; return false; }
            foreach (var lFlag in pFlags) if (!IsValidFlag(lFlag, pAllowAsterisk, pAllowRecent)) { rFlags = null; return false; }
            rFlags = new cMessageFlagList(pAllowAsterisk, pAllowRecent, pFlags, true);
            return true;
        }

        public static bool IsValidFlag(string pFlag, bool pAllowAsterisk, bool pAllowRecent)
        {
            if (pFlag == null) return false;
            if (pFlag.Length == 0) return false;

            if (pFlag == cMessageFlags.Asterisk) return pAllowAsterisk;
            if (pFlag.Equals(cMessageFlags.Recent, StringComparison.InvariantCultureIgnoreCase)) return pAllowRecent;

            string lFlag;
            if (pFlag[0] == '\\') lFlag = pFlag.Remove(0, 1);
            else lFlag = pFlag;

            return cCommandPartFactory.TryAsAtom(lFlag, out _);
        }
    }

    public abstract class cMessageFlagsBase : cMessageFlagList
    {
        public cMessageFlagsBase(bool pAllowRecent) : base(false, pAllowRecent) { }
        public cMessageFlagsBase(bool pAllowRecent, IEnumerable<string> pFlags) : base(false, pAllowRecent, pFlags) { }

        public bool IsAnswered
        {
            get => Contains(cMessageFlags.Answered);

            set
            {
                if (value) Add(cMessageFlags.Answered);
                else Remove(cMessageFlags.Answered);
            }
        }

        public bool IsFlagged
        {
            get => Contains(cMessageFlags.Flagged);

            set
            {
                if (value) Add(cMessageFlags.Flagged);
                else Remove(cMessageFlags.Flagged);
            }
        }

        public bool IsDeleted
        {
            get => Contains(cMessageFlags.Deleted);

            set
            {
                if (value) Add(cMessageFlags.Deleted);
                else Remove(cMessageFlags.Deleted);
            }
        }

        public bool IsSeen
        {
            get => Contains(cMessageFlags.Seen);

            set
            {
                if (value) Add(cMessageFlags.Seen);
                else Remove(cMessageFlags.Seen);
            }
        }

        public bool IsDraft
        {
            get => Contains(cMessageFlags.Draft);

            set
            {
                if (value) Add(cMessageFlags.Draft);
                else Remove(cMessageFlags.Draft);
            }
        }

        public bool IsMDNSent
        {
            get => Contains(cMessageFlags.MDNSent);

            set
            {
                if (value) Add(cMessageFlags.MDNSent);
                else Remove(cMessageFlags.MDNSent);
            }
        }

        public bool IsForwarded
        {
            get => Contains(cMessageFlags.Forwarded);

            set
            {
                if (value) Add(cMessageFlags.Forwarded);
                else Remove(cMessageFlags.Forwarded);
            }
        }

        public bool IsSubmitPending
        {
            get => Contains(cMessageFlags.SubmitPending);

            set
            {
                if (value) Add(cMessageFlags.SubmitPending);
                else Remove(cMessageFlags.SubmitPending);
            }
        }

        public bool IsSubmitted
        {
            get => Contains(cMessageFlags.Submitted);

            set
            {
                if (value) Add(cMessageFlags.Submitted);
                else Remove(cMessageFlags.Submitted);
            }
        }
    }

    public class cFetchableFlags : cMessageFlagsBase
    {
        public cFetchableFlags() : base(true) { }
        public cFetchableFlags(IEnumerable<string> pFlags) : base(true, pFlags) { }
        public cFetchableFlags(params string[] pFlags) : base(true, pFlags) { }

        public bool IsRecent
        {
            get => Contains(cMessageFlags.Recent);

            set
            {
                if (value) Add(cMessageFlags.Recent);
                else Remove(cMessageFlags.Recent);
            }
        }
    }

    public class cSettableFlags : cMessageFlagsBase
    {
        public cSettableFlags() : base(false) { }
        public cSettableFlags(IEnumerable<string> pFlags) : base(false, pFlags) { }
        public cSettableFlags(params string[] pFlags) : base(false, pFlags) { }
    }
}
