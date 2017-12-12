using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    internal partial class cBytesCursor
    {
        private static readonly cBytes kCapabilityAuthEquals = new cBytes("AUTH=");

        public bool ProcessCapability(out cStrings rCapabilities, out cStrings rAuthenticationMechanisms, cTrace.cContext pParentContext)
        {
            // NOTE: this routine does not return the cursor to its original position if it fails
            //  (that is why it is called PROCESScapablity not GETcapability)

            var lContext = pParentContext.NewMethod(nameof(cBytesCursor), nameof(ProcessCapability));

            var lCapabilities = new List<string>();
            var lAuthenticationMechanisms = new List<string>();

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

                    lAuthenticationMechanisms.Add(lAtom);
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

                    lCapabilities.Add(lAtom);
                }

                if (!SkipByte(cASCII.SPACE)) break;
            }

            if (!lCapabilities.Contains("IMAP4rev1", StringComparer.InvariantCultureIgnoreCase))
            {
                lContext.TraceWarning("likely malformed capability: not imap4rev1?");
                rCapabilities = null;
                rAuthenticationMechanisms = null;
                return false;
            }

            rCapabilities = new cStrings(lCapabilities);
            rAuthenticationMechanisms = new cStrings(lAuthenticationMechanisms);
            return true;
        }

        [Conditional("DEBUG")]
        private static void _Tests_Capability(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cBytesCursor), nameof(_Tests_Capability));

            cBytesCursor lCursor;
            cCapabilities lCapabilities;
            cStrings lCapabilityList;
            cStrings lAuthenticationMechanisms;

            lCursor = new cBytesCursor("auth=angus auth=fred charlie bob");
            if (lCursor.ProcessCapability(out _, out _, lContext)) throw new cTestsException("should have failed", lContext);
            if (!lCursor.Position.AtEnd) throw new cTestsException("should have read entire response", lContext);

            lCursor = new cBytesCursor("auth=angus auth=fred imap4rev1 charlie bob");
            if (!lCursor.ProcessCapability(out lCapabilityList, out lAuthenticationMechanisms, lContext)) throw new cTestsException("should have succeeded", lContext);
            if (!lCursor.Position.AtEnd) throw new cTestsException("should have read entire response", lContext);
            if (lAuthenticationMechanisms.Count != 2 || !lAuthenticationMechanisms.Contains("angus") || !lAuthenticationMechanisms.Contains("fred")) throw new cTestsException("authenticatemechanismnames not as expected", lContext);
            if (lCapabilityList.Count != 3) throw new cTestsException("capabilities not as expected", lContext);

            lCursor = new cBytesCursor("auth=angus idle auth=fred literal- imap4rev1 charlie utf8=accept bob");
            if (!lCursor.ProcessCapability(out lCapabilityList, out lAuthenticationMechanisms, lContext)) throw new cTestsException("should have succeeded", lContext);
            if (!lCursor.Position.AtEnd) throw new cTestsException("should have read entire response", lContext);
            if (lAuthenticationMechanisms.Count != 2 || !lAuthenticationMechanisms.Contains("angus") || !lAuthenticationMechanisms.Contains("fred")) throw new cTestsException("authenticatemechanismnames not as expected", lContext);
            if (lCapabilityList.Count != 6) throw new cTestsException("capabilities not as expected", lContext);

            lCapabilities = new cCapabilities(lCapabilityList, lAuthenticationMechanisms, 0);
            if (lCapabilities.LoginDisabled || !lCapabilities.Idle || lCapabilities.LiteralPlus || !lCapabilities.LiteralMinus || lCapabilities.Enable || !lCapabilities.UTF8Accept) throw new cTestsException("properties not as expected", lContext);
            //if (!lAuthenticationMechanisms.Contains("ANGUS") || !lAuthenticationMechanisms.Contains("FRED") || lAuthenticationMechanisms.Contains("fr€d")) throw new cTestsException("properties not as expected", lContext);

            lCapabilities = new cCapabilities(lCapabilityList, lAuthenticationMechanisms, fCapabilities.idle | fCapabilities.literalminus);
            if (lCapabilities.LoginDisabled || lCapabilities.Idle || lCapabilities.LiteralPlus || lCapabilities.LiteralMinus || lCapabilities.Enable || !lCapabilities.UTF8Accept) throw new cTestsException("properties not as expected", lContext);

            lCursor = new cBytesCursor("auth=angus idle auth=(fred literal- imap4rev1 charlie utf8=accept bob");
            if (lCursor.ProcessCapability(out _, out _, lContext)) throw new cTestsException("should have failed", lContext);
            if (lCursor.Position.AtEnd) throw new cTestsException("should not have read entire response", lContext);

            lCursor = new cBytesCursor("auth=angus idle auth=fred )literal- imap4rev1 charlie utf8=accept bob");
            if (lCursor.ProcessCapability(out _, out _, lContext)) throw new cTestsException("should have failed", lContext);
            if (lCursor.Position.AtEnd) throw new cTestsException("should not have read entire response", lContext);

            lCursor = new cBytesCursor("auth=angus idle auth=fred literal- imap4rev1 charlie utf8=accept bob) and there is more");
            if (!lCursor.ProcessCapability(out _, out _, lContext)) throw new cTestsException("should have succeeded", lContext);
            if (lCursor.GetRestAsString() != ") and there is more") throw new cTestsException("should have stopped at the )", lContext);
        }
    }
}