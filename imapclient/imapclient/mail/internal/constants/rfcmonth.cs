using System;
using System.Collections.ObjectModel;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    internal static class cRFCMonth
    {
        public const string cJan = "JAN";
        public const string cFeb = "FEB";
        public const string cMar = "MAR";
        public const string cApr = "APR";
        public const string cMay = "MAY";
        public const string cJun = "JUN";
        public const string cJul = "JUL";
        public const string cAug = "AUG";
        public const string cSep = "SEP";
        public const string cOct = "OCT";
        public const string cNov = "NOV";
        public const string cDec = "DEC";

        public static readonly cBytes aJan = new cBytes(cJan);
        public static readonly cBytes aFeb = new cBytes(cFeb);
        public static readonly cBytes aMar = new cBytes(cMar);
        public static readonly cBytes aApr = new cBytes(cApr);
        public static readonly cBytes aMay = new cBytes(cMay);
        public static readonly cBytes aJun = new cBytes(cJun);
        public static readonly cBytes aJul = new cBytes(cJul);
        public static readonly cBytes aAug = new cBytes(cAug);
        public static readonly cBytes aSep = new cBytes(cSep);
        public static readonly cBytes aOct = new cBytes(cOct);
        public static readonly cBytes aNov = new cBytes(cNov);
        public static readonly cBytes aDec = new cBytes(cDec);

        public static readonly ReadOnlyCollection<string> cName = Array.AsReadOnly(new string[] { cJan, cFeb, cMar, cApr, cMay, cJun, cJul, cAug, cSep, cOct, cNov, cDec });
        public static readonly ReadOnlyCollection<cBytes> aName = Array.AsReadOnly(new cBytes[] { aJan, aFeb, aMar, aApr, aMay, aJun, aJul, aAug, aSep, aOct, aNov, aDec });
    }
}