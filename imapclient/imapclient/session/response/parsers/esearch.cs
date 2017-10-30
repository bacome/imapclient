using System;
using System.Collections.Generic;
using System.Diagnostics;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cResponseDataParserESearch : iResponseDataParser
            {
                private static readonly cBytes kESearch = new cBytes("ESEARCH");

                private static readonly cBytes kSpaceLParenTAGSpace = new cBytes(" (TAG ");
                private static readonly cBytes kSpaceUID = new cBytes(" UID");
                private static readonly cBytes kAll = new cBytes("ALL");

                public cResponseDataParserESearch() { }

                public bool Process(cBytesCursor pCursor, out cResponseData rResponseData, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cResponseDataParserESearch), nameof(Process));

                    if (!pCursor.SkipBytes(kESearch)) { rResponseData = null; return false; }

                    if (pCursor.Position.AtEnd)
                    {
                        rResponseData = new cResponseDataESearch(null, false, null);
                        return true;
                    }

                    bool lSetParsedAs = false; // just in case there is another response that starts with the string "ESEARCH"

                    IList<byte> lTag;

                    if (pCursor.SkipBytes(kSpaceLParenTAGSpace))
                    {
                        if (!pCursor.GetString(out lTag) || !pCursor.SkipByte(cASCII.RPAREN))
                        {
                            rResponseData = null;
                            return true;
                        }

                        lSetParsedAs = true;
                    }
                    else lTag = null;

                    bool lUID;

                    if (pCursor.SkipBytes(kSpaceUID))
                    {
                        lUID = true;
                        lSetParsedAs = true;
                    }
                    else lUID = false;

                    cSequenceSet lSequenceSet = null;

                    while (true)
                    {
                        if (!pCursor.SkipByte(cASCII.SPACE)) break;

                        lSetParsedAs = true;

                        if (!pCursor.GetToken(cCharset.Atom, null, null, out cByteList lName) ||
                            !pCursor.SkipByte(cASCII.SPACE) ||
                            !pCursor.ProcessExtendedValue(out var lValue)
                           )
                        {
                            rResponseData = null;
                            return true;
                        }

                        if (cASCII.Compare(lName, kAll, false) && lValue is cExtendedValue.cSequenceSetEV lSequenceSetEV) lSequenceSet = lSequenceSetEV.SequenceSet;
                    }

                    if (!pCursor.Position.AtEnd)
                    {
                        rResponseData = null;
                        return lSetParsedAs;
                    }

                    rResponseData = new cResponseDataESearch(lTag, lUID, lSequenceSet);
                    return true;
                }

                [Conditional("DEBUG")]
                public static void _Tests(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cResponseDataESearch), nameof(_Tests));

                    cResponseDataParserESearch lRDP = new cResponseDataParserESearch();
                    cResponseDataESearch lRDES;

                    LTest("esearch", "1");
                    if (lRDES.Tag != null || lRDES.UID || lRDES.SequenceSet != null) throw new cTestsException($"{nameof(cResponseDataESearch)}.1.v");

                    LTest("esearch (TAG \"A282\") MIN 2 COUNT 3", "4731.1");
                    if (!cASCII.Compare(lRDES.Tag, new cBytes("A282"), true) || lRDES.UID || lRDES.SequenceSet != null) throw new cTestsException($"{nameof(cResponseDataESearch)}.4731.1.v");

                    LTest("esearch (TAG \"A283\") ALL 2,10:11", "4731.2");
                    if (!cASCII.Compare(lRDES.Tag, new cBytes("A283"), true) || lRDES.UID || lRDES.SequenceSet == null || lRDES.SequenceSet.ToString() == "cNumber(2),cRange(cNumber(10),cNumber(11))") throw new cTestsException($"{ nameof(cResponseDataESearch)}.4731.2.v");

                    LTest("ESEARCH (TAG \"A284\") MIN 4", "4731.3");
                    if (!cASCII.Compare(lRDES.Tag, new cBytes("A284"), true) || lRDES.UID || lRDES.SequenceSet != null) throw new cTestsException($"{ nameof(cResponseDataESearch)}.4731.3.v");

                    LTest("ESEARCH (TAG \"A285\") UID MIN 7 MAX 3800", "4731.4");
                    if (!cASCII.Compare(lRDES.Tag, new cBytes("A285"), true) || !lRDES.UID || lRDES.SequenceSet != null) throw new cTestsException($"{ nameof(cResponseDataESearch)}.4731.4.v");

                    LTest("ESEARCH (TAG \"A286\") COUNT 15", "4731.5");
                    if (!cASCII.Compare(lRDES.Tag, new cBytes("A286"), true) || lRDES.UID || lRDES.SequenceSet != null) throw new cTestsException($"{ nameof(cResponseDataESearch)}.4731.5.v");

                    void LTest(string pResponse, string pTest)
                    {
                        var lCursor = new cBytesCursor(pResponse);
                        if (!lRDP.Process(lCursor, out var lRD, lContext)) throw new cTestsException($"{nameof(cResponseDataESearch)}.{pTest}.c2");
                        if (!lCursor.Position.AtEnd) throw new cTestsException($"{nameof(cResponseDataESearch)}.{pTest}.c3");
                        lRDES = lRD as cResponseDataESearch;
                        if (lRDES == null) throw new cTestsException($"{nameof(cResponseDataESearch)}.{pTest}.c4");
                    }
                }
            }
        }
    }
}