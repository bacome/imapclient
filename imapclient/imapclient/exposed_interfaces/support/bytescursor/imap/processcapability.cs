using System;
using System.Diagnostics;
using work.bacome.trace;

namespace work.bacome.imapclient.support
{
    public partial class cBytesCursor
    {
        private static readonly cBytes kCapabilityAuthEquals = new cBytes("AUTH=");

        public bool ProcessCapability(out cCapabilities rCapabilities, out cCapabilities rAuthenticationMechanisms, cTrace.cContext pParentContext)
        {
            // NOTE: this routine does not return the cursor to its original position if it fails
            //  (that is why it is called PROCESScapablity not GETcapability)

            var lContext = pParentContext.NewMethod(nameof(cBytesCursor), nameof(ProcessCapability));

            rCapabilities = new cCapabilities();
            rAuthenticationMechanisms = new cCapabilities();

            while (true)
            {
                if (SkipBytes(kCapabilityAuthEquals))
                {
                    if (!GetToken(cCharset.Atom, null, null, out string lAtom))
                    {
                        lContext.TraceWarning("likely malformed capability: auth=<not atom>?");
                        rCapabilities = null;
                        rAuthenticationMechanisms = null;
                        return false;
                    }

                    rAuthenticationMechanisms.Set(lAtom);
                }
                else
                {
                    if (!GetToken(cCharset.Atom, null, null, out string lAtom))
                    {
                        lContext.TraceWarning("likely malformed capability: not atom?");
                        rCapabilities = null;
                        rAuthenticationMechanisms = null;
                        return false;
                    }

                    rCapabilities.Set(lAtom);
                }

                if (!SkipByte(cASCII.SPACE)) break;
            }

            if (!rCapabilities.Has("IMAP4rev1"))
            {
                lContext.TraceWarning("likely malformed capability: not imap4rev1?");
                rCapabilities = null;
                rAuthenticationMechanisms = null;
                return false;
            }

            return true;
        }

        public static partial class cTests
        {
            [Conditional("DEBUG")]
            private static void ZCapabilityTests(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZCapabilityTests));

                cBytesCursor lCursor;
                cCapability lCapability;
                cCapabilities lCapabilities;
                cCapabilities lAuthenticationMechanisms;

                lCursor = MakeCursor("auth=angus auth=fred charlie bob");
                if (lCursor.ProcessCapability(out _, out _, lContext)) throw new cTestsException("should have failed", lContext);
                if (!lCursor.Position.AtEnd) throw new cTestsException("should have read entire response", lContext);

                lCursor = cBytesCursor.cTests.MakeCursor("auth=angus auth=fred imap4rev1 charlie bob");
                if (!lCursor.ProcessCapability(out lCapabilities, out lAuthenticationMechanisms, lContext)) throw new cTestsException("should have succeeded", lContext);
                if (!lCursor.Position.AtEnd) throw new cTestsException("should have read entire response", lContext);
                if (lAuthenticationMechanisms.Count != 2 || !lAuthenticationMechanisms.Has("aNgUs") || !lAuthenticationMechanisms.Has("fred")) throw new cTestsException("authenticatemechanismnames not as expected", lContext);
                if (lCapabilities.Count != 3) throw new cTestsException("capabilities not as expected", lContext);

                lCursor = cBytesCursor.cTests.MakeCursor("auth=angus idle auth=fred literal- imap4rev1 charlie utf8=accept bob");
                if (!lCursor.ProcessCapability(out lCapabilities, out lAuthenticationMechanisms, lContext)) throw new cTestsException("should have succeeded", lContext);
                if (!lCursor.Position.AtEnd) throw new cTestsException("should have read entire response", lContext);
                if (lAuthenticationMechanisms.Count != 2 || !lAuthenticationMechanisms.Has("anGus") || !lAuthenticationMechanisms.Has("freD")) throw new cTestsException("authenticatemechanismnames not as expected", lContext);
                if (lCapabilities.Count != 6) throw new cTestsException("capabilities not as expected", lContext);

                lCapability = new cCapability(lCapabilities, lAuthenticationMechanisms, 0);
                if (lCapability.LoginDisabled || !lCapability.Idle || lCapability.LiteralPlus || !lCapability.LiteralMinus || lCapability.Enable || !lCapability.UTF8Accept) throw new cTestsException("properties not as expected", lContext);
                if (!lCapability.SupportsAuthenticationMechanism("ANGUS") || !lCapability.SupportsAuthenticationMechanism("FRED") || lCapability.SupportsAuthenticationMechanism("fr€d")) throw new cTestsException("properties not as expected", lContext);

                lCapability = new cCapability(lCapabilities, lAuthenticationMechanisms, fCapabilities.Idle | fCapabilities.LiteralMinus);
                if (lCapability.LoginDisabled || lCapability.Idle || lCapability.LiteralPlus || lCapability.LiteralMinus || lCapability.Enable || !lCapability.UTF8Accept) throw new cTestsException("properties not as expected", lContext);

                lCursor = cBytesCursor.cTests.MakeCursor("auth=angus idle auth=(fred literal- imap4rev1 charlie utf8=accept bob");
                if (lCursor.ProcessCapability(out _, out _, lContext)) throw new cTestsException("should have failed", lContext);
                if (lCursor.Position.AtEnd) throw new cTestsException("should not have read entire response", lContext);

                lCursor = cBytesCursor.cTests.MakeCursor("auth=angus idle auth=fred )literal- imap4rev1 charlie utf8=accept bob");
                if (lCursor.ProcessCapability(out _, out _, lContext)) throw new cTestsException("should have failed", lContext);
                if (lCursor.Position.AtEnd) throw new cTestsException("should not have read entire response", lContext);

                lCursor = cBytesCursor.cTests.MakeCursor("auth=angus idle auth=fred literal- imap4rev1 charlie utf8=accept bob) and there is more");
                if (!lCursor.ProcessCapability(out _, out _, lContext)) throw new cTestsException("should have succeeded", lContext);
                if (lCursor.GetRestAsString() != ") and there is more") throw new cTestsException("should have stopped at the )", lContext);
            }
        }
    }
}