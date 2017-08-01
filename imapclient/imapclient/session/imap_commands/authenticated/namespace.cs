using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            private static readonly cCommandPart kNamespaceCommandPart = new cCommandPart("NAMESPACE");

            private cNamespaceDataProcessor mNamespaceDataProcessor;

            // NOTE that this needs extract info for the languages extension (translations of the responses)

            public async Task NamespaceAsync(cMethodControl pMC, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(NamespaceAsync), pMC);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_State != eState.notselected && _State != eState.selected) throw new InvalidOperationException();

                if (mNamespaceDataProcessor == null)
                {
                    mNamespaceDataProcessor = new cNamespaceDataProcessor((EnabledExtensions & fEnableableExtensions.utf8) != 0, SetNamespaces);
                    mPipeline.Install(mNamespaceDataProcessor);
                }

                using (var lCommand = new cCommand())
                {
                    lCommand.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select
                    lCommand.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // this command is msnunsafe

                    lCommand.Add(kNamespaceCommandPart);

                    object lPersonalNamespaces = PersonalNamespaces;

                    var lResult = await mPipeline.ExecuteAsync(pMC, lCommand, lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("namespace success");
                        if (ReferenceEquals(PersonalNamespaces, lPersonalNamespaces)) throw new cUnexpectedServerActionException(fCapabilities.Namespace, "namespace not received", lContext);
                        return;
                    }

                    throw new cProtocolErrorException(lResult, fCapabilities.Namespace, lContext);
                }
            }

            private class cNamespaceDataProcessor : iUnsolicitedDataProcessor
            {
                private static readonly cBytes kNamespaceSpace = new cBytes("NAMESPACE ");

                private bool mUTF8Enabled;
                private readonly Action<cNamespaceList, cNamespaceList, cNamespaceList, cTrace.cContext> mSetNamespaces;

                public cNamespaceDataProcessor(bool pUTF8Enabled, Action<cNamespaceList, cNamespaceList, cNamespaceList, cTrace.cContext> pSetNamespaces)
                {
                    mUTF8Enabled = pUTF8Enabled;
                    mSetNamespaces = pSetNamespaces ?? throw new ArgumentNullException(nameof(pSetNamespaces));
                }

                public eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cNamespaceDataProcessor), nameof(ProcessData));

                    if (!pCursor.SkipBytes(kNamespaceSpace)) return eProcessDataResult.notprocessed;

                    if (ZProcessData(pCursor, out var lPersonal, lContext) &&
                        pCursor.SkipByte(cASCII.SPACE) &&
                        ZProcessData(pCursor, out var lOtherUsers, lContext) &&
                        pCursor.SkipByte(cASCII.SPACE) &&
                        ZProcessData(pCursor, out var lShared, lContext) &&
                        pCursor.Position.AtEnd
                        )
                    {
                        lContext.TraceVerbose("got namespaces: {0} {1} {2}", lPersonal, lOtherUsers, lShared);
                        mSetNamespaces(lPersonal, lOtherUsers, lShared, lContext);
                        return eProcessDataResult.processed;
                    }

                    lContext.TraceWarning("likely malformed namespace");
                    return eProcessDataResult.notprocessed;
                }

                private bool ZProcessData(cBytesCursor pCursor, out cNamespaceList rNamespaceList, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cNamespaceDataProcessor), nameof(ZProcessData));

                    if (pCursor.SkipBytes(cBytesCursor.Nil))
                    {
                        rNamespaceList = null;
                        return true;
                    }

                    if (!pCursor.SkipByte(cASCII.LPAREN)) { rNamespaceList = null; return false; }

                    rNamespaceList = new cNamespaceList();

                    while (true)
                    {
                        if (!pCursor.SkipByte(cASCII.LPAREN) ||
                            !pCursor.GetString(out IList<byte> lPrefix) ||
                            !pCursor.SkipByte(cASCII.SPACE) ||
                            !pCursor.GetMailboxDelimiter(out var lDelimiter) ||
                            !ZProcessNamespaceExtension(pCursor) ||
                            !pCursor.SkipByte(cASCII.RPAREN)
                           )
                        {
                            rNamespaceList = null;
                            return false;
                        }

                        if (!cNamespaceName.TryConstruct(lPrefix, lDelimiter, mUTF8Enabled, out var lNamespaceName))
                        {
                            rNamespaceList = null;
                            return false;
                        }

                        rNamespaceList.Add(lNamespaceName);

                        if (pCursor.SkipByte(cASCII.RPAREN)) return true;
                    }
                }

                private bool ZProcessNamespaceExtension(cBytesCursor pCursor)
                {
                    while (true)
                    {
                        if (!pCursor.SkipByte(cASCII.SPACE)) return true;

                        if (!pCursor.GetString(out IList<byte> _) ||
                            !pCursor.SkipByte(cASCII.SPACE) ||
                            !pCursor.SkipByte(cASCII.LPAREN) ||
                            !pCursor.GetString(out IList<byte> _)
                           )
                        {
                            return false;
                        }

                        while (true)
                        {
                            if (!pCursor.SkipByte(cASCII.SPACE)) break;
                            if (!pCursor.GetString(out IList<byte> _)) return false;
                        }

                        if (!pCursor.SkipByte(cASCII.RPAREN)) return false;
                    }
                }

                [Conditional("DEBUG")]
                public static void _Tests(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cNamespaceDataProcessor), nameof(_Tests));

                    cNamespaces lNamespaces;

                    lNamespaces = LTest("NAMESPACE ((\"\" \"/\")) NIL NIL", false);
                    if (lNamespaces.Personal[0].Prefix.Length != 0 || lNamespaces.Personal[0].NamespaceName.Delimiter.Value != '/') throw new cTestsException("rfc 2342 5.1 failed");

                    lNamespaces = LTest("NAMESPACE NIL NIL ((\"\" \".\"))", false);
                    if (lNamespaces.Shared[0].Prefix.Length != 0 || lNamespaces.Shared[0].NamespaceName.Delimiter.Value != '.') throw new cTestsException("rfc 2342 5.2 failed");

                    lNamespaces = LTest("NAMESPACE ((\"\" \"/\")) NIL ((\"Public Folders/\" \"/\"))", false);
                    if (lNamespaces.Personal[0].Prefix.Length != 0 || lNamespaces.Personal[0].NamespaceName.Delimiter.Value != '/') throw new cTestsException("rfc 2342 5.3.1 failed");
                    if (lNamespaces.Shared[0].Prefix != "Public Folders/" || lNamespaces.Shared[0].NamespaceName.Delimiter.Value != '/') throw new cTestsException("rfc 2342 5.3.2 failed");

                    lNamespaces = LTest("NAMESPACE ((\"\" \"/\")) ((\"~\" \"/\")) ((\"#shared/\" \"/\")(\"#public/\" \"/\")(\"#ftp/\" \"/\")(\"#news.\" \".\"))", false);
                    if (lNamespaces.Personal[0].Prefix.Length != 0 || lNamespaces.Personal[0].NamespaceName.Delimiter.Value != '/') throw new cTestsException("rfc 2342 5.4.1 failed");
                    if (lNamespaces.OtherUsers[0].Prefix != "~" || lNamespaces.OtherUsers[0].NamespaceName.Delimiter.Value != '/') throw new cTestsException("rfc 2342 5.4.2 failed");
                    if (lNamespaces.Shared[0].Prefix != "#shared/" || lNamespaces.Shared[0].NamespaceName.Delimiter.Value != '/') throw new cTestsException("rfc 2342 5.4.3 failed");
                    if (lNamespaces.Shared[1].Prefix != "#public/" || lNamespaces.Shared[1].NamespaceName.Delimiter.Value != '/') throw new cTestsException("rfc 2342 5.4.4 failed");
                    if (lNamespaces.Shared[2].Prefix != "#ftp/" || lNamespaces.Shared[2].NamespaceName.Delimiter.Value != '/') throw new cTestsException("rfc 2342 5.4.5 failed");
                    if (lNamespaces.Shared[3].Prefix != "#news." || lNamespaces.Shared[3].NamespaceName.Delimiter.Value != '.') throw new cTestsException("rfc 2342 5.4.6 failed");

                    lNamespaces = LTest("NAMESPACE ((\"INBOX.\" \".\")) NIL NIL", false);
                    if (lNamespaces.Personal[0].Prefix != "INBOX." || lNamespaces.Personal[0].NamespaceName.Delimiter.Value != '.') throw new cTestsException("rfc 2342 5.5.1 failed");

                    lNamespaces = LTest("NAMESPACE ((\"\" \"/\")(\"#mh/\" \"/\" \"X-PARAM\" (\"FLAG1\" \"FLAG2\"))) nil nil", false);
                    if (lNamespaces.Personal[0].Prefix.Length != 0 || lNamespaces.Personal[0].NamespaceName.Delimiter.Value != '/') throw new cTestsException("rfc 2342 5.6.1 failed");
                    if (lNamespaces.Personal[1].Prefix != "#mh/" || lNamespaces.Personal[1].NamespaceName.Delimiter.Value != '/') throw new cTestsException("rfc 2342 5.6.2 failed");

                    lNamespaces = LTest("NAMESPACE ((\"\" \"/\")) ((\"Other Users/\" \"/\")) NIL", false);
                    if (lNamespaces.Personal[0].Prefix.Length != 0 || lNamespaces.Personal[0].NamespaceName.Delimiter.Value != '/') throw new cTestsException("rfc 2342 5.7.1 failed");
                    if (lNamespaces.OtherUsers[0].Prefix != "Other Users/" || lNamespaces.OtherUsers[0].NamespaceName.Delimiter.Value != '/') throw new cTestsException("rfc 2342 5.7.2 failed");

                    lNamespaces = LTest("NAMESPACE ((\"\" \"/\")) ((\"~\" \"/\")) NIL", false);
                    if (lNamespaces.Personal[0].Prefix.Length != 0 || lNamespaces.Personal[0].NamespaceName.Delimiter.Value != '/') throw new cTestsException("rfc 2342 5.8.1 failed");
                    if (lNamespaces.OtherUsers[0].Prefix != "~" || lNamespaces.OtherUsers[0].NamespaceName.Delimiter.Value != '/') throw new cTestsException("rfc 2342 5.8.2 failed");

                    // utf8
                    //lNamespaces = ZTest(new cBytes(Array.AsReadOnly(new byte[] { cASCII.n, cASCII.a, cASCII.m, cASCII.e, cASCII.s, cASCII.p, cASCII.a, cASCII.c, cASCII.e, cASCII.SPACE, cASCII.n, cASCII.i, cASCII.l, cASCII.SPACE, cASCII.n, cASCII.i, cASCII.l, cASCII.SPACE, cASCII.LPAREN, cASCII.LPAREN, cASCII.DQUOTE, cASCII.f, cASCII.r, 226, 130, 172, cASCII.d, cASCII.DQUOTE, cASCII.SPACE, cASCII.n, cASCII.i, cASCII.l, cASCII.RPAREN, cASCII.RPAREN })), true, lContext);
                    lNamespaces = LTestUTF8("namespace nil nil ((\"fr€d\" nil))", true);
                    if (lNamespaces.Personal != null) throw new cTestsException("UTF8 1 failed");
                    if (lNamespaces.OtherUsers != null) throw new cTestsException("UTF8 2 failed");
                    if (lNamespaces.Shared[0].Prefix != "fr€d" || lNamespaces.Shared[0].NamespaceName.Delimiter != null) throw new cTestsException("UTF8 3 failed");

                    // utf7

                    //string lTest = cIMAPClient.cTools.BytesToString(cModifiedUTF7.Encode("fr€d", lContext));

                    lNamespaces = LTest("namespace nil nil ((\"fr&IKw-d\" nil))", false);
                    if (lNamespaces.Personal != null) throw new cTestsException("UTF7 1 failed");
                    if (lNamespaces.OtherUsers != null) throw new cTestsException("UTF7 2 failed");
                    if (lNamespaces.Shared[0].Prefix != "fr€d" || lNamespaces.Shared[0].NamespaceName.Delimiter != null) throw new cTestsException("UTF7 3 failed");

                    // check that () is rejected
                    LTestFail("NAMESPACE ((\"\" \"/\")) ((\"~\" \"/\")) ()", false);

                   cNamespaces LTest(string pExample, bool pUTF8Enabled)
                    {
                        cNamespaces lResult = null;
                        if (!cBytesCursor.TryConstruct(pExample, out var lCursor)) throw new cTestsException();
                        cIMAPClient lClient = new cIMAPClient();
                        cAccountId lAccountId = new cAccountId("x", "y");
                        cNamespaceDataProcessor lNRDP = new cNamespaceDataProcessor(pUTF8Enabled, (pPersonal, pOtherUsers, pShared, pContext) => { lResult = new cNamespaces(lClient, lAccountId, pPersonal, pOtherUsers, pShared); });
                        if (lNRDP.ProcessData(lCursor, lContext) != eProcessDataResult.processed || !lCursor.Position.AtEnd) throw new cTestsException($"namespace: didn't process '{pExample}'");
                        return lResult;
                    }

                    void LTestFail(string pExample, bool pUTF8Enabled)
                    {
                        if (!cBytesCursor.TryConstruct(pExample, out var lCursor)) throw new cTestsException();
                        cNamespaceDataProcessor lNRDP = new cNamespaceDataProcessor(pUTF8Enabled, (pPersonal, pOtherUsers, pShared, pContext) => { });
                        if (lNRDP.ProcessData(lCursor, lContext) == eProcessDataResult.processed) throw new cTestsException($"namespace: processed '{pExample}'");
                    }

                    cNamespaces LTestUTF8(string pExample, bool pUTF8Enabled)
                    {
                        cNamespaces lResult = null;
                        List<cBytesLine> lLines = new List<cBytesLine>();
                        lLines.Add(new cBytesLine(false, System.Text.Encoding.UTF8.GetBytes(pExample)));
                        cBytesCursor lCursor = new cBytesCursor(new cBytesLines(lLines));
                        cIMAPClient lClient = new cIMAPClient();
                        cAccountId lAccountId = new cAccountId("x", "y");
                        cNamespaceDataProcessor lNRDP = new cNamespaceDataProcessor(pUTF8Enabled, (pPersonal, pOtherUsers, pShared, pContext) => { lResult = new cNamespaces(lClient, lAccountId, pPersonal, pOtherUsers, pShared); });
                        if (lNRDP.ProcessData(lCursor, lContext) != eProcessDataResult.processed || !lCursor.Position.AtEnd) throw new cTestsException($"namespace: didn't process '{pExample}'");
                        return lResult;
                    }

                }
            }
        }
    }
}