﻿using System;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    internal partial class cBytesCursor
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
        private static readonly cBytes kRFC2822UnspecifiedZone = new cBytes("-0000");

        public bool GetRFC822DateTime(out DateTimeOffset rDateTimeOffset, out DateTime rDateTime)
        {
            var lBookmark = Position;

            // hack
            rDateTimeOffset = new DateTimeOffset();
            rDateTime = new DateTime(); // either local or unspecified

            // optional leading spaces
            SkipRFC822CFWS();

            // day-of-week (optional, ignored)
            if (SkipBytes(kMon) || SkipBytes(kTue) || SkipBytes(kWed) || SkipBytes(kThu) || SkipBytes(kFri) || SkipBytes(kSat) || SkipBytes(kSun))
            {
                SkipRFC822CFWS(); // optional spaces
                if (!SkipByte(cASCII.COMMA)) { Position = lBookmark; return false; }
                SkipRFC822CFWS(); // optional spaces
            }

            // day and month
            if (!GetNumber(out _, out var lDay, 1, 2) || lDay < 1 || lDay > 31) { Position = lBookmark; return false; }
            if (!SkipRFC822CFWS()) { Position = lBookmark; return false; }
            if (!ZGetMonth(out var lMonth)) { Position = lBookmark; return false; }
            if (!SkipRFC822CFWS()) { Position = lBookmark; return false; }

            // year

            if (!GetNumber(out var lBytes, out var lYear, 2, 4)) { Position = lBookmark; return false; }

            if (lBytes.Count == 2)
            {
                if (lYear < 50) lYear = 2000 + lYear;
                else lYear = 1900 + lYear;
            }
            else if (lBytes.Count == 3) lYear = 1900 + lYear;

            // mandatory delimiter between date and time
            if (!SkipRFC822CFWS()) { Position = lBookmark; return false; }

            // hour and minute
            if (!GetNumber(out _, out var lHour, 2, 2) || lHour > 23) { Position = lBookmark; return false; }
            SkipRFC822CFWS();
            if (!SkipByte(cASCII.COLON)) { Position = lBookmark; return false; }
            SkipRFC822CFWS();
            if (!GetNumber(out _, out var lMinute, 2, 2) || lMinute > 59) { Position = lBookmark; return false; }

            // optional second

            bool lDelimiter;

            lDelimiter = SkipRFC822CFWS(); // this is possibly the delimiter between time and zone

            uint lSecond;

            if (SkipByte(cASCII.COLON))
            {
                SkipRFC822CFWS();
                if (!GetNumber(out _, out lSecond, 2, 2) || lSecond > 60) { Position = lBookmark; return false; } // note: 60 is explicitly allowed to cater for leap seconds
                if (lSecond == 60) lSecond = 59; // dot net doesn't handle leap seconds
                lDelimiter = SkipRFC822CFWS(); // this the delimiter between time and zone
            }
            else lSecond = 0;

            // check that there was a delimiter
            if (!lDelimiter) { Position = lBookmark; return false; }

            // zone

            TimeSpan lOffset;
            bool lUnspecifiedZone;

            if (SkipBytes(kRFC2822UnspecifiedZone))
            {
                lOffset = TimeSpan.Zero;
                lUnspecifiedZone = true;
            }
            else if (SkipByte(cASCII.PLUS))
            {
                if (!LGetZone(out lOffset)) { Position = lBookmark; return false; }
                lUnspecifiedZone = false;
            }
            else if (SkipByte(cASCII.HYPEN))
            {
                if (!LGetZone(out lOffset)) { Position = lBookmark; return false; }
                lOffset = lOffset.Negate();
                lUnspecifiedZone = false;

            }
            else if (GetToken(cCharset.Alpha, null, null, out var lAlphaZone, 1))
            {
                // obsolete zone format

                if (cASCII.Compare(lAlphaZone, kUT, false) || cASCII.Compare(lAlphaZone, kGMT, false))
                {
                    lOffset = TimeSpan.Zero;
                    lUnspecifiedZone = false;
                }
                else if (cASCII.Compare(lAlphaZone, kEDT, false))
                {
                    lOffset = new TimeSpan(-4, 0, 0);
                    lUnspecifiedZone = false;
                }
                else if (cASCII.Compare(lAlphaZone, kEST, false) || cASCII.Compare(lAlphaZone, kCDT, false))
                {
                    lOffset = new TimeSpan(-5, 0, 0);
                    lUnspecifiedZone = false;
                }
                else if (cASCII.Compare(lAlphaZone, kCST, false) || cASCII.Compare(lAlphaZone, kMDT, false))
                {
                    lOffset = new TimeSpan(-6, 0, 0);
                    lUnspecifiedZone = false;
                }
                else if (cASCII.Compare(lAlphaZone, kMST, false) || cASCII.Compare(lAlphaZone, kPDT, false))
                {
                    lOffset = new TimeSpan(-7, 0, 0);
                    lUnspecifiedZone = false;
                }
                else if (cASCII.Compare(lAlphaZone, kPST, false))
                {
                    lOffset = new TimeSpan(-8, 0, 0);
                    lUnspecifiedZone = false;
                }
                else
                {
                    lOffset = TimeSpan.Zero;
                    lUnspecifiedZone = true;
                }
            }
            else { Position = lBookmark; return false; }

            // optional trailing spaces
            SkipRFC822CFWS();

            // generate output

            try
            {
                rDateTime = new DateTime((int)lYear, lMonth, (int)lDay, (int)lHour, (int)lMinute, (int)lSecond); // kind = unspecified
                rDateTimeOffset = new DateTimeOffset(rDateTime, lOffset);
            }
            catch { Position = lBookmark; return false; }

            if (!lUnspecifiedZone) rDateTime = rDateTimeOffset.LocalDateTime; // kind = local

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