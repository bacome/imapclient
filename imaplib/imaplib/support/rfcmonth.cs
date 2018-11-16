using System;
using System.Collections.ObjectModel;

namespace work.bacome.imapsupport
{
    /// <summary>
    /// RFC month constants.
    /// </summary>
    public static class kRFCMonth
    {
        /** <summary>The string for January.</summary> **/
        public const string cJan = "JAN";
        /** <summary>The string for February.</summary> **/
        public const string cFeb = "FEB";
        /** <summary>The string for March.</summary> **/
        public const string cMar = "MAR";
        /** <summary>The string for April.</summary> **/
        public const string cApr = "APR";
        /** <summary>The string for May.</summary> **/
        public const string cMay = "MAY";
        /** <summary>The string for June.</summary> **/
        public const string cJun = "JUN";
        /** <summary>The string for July.</summary> **/
        public const string cJul = "JUL";
        /** <summary>The string for August.</summary> **/
        public const string cAug = "AUG";
        /** <summary>The string for September.</summary> **/
        public const string cSep = "SEP";
        /** <summary>The string for October.</summary> **/
        public const string cOct = "OCT";
        /** <summary>The string for November.</summary> **/
        public const string cNov = "NOV";
        /** <summary>The string for December.</summary> **/
        public const string cDec = "DEC";

        /** <summary>The bytes for January.</summary> **/
        public static readonly cBytes aJan = new cBytes(cJan);
        /** <summary>The bytes for February.</summary> **/
        public static readonly cBytes aFeb = new cBytes(cFeb);
        /** <summary>The bytes for March.</summary> **/
        public static readonly cBytes aMar = new cBytes(cMar);
        /** <summary>The bytes for April.</summary> **/
        public static readonly cBytes aApr = new cBytes(cApr);
        /** <summary>The bytes for May.</summary> **/
        public static readonly cBytes aMay = new cBytes(cMay);
        /** <summary>The bytes for June.</summary> **/
        public static readonly cBytes aJun = new cBytes(cJun);
        /** <summary>The bytes for July.</summary> **/
        public static readonly cBytes aJul = new cBytes(cJul);
        /** <summary>The bytes for August.</summary> **/
        public static readonly cBytes aAug = new cBytes(cAug);
        /** <summary>The bytes for September.</summary> **/
        public static readonly cBytes aSep = new cBytes(cSep);
        /** <summary>The bytes for October.</summary> **/
        public static readonly cBytes aOct = new cBytes(cOct);
        /** <summary>The bytes for November.</summary> **/
        public static readonly cBytes aNov = new cBytes(cNov);
        /** <summary>The bytes for December.</summary> **/
        public static readonly cBytes aDec = new cBytes(cDec);

        /// <summary>
        /// A collection of month strings in order.
        /// </summary>
        public static readonly ReadOnlyCollection<string> cName = Array.AsReadOnly(new string[] { cJan, cFeb, cMar, cApr, cMay, cJun, cJul, cAug, cSep, cOct, cNov, cDec });

        /// <summary>
        /// A collection of month bytes in order.
        /// </summary>
        public static readonly ReadOnlyCollection<cBytes> aName = Array.AsReadOnly(new cBytes[] { aJan, aFeb, aMar, aApr, aMay, aJun, aJul, aAug, aSep, aOct, aNov, aDec });
    }
}