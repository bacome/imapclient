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

            public cIdDictionary ServerId => mIdResponseDataProcessor?.ServerId;

            public async Task IdAsync(cMethodControl pMC, cIdDictionary pClientId, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(IdAsync), pMC, pClientId);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState < eConnectionState.notauthenticated || _ConnectionState > eConnectionState.selected) throw new InvalidOperationException();

                // install the permanant response data processor
                if (mIdResponseDataProcessor == null)
                {
                    mIdResponseDataProcessor = new cIdDataProcessor(mEventSynchroniser);
                    mPipeline.Install(mIdResponseDataProcessor);
                }

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    lBuilder.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select
                    lBuilder.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // this command is msnunsafe

                    lBuilder.Add(kIdCommandPart);

                    if (pClientId == null) lBuilder.Add(cCommandPart.Nil);
                    else
                    {
                        lBuilder.BeginList(eListBracketing.bracketed);

                        foreach (var lPair in pClientId)
                        {
                            lBuilder.Add(mCommandPartFactory.AsString(lPair.Key));
                            lBuilder.Add(mCommandPartFactory.AsNString(lPair.Value));
                        }

                        lBuilder.EndList();
                    }

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("id success");
                        return;
                    }

                    throw new cProtocolErrorException(lResult, fKnownCapabilities.id, lContext);
                }
            }

            private class cIdDataProcessor : cUnsolicitedDataProcessor
            {
                private static readonly cBytes kIdSpace = new cBytes("ID ");

                private readonly cEventSynchroniser mEventSynchroniser;
                private cIdDictionary mServerId = null;

                public cIdDataProcessor(cEventSynchroniser pEventSynchroniser)
                {
                    mEventSynchroniser = pEventSynchroniser ?? throw new ArgumentNullException(nameof(pEventSynchroniser));
                }

                public cIdDictionary ServerId => new cIdDictionary(mServerId);

                public override eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cIdDataProcessor), nameof(ProcessData));

                    if (pCursor.SkipBytes(kIdSpace))
                    {
                        if (ZGetId(pCursor, out var lServerId, lContext) && pCursor.Position.AtEnd)
                        {
                            lContext.TraceVerbose("got id: {0}", lServerId);
                            mServerId = lServerId;
                            mEventSynchroniser.FirePropertyChanged(nameof(cIMAPClient.ServerId), lContext);
                            return eProcessDataResult.processed;
                        }

                        lContext.TraceWarning("likely malformed id response");
                    }

                    return eProcessDataResult.notprocessed;
                }

                private static bool ZGetId(cBytesCursor pCursor, out cIdDictionary rServerId, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cIdDataProcessor), nameof(ZGetId));

                    if (pCursor.SkipBytes(cBytesCursor.Nil)) { rServerId = null; return true; }

                    if (!pCursor.SkipByte(cASCII.LPAREN)) { rServerId = null; return false; }

                    rServerId = new cIdDictionary();

                    try
                    {
                        bool lFirst = true;

                        while (true)
                        {
                            if (pCursor.SkipByte(cASCII.RPAREN)) return true;

                            if (lFirst) lFirst = false;
                            else if (!pCursor.SkipByte(cASCII.SPACE)) return false;

                            if (!pCursor.GetString(out string lField) || !pCursor.SkipByte(cASCII.SPACE)) return false;
                            if (!pCursor.GetNString(out string lValue)) return false;

                            rServerId.Add(lField, lValue);
                        }
                    }
                    catch (Exception e)
                    {
                        lContext.TraceException("error when constructing the id dictionary", e);
                        return false;
                    }
                }

                [Conditional("DEBUG")]
                public static void _Tests(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cIdDataProcessor), nameof(_Tests));

                    cIdDictionary lId;
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