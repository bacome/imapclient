using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private static readonly cCommandPart kIdCommandPart = new cTextCommandPart("ID ");

            private cIdDataProcessor mIdResponseDataProcessor;

            public cIMAPId ServerId => mIdResponseDataProcessor?.ServerId;

            public async Task IdAsync(cMethodControl pMC, cIMAPId pClientId, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(IdAsync), pMC, pClientId);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState < eIMAPConnectionState.notauthenticated || _ConnectionState > eIMAPConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);

                // install the permanent response data processor
                if (mIdResponseDataProcessor == null)
                {
                    mIdResponseDataProcessor = new cIdDataProcessor(mSynchroniser);
                    mPipeline.Install(mIdResponseDataProcessor);
                }

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    if (!_Capabilities.QResync) lBuilder.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select if mailbox-data delivered during the command would be ambiguous
                    lBuilder.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // this command is msnunsafe

                    lBuilder.Add(kIdCommandPart);

                    if (pClientId == null || pClientId.Count == 0) lBuilder.Add(cCommandPart.Nil);
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

                    if (lResult.ResultType == eIMAPCommandResultType.ok)
                    {
                        lContext.TraceInformation("id success");
                        return;
                    }

                    throw new cIMAPProtocolErrorException(lResult, fIMAPCapabilities.id, lContext);
                }
            }

            private class cIdDataProcessor : cUnsolicitedDataProcessor
            {
                private static readonly cBytes kIdSpace = new cBytes("ID ");

                private readonly cCallbackSynchroniser mSynchroniser;
                private cIMAPId mServerId = null;

                public cIdDataProcessor(cCallbackSynchroniser pSynchroniser)
                {
                    mSynchroniser = pSynchroniser ?? throw new ArgumentNullException(nameof(pSynchroniser));
                }

                public cIMAPId ServerId => mServerId;

                public override eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cIdDataProcessor), nameof(ProcessData));

                    if (pCursor.SkipBytes(kIdSpace))
                    {
                        if (ZGetId(pCursor, out var lServerId, lContext) && pCursor.Position.AtEnd)
                        {
                            lContext.TraceVerbose("got id: {0}", lServerId);
                            mServerId = lServerId;
                            mSynchroniser.InvokePropertyChanged(nameof(cIMAPClient.ServerId), lContext);
                            return eProcessDataResult.processed;
                        }

                        lContext.TraceWarning("likely malformed id response");
                    }

                    return eProcessDataResult.notprocessed;
                }

                private static bool ZGetId(cBytesCursor pCursor, out cIMAPId rServerId, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cIdDataProcessor), nameof(ZGetId));

                    if (pCursor.SkipBytes(cBytesCursor.Nil)) { rServerId = null; return true; }

                    if (!pCursor.SkipByte(cASCII.LPAREN)) { rServerId = null; return false; }

                    Dictionary<string, string> lDictionary = new Dictionary<string, string>();

                    try
                    {
                        bool lFirst = true;

                        while (true)
                        {
                            if (pCursor.SkipByte(cASCII.RPAREN)) break;

                            if (lFirst) lFirst = false;
                            else if (!pCursor.SkipByte(cASCII.SPACE)) { rServerId = null; return false; }

                            if (!pCursor.GetString(out string lField) || !pCursor.SkipByte(cASCII.SPACE)) { rServerId = null; return false; }
                            if (!pCursor.GetNString(out string lValue)) { rServerId = null; return false; }

                            lDictionary[lField] = lValue;
                        }

                        rServerId = new cIMAPId(lDictionary);
                        return true;
                    }
                    catch (Exception e)
                    {
                        lContext.TraceException("error when constructing the id dictionary", e);
                        rServerId = null;
                        return false;
                    }
                }

                [Conditional("DEBUG")]
                public static void _Tests(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cIdDataProcessor), nameof(_Tests));

                    cIMAPId lId;
                    cBytesCursor lCursor;

                    lCursor = new cBytesCursor("(\"name\" \"Cyrus\" \"version\" \"1.5\" \"os\" \"sunos\" \"os-version\" \"5.5\" \"support-url\" \"mailto:cyrus-bugs+@andrew.cmu.edu\")");
                    if (!ZGetId(lCursor, out lId, lContext)) throw new cTestsException("should be ok");
                    if (!lCursor.Position.AtEnd) throw new cTestsException("should be at end");
                    if (lId.Name != "Cyrus" || lId.Version != "1.5" || lId.OS != "sunos" || lId.OSVersion != "5.5" || lId.SupportURL != "mailto:cyrus-bugs+@andrew.cmu.edu" || lId.Vendor != null || lId.Address != null || lId.Arguments != null || lId.Command != null || lId.Date != null || lId.Environment != null || lId.Count != 5) throw new cTestsException("unexpected id values");

                    lCursor = new cBytesCursor("()");
                    if (!ZGetId(lCursor, out lId, lContext)) throw new cTestsException("should be ok");
                    if (!lCursor.Position.AtEnd) throw new cTestsException("should be at end");
                    if (lId.Count != 0) throw new cTestsException("unexpected values");

                    lCursor = new cBytesCursor("nil");
                    if (!ZGetId(lCursor, out lId, lContext)) throw new cTestsException("should be ok");
                    if (!lCursor.Position.AtEnd) throw new cTestsException("should be at end");
                    if (lId != null) throw new cTestsException("unexpected value");

                    List<cResponseLine> lLines = new List<cResponseLine>();
                    lLines.Add(new cResponseLine(false, Encoding.UTF8.GetBytes("(\"name\" nil \"versionx\" \"fr€d\")")));
                    lCursor = new cBytesCursor(new cResponse(lLines));
                        
                    if (!ZGetId(lCursor, out lId, lContext)) throw new cTestsException("should be ok");
                    if (!lCursor.Position.AtEnd) throw new cTestsException("should be at end");
                    if (lId.Name != null || lId.Version != null || lId.Count != 2 || lId["versionx"] != "fr€d") throw new cTestsException("unexpected id values");

                    lCursor = new cBytesCursor("(\"name\" \"Cyrus\" \"version\" \"1.5\" \"os\" \"sunos\" \"os-version\" \"5.5\" \"support-url\" \"mailto:cyrus-bugs+@andrew.cmu.edu\"");
                    if (ZGetId(lCursor, out lId, lContext)) throw new cTestsException("should not be ok");

                    lCursor = new cBytesCursor("\"name\" \"Cyrus\" \"version\" \"1.5\" \"os\" \"sunos\" \"os-version\" \"5.5\" \"support-url\" \"mailto:cyrus-bugs+@andrew.cmu.edu\")");
                    if (ZGetId(lCursor, out lId, lContext)) throw new cTestsException("should not be ok");

                    lCursor = new cBytesCursor("(nil \"Cyrus\" \"version\" \"1.5\" \"os\" \"sunos\" \"os-version\" \"5.5\" \"support-url\" \"mailto:cyrus-bugs+@andrew.cmu.edu\")");
                    if (ZGetId(lCursor, out lId, lContext)) throw new cTestsException("should not be ok");

                    lCursor = new cBytesCursor("(\"name\" \"Cyrus\" \"version\" \"1.5\"  \"os\" \"sunos\" \"os-version\" \"5.5\" \"support-url\" \"mailto:cyrus-bugs+@andrew.cmu.edu\")");
                    if (ZGetId(lCursor, out lId, lContext)) throw new cTestsException("should not be ok");
                }
            }
        }
    }
}