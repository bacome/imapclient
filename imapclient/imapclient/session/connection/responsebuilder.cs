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
            private partial class cConnection
            {
                private class cResponseBuilder
                {
                    private List<cBytesLine> mLines = new List<cBytesLine>();
                    private cByteList mBytes = new cByteList();

                    private bool mBufferedCR = false; // true if we've read a CR and are waiting for an LF
                    private uint mBytesToGo = 0; // when reading in a literal

                    public cResponseBuilder() { }

                    public bool Binary { get; set; } = false;

                    public cBytesLines BuildFromBuffer(byte[] pBuffer, ref int pBufferPosition, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cResponseBuilder), nameof(BuildFromBuffer));

                        while (pBufferPosition < pBuffer.Length)
                        {
                            byte lByte = pBuffer[pBufferPosition++];

                            if (mBytesToGo == 0)
                            {
                                if (mBufferedCR)
                                {
                                    mBufferedCR = false;

                                    if (lByte == cASCII.LF)
                                    {
                                        if (ZBuildLineFromBytes(lContext))
                                        {
                                            cBytesLines lLines = new cBytesLines(mLines);
                                            mLines = new List<cBytesLine>();
                                            return lLines;
                                        }

                                        continue;
                                    }

                                    mBytes.Add(cASCII.CR);
                                }

                                if (lByte == cASCII.CR) mBufferedCR = true;
                                else mBytes.Add(lByte);
                            }
                            else
                            {
                                mBytes.Add(lByte);
                                if (--mBytesToGo == 0) ZAddWholeLine(true, lContext);
                            }
                        }

                        return null;
                    }

                    private void ZAddWholeLine(bool pLiteral, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cResponseBuilder), nameof(ZAddWholeLine), pLiteral);

                        if (!pLiteral && mBytes.Count == 0)
                        {
                            if (!lContext.ContextTraceDelay) lContext.TraceVerbose("received line an empty line");
                            return;
                        }

                        var lLine = new cBytesLine(pLiteral, mBytes);
                        mBytes = new cByteList();

                        if (!lContext.ContextTraceDelay) lContext.TraceVerbose("received {0}", lLine);

                        mLines.Add(lLine);
                    }

                    private bool ZBuildLineFromBytes(cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cResponseBuilder), nameof(ZBuildLineFromBytes));

                        if (mBytes.Count < 3 || mBytes[mBytes.Count - 1] != cASCII.RBRACE)
                        {
                            ZAddWholeLine(false, lContext);
                            return true; // complete
                        }

                        int lBytePosition = mBytes.Count - 2;

                        while (lBytePosition > 0)
                        {
                            byte lByte = mBytes[lBytePosition];
                            if (lByte < cASCII.ZERO || lByte > cASCII.NINE) break;
                            lBytePosition--;
                        }

                        if (lBytePosition == mBytes.Count - 2 || mBytes[lBytePosition] != cASCII.LBRACE)
                        {
                            ZAddWholeLine(false, lContext);
                            return true; // complete
                        }

                        mBytesToGo = 0;

                        checked
                        {
                            for (int i = lBytePosition + 1; i < mBytes.Count - 1; i++) mBytesToGo = mBytesToGo * 10 + mBytes[i] - cASCII.ZERO;
                        }

                        if (Binary && lBytePosition != 0 && mBytes[lBytePosition - 1] == cASCII.TILDA) lBytePosition--;

                        mBytes.RemoveRange(lBytePosition, mBytes.Count - lBytePosition);
                        ZAddWholeLine(false, lContext);

                        if (mBytesToGo == 0)
                        {
                            if (!lContext.ContextTraceDelay) lContext.TraceVerbose("adding empty literal line");
                            mLines.Add(new cBytesLine(true, new byte[0]));
                        }
                        else if(!lContext.ContextTraceDelay) lContext.TraceVerbose("expecting literal of length {0}", mBytesToGo);

                        return false; // still need more bytes
                    }

                    [Conditional("DEBUG")]
                    public static void _Tests(cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cResponseBuilder), nameof(_Tests));

                        cResponseBuilder lBuilder;
                        byte[] lBytes;
                        int lBufferPosition;
                        cBytesLines lLines;
                        cBytesLine lLine;

                        lBuilder = new cResponseBuilder();
                        lBytes = LMakeBuffer("fred\r\n");
                        lBufferPosition = 0;

                        lLines = lBuilder.BuildFromBuffer(lBytes, ref lBufferPosition, lContext);

                        if (lBufferPosition != lBytes.Length) throw new cTestsException("the buffer should have been fully read", lContext);
                        if (lLines == null) throw new cTestsException("a response should have been generated", lContext);
                        if (lLines.Count != 1) throw new cTestsException("there should have been one line", lContext);
                        if (!cASCII.Compare(lLines[0], new cBytes("fred"), false)) throw new cTestsException("the line should have been fred", lContext);
                        if (lLines[0].Literal) throw new cTestsException("the line should not have been a literal", lContext);


                        lBuilder = new cResponseBuilder();
                        lBytes = LMakeBuffer("fred{5}\r\nanguschar");
                        lBufferPosition = 0;

                        lLines = lBuilder.BuildFromBuffer(lBytes, ref lBufferPosition, lContext);

                        if (lBufferPosition != lBytes.Length) throw new cTestsException("the buffer should have been fully read", lContext);
                        if (lLines != null) throw new cTestsException("no response should have been generated", lContext);

                        lBytes = LMakeBuffer("lie\r\nfred\r\n");
                        lBufferPosition = 0;

                        lLines = lBuilder.BuildFromBuffer(lBytes, ref lBufferPosition, lContext);

                        if (lBufferPosition == lBytes.Length) throw new cTestsException("the buffer should not have been fully read", lContext);
                        if (lLines == null) throw new cTestsException("a response should have been generated", lContext);
                        if (lLines.Count != 3) throw new cTestsException("there should have been three lines", lContext);

                        lLine = lLines[0];
                        if (!cASCII.Compare(lLine, new cBytes("fred"), false) || lLine.Literal) throw new cTestsException("the line should have been fred", lContext);

                        lLine = lLines[1];
                        if (!cASCII.Compare(lLine, new cBytes("angus"), false) || !lLine.Literal) throw new cTestsException("the line should have been literal angus", lContext);

                        lLine = lLines[2];
                        if (!cASCII.Compare(lLine, new cBytes("charlie"), false) || lLine.Literal) throw new cTestsException("the line should have been charlie", lContext);

                        lLines = lBuilder.BuildFromBuffer(lBytes, ref lBufferPosition, lContext);

                        if (lBufferPosition != lBytes.Length) throw new cTestsException("the buffer should have been fully read", lContext);
                        if (lLines == null) throw new cTestsException("a response should have been generated", lContext);
                        if (lLines.Count != 1) throw new cTestsException("there should have been one line", lContext);
                        if (!cASCII.Compare(lLines[0], new cBytes("fred"), false)) throw new cTestsException("the line should have been fred", lContext);
                        if (lLines[0].Literal) throw new cTestsException("the line should not have been a literal", lContext);


                        lBuilder = new cResponseBuilder();
                        lBytes = LMakeBuffer("f\ra\n\r\n");
                        lBufferPosition = 0;

                        lLines = lBuilder.BuildFromBuffer(lBytes, ref lBufferPosition, lContext);

                        if (lBufferPosition != lBytes.Length) throw new cTestsException("the buffer should have been fully read", lContext);
                        if (lLines == null) throw new cTestsException("a response should have been generated", lContext);
                        if (lLines.Count != 1) throw new cTestsException("there should have been one line", lContext);
                        if (!cASCII.Compare(lLines[0], new cBytes("f\ra\n"), false)) throw new cTestsException("the line should have been fran", lContext);
                        if (lLines[0].Literal) throw new cTestsException("the line should not have been a literal", lContext);


                        lBuilder = new cResponseBuilder();
                        lBytes = LMakeBuffer("{}\r\n");
                        lBufferPosition = 0;

                        lLines = lBuilder.BuildFromBuffer(lBytes, ref lBufferPosition, lContext);

                        if (lBufferPosition != lBytes.Length) throw new cTestsException("the buffer should have been fully read", lContext);
                        if (lLines == null) throw new cTestsException("a response should have been generated", lContext);
                        if (lLines.Count != 1) throw new cTestsException("there should have been one line", lContext);
                        if (!cASCII.Compare(lLines[0], new cBytes("{}"), false)) throw new cTestsException("the line should have been {}", lContext);
                        if (lLines[0].Literal) throw new cTestsException("the line should not have been a literal", lContext);



                        lBuilder = new cResponseBuilder();
                        lBytes = LMakeBuffer("fred{}\r\n");
                        lBufferPosition = 0;

                        lLines = lBuilder.BuildFromBuffer(lBytes, ref lBufferPosition, lContext);

                        if (lBufferPosition != lBytes.Length) throw new cTestsException("the buffer should have been fully read", lContext);
                        if (lLines == null) throw new cTestsException("a response should have been generated", lContext);
                        if (lLines.Count != 1) throw new cTestsException("there should have been one line", lContext);
                        if (!cASCII.Compare(lLines[0], new cBytes("fred{}"), false)) throw new cTestsException("the line should have been fred{}", lContext);
                        if (lLines[0].Literal) throw new cTestsException("the line should not have been a literal", lContext);


                        lBuilder = new cResponseBuilder();
                        lBytes = LMakeBuffer("fred{a}\r\n");
                        lBufferPosition = 0;

                        lLines = lBuilder.BuildFromBuffer(lBytes, ref lBufferPosition, lContext);

                        if (lBufferPosition != lBytes.Length) throw new cTestsException("the buffer should have been fully read", lContext);
                        if (lLines == null) throw new cTestsException("a response should have been generated", lContext);
                        if (lLines.Count != 1) throw new cTestsException("there should have been one line", lContext);
                        if (!cASCII.Compare(lLines[0], new cBytes("fred{a}"), false)) throw new cTestsException("the line should have been fred{a}", lContext);
                        if (lLines[0].Literal) throw new cTestsException("the line should not have been a literal", lContext);


                        lBuilder = new cResponseBuilder();
                        lBytes = LMakeBuffer("fred123}\r\n");
                        lBufferPosition = 0;

                        lLines = lBuilder.BuildFromBuffer(lBytes, ref lBufferPosition, lContext);

                        if (lBufferPosition != lBytes.Length) throw new cTestsException("the buffer should have been fully read", lContext);
                        if (lLines == null) throw new cTestsException("a response should have been generated", lContext);
                        if (lLines.Count != 1) throw new cTestsException("there should have been one line", lContext);
                        if (!cASCII.Compare(lLines[0], new cBytes("fred123}"), false)) throw new cTestsException("the line should have been fred123}", lContext);
                        if (lLines[0].Literal) throw new cTestsException("the line should not have been a literal", lContext);



                        lBuilder = new cResponseBuilder();
                        lBytes = LMakeBuffer("fred{12345678901}\r\n");
                        lBufferPosition = 0;

                        bool lException = false;

                        try { lLines = lBuilder.BuildFromBuffer(lBytes, ref lBufferPosition, lContext); }
                        catch { lException = true; }
                        if (!lException) throw new cTestsException("expected failure", lContext);



                        lBuilder = new cResponseBuilder();
                        lBytes = LMakeBuffer("fred{0}\r\n{00}\r\n\r\n");
                        lBufferPosition = 0;

                        lLines = lBuilder.BuildFromBuffer(lBytes, ref lBufferPosition, lContext);

                        if (lBufferPosition != lBytes.Length) throw new cTestsException("the buffer should have been fully read", lContext);
                        if (lLines == null) throw new cTestsException("a response should have been generated", lContext);
                        if (lLines.Count != 3) throw new cTestsException("there should have been three lines", lContext);

                        lLine = lLines[0];
                        if (!cASCII.Compare(lLine, new cBytes("fred"), false) || lLine.Literal) throw new cTestsException("the line should have been fred", lContext);

                        lLine = lLines[1];
                        if (lLine.Count != 0 || !lLine.Literal) throw new cTestsException("the line should have been empty literal", lContext);

                        lLine = lLines[2];
                        if (lLine.Count != 0 || !lLine.Literal) throw new cTestsException("the line should have been empty literal", lContext);



                        // test literal8

                        lBuilder = new cResponseBuilder();
                        lBuilder.Binary = true;
                        lBytes = LMakeBuffer("fred~{0}\r\n~{00}\r\n\r\n");
                        lBufferPosition = 0;

                        lLines = lBuilder.BuildFromBuffer(lBytes, ref lBufferPosition, lContext);

                        if (lBufferPosition != lBytes.Length) throw new cTestsException("the buffer should have been fully read", lContext);
                        if (lLines == null) throw new cTestsException("a response should have been generated", lContext);
                        if (lLines.Count != 3) throw new cTestsException("there should have been three lines", lContext);

                        lLine = lLines[0];
                        if (!cASCII.Compare(lLine, new cBytes("fred"), false) || lLine.Literal) throw new cTestsException("the line should have been fred", lContext);

                        lLine = lLines[1];
                        if (lLine.Count != 0 || !lLine.Literal) throw new cTestsException("the line should have been empty literal", lContext);

                        lLine = lLines[2];
                        if (lLine.Count != 0 || !lLine.Literal) throw new cTestsException("the line should have been empty literal", lContext);

                        byte[] LMakeBuffer(string pString)
                        {
                            byte[] lResult = new byte[pString.Length];
                            for (int i = 0, j = 0; i < pString.Length; i++, j++) lResult[j] = (byte)pString[i];
                            return lResult;
                        }
                    }
                }
            }
        }
    }
}