using System;
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
            ;?; // do not expose this

            private static readonly cCommandPart kEnableCommandPartEnable = new cCommandPart("ENABLE");
            private static readonly cBytes kEnableExtensionUTF8 = new cBytes("UTF8=ACCEPT");
            private static readonly cCommandPart kEnableCommandPartUTF8 = new cCommandPart(kEnableExtensionUTF8);

            public async Task EnableAsync(cMethodControl pMC, fEnableableExtensions pExtensions, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(EnableAsync), pMC, pExtensions);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (mEnableDone) throw new InvalidOperationException();            

                using (var lCommand = new cCommand())
                {
                    //  note the lack of locking - this is only called during connect

                    lCommand.BeginList(eListBracketing.none);
                    lCommand.Add(kEnableCommandPartEnable);
                    if ((pExtensions & fEnableableExtensions.utf8) != 0) lCommand.Add(kEnableCommandPartUTF8);
                    // more here as required
                    lCommand.EndList();

                    var lHook = new cEnableCommandHook();
                    lCommand.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lCommand, lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        var lEnabledExtensions = lHook.EnabledExtensions;
                        lContext.TraceInformation("enabled extensions {0}", lEnabledExtensions);
                        EnabledExtensions = EnabledExtensions | lEnabledExtensions;
                        lContext.TraceVerbose("current enabled extensions {0}", EnabledExtensions);
                        return;
                    }

                    if (lHook.EnabledExtensions != fEnableableExtensions.none) lContext.TraceError("received enabled on a failed enable");

                    throw new cProtocolErrorException(lResult, fCapabilities.Enable, lContext);
                }
            }

            private class cEnableCommandHook : cCommandHook
            {
                private static readonly cBytes kEnabled = new cBytes("ENABLED");

                private fEnableableExtensions mEnabledExtensions = fEnableableExtensions.none;

                public cEnableCommandHook() { }

                public fEnableableExtensions EnabledExtensions => mEnabledExtensions;

                public override eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cEnableCommandHook), nameof(ProcessData));

                    if (!pCursor.SkipBytes(kEnabled)) return eProcessDataResult.notprocessed;

                    fEnableableExtensions lEnabledExtensions = fEnableableExtensions.none;

                    while (true)
                    {
                        if (!pCursor.SkipByte(cASCII.SPACE)) break;

                        if (!pCursor.GetToken(cCharset.Atom, null, null, out cByteList lAtom))
                        {
                            lContext.TraceWarning("likely malformed enabled: not an atom list?");
                            return eProcessDataResult.notprocessed;
                        }

                        lContext.TraceVerbose("got an enabled for {0}", lAtom);

                        if (cASCII.Compare(lAtom, kEnableExtensionUTF8, false)) lEnabledExtensions = lEnabledExtensions | fEnableableExtensions.utf8;
                        // more here as required
                        else lContext.TraceError("unknown extension enabled: {0}", lAtom);
                    }

                    if (!pCursor.Position.AtEnd)
                    {
                        lContext.TraceWarning("likely malformed enabled: not at end?");
                        return eProcessDataResult.notprocessed;
                    }

                    mEnabledExtensions |= lEnabledExtensions;
                    return eProcessDataResult.processed;
                }
            }
        }
    }
}
