using System;
using System.Diagnostics;
using work.bacome.trace;

namespace work.bacome.imapclient.support
{
    public partial class cBytesCursor
    {
        private static readonly cBytes kCapabilityAuthEquals = new cBytes("AUTH=");

        public bool ProcessCapability(out cUniqueIgnoreCaseStringList rCapabilities, out cUniqueIgnoreCaseStringList rAuthenticationMechanisms, cTrace.cContext pParentContext)
        {
            // NOTE: this routine does not return the cursor to its original position if it fails
            //  (that is why it is called PROCESScapablity not GETcapability)

            var lContext = pParentContext.NewMethod(nameof(cBytesCursor), nameof(ProcessCapability));

            rCapabilities = new cUniqueIgnoreCaseStringList();
            rAuthenticationMechanisms = new cUniqueIgnoreCaseStringList();

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

                    rAuthenticationMechanisms.Add(lAtom);
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

                    rCapabilities.Add(lAtom);
                }

                if (!SkipByte(cASCII.SPACE)) break;
            }

            if (!rCapabilities.Contains("IMAP4rev1"))
            {
                lContext.TraceWarning("likely malformed capability: not imap4rev1?");
                rCapabilities = null;
                rAuthenticationMechanisms = null;
                return false;
            }

            return true;
        }

        [Conditional("DEBUG")]
        private static void _Tests_Capability(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cBytesCursor), nameof(_Tests_Capability));

            cBytesCursor lCursor;
            cCapabilities lCapabilities;
            cUniqueIgnoreCaseStringList lCapabilityList;
            cUniqueIgnoreCaseStringList lAuthenticationMechanisms;

            lCursor = new cBytesCursor(new cBytes("auth=angus auth=fred charlie bob"));
            if (lCursor.ProcessCapability(out _, out _, lContext)) throw new cTestsException("should have failed", lContext);
            if (!lCursor.Position.AtEnd) throw new cTestsException("should have read entire response", lContext);

            lCursor = new cBytesCursor(new cBytes("auth=angus auth=fred imap4rev1 charlie bob"));
            if (!lCursor.ProcessCapability(out lCapabilityList, out lAuthenticationMechanisms, lContext)) throw new cTestsException("should have succeeded", lContext);
            if (!lCursor.Position.AtEnd) throw new cTestsException("should have read entire response", lContext);
            if (lAuthenticationMechanisms.Count != 2 || !lAuthenticationMechanisms.Contains("aNgUs") || !lAuthenticationMechanisms.Contains("fred")) throw new cTestsException("authenticatemechanismnames not as expected", lContext);
            if (lCapabilityList.Count != 3) throw new cTestsException("capabilities not as expected", lContext);

            lCursor = new cBytesCursor(new cBytes("auth=angus idle auth=fred literal- imap4rev1 charlie utf8=accept bob"));
            if (!lCursor.ProcessCapability(out lCapabilityList, out lAuthenticationMechanisms, lContext)) throw new cTestsException("should have succeeded", lContext);
            if (!lCursor.Position.AtEnd) throw new cTestsException("should have read entire response", lContext);
            if (lAuthenticationMechanisms.Count != 2 || !lAuthenticationMechanisms.Contains("anGus") || !lAuthenticationMechanisms.Contains("freD")) throw new cTestsException("authenticatemechanismnames not as expected", lContext);
            if (lCapabilityList.Count != 6) throw new cTestsException("capabilities not as expected", lContext);

            lCapabilities = new cCapabilities(lCapabilityList, lAuthenticationMechanisms, 0);
            if (lCapabilities.LoginDisabled || !lCapabilities.Idle || lCapabilities.LiteralPlus || !lCapabilities.LiteralMinus || lCapabilities.Enable || !lCapabilities.UTF8Accept) throw new cTestsException("properties not as expected", lContext);
            if (!lAuthenticationMechanisms.Contains("ANGUS") || !lAuthenticationMechanisms.Contains("FRED") || lAuthenticationMechanisms.Contains("fr€d")) throw new cTestsException("properties not as expected", lContext);

            lCapabilities = new cCapabilities(lCapabilityList, lAuthenticationMechanisms, fKnownCapabilities.Idle | fKnownCapabilities.LiteralMinus);
            if (lCapabilities.LoginDisabled || lCapabilities.Idle || lCapabilities.LiteralPlus || lCapabilities.LiteralMinus || lCapabilities.Enable || !lCapabilities.UTF8Accept) throw new cTestsException("properties not as expected", lContext);

            lCursor = new cBytesCursor(new cBytes("auth=angus idle auth=(fred literal- imap4rev1 charlie utf8=accept bob"));
            if (lCursor.ProcessCapability(out _, out _, lContext)) throw new cTestsException("should have failed", lContext);
            if (lCursor.Position.AtEnd) throw new cTestsException("should not have read entire response", lContext);

            lCursor = new cBytesCursor(new cBytes("auth=angus idle auth=fred )literal- imap4rev1 charlie utf8=accept bob"));
            if (lCursor.ProcessCapability(out _, out _, lContext)) throw new cTestsException("should have failed", lContext);
            if (lCursor.Position.AtEnd) throw new cTestsException("should not have read entire response", lContext);

            lCursor = new cBytesCursor(new cBytes("auth=angus idle auth=fred literal- imap4rev1 charlie utf8=accept bob) and there is more"));
            if (!lCursor.ProcessCapability(out _, out _, lContext)) throw new cTestsException("should have succeeded", lContext);
            if (lCursor.GetRestAsString() != ") and there is more") throw new cTestsException("should have stopped at the )", lContext);
        }
    }
}