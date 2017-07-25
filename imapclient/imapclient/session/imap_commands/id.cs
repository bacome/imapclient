using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private static readonly cCommandPart kIdCommandPart = new cCommandPart("ID ");

            private cIdDataProcessor mIdResponseDataProcessor;

            public cIdReadOnlyDictionary ServerId => mIdResponseDataProcessor?.Dictionary;

            public async Task IdAsync(cMethodControl pMC, cIdReadOnlyDictionary pClientDictionary, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(IdAsync), pMC, pClientDictionary);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_State != eState.notselected && _State != eState.selected) throw new InvalidOperationException();

                // install the permanant response data processor
                if (mIdResponseDataProcessor == null)
                {
                    mIdResponseDataProcessor = new cIdDataProcessor(mEventSynchroniser);
                    mPipeline.Install(mIdResponseDataProcessor);
                }

                using (var lCommand = new cCommand())
                {
                    ;?; // may not be

                    //  note the lack of locking - this is only called during connect

                    lCommand.Add(kIdCommandPart);

                    if (pClientDictionary == null) lCommand.Add(cCommandPart.Nil);
                    else
                    {
                        lCommand.BeginList(eListBracketing.bracketed);

                        foreach (var lFieldValuePair in pClientDictionary)
                        {
                            ;?; // this won't work
                            lCommand.Add(mStringFactory.AsString(lFieldValuePair.Key));
                            lCommand.Add(mStringFactory.AsNString(lFieldValuePair.Value));
                        }

                        lCommand.EndList();
                    }

                    object lDictionary = mIdResponseDataProcessor.Dictionary;

                    var lResult = await mPipeline.ExecuteAsync(pMC, lCommand, lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("id success");
                        if (ReferenceEquals(mIdResponseDataProcessor.Dictionary, lDictionary)) throw new cUnexpectedServerActionException(fCapabilities.Id, "id not received", lContext);
                        return;
                    }

                    throw new cProtocolErrorException(lResult, fCapabilities.Id, lContext);
                }
            }

            private class cIdDataProcessor : iUnsolicitedDataProcessor
            {
                private static readonly cBytes kIdSpace = new cBytes("ID ");

                private readonly cEventSynchroniser mEventSynchroniser;
                private cIdReadOnlyDictionary mDictionary = null;

                public cIdDataProcessor(cEventSynchroniser pEventSynchroniser)
                {
                    mEventSynchroniser = pEventSynchroniser ?? throw new ArgumentNullException(nameof(pEventSynchroniser));
                }

                public cIdReadOnlyDictionary Dictionary => mDictionary;

                public eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cIdDataProcessor), nameof(ProcessData));

                    if (pCursor.SkipBytes(kIdSpace))
                    {
                        if (ZGetId(pCursor, out mDictionary, lContext) && pCursor.Position.AtEnd)
                        {
                            lContext.TraceVerbose("got id: {0}", mDictionary);
                            mEventSynchroniser.FirePropertyChanged(nameof(cIMAPClient.ServerId), lContext);
                            return eProcessDataResult.processed;
                        }

                        lContext.TraceWarning("likely malformed id response");
                    }

                    return eProcessDataResult.notprocessed;
                }

                private static bool ZGetId(cBytesCursor pCursor, out cIdReadOnlyDictionary rDictionary, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cIdDataProcessor), nameof(ZGetId));

                    if (pCursor.SkipBytes(cBytesCursor.Nil)) { rDictionary = null; return true; }

                    if (!pCursor.SkipByte(cASCII.LPAREN)) { rDictionary = null; return false; }

                    cIdDictionary lDictionary = new cIdDictionary();

                    try
                    {
                        bool lFirst = true;

                        while (true)
                        {
                            if (pCursor.SkipByte(cASCII.RPAREN)) { rDictionary = new cIdReadOnlyDictionary(lDictionary); return true; }

                            if (lFirst) lFirst = false;
                            else if (!pCursor.SkipByte(cASCII.SPACE)) { rDictionary = null; return false; }

                            if (!pCursor.GetString(out string lField) || !pCursor.SkipByte(cASCII.SPACE)) { rDictionary = null; return false; }
                            if (!pCursor.GetNString(out string lValue)) { rDictionary = null; return false; }

                            lDictionary.Add(lField, lValue);
                        }
                    }
                    catch (Exception e)
                    {
                        lContext.TraceException("error when constructing the id dictionary", e);
                    }

                    rDictionary = null;
                    return false;
                }

                [Conditional("DEBUG")]
                public static void _Tests(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cIdDataProcessor), nameof(_Tests));

                    cIdReadOnlyDictionary lId;
                    cBytesCursor lCursor;

                    cBytesCursor.TryConstruct("(\"name\" \"Cyrus\" \"version\" \"1.5\" \"os\" \"sunos\" \"os-version\" \"5.5\" \"support-url\" \"mailto:cyrus-bugs+@andrew.cmu.edu\")", out lCursor);
                    if (!ZGetId(lCursor, out lId, lContext)) throw new cTestsException("should be ok");
                    if (!lCursor.Position.AtEnd) throw new cTestsException("should be at end");
                    if (lId.Name != "Cyrus" || lId.Version != "1.5" || lId.OS != "sunos" || lId.OSVersion != "5.5" || lId.SupportURL != "mailto:cyrus-bugs+@andrew.cmu.edu" || lId.Vendor != null || lId.Address != null || lId.Arguments != null || lId.Command != null || lId.Date != null || lId.Environment != null || lId.Count != 5) throw new cTestsException("unexpected id values");

                    cBytesCursor.TryConstruct("()", out lCursor);
                    if (!ZGetId(lCursor, out lId, lContext)) throw new cTestsException("should be ok");
                    if (!lCursor.Position.AtEnd) throw new cTestsException("should be at end");
                    if (lId.Count != 0) throw new cTestsException("unexpected values");

                    cBytesCursor.TryConstruct("nil", out lCursor);
                    if (!ZGetId(lCursor, out lId, lContext)) throw new cTestsException("should be ok");
                    if (!lCursor.Position.AtEnd) throw new cTestsException("should be at end");
                    if (lId != null) throw new cTestsException("unexpected value");

                    List<cBytesLine> lLines = new List<cBytesLine>();
                    lLines.Add(new cBytesLine(false, Encoding.UTF8.GetBytes("(\"name\" nil \"versionx\" \"fr€d\")")));
                    lCursor = new cBytesCursor(new cBytesLines(lLines));
                        
                    if (!ZGetId(lCursor, out lId, lContext)) throw new cTestsException("should be ok");
                    if (!lCursor.Position.AtEnd) throw new cTestsException("should be at end");
                    if (lId.Name != null || lId.Version != null || lId.Count != 2 || lId["versionx"] != "fr€d") throw new cTestsException("unexpected id values");

                    cBytesCursor.TryConstruct("(\"name\" \"Cyrus\" \"version\" \"1.5\" \"os\" \"sunos\" \"os-version\" \"5.5\" \"support-url\" \"mailto:cyrus-bugs+@andrew.cmu.edu\"", out lCursor);
                    if (ZGetId(lCursor, out lId, lContext)) throw new cTestsException("should not be ok");

                    cBytesCursor.TryConstruct("\"name\" \"Cyrus\" \"version\" \"1.5\" \"os\" \"sunos\" \"os-version\" \"5.5\" \"support-url\" \"mailto:cyrus-bugs+@andrew.cmu.edu\")", out lCursor);
                    if (ZGetId(lCursor, out lId, lContext)) throw new cTestsException("should not be ok");

                    cBytesCursor.TryConstruct("(nil \"Cyrus\" \"version\" \"1.5\" \"os\" \"sunos\" \"os-version\" \"5.5\" \"support-url\" \"mailto:cyrus-bugs+@andrew.cmu.edu\")", out lCursor);
                    if (ZGetId(lCursor, out lId, lContext)) throw new cTestsException("should not be ok");

                    cBytesCursor.TryConstruct("(\"name\" \"Cyrus\" \"version\" \"1.5\"  \"os\" \"sunos\" \"os-version\" \"5.5\" \"support-url\" \"mailto:cyrus-bugs+@andrew.cmu.edu\")", out lCursor);
                    if (ZGetId(lCursor, out lId, lContext)) throw new cTestsException("should not be ok");
                }
            }
        }
    }
}