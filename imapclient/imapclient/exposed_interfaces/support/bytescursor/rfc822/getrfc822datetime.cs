using System;

namespace work.bacome.imapclient.support
{
    public partial class cBytesCursor
    {
        private static readonly cBytes kMon = new cBytes("MON");
        private static readonly cBytes kTue = new cBytes("TUE");
        private static readonly cBytes kWed = new cBytes("WED");
        private static readonly cBytes kThu = new cBytes("THU");
        private static readonly cBytes kFri = new cBytes("FRI");
        private static readonly cBytes kSat = new cBytes("SAT");
        private static readonly cBytes kSun = new cBytes("SUN");
        private static readonly cBytes kUT = new cBytes("UT");
        private static readonly cBytes kGMT = new cBytes("GMT");
        private static readonly cBytes kEST = new cBytes("EST");
        private static readonly cBytes kEDT = new cBytes("EDT");
        private static readonly cBytes kCST = new cBytes("CST");
        private static readonly cBytes kCDT = new cBytes("CDT");
        private static readonly cBytes kMST = new cBytes("MST");
        private static readonly cBytes kMDT = new cBytes("MDT");
        private static readonly cBytes kPST = new cBytes("PST");
        private static readonly cBytes kPDT = new cBytes("PDT");

        public bool GetRFC822DateTime(out DateTime rDateTime)
        {
            var lBookmark = Position;

            // optional leading spaces
            SkipRFC822CFWS();

            // day-of-week (optional, ignored)
            if (SkipBytes(kMon) || SkipBytes(kTue) || SkipBytes(kWed) || SkipBytes(kThu) || SkipBytes(kFri) || SkipBytes(kSat) || SkipBytes(kSun))
            {
                SkipRFC822CFWS(); // optional spaces
                if (!SkipByte(cASCII.COMMA)) { Position = lBookmark; rDateTime = new DateTime(); return false; }
                SkipRFC822CFWS(); // optional spaces
            }

            // day and month
            if (!GetNumber(out _, out var lDay, 1, 2) || lDay < 1 || lDay > 31) { Position = lBookmark; rDateTime = new DateTime(); return false; }
            if (!SkipRFC822CFWS()) { Position = lBookmark; rDateTime = new DateTime(); return false; }
            if (!ZGetMonth(out var lMonth)) { Position = lBookmark; rDateTime = new DateTime(); return false; }
            if (!SkipRFC822CFWS()) { Position = lBookmark; rDateTime = new DateTime(); return false; }

            // year

            if (!GetNumber(out var lBytes, out var lYear, 2, 4)) { Position = lBookmark; rDateTime = new DateTime(); return false; }

            if (lBytes.Count == 2)
            {
                if (lYear < 50) lYear = 2000 + lYear;
                else lYear = 1900 + lYear;
            }
            else if (lBytes.Count == 3) lYear = 1900 + lYear;

            // mandatory delimiter between date and time
            if (!SkipRFC822CFWS()) { Position = lBookmark; rDateTime = new DateTime(); return false; }

            // hour and minute
            if (!GetNumber(out _, out var lHour, 2, 2) || lHour > 23) { Position = lBookmark; rDateTime = new DateTime(); return false; }
            SkipRFC822CFWS();
            if (!SkipByte(cASCII.COLON)) { Position = lBookmark; rDateTime = new DateTime(); return false; }
            SkipRFC822CFWS();
            if (!GetNumber(out _, out var lMinute, 2, 2) || lMinute > 59) { Position = lBookmark; rDateTime = new DateTime(); return false; }

            // optional second

            bool lDelimiter;

            lDelimiter = SkipRFC822CFWS(); // this is possibly the delimiter between time and zone

            uint lSecond;

            if (SkipByte(cASCII.COLON))
            {
                SkipRFC822CFWS();
                if (!GetNumber(out _, out lSecond, 2, 2) || lSecond > 60) { Position = lBookmark; rDateTime = new DateTime(); return false; } // note: 60 is explicitly allowed to cater for leap seconds
                if (lSecond == 60) lSecond = 59; // dot net doesn't handle leap seconds
                lDelimiter = SkipRFC822CFWS(); // this the delimiter between time and zone
            }
            else lSecond = 0;

            // check that there was a delimiter
            if (!lDelimiter) { Position = lBookmark; rDateTime = new DateTime(); return false; }

            // zone

            DateTimeKind lDateTimeKind;
            TimeSpan lZone;

            if (SkipByte(cASCII.PLUS))
            {
                lDateTimeKind = DateTimeKind.Utc;
                if (!LGetZone(out lZone)) { Position = lBookmark; rDateTime = new DateTime(); return false; }
            }
            else if (SkipByte(cASCII.HYPEN))
            {
                lDateTimeKind = DateTimeKind.Utc;
                if (!LGetZone(out lZone)) { Position = lBookmark; rDateTime = new DateTime(); return false; }
                lZone = lZone.Negate();
            }
            else if (GetToken(cCharset.Alpha, null, null, out var lAlphaZone, 1))
            {
                // obsolete zone format

                if (cASCII.Compare(lAlphaZone, kUT, false) || cASCII.Compare(lAlphaZone, kGMT, false)) { lDateTimeKind = DateTimeKind.Utc; lZone = new TimeSpan(0, 0, 0); }
                else if (cASCII.Compare(lAlphaZone, kEDT, false)) { lDateTimeKind = DateTimeKind.Utc; lZone = new TimeSpan(-4, 0, 0); }
                else if (cASCII.Compare(lAlphaZone, kEST, false) || cASCII.Compare(lAlphaZone, kCDT, false)) { lDateTimeKind = DateTimeKind.Utc; lZone = new TimeSpan(-5, 0, 0); }
                else if (cASCII.Compare(lAlphaZone, kCST, false) || cASCII.Compare(lAlphaZone, kMDT, false)) { lDateTimeKind = DateTimeKind.Utc; lZone = new TimeSpan(-6, 0, 0); }
                else if (cASCII.Compare(lAlphaZone, kMST, false) || cASCII.Compare(lAlphaZone, kPDT, false)) { lDateTimeKind = DateTimeKind.Utc; lZone = new TimeSpan(-7, 0, 0); }
                else if (cASCII.Compare(lAlphaZone, kPST, false)) { lDateTimeKind = DateTimeKind.Utc; lZone = new TimeSpan(-8, 0, 0); }
                else
                {
                    lDateTimeKind = DateTimeKind.Local;
                    lZone = new TimeSpan(0, 0, 0);
                }
            }
            else { Position = lBookmark; rDateTime = new DateTime(); return false; }

            // optional trailing spaces
            SkipRFC822CFWS();

            // convert to a datetime
            try { rDateTime = new DateTime((int)lYear, lMonth, (int)lDay, (int)lHour, (int)lMinute, (int)lSecond, lDateTimeKind) - lZone; }
            catch { Position = lBookmark; rDateTime = new DateTime(); return false; }

            // done
            return true;

            // helper function
            bool LGetZone(out TimeSpan rTimeSpan)
            {
                if (!GetNumber(out _, out uint lHours, 2, 2) || lHours > 99) { rTimeSpan = new TimeSpan(); return false; }
                if (!GetNumber(out _, out uint lMinutes, 2, 2) || lMinutes > 59) { rTimeSpan = new TimeSpan(); return false; }
                rTimeSpan = new TimeSpan((int)lHours, (int)lMinutes, 0);
                return true;
            }
        }
    }
}