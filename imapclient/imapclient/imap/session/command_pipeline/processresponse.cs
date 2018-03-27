using System;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private partial class cCommandPipeline
            {
                private static readonly cBytes kProcessResponseAsteriskSpace = new cBytes("* ");
                private static readonly cBytes kProcessResponseOKSpace = new cBytes("OK ");
                private static readonly cBytes kProcessResponseNoSpace = new cBytes("NO ");
                private static readonly cBytes kProcessResponseBadSpace = new cBytes("BAD ");
                private static readonly cBytes kProcessResponseCapabilitySpace = new cBytes("CAPABILITY ");
                private static readonly cBytes kProcessResponseByeSpace = new cBytes("BYE ");

                private bool ZProcessDataResponse(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZProcessDataResponse));

                    if (!pCursor.SkipBytes(kProcessResponseAsteriskSpace)) return false;

                    lContext.TraceVerbose("got data");

                    if (pCursor.SkipBytes(kProcessResponseOKSpace))
                    {
                        lContext.TraceVerbose("got information");
                        mResponseTextProcessor.Process(eIMAPResponseTextContext.information, pCursor, mActiveCommands, lContext);
                        return true;
                    }

                    if (pCursor.SkipBytes(kProcessResponseNoSpace))
                    {
                        lContext.TraceVerbose("got a warning");
                        mResponseTextProcessor.Process(eIMAPResponseTextContext.warning, pCursor, mActiveCommands, lContext);
                        return true;
                    }

                    if (pCursor.SkipBytes(kProcessResponseBadSpace))
                    {
                        lContext.TraceVerbose("got a protocol error");
                        mResponseTextProcessor.Process(eIMAPResponseTextContext.protocolerror, pCursor, mActiveCommands, lContext);
                        return true;
                    }

                    if (pCursor.SkipBytes(kProcessResponseCapabilitySpace))
                    {
                        if (mState == eState.connected)
                        {
                            if (pCursor.ProcessCapability(out var lCapabilities, out var lAuthenticationMechanisms, lContext) && pCursor.Position.AtEnd)
                            {
                                lContext.TraceVerbose("got capabilities: {0} {1}", lCapabilities, lAuthenticationMechanisms);
                                mCapabilities = lCapabilities;
                                mAuthenticationMechanisms = lAuthenticationMechanisms;
                            }
                            else lContext.TraceWarning("likely malformed capability");
                        }
                        else lContext.TraceWarning("capability response received at the wrong time - not processed");

                        return true;
                    }

                    var lResult = eProcessDataResult.notprocessed;

                    if (pCursor.SkipBytes(kProcessResponseByeSpace))
                    {
                        lContext.TraceVerbose("got a bye");

                        cIMAPResponseText lResponseText = mResponseTextProcessor.Process(eIMAPResponseTextContext.bye, pCursor, null, lContext);
                        cResponseDataBye lData = new cResponseDataBye(lResponseText);

                        foreach (var lCommand in mActiveCommands) ZProcessDataResponseWorker(ref lResult, lCommand.Hook.ProcessData(lData, lContext), lContext);

                        if (lResult == eProcessDataResult.notprocessed)
                        {
                            lContext.TraceVerbose("got a unilateral bye");
                            throw new cUnilateralByeException(lResponseText, lContext);
                        }

                        return true;
                    }

                    var lBookmark = pCursor.Position;

                    foreach (var lParser in mResponseDataParsers)
                    {
                        if (lParser.Process(pCursor, out var lData, lContext))
                        {
                            if (mMailboxCache != null) ZProcessDataResponseWorker(ref lResult, mMailboxCache.ProcessData(lData, lContext), lContext);
                            foreach (var lCommand in mActiveCommands) ZProcessDataResponseWorker(ref lResult, lCommand.Hook.ProcessData(lData, lContext), lContext);
                            foreach (var lDataProcessor in mUnsolicitedDataProcessors) ZProcessDataResponseWorker(ref lResult, lDataProcessor.ProcessData(lData, lContext), lContext);

                            if (lResult == eProcessDataResult.notprocessed) lContext.TraceWarning("unprocessed data response: {0}", lData);

                            return true;
                        }

                        pCursor.Position = lBookmark;
                    }

                    if (mMailboxCache != null)
                    {
                        ZProcessDataResponseWorker(ref lResult, mMailboxCache.ProcessData(pCursor, lContext), lContext);
                        pCursor.Position = lBookmark;
                    }

                    foreach (var lCommand in mActiveCommands)
                    {
                        ZProcessDataResponseWorker(ref lResult, lCommand.Hook.ProcessData(pCursor, lContext), lContext);
                        pCursor.Position = lBookmark;
                    }

                    foreach (var lDataProcessor in mUnsolicitedDataProcessors)
                    {
                        ZProcessDataResponseWorker(ref lResult, lDataProcessor.ProcessData(pCursor, lContext), lContext);
                        pCursor.Position = lBookmark;
                    }

                    if (lResult == eProcessDataResult.notprocessed) lContext.TraceWarning("unrecognised data response: {0}", pCursor);

                    return true;
                }

                private void ZProcessDataResponseWorker(ref eProcessDataResult pResult, eProcessDataResult pProcessDataResult, cTrace.cContext pContext)
                {
                    if (pProcessDataResult == eProcessDataResult.processed)
                    {
                        if (pResult != eProcessDataResult.notprocessed) throw new cPipelineConflictException(pContext);
                        pResult = eProcessDataResult.processed;
                    }
                    else if (pProcessDataResult == eProcessDataResult.observed)
                    {
                        if (pResult == eProcessDataResult.processed) throw new cPipelineConflictException(pContext);
                        pResult = eProcessDataResult.observed;
                    }
                }

                private cIMAPCommandResult ZProcessCommandCompletionResponse(cBytesCursor pCursor, cCommandTag pTag, bool pIsAuthentication, iTextCodeProcessor pTextCodeProcessor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZProcessCommandCompletionResponse), pTag);

                    var lBookmark = pCursor.Position;

                    if (!pCursor.SkipBytes(pTag, true)) return null;

                    if (!pCursor.SkipByte(cASCII.SPACE))
                    {
                        lContext.TraceWarning("likely badly formed command completion: {0}", pCursor);
                        pCursor.Position = lBookmark;
                        return null;
                    }

                    eIMAPCommandResultType lResultType;
                    eIMAPResponseTextContext lTextContext;

                    if (pCursor.SkipBytes(kProcessResponseOKSpace))
                    {
                        lResultType = eIMAPCommandResultType.ok;
                        lTextContext = eIMAPResponseTextContext.success;
                    }
                    else if (pCursor.SkipBytes(kProcessResponseNoSpace))
                    {
                        lResultType = eIMAPCommandResultType.no;
                        lTextContext = eIMAPResponseTextContext.failure;
                    }
                    else if (pCursor.SkipBytes(kProcessResponseBadSpace))
                    {
                        lResultType = eIMAPCommandResultType.bad;
                        if (pIsAuthentication) lTextContext = eIMAPResponseTextContext.authenticationcancelled;
                        else lTextContext = eIMAPResponseTextContext.error;
                    }
                    else
                    {
                        lContext.TraceWarning("likely badly formed command completion: {0}", pCursor);
                        pCursor.Position = lBookmark;
                        return null;
                    }

                    var lResult = new cIMAPCommandResult(lResultType, mResponseTextProcessor.Process(lTextContext, pCursor, pTextCodeProcessor, lContext));

                    if (mMailboxCache != null) mMailboxCache.CommandCompletion(lContext);

                    return lResult;
                }
            }
        }
    }
}