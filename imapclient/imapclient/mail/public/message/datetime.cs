using System;

namespace work.bacome.mailclient
{
    [Serializable]
    public class cTimestamp : IEquatable<cTimestamp>, IComparable<cTimestamp>
    {
        public readonly DateTimeOffset DateTimeOffset;
        public readonly bool UnknownLocalOffset;

        public cTimestamp(int pYYYY, int pMonth, int pDay, int pHour, int pMinute, int pSecond, int pMillisecond, bool pWest, int pOffsetHour, int pOffsetMinute)
        {
            // west = a negative offset

            if (pMonth < 1 || pMonth > 12) throw new ArgumentOutOfRangeException(nameof(pMonth));
            if (pDay < 1 || pDay > 31) throw new ArgumentOutOfRangeException(nameof(pDay));
            if (pHour < 0 || pHour > 23) throw new ArgumentOutOfRangeException(nameof(pHour));
            if (pMinute < 0 || pMinute > 59) throw new ArgumentOutOfRangeException(nameof(pMinute));
            if (pSecond < 0 || pSecond > 60) throw new ArgumentOutOfRangeException(nameof(pSecond)); // 60 is allowed for leap seconds
            if (pMillisecond < 0 || pMillisecond > 999) throw new ArgumentOutOfRangeException(nameof(pMillisecond));
            if (pOffsetHour < 0 || pOffsetHour > 23) throw new ArgumentOutOfRangeException(nameof(pOffsetHour));
            if (pOffsetMinute < 0 || pOffsetMinute > 59) throw new ArgumentOutOfRangeException(nameof(pOffsetMinute));

            if (pSecond == 60) pSecond = 59; // dot net doesn't handle leap seconds

            var lDateTime = new DateTime(pYYYY, pMonth, pDay, pHour, pMinute, pSecond, pMillisecond);

            if (pWest)
            {
                DateTimeOffset = new DateTimeOffset(lDateTime, new TimeSpan(-pOffsetHour, -pOffsetMinute, 0));
                UnknownLocalOffset = (pOffsetHour == 0 && pOffsetMinute == 0);
            }
            else
            {
                DateTimeOffset = new DateTimeOffset(lDateTime, new TimeSpan(pOffsetHour, pOffsetMinute, 0));
                UnknownLocalOffset = false;
            }
        }

        public DateTime Date => DateTimeOffset.Date;
        public DateTime DateTime => DateTimeOffset.DateTime;
        public TimeSpan Offset => DateTimeOffset.Offset;
        public DateTime LocalDateTime => DateTimeOffset.LocalDateTime;
        public DateTime UtcDateTime => DateTimeOffset.UtcDateTime;

        public bool Equals(cTimestamp pObject) => this == pObject;

        public int CompareTo(cTimestamp pOther)
        {
            if (pOther == null) return 1;
            int lCompareTo;
            if ((lCompareTo = DateTimeOffset.CompareTo(pOther.DateTimeOffset)) != 0) return lCompareTo;
            return UnknownLocalOffset.CompareTo(pOther.UnknownLocalOffset);
        }

        public override bool Equals(object pObject) => this == pObject as cTimestamp;

        public override int GetHashCode()
        {
            unchecked
            {
                int lHash = 17;

                lHash = lHash * 23 + DateTimeOffset.GetHashCode();
                lHash = lHash * 23 + UnknownLocalOffset.GetHashCode();

                return lHash;
            }
        }

        public override string ToString() => $"{nameof(cTimestamp)}({DateTimeOffset},{UnknownLocalOffset})";

        public static bool operator ==(cTimestamp pA, cTimestamp pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return pA.DateTimeOffset == pB.DateTimeOffset && pA.UnknownLocalOffset == pB.UnknownLocalOffset;
        }

        public static bool operator !=(cTimestamp pA, cTimestamp pB) => !(pA == pB);
    }
}