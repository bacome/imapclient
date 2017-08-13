using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
                if (_ConnectionState != eConnectionState.enabled && _ConnectionState != eConnectionState.notselected && _ConnectionState != eConnectionState.selected) throw new InvalidOperationException();

                if (mNamespaceDataProcessor == null)
                {
                    mNamespaceDataProcessor = new cNamespaceDataProcessor(mEventSynchroniser, (EnabledExtensions & fEnableableExtensions.utf8) != 0);
                    mPipeline.Install(mNamespaceDataProcessor);
                }

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    lBuilder.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select
                    lBuilder.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // this command is msnunsafe

                    lBuilder.Add(kNamespaceCommandPart);

                    object lPersonalNamespaces = PersonalNamespaces;

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("namespace success");
                        return;
                    }

                    throw new cProtocolErrorException(lResult, fKnownCapabilities.namespaces, lContext);
                }
            }

            private class cNamespaceDataProcessor : cUnsolicitedDataProcessor
            {
                private static readonly cBytes kNamespaceSpace = new cBytes("NAMESPACE ");

                private cEventSynchroniser mEventSynchroniser;
                private bool mUTF8Enabled;

                public cNamespaceDataProcessor(cEventSynchroniser pEventSynchroniser, bool pUTF8Enabled)
                {
                    mEventSynchroniser = pEventSynchroniser ?? throw new ArgumentNullException(nameof(pEventSynchroniser));
                    mUTF8Enabled = pUTF8Enabled;
                }

                public ReadOnlyCollection<cNamespaceName> Personal { get; private set; } = null;
                public ReadOnlyCollection<cNamespaceName> OtherUsers { get; private set; } = null;
                public ReadOnlyCollection<cNamespaceName> Shared { get; private set; } = null;

                public override eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext)
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

                        Personal = lPersonal.AsReadOnly();
                        OtherUsers = lOtherUsers.AsReadOnly();
                        Shared = lShared.AsReadOnly();

                        mEventSynchroniser.FirePropertyChanged(nameof(cIMAPClient.Namespaces), lContext);

                        return eProcessDataResult.processed;
                    }

                    lContext.TraceWarning("likely malformed namespace");
                    return eProcessDataResult.notprocessed;
                }

                private bool ZProcessData(cBytesCursor pCursor, out cNamespaceNameList rNames, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cNamespaceDataProcessor), nameof(ZProcessData));

                    if (pCursor.SkipBytes(cBytesCursor.Nil))
                    {
                        rNames = null;
                        return true;
                    }

                    if (!pCursor.SkipByte(cASCII.LPAREN)) { rNames = null; return false; }

                    rNames = new cNamespaceNameList();

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
                            rNames = null;
                            return false;
                        }

                        if (!cNamespaceName.TryConstruct(lPrefix, lDelimiter, mUTF8Enabled, out var lNamespaceName))
                        {
                            rNames = null;
                            return false;
                        }

                        rNames.Add(lNamespaceName);

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

                private class cNamespaceNameList : List<cNamespaceName>
                {
                    public cNamespaceNameList() { }

                    public override string ToString()
                    {
                        cListBuilder lBuilder = new cListBuilder(nameof(cNamespaceNameList));
                        foreach (var lNamespace in this) lBuilder.Append(lNamespace);
                        return lBuilder.ToString();
                    }
                }

                [Conditional("DEBUG")]
                public static void _Tests(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cNamespaceDataProcessor), nameof(_Tests));

                    using (cEventSynchroniser lES = new cEventSynchroniser(new object(), lContext))
                    {
                        cNamespaceDataProcessor lNRDPASCII = new cNamespaceDataProcessor(lES, false);
                        cNamespaceDataProcessor lNRDPUTF8 = new cNamespaceDataProcessor(lES, true);

                        LTest(lNRDPASCII, "NAMESPACE ((\"\" \"/\")) NIL NIL");
                        if (lNRDPASCII.Personal[0].Prefix.Length != 0 || lNRDPASCII.Personal[0].Delimiter.Value != '/') throw new cTestsException("rfc 2342 5.1 failed");

                        LTest(lNRDPASCII, "NAMESPACE NIL NIL ((\"\" \".\"))");
                        if (lNRDPASCII.Shared[0].Prefix.Length != 0 || lNRDPASCII.Shared[0].Delimiter.Value != '.') throw new cTestsException("rfc 2342 5.2 failed");

                        LTest(lNRDPASCII, "NAMESPACE ((\"\" \"/\")) NIL ((\"Public Folders/\" \"/\"))");
                        if (lNRDPASCII.Personal[0].Prefix.Length != 0 || lNRDPASCII.Personal[0].Delimiter.Value != '/') throw new cTestsException("rfc 2342 5.3.1 failed");
                        if (lNRDPASCII.Shared[0].Prefix != "Public Folders/" || lNRDPASCII.Shared[0].Delimiter.Value != '/') throw new cTestsException("rfc 2342 5.3.2 failed");

                        LTest(lNRDPASCII, "NAMESPACE ((\"\" \"/\")) ((\"~\" \"/\")) ((\"#shared/\" \"/\")(\"#public/\" \"/\")(\"#ftp/\" \"/\")(\"#news.\" \".\"))");
                        if (lNRDPASCII.Personal[0].Prefix.Length != 0 || lNRDPASCII.Personal[0].Delimiter.Value != '/') throw new cTestsException("rfc 2342 5.4.1 failed");
                        if (lNRDPASCII.OtherUsers[0].Prefix != "~" || lNRDPASCII.OtherUsers[0].Delimiter.Value != '/') throw new cTestsException("rfc 2342 5.4.2 failed");
                        if (lNRDPASCII.Shared[0].Prefix != "#shared/" || lNRDPASCII.Shared[0].Delimiter.Value != '/') throw new cTestsException("rfc 2342 5.4.3 failed");
                        if (lNRDPASCII.Shared[1].Prefix != "#public/" || lNRDPASCII.Shared[1].Delimiter.Value != '/') throw new cTestsException("rfc 2342 5.4.4 failed");
                        if (lNRDPASCII.Shared[2].Prefix != "#ftp/" || lNRDPASCII.Shared[2].Delimiter.Value != '/') throw new cTestsException("rfc 2342 5.4.5 failed");
                        if (lNRDPASCII.Shared[3].Prefix != "#news." || lNRDPASCII.Shared[3].Delimiter.Value != '.') throw new cTestsException("rfc 2342 5.4.6 failed");

                        LTest(lNRDPASCII, "NAMESPACE ((\"INBOX.\" \".\")) NIL NIL");
                        if (lNRDPASCII.Personal[0].Prefix != "INBOX." || lNRDPASCII.Personal[0].Delimiter.Value != '.') throw new cTestsException("rfc 2342 5.5.1 failed");

                        LTest(lNRDPASCII, "NAMESPACE ((\"\" \"/\")(\"#mh/\" \"/\" \"X-PARAM\" (\"FLAG1\" \"FLAG2\"))) nil nil");
                        if (lNRDPASCII.Personal[0].Prefix.Length != 0 || lNRDPASCII.Personal[0].Delimiter.Value != '/') throw new cTestsException("rfc 2342 5.6.1 failed");
                        if (lNRDPASCII.Personal[1].Prefix != "#mh/" || lNRDPASCII.Personal[1].Delimiter.Value != '/') throw new cTestsException("rfc 2342 5.6.2 failed");

                        LTest(lNRDPASCII, "NAMESPACE ((\"\" \"/\")) ((\"Other Users/\" \"/\")) NIL");
                        if (lNRDPASCII.Personal[0].Prefix.Length != 0 || lNRDPASCII.Personal[0].Delimiter.Value != '/') throw new cTestsException("rfc 2342 5.7.1 failed");
                        if (lNRDPASCII.OtherUsers[0].Prefix != "Other Users/" || lNRDPASCII.OtherUsers[0].Delimiter.Value != '/') throw new cTestsException("rfc 2342 5.7.2 failed");

                        LTest(lNRDPASCII, "NAMESPACE ((\"\" \"/\")) ((\"~\" \"/\")) NIL");
                        if (lNRDPASCII.Personal[0].Prefix.Length != 0 || lNRDPASCII.Personal[0].Delimiter.Value != '/') throw new cTestsException("rfc 2342 5.8.1 failed");
                        if (lNRDPASCII.OtherUsers[0].Prefix != "~" || lNRDPASCII.OtherUsers[0].Delimiter.Value != '/') throw new cTestsException("rfc 2342 5.8.2 failed");

                        // utf8
                        //lNamespaces = ZTest(new cBytes(Array.AsReadOnly(new byte[] { cASCII.n, cASCII.a, cASCII.m, cASCII.e, cASCII.s, cASCII.p, cASCII.a, cASCII.c, cASCII.e, cASCII.SPACE, cASCII.n, cASCII.i, cASCII.l, cASCII.SPACE, cASCII.n, cASCII.i, cASCII.l, cASCII.SPACE, cASCII.LPAREN, cASCII.LPAREN, cASCII.DQUOTE, cASCII.f, cASCII.r, 226, 130, 172, cASCII.d, cASCII.DQUOTE, cASCII.SPACE, cASCII.n, cASCII.i, cASCII.l, cASCII.RPAREN, cASCII.RPAREN })), true, lContext);
                        LTest(lNRDPUTF8, "namespace nil nil ((\"fr€d\" nil))");
                        if (lNRDPUTF8.Personal != null) throw new cTestsException("UTF8 1 failed");
                        if (lNRDPUTF8.OtherUsers != null) throw new cTestsException("UTF8 2 failed");
                        if (lNRDPUTF8.Shared[0].Prefix != "fr€d" || lNRDPUTF8.Shared[0].Delimiter != null) throw new cTestsException("UTF8 3 failed");

                        // utf7

                        //string lTest = cIMAPClient.cTools.BytesToString(cModifiedUTF7.Encode("fr€d", lContext));

                        LTest(lNRDPASCII, "namespace nil nil ((\"fr&IKw-d\" nil))");
                        if (lNRDPASCII.Personal != null) throw new cTestsException("UTF7 1 failed");
                        if (lNRDPASCII.OtherUsers != null) throw new cTestsException("UTF7 2 failed");
                        if (lNRDPASCII.Shared[0].Prefix != "fr€d" || lNRDPASCII.Shared[0].Delimiter != null) throw new cTestsException("UTF7 3 failed");

                        // check that () is rejected
                        LTestFail(lNRDPASCII, "NAMESPACE ((\"\" \"/\")) ((\"~\" \"/\")) ()");
                    }

                    void LTestFail(cNamespaceDataProcessor pNDP, string pExample)
                    {
                        List<cBytesLine> lLines = new List<cBytesLine>();
                        lLines.Add(new cBytesLine(false, System.Text.Encoding.UTF8.GetBytes(pExample)));
                        cBytesCursor lCursor = new cBytesCursor(new cBytesLines(lLines));
                        if (pNDP.ProcessData(lCursor, lContext) == eProcessDataResult.processed) throw new cTestsException($"namespace: processed '{pExample}'");
                    }

                    void LTest(cNamespaceDataProcessor pNDP, string pExample)
                    {
                        List<cBytesLine> lLines = new List<cBytesLine>();
                        lLines.Add(new cBytesLine(false, System.Text.Encoding.UTF8.GetBytes(pExample)));
                        cBytesCursor lCursor = new cBytesCursor(new cBytesLines(lLines));
                        if (pNDP.ProcessData(lCursor, lContext) != eProcessDataResult.processed) throw new cTestsException($"namespace: didn't process '{pExample}'");
                    }
                }
            }
        }
    }
}