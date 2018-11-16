using System;
using work.bacome.imapsupport;

namespace work.bacome.imapclient
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

        public bool GetRFC822DateTime(out cTimestamp rTimestamp)
        {
            var lBookmark = Position;

            // optional leading spaces
            SkipRFC822CFWS();

            // day-of-week (optional, ignored)
            if (SkipBytes(kMon) || SkipBytes(kTue) || SkipBytes(kWed) || SkipBytes(kThu) || SkipBytes(kFri) || SkipBytes(kSat) || SkipBytes(kSun))
            {
                SkipRFC822CFWS(); // optional spaces
                if (!SkipByte(cASCII.COMMA)) { Position = lBookmark; rTimestamp = null; return false; }
                SkipRFC822CFWS(); // optional spaces
            }

            // day and month
            if (!GetNumber(out _, out var lDay, 1, 2) || lDay < 1 || lDay > 31) { Position = lBookmark; rTimestamp = null; return false; }
            if (!SkipRFC822CFWS()) { Position = lBookmark; rTimestamp = null; return false; }
            if (!ZGetMonth(out var lMonth)) { Position = lBookmark; rTimestamp = null; return false; }
            if (!SkipRFC822CFWS()) { Position = lBookmark; rTimestamp = null; return false; }

            // year

            if (!GetNumber(out var lBytes, out var lYear, 2, 4)) { Position = lBookmark; rTimestamp = null; return false; }

            if (lBytes.Count == 2)
            {
                if (lYear < 50) lYear = 2000 + lYear;
                else lYear = 1900 + lYear;
            }
            else if (lBytes.Count == 3) lYear = 1900 + lYear;

            // mandatory delimiter between date and time
            if (!SkipRFC822CFWS()) { Position = lBookmark; rTimestamp = null; return false; }

            // hour and minute
            if (!GetNumber(out _, out var lHour, 2, 2) || lHour > 23) { Position = lBookmark; rTimestamp = null; return false; }
            SkipRFC822CFWS();
            if (!SkipByte(cASCII.COLON)) { Position = lBookmark; rTimestamp = null; return false; }
            SkipRFC822CFWS();
            if (!GetNumber(out _, out var lMinute, 2, 2) || lMinute > 59) { Position = lBookmark; rTimestamp = null; return false; }

            // optional second

            bool lDelimiter;

            lDelimiter = SkipRFC822CFWS(); // this is possibly the delimiter between time and zone

            uint lSecond;

            if (SkipByte(cASCII.COLON))
            {
                SkipRFC822CFWS();
                if (!GetNumber(out _, out lSecond, 2, 2) || lSecond > 60) { Position = lBookmark; rTimestamp = null; return false; } // note: 60 is explicitly allowed to cater for leap seconds
                lDelimiter = SkipRFC822CFWS(); // this the delimiter between time and zone
            }
            else lSecond = 0;

            // check that there was a delimiter
            if (!lDelimiter) { Position = lBookmark; rTimestamp = null; return false; }

            // zone

            bool lWest;
            uint lzHH;
            uint lzMM;

            if (SkipByte(cASCII.PLUS))
            {
                lWest = false;
                if (!GetNumber(out _, out lzHH, 2, 2) || lzHH > 99) { Position = lBookmark; rTimestamp = null; return false; }
                if (!GetNumber(out _, out lzMM, 2, 2) || lzMM > 59) { Position = lBookmark; rTimestamp = null; return false; }
            }
            else if (SkipByte(cASCII.HYPEN))
            {
                lWest = true;
                if (!GetNumber(out _, out lzHH, 2, 2) || lzHH > 99) { Position = lBookmark; rTimestamp = null; return false; }
                if (!GetNumber(out _, out lzMM, 2, 2) || lzMM > 59) { Position = lBookmark; rTimestamp = null; return false; }

            }
            else if (GetToken(cCharset.Alpha, null, null, out var lAlphaZone, 1))
            {
                // obsolete zone format

                if (cASCII.Compare(lAlphaZone, kUT, false) || cASCII.Compare(lAlphaZone, kGMT, false))
                {
                    lWest = false;
                    lzHH = 0;
                }
                else if (cASCII.Compare(lAlphaZone, kEDT, false))
                {
                    lWest = true;
                    lzHH = 4;
                }
                else if (cASCII.Compare(lAlphaZone, kEST, false) || cASCII.Compare(lAlphaZone, kCDT, false))
                {
                    lWest = true;
                    lzHH = 5;
                }
                else if (cASCII.Compare(lAlphaZone, kCST, false) || cASCII.Compare(lAlphaZone, kMDT, false))
                {
                    lWest = true;
                    lzHH = 6;
                }
                else if (cASCII.Compare(lAlphaZone, kMST, false) || cASCII.Compare(lAlphaZone, kPDT, false))
                {
                    lWest = true;
                    lzHH = 7;
                }
                else if (cASCII.Compare(lAlphaZone, kPST, false))
                {
                    lWest = true;
                    lzHH = 8;
                }
                else
                {
                    lWest = true;
                    lzHH = 0;
                }

                lzMM = 0;
            }
            else { Position = lBookmark; rTimestamp = null; return false; }

            // optional trailing spaces
            SkipRFC822CFWS();

            // generate output

            try
            {
                rTimestamp = new cTimestamp((int)lYear, lMonth, (int)lDay, (int)lHour, (int)lMinute, (int)lSecond, 0, lWest, (int)lzHH, (int)lzMM);
                return true;
            }
            catch
            {
                Position = lBookmark;
                rTimestamp = null;
                return false;
            }

        }
    }
}