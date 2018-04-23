using System;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cResponseTextCodeParserSelect : iResponseTextCodeParser
            {
                private static readonly cBytes kPermanentFlags = new cBytes("PERMANENTFLAGS");
                private static readonly cBytes kUIDNext = new cBytes("UIDNEXT");
                private static readonly cBytes kUIDValidity = new cBytes("UIDVALIDITY");
                private static readonly cBytes kHighestModSeq = new cBytes("HIGHESTMODSEQ");
                private static readonly cBytes kReadWrite = new cBytes("READ-WRITE");
                private static readonly cBytes kReadOnly = new cBytes("READ-ONLY");

                private cIMAPCapabilities mCapabilities;

                public cResponseTextCodeParserSelect(cIMAPCapabilities pCapabilities)
                {
                    mCapabilities = pCapabilities ?? throw new ArgumentNullException(nameof(pCapabilities));
                }

                public bool Process(cByteList pCode, cByteList pArguments, out cResponseData rResponseData, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cResponseTextCodeParserSelect), nameof(Process));
                    
                    if (pCode.Equals(kPermanentFlags))
                    {
                        if (pArguments != null)
                        {
                            cBytesCursor lCursor = new cBytesCursor(pArguments);

                            if (lCursor.GetFlags(out var lRawFlags) && lCursor.Position.AtEnd && cPermanentFlags.TryConstruct(lRawFlags, out var lFlags))
                            {
                                rResponseData = new cResponseDataPermanentFlags(lFlags);
                                return true;
                            }
                        }

                        lContext.TraceWarning("likely malformed permanentflags");

                        rResponseData = null;
                        return false;
                    }

                    if (pCode.Equals(kUIDNext))
                    {
                        if (pArguments != null)
                        {
                            cBytesCursor lCursor = new cBytesCursor(pArguments);

                            if (lCursor.GetNZNumber(out _, out var lNumber) && lCursor.Position.AtEnd)
                            {
                                rResponseData = new cResponseDataUIDNext(lNumber);
                                return true;
                            }
                        }

                        lContext.TraceWarning("likely malformed uidnext");

                        rResponseData = null;
                        return false;
                    }

                    if (pCode.Equals(kUIDValidity))
                    {
                        if (pArguments != null)
                        {
                            cBytesCursor lCursor = new cBytesCursor(pArguments);

                            if (lCursor.GetNZNumber(out _, out var lNumber) && lCursor.Position.AtEnd)
                            {
                                rResponseData = new cResponseDataUIDValidity(lNumber);
                                return true;
                            }
                        }

                        lContext.TraceWarning("likely malformed uidvalidity");

                        rResponseData = null;
                        return false;
                    }

                    if (mCapabilities.CondStore)
                    {
                        if (pCode.Equals(kHighestModSeq))
                        {
                            if (pArguments != null)
                            {
                                cBytesCursor lCursor = new cBytesCursor(pArguments);

                                if (lCursor.GetNZNumber(out _, out var lNumber) && lCursor.Position.AtEnd)
                                {
                                    rResponseData = new cResponseDataHighestModSeq(lNumber);
                                    return true;
                                }
                            }

                            lContext.TraceWarning("likely malformed highestmodseq");

                            rResponseData = null;
                            return false;
                        }
                    }

                    if (pCode.Equals(kReadWrite) && pArguments == null)
                    {
                        rResponseData = new cResponseDataAccess(false);
                        return true;
                    }

                    if (pCode.Equals(kReadOnly) && pArguments == null)
                    {
                        rResponseData = new cResponseDataAccess(true);
                        return true;
                    }

                    rResponseData = null;
                    return false;
                }
            }
        }
    }
}