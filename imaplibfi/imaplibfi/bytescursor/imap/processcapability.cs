using System;
using System.Collections.Generic;
using System.Linq;
using work.bacome.imapclient;
using work.bacome.imapsupport;

namespace work.bacome.imapinternals
{
    public partial class cBytesCursor
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
    }
}