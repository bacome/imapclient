using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using work.bacome.imapclient;

namespace work.bacome.imapclienttests
{
    [TestClass]
    public class cCapabilitiesTests
    {
        [TestMethod]
        public void cCapabilities_Tests()
        {
            cBytesCursor lCursor;
            cIMAPCapabilities lCapabilities;
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

            lCapabilities = new cIMAPCapabilities(lCapabilityList, lAuthenticationMechanisms, 0);
            if (lCapabilities.LoginDisabled || !lCapabilities.Idle || lCapabilities.LiteralPlus || !lCapabilities.LiteralMinus || lCapabilities.Enable || !lCapabilities.UTF8Accept) throw new cTestsException("properties not as expected", lContext);
            //if (!lAuthenticationMechanisms.Contains("ANGUS") || !lAuthenticationMechanisms.Contains("FRED") || lAuthenticationMechanisms.Contains("fr€d")) throw new cTestsException("properties not as expected", lContext);

            lCapabilities = new cIMAPCapabilities(lCapabilityList, lAuthenticationMechanisms, fIMAPCapabilities.idle | fIMAPCapabilities.literalminus);
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
