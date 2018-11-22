using System;
using System.Runtime.Serialization;
using work.bacome.imapinternals;
using work.bacome.imapsupport;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents an internet format timestamp.
    /// </summary>
    [Serializable]
    public class cTimestamp : IEquatable<cTimestamp>, IComparable<cTimestamp>
    {
        /// <summary>
        /// The <see cref="DateTimeOffset"/>.
        /// </summary>
        public readonly DateTimeOffset DateTimeOffset;

        /// <summary>
        /// Indicates if the local offset from UTC is known or not.
        /// </summary>
        public readonly bool UnknownLocalOffset;

        /// <summary>
        /// Initialises a new instance with the specified values.
        /// </summary>
        /// <remarks>
        /// To specify that the local offset from UTC is unknown set <paramref name="pWest"/> to <see langword="true"/> and the local offset to zero.
        /// </remarks>
        /// <param name="pYYYY"></param>
        /// <param name="pMonth"></param>
        /// <param name="pDay"></param>
        /// <param name="pHour"></param>
        /// <param name="pMinute"></param>
        /// <param name="pSecond"></param>
        /// <param name="pMillisecond"></param>
        /// <param name="pWest">True if the local offset from UTC is negative.</param>
        /// <param name="pOffsetHour"></param>
        /// <param name="pOffsetMinute"></param>
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

        /// <summary>
        /// Initialises a new instance with the specified value.
        /// </summary>
        public cTimestamp(DateTime pDateTime)
        {
            DateTimeOffset = new DateTimeOffset(pDateTime);
            UnknownLocalOffset = false;
        }

        /// <summary>
        /// Initialises a new instance with the specified value.
        /// </summary>
        public cTimestamp(DateTimeOffset pDateTimeOffset)
        {
            DateTimeOffset = pDateTimeOffset;
            UnknownLocalOffset = false;
        }

        [OnDeserialized]
        private void OnDeserialised(StreamingContext pSC)
        {
            if (UnknownLocalOffset && DateTimeOffset.Offset != TimeSpan.Zero) throw new cDeserialiseException(nameof(cTimestamp), nameof(UnknownLocalOffset), kDeserialiseExceptionMessage.IsInconsistent);
        }

        /** <summary>Gets a value that represents the date component of the timestamp.</summary> **/
        public DateTime Date => DateTimeOffset.Date;
        /** <summary>Gets a value that represents the date and time of the timestamp.</summary> **/
        public DateTime DateTime => DateTimeOffset.DateTime;
        /** <summary>Gets the time's offset from UTC.</summary> **/
        public TimeSpan Offset => DateTimeOffset.Offset;
        /** <summary>Gets a value that represents the local date and time of the timestamp.</summary> **/
        public DateTime LocalDateTime => DateTimeOffset.LocalDateTime;
        /** <summary>Gets a value that represents the UTC date and time of the timestamp.</summary> **/
        public DateTime UtcDateTime => DateTimeOffset.UtcDateTime;
        /** <summary>Gets the day of the month the timestamp.</summary> **/
        public int Day => DateTimeOffset.Day;
        /** <summary>Gets the month component of the timestamp.</summary> **/
        public int Month => DateTimeOffset.Month;
        /** <summary>Gets the year component of the timestamp.</summary> **/
        public int Year => DateTimeOffset.Year;
        /** <summary>Gets the hour component of the timestamp.</summary> **/
        public int Hour => DateTimeOffset.Hour;
        /** <summary>Gets the minute component of the timestamp.</summary> **/
        public int Minute => DateTimeOffset.Minute;
        /** <summary>Gets the second component of the timestamp.</summary> **/
        public int Second => DateTimeOffset.Second;
        /** <summary>Gets the millisecond component of the timestamp.</summary> **/
        public int Millisecond => DateTimeOffset.Millisecond;

        /// <summary>
        /// Gets a string representation of the timestamp.
        /// </summary>
        /// <returns></returns>
        public string GetRFC822DateTimeString()
        {
            string lSign;
            string lZone;

            if (UnknownLocalOffset)
            {
                lSign = "-";
                lZone = "0000";
            }
            else
            {
                var lOffset = DateTimeOffset.Offset;

                if (lOffset < TimeSpan.Zero)
                {
                    lSign = "-";
                    lOffset = -lOffset;
                }
                else lSign = "+";

                lZone = lOffset.ToString("hhmm");
            }

            var lMonth = kRFCMonth.cName[DateTimeOffset.Month - 1];

            return string.Format("{0:dd} {1} {0:yyyy} {0:HH}:{0:mm}:{0:ss} {2}{3}", DateTimeOffset, lMonth, lSign, lZone);
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cTimestamp pObject) => this == pObject;

        /// <inheritdoc cref="cAPIDocumentationTemplate.CompareTo"/>
        public int CompareTo(cTimestamp pOther)
        {
            if (pOther == null) return 1;
            int lCompareTo;
            if ((lCompareTo = DateTimeOffset.CompareTo(pOther.DateTimeOffset)) != 0) return lCompareTo;
            return UnknownLocalOffset.CompareTo(pOther.UnknownLocalOffset);
        }

        /// <inheritdoc />
        public override bool Equals(object pObject) => this == pObject as cTimestamp;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetHashCode"/>
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

        /// <inheritdoc/>
        public override string ToString() => $"{nameof(cTimestamp)}({DateTimeOffset},{UnknownLocalOffset})";

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality"/>
        public static bool operator ==(cTimestamp pA, cTimestamp pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return pA.DateTimeOffset == pB.DateTimeOffset && pA.UnknownLocalOffset == pB.UnknownLocalOffset;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality"/>
        public static bool operator !=(cTimestamp pA, cTimestamp pB) => !(pA == pB);

        /// <summary>
        /// Gets a timestamp that is set to the current date and time as set on the current computer.
        /// </summary>
        public static cTimestamp Now => new cTimestamp(DateTimeOffset.Now);
    }
}