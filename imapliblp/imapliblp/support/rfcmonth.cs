using System;
using System.Collections.ObjectModel;

namespace work.bacome.imapsupport
{
    /// <summary>
    /// A collection of constants used when parsing internet format dates.
    /// </summary>
    public static class kRFCMonth
    {
        /** <summary>A constant used when parsing internet format dates.</summary> **/
        public const string cJan = "JAN";
        /** <summary>A constant used when parsing internet format dates.</summary> **/
        public const string cFeb = "FEB";
        /** <summary>A constant used when parsing internet format dates.</summary> **/
        public const string cMar = "MAR";
        /** <summary>A constant used when parsing internet format dates.</summary> **/
        public const string cApr = "APR";
        /** <summary>A constant used when parsing internet format dates.</summary> **/
        public const string cMay = "MAY";
        /** <summary>A constant used when parsing internet format dates.</summary> **/
        public const string cJun = "JUN";
        /** <summary>A constant used when parsing internet format dates.</summary> **/
        public const string cJul = "JUL";
        /** <summary>A constant used when parsing internet format dates.</summary> **/
        public const string cAug = "AUG";
        /** <summary>A constant used when parsing internet format dates.</summary> **/
        public const string cSep = "SEP";
        /** <summary>A constant used when parsing internet format dates.</summary> **/
        public const string cOct = "OCT";
        /** <summary>A constant used when parsing internet format dates.</summary> **/
        public const string cNov = "NOV";
        /** <summary>A constant used when parsing internet format dates.</summary> **/
        public const string cDec = "DEC";

        /** <summary>A constant used when parsing internet format dates.</summary> **/
        public static readonly cBytes aJan = new cBytes(cJan);
        /** <summary>A constant used when parsing internet format dates.</summary> **/
        public static readonly cBytes aFeb = new cBytes(cFeb);
        /** <summary>A constant used when parsing internet format dates.</summary> **/
        public static readonly cBytes aMar = new cBytes(cMar);
        /** <summary>A constant used when parsing internet format dates.</summary> **/
        public static readonly cBytes aApr = new cBytes(cApr);
        /** <summary>A constant used when parsing internet format dates.</summary> **/
        public static readonly cBytes aMay = new cBytes(cMay);
        /** <summary>A constant used when parsing internet format dates.</summary> **/
        public static readonly cBytes aJun = new cBytes(cJun);
        /** <summary>A constant used when parsing internet format dates.</summary> **/
        public static readonly cBytes aJul = new cBytes(cJul);
        /** <summary>A constant used when parsing internet format dates.</summary> **/
        public static readonly cBytes aAug = new cBytes(cAug);
        /** <summary>A constant used when parsing internet format dates.</summary> **/
        public static readonly cBytes aSep = new cBytes(cSep);
        /** <summary>A constant used when parsing internet format dates.</summary> **/
        public static readonly cBytes aOct = new cBytes(cOct);
        /** <summary>A constant used when parsing internet format dates.</summary> **/
        public static readonly cBytes aNov = new cBytes(cNov);
        /** <summary>A constant used when parsing internet format dates.</summary> **/
        public static readonly cBytes aDec = new cBytes(cDec);

        /** <summary>A collection of constants used when parsing internet format dates.</summary> **/
        public static readonly ReadOnlyCollection<string> cName = Array.AsReadOnly(new string[] { cJan, cFeb, cMar, cApr, cMay, cJun, cJul, cAug, cSep, cOct, cNov, cDec });
        /** <summary>A collection of constants used when parsing internet format dates.</summary> **/
        public static readonly ReadOnlyCollection<cBytes> aName = Array.AsReadOnly(new cBytes[] { aJan, aFeb, aMar, aApr, aMay, aJun, aJul, aAug, aSep, aOct, aNov, aDec });
    }
}