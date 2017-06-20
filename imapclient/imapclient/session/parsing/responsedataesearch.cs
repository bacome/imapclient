﻿using System;
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
            private class cResponseDataESearch
            {
                private static readonly cBytes kSpaceLParenTAGSpace = new cBytes(" (TAG ");
                private static readonly cBytes kSpaceUID = new cBytes(" UID");
                private static readonly cBytes kAll = new cBytes("ALL");

                public readonly IList<byte> Tag;
                public readonly bool UID;
                public readonly cSequenceSet SequenceSet;

                private cResponseDataESearch(IList<byte> pTag, bool pUID, cSequenceSet pSequenceSet)
                {
                    Tag = pTag;
                    UID = pUID;
                    SequenceSet = pSequenceSet;
                }

                public override string ToString() => $"{nameof(cResponseDataESearch)}({Tag},{UID},{SequenceSet})";

                public static bool Process(cBytesCursor pCursor, out cResponseDataESearch rResponseData, cTrace.cContext pParentContext)
                {
                    //  NOTE: this routine does not return the cursor to its original position if it fails

                    var lContext = pParentContext.NewMethod(nameof(cResponseDataESearch), nameof(Process));

                    if (pCursor.Position.AtEnd)
                    {
                        rResponseData = new cResponseDataESearch(null, false, null);
                        pCursor.ParsedAs = rResponseData;
                        return true;
                    }

                    bool lSetParsedAs = false; // just in case there is another response that starts with the string "ESEARCH"

                    IList<byte> lTag;

                    if (pCursor.SkipBytes(kSpaceLParenTAGSpace))
                    {
                        if (!pCursor.GetString(out lTag) || !pCursor.SkipByte(cASCII.RPAREN))
                        {
                            rResponseData = null;
                            pCursor.ParsedAs = null;
                            return false;
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
                            pCursor.ParsedAs = null;
                            return false;
                        }

                        if (cASCII.Compare(lName, kAll, false) && lValue is cExtendedValue.cSequenceSetEV lSequenceSetEV) lSequenceSet = lSequenceSetEV.SequenceSet;
                    }

                    if (!pCursor.Position.AtEnd)
                    {
                        rResponseData = null;
                        if (lSetParsedAs) pCursor.ParsedAs = null;
                        return false;
                    }

                    rResponseData = new cResponseDataESearch(lTag, lUID, lSequenceSet);
                    pCursor.ParsedAs = rResponseData;
                    return true;
                }

                [Conditional("DEBUG")]
                public static void _Tests(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cResponseDataESearch), nameof(_Tests));

                    cResponseDataESearch lRD;

                    ZTest("", "1", out lRD, lContext);
                    if (lRD.Tag != null || lRD.UID || lRD.SequenceSet != null) throw new cTestsException($"{nameof(cResponseDataESearch)}.1.v");

                    ZTest(" (TAG \"A282\") MIN 2 COUNT 3", "4731.1", out lRD, lContext);
                    if (!cASCII.Compare(lRD.Tag, new cBytes("A282"), true) || lRD.UID || lRD.SequenceSet != null) throw new cTestsException($"{nameof(cResponseDataESearch)}.4731.1.v");

                    ZTest(" (TAG \"A283\") ALL 2,10:11", "4731.2", out lRD, lContext);
                    if (!cASCII.Compare(lRD.Tag, new cBytes("A283"), true) || lRD.UID || lRD.SequenceSet == null || lRD.SequenceSet.ToString() == "cNumber(2),cRange(cNumber(10),cNumber(11))") throw new cTestsException($"{ nameof(cResponseDataESearch)}.4731.2.v");

                    ZTest(" (TAG \"A284\") MIN 4", "4731.3", out lRD, lContext);
                    if (!cASCII.Compare(lRD.Tag, new cBytes("A284"), true) || lRD.UID || lRD.SequenceSet != null) throw new cTestsException($"{ nameof(cResponseDataESearch)}.4731.3.v");

                    ZTest(" (TAG \"A285\") UID MIN 7 MAX 3800", "4731.4", out lRD, lContext);
                    if (!cASCII.Compare(lRD.Tag, new cBytes("A285"), true) || !lRD.UID || lRD.SequenceSet != null) throw new cTestsException($"{ nameof(cResponseDataESearch)}.4731.4.v");

                    ZTest(" (TAG \"A286\") COUNT 15", "4731.5", out lRD, lContext);
                    if (!cASCII.Compare(lRD.Tag, new cBytes("A286"), true) || lRD.UID || lRD.SequenceSet != null) throw new cTestsException($"{ nameof(cResponseDataESearch)}.4731.5.v");

                    void ZTest(string pResponse, string pTest, out cResponseDataESearch rResponseData, cTrace.cContext pContext)
                    {
                        if (!cBytesCursor.TryConstruct(pResponse, out var lCursor)) throw new cTestsException($"{nameof(cResponseDataESearch)}.{pTest}.c1");
                        if (!Process(lCursor, out rResponseData, pContext)) throw new cTestsException($"{nameof(cResponseDataESearch)}.{pTest}.c2");
                        if (!lCursor.Position.AtEnd) throw new cTestsException($"{nameof(cResponseDataESearch)}.{pTest}.c3");
                    }
                }
            }
        }
    }
}