using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.imapclient;
using work.bacome.imapclient.support;

namespace testharness2
{
    public static class cTests
    {
        private static Random mRandom = new Random();

        public static void CurrentTest(cTrace.cContext pParentContext)
        {
            // quickly get to the test I'm working on
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(CurrentTest));
            //cIMAPClient._Tests(lContext);
            ZTestAppendNoCatenateNoBinaryNoUTF8(lContext);
        }

        public static void Tests(bool pQuick, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(Tests));

            try
            {
                if (!pQuick) cIMAPClient._Tests(lContext);

                ZTestByeAtStartup1(cTrace.cContext.None); // tests BYE at startup and ALERT

                ZTestByeAtStartup2(cTrace.cContext.None); // tests BYE at startup and greeting
                ZTestByeAtStartup3(lContext); // tests BYE at startup with referral

                if (!pQuick) ZTestPreauthAtStartup1(lContext); // tests capability in greeting and logout
                ZTestPreauthAtStartup1_2(lContext); // tests utf8 id
                ZTestPreauthAtStartup1_3(lContext); // tests utf8 id 2

                ZTestPreauthAtStartup2(lContext); // tests capability command, enabling UTF8, information, warning and error messages, 
                ZTestPreauthAtStartup3(cTrace.cContext.None); // tests that when there is nothing to enable that the enable isn't done

                ZTestAuthAtStartup1(cTrace.cContext.None); // tests that connect fails when there are no credentials that can be used
                ZTestAuthAtStartup2(cTrace.cContext.None); // tests login, literal+, capability response on successful login, disconnect
                ZTestAuthAtStartup3(cTrace.cContext.None); // tests auth=plain, capability response on successful authenticate
                ZTestAuthAtStartup4(cTrace.cContext.None); // tests auth failure falls back to login, synchronised literals, that failure to authenticate causes connect to fail, capability response where it shouldn't be, failure responses
                ZTestAuthAtStartup4_1(cTrace.cContext.None); // tests referral terminates login sequence
                ZTestAuthAtStartup4_2(cTrace.cContext.None); // tests authfailed terminates login sequence
                if (!pQuick) ZTestAuthAtStartup5(cTrace.cContext.None); // tests capability is issued when there isn't one in the OK, tests IDLE
                ZTestAuthAtStartup5_1(cTrace.cContext.None); // tests referral on an ok

                ZTestLiteralMinus(cTrace.cContext.None); // tests literal-
                if (!pQuick) ZTestNonIdlePolling(cTrace.cContext.None); // tests polling when idle is not available

                ZTestAuth1(lContext); // tests auth=anon and multiple auth methods with forced try
                ZTestSASLIR(lContext);



                //ZTestAuth3(lContext); // tests various weird conditions


                ZTestSearch1(lContext);
                ZTestSearch2(lContext);
                ZTestSearch3(lContext);

                if (!pQuick) ZTestIdleRestart(lContext);
                ZTestUIDFetch1(lContext);

                ZTestBadCharsetUIDNotSticky(lContext);

                if (!pQuick) ZTestPipelineCancellation(lContext);

                if (!pQuick) ZTestEarlyTermination1(lContext);
                if (!pQuick) ZTestEarlyTermination2(lContext);

                ZTestAppendNoCatenateNoBinaryNoUTF8(lContext);
            }
            catch (Exception e) when (lContext.TraceException(e)) { }
        }

        private static void ZTestByeAtStartup1(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestByeAtStartup1));

            using (cServer lServer = new cServer(lContext, nameof(ZTestByeAtStartup1)))
            using (cIMAPClient lClient = lServer.NewClient())
            {
                lServer.AddSendData("* BYE [ALERT] this is the text\r\n");
                lServer.AddExpectClose();

                lClient.SetPlainAuthenticationParameters("fred", "angus");

                cResponseTextExpecter lExpecter = new cResponseTextExpecter(lClient, lContext);
                lExpecter.Expect(eResponseTextContext.greetingbye, eResponseTextCode.alert, "this is the text");

                try
                {
                    lClient.Connect();
                    throw new cTestsException("connect should have failed", lContext);
                }
                catch (cConnectByeException) { /* expected */ }

                if (lClient.HomeServerReferral != null) throw new cTestsException("referral should be null", lContext);

                lServer.ThrowAnyErrors();
                lExpecter.Done();
            }
        }

        private static void ZTestByeAtStartup2(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestByeAtStartup2));

            using (cServer lServer = new cServer(lContext, nameof(ZTestByeAtStartup2)))
            using (cIMAPClient lClient = lServer.NewClient())
            {
                lServer.AddSendData("* BYE this is the text\r\n");
                lServer.AddExpectClose();

                lClient.SetPlainAuthenticationParameters("fred", "angus");

                cResponseTextExpecter lExpecter = new cResponseTextExpecter(lClient, lContext);
                lExpecter.Expect(eResponseTextContext.greetingbye, eResponseTextCode.none, "this is the text");

                try
                {
                    lClient.Connect();
                    throw new cTestsException("connect should have failed", lContext);
                }
                catch (cConnectByeException) { }

                if (lClient.HomeServerReferral != null) throw new cTestsException("referral should be null", lContext);

                lServer.ThrowAnyErrors();
                lExpecter.Done();
            }
        }

        private static void ZTestByeAtStartup3(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestByeAtStartup3));

            using (cServer lServer = new cServer(lContext, nameof(ZTestByeAtStartup3)))
            using (cIMAPClient lClient = lServer.NewClient())
            {
                lServer.AddSendData("* BYE [REFERRAL IMAP://user;AUTH=*@SERVER2/] Server not accepting connections.Try SERVER2\r\n");
                lServer.AddExpectClose();

                lClient.SetPlainAuthenticationParameters("fred", "angus");

                cResponseTextExpecter lExpecter = new cResponseTextExpecter(lClient, lContext);
                lExpecter.Expect(eResponseTextContext.greetingbye, eResponseTextCode.referral, "Server not accepting connections.Try SERVER2");

                try
                {
                    lClient.Connect();
                    throw new cTestsException("connect should have failed", lContext);
                }
                catch (cHomeServerReferralException) { }

                if (lClient.HomeServerReferral == null) throw new cTestsException("referral should be set", lContext);
                if (lClient.HomeServerReferral.MustUseAnonymous || lClient.HomeServerReferral.UserId != "user" || lClient.HomeServerReferral.MechanismName != null || lClient.HomeServerReferral.Host != "SERVER2" || lClient.HomeServerReferral.Port != 143) throw new cTestsException("referral isn't what is expected", lContext);

                lServer.ThrowAnyErrors();
                lExpecter.Done();
            }
        }

        private static void ZTestPreauthAtStartup1(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestPreauthAtStartup1));

            using (cServer lServer = new cServer(lContext, nameof(ZTestPreauthAtStartup1)))
            using (cIMAPClient lClient = lServer.NewClient())
            {
                lServer.AddSendData("* PREAUTH [CAPABILITY IMAP4rev1 AUTH=GSSAPI AUTH=PLAIN ID NAMESPACE] this is the text\r\n");
                lServer.AddExpectTagged("ID (\"name\" \"fred\")\r\n");
                lServer.AddExpectTagged("NAMESPACE\r\n");
                lServer.AddSendData("* NAMESPACE ((\"\" \"/\")) ((\"~\" \"/\")) NIL\r\n");
                lServer.AddSendData("* ID (\"name\" \"Cyrus\" \"version\" \"1.5\" \"os\" \"sunos\" \"os-version\" \"5.5\" \"support-url\" \"mailto:cyrus-bugs+@andrew.cmu.edu\")\r\n");
                lServer.AddSendTagged("OK NAMESPACE command completed\r\n");
                lServer.AddDelay(1000);
                lServer.AddSendTagged("OK ID command completed\r\n");
                lServer.AddExpectTagged("LOGOUT\r\n");
                lServer.AddSendData("* BYE logging out\r\n");
                lServer.AddSendTagged("OK logged out\r\n");
                lServer.AddExpectClose();

                lClient.SetPlainAuthenticationParameters("fred", "angus");

                cIdDictionary lIdDictionary = new cIdDictionary(false);
                lIdDictionary.Name = "fred";
                lClient.ClientId = lIdDictionary;

                cResponseTextExpecter lExpecter = new cResponseTextExpecter(lClient, lContext);
                lExpecter.Expect(eResponseTextContext.greetingpreauth, eResponseTextCode.other, "this is the text");
                lExpecter.Expect(eResponseTextContext.success, eResponseTextCode.none, "NAMESPACE command completed");
                lExpecter.Expect(eResponseTextContext.success, eResponseTextCode.none, "ID command completed");
                lExpecter.Expect(eResponseTextContext.bye, eResponseTextCode.none, "logging out");
                lExpecter.Expect(eResponseTextContext.success, eResponseTextCode.none, "logged out");

                lClient.Connect();
                lClient.Disconnect();

                lServer.ThrowAnyErrors();
                lExpecter.Done();

                if (lClient.ServerId.Name != "Cyrus") throw new cTestsException("serverid failure 1");
                if (lClient.ServerId.Count != 5) throw new cTestsException("serverid failure 2");

                if (lClient.Namespaces.Personal.Count != 1 || lClient.Namespaces.Personal[0].Prefix.Length != 0 || lClient.Namespaces.Personal[0].NamespaceName.Delimiter != '/') throw new cTestsException("namespace failure 1", lContext);
                if (lClient.Namespaces.OtherUsers.Count != 1 || lClient.Namespaces.OtherUsers[0].Prefix != "~" || lClient.Namespaces.OtherUsers[0].NamespaceName.Delimiter != '/') throw new cTestsException("namespace failure 1", lContext);
                if (lClient.Namespaces.Shared != null) throw new cTestsException("namespace failure 1", lContext);
            }
        }

        private static void ZTestPreauthAtStartup1_2(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestPreauthAtStartup1_2));

            using (cServer lServer = new cServer(lContext, nameof(ZTestPreauthAtStartup1_2)))
            using (cIMAPClient lClient = lServer.NewClient())
            {
                lServer.AddSendData("* PREAUTH [CAPABILITY IMAP4rev1 AUTH=GSSAPI AUTH=PLAIN ID] this is the text\r\n");
                lServer.AddExpectTagged("ID (\"name\" \"fr?d\")\r\n");
                lServer.AddSendData("* ID (\"name\" \"Cyrus\" \"version\" \"1.5\" \"os\" \"sunos\" \"os-version\" \"5.5\" \"support-url\" \"mailto:cyrus-bugs+@andrew.cmu.edu\")\r\n");
                lServer.AddSendTagged("OK ID command completed\r\n");

                lServer.AddExpectTagged("LIST \"\" \"\"\r\n");
                lServer.AddSendData("* LIST () nil \"\"\r\n");
                lServer.AddSendTagged("OK LIST command completed\r\n");


                lServer.AddExpectTagged("LOGOUT\r\n");
                lServer.AddSendData("* BYE logging out\r\n");
                lServer.AddSendTagged("OK logged out\r\n");
                lServer.AddExpectClose();

                lClient.SetPlainAuthenticationParameters("fred", "angus");

                cIdDictionary lIdDictionary = new cIdDictionary();
                lIdDictionary.Name = "fr€d";

                bool lFailed = false;
                try { lClient.ClientId = lIdDictionary; }
                catch (Exception) { lFailed = true; }
                if (!lFailed) throw new cTestsException("ZTestPreauthAtStartup1_2: utf8 client id should have failed");

                lIdDictionary = new cIdDictionary(false);
                lIdDictionary.Name = "fr?d";
                lClient.ClientId = lIdDictionary;

                lClient.IdleConfiguration = null;

                cResponseTextExpecter lExpecter = new cResponseTextExpecter(lClient, lContext);
                lExpecter.Expect(eResponseTextContext.greetingpreauth, eResponseTextCode.other, "this is the text");
                lExpecter.Expect(eResponseTextContext.success, eResponseTextCode.none, "ID command completed");
                lExpecter.Expect(eResponseTextContext.success, eResponseTextCode.none, "LIST command completed");
                lExpecter.Expect(eResponseTextContext.bye, eResponseTextCode.none, "logging out");
                lExpecter.Expect(eResponseTextContext.success, eResponseTextCode.none, "logged out");

                lClient.Connect();
                lClient.Disconnect();

                lServer.ThrowAnyErrors();
                lExpecter.Done();

                if (lClient.ServerId.Name != "Cyrus") throw new cTestsException("serverid failure 1");
                if (lClient.ServerId.Count != 5) throw new cTestsException("serverid failure 2");
            }
        }

        private static void ZTestPreauthAtStartup1_3(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestPreauthAtStartup1_3));

            using (cServer lServer = new cServer(lContext, nameof(ZTestPreauthAtStartup1_3)))
            using (cIMAPClient lClient = lServer.NewClient())
            {
                lServer.AddSendData("* OK [CAPABILITY ENABLE IMAP4rev1 AUTH=GSSAPI UTF8=ACCEPT LITERAL- ID] this is the text\r\n");
                //lServer.AddExpectTagged("ID (\"name\" \"fr?d\")\r\n");
                //lServer.AddSendData("* ID (\"name\" \"Cyrus\" \"version\" \"1.5\" \"os\" \"sunos\" \"os-version\" \"5.5\" \"support-url\" \"mailto:cyrus-bugs+@andrew.cmu.edu\")\r\n");
                //lServer.AddSendTagged("OK ID command completed\r\n");


                lServer.AddExpectTagged("LOGIN {4+}\r\nfred {5+}\r\nangus\r\n");
                lServer.AddSendTagged("OK [CAPABILITY ENABLE IMAP4rev1 AUTH=GSSAPI UTF8=ACCEPT LITERAL- ID] logged in\r\n");

                lServer.AddExpectTagged("ENABLE UTF8=ACCEPT\r\n");
                lServer.AddSendData("* ENABLED UTF8=ACCEPT\r\n");
                lServer.AddSendTagged("OK enable done\r\n");

                lServer.AddExpectTagged(Encoding.UTF8.GetBytes("ID (\"name\" \"fr€d\")\r\n"));
                lServer.AddSendData(Encoding.UTF8.GetBytes("* ID (\"name\" \"Cyrus\" \"version\" \"1.5\" \"os\" \"sunos\" \"os-version\" \"5.5\" \"support-url\" \"mailto:cyrus-bugs+@andr€w.cmu.€du\")\r\n"));
                lServer.AddSendTagged("OK ID command completed\r\n");

                lServer.AddExpectTagged("LIST \"\" \"\"\r\n");
                lServer.AddSendData("* LIST () nil \"\"\r\n");
                lServer.AddSendTagged("OK LIST command completed\r\n");

                lServer.AddExpectTagged("LOGOUT\r\n");
                lServer.AddSendData("* BYE logging out\r\n");
                lServer.AddSendTagged("OK logged out\r\n");
                lServer.AddExpectClose();

                lClient.SetPlainAuthenticationParameters("fred", "angus", eTLSRequirement.indifferent);

                cIdDictionary lIdDictionary = new cIdDictionary(false);
                lIdDictionary.Name = "fr€d";
                lClient.ClientIdUTF8 = lIdDictionary;

                cResponseTextExpecter lExpecter = new cResponseTextExpecter(lClient, lContext);
                lExpecter.Expect(eResponseTextContext.greetingok, eResponseTextCode.other, "this is the text");
                //lExpecter.Expect(eResponseTextContext.success, eResponseTextCode.none, "ID command completed");
                lExpecter.Expect(eResponseTextContext.success, eResponseTextCode.other, "logged in");
                lExpecter.Expect(eResponseTextContext.success, eResponseTextCode.none, "enable done");
                lExpecter.Expect(eResponseTextContext.success, eResponseTextCode.none, "ID command completed");
                lExpecter.Expect(eResponseTextContext.success, eResponseTextCode.none, "LIST command completed");
                lExpecter.Expect(eResponseTextContext.bye, eResponseTextCode.none, "logging out");
                lExpecter.Expect(eResponseTextContext.success, eResponseTextCode.none, "logged out");

                lClient.Connect();
                lClient.Disconnect();

                lServer.ThrowAnyErrors();
                lExpecter.Done();

                if (lClient.ServerId.Count != 5) throw new cTestsException("expected 5 fields");
                if (lClient.ServerId.Name != "Cyrus") throw new cTestsException("expected cyrus");
                if (lClient.ServerId.SupportURL != "mailto:cyrus-bugs+@andr€w.cmu.€du") throw new cTestsException("expected UTF8 in the support URL");
                if (lClient.Namespaces.Personal.Count != 1 || lClient.Namespaces.OtherUsers != null || lClient.Namespaces.Shared != null || lClient.Namespaces.Personal[0].NamespaceName.Delimiter != null || lClient.Namespaces.Personal[0].Prefix != "") throw new cTestsException("namespace problem");
            }
        }

        private static void ZTestPreauthAtStartup2(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestPreauthAtStartup2));

            using (cServer lServer = new cServer(lContext, nameof(ZTestPreauthAtStartup2)))
            using (cIMAPClient lClient = lServer.NewClient())
            {

                lServer.AddSendData("* PREAUTH this is the text\r\n");
                lServer.AddExpectTagged("CAPABILITY\r\n");
                lServer.AddSendData("* CAPABILITY ENABLE IDLE LITERAL+ UTF8=ACCEPT IMAP4rev1 AUTH=PLAIN XSOMEFEATURE XSOMEOTHERFEATURE LOGINDISABLED\r\n");
                lServer.AddSendTagged("OK capability done\r\n");

                lServer.AddExpectTagged("ENABLE UTF8=ACCEPT\r\n");
                lServer.AddSendData("* ENABLED UTF8=ACCEPT\r\n");
                lServer.AddSendData("* OK information message\r\n");
                lServer.AddSendData("* NO warning message\r\n");
                lServer.AddSendData("* BAD error message\r\n");
                lServer.AddSendData("* ENABLED UTF8=ACCEPT\r\n");
                lServer.AddSendTagged("OK enable done\r\n");

                lServer.AddExpectTagged("LIST \"\" \"\"\r\n");
                lServer.AddSendData("* LIST () \"/\" \"\"\r\n");
                lServer.AddSendTagged("OK LIST command completed\r\n");

                lServer.AddExpectTagged("LOGOUT\r\n");
                lServer.AddSendData("* BYE logging out\r\n");
                lServer.AddSendTagged("OK logged out\r\n");
                lServer.AddExpectClose();

                lClient.SetPlainAuthenticationParameters("fred", "angus");

                cResponseTextExpecter lExpecter = new cResponseTextExpecter(lClient, lContext);
                lExpecter.Expect(eResponseTextContext.greetingpreauth, eResponseTextCode.none, "this is the text");
                lExpecter.Expect(eResponseTextContext.success, eResponseTextCode.none, "capability done");
                lExpecter.Expect(eResponseTextContext.information, eResponseTextCode.none, "information message");
                lExpecter.Expect(eResponseTextContext.warning, eResponseTextCode.none, "warning message");
                lExpecter.Expect(eResponseTextContext.protocolerror, eResponseTextCode.none, "error message");
                lExpecter.Expect(eResponseTextContext.success, eResponseTextCode.none, "enable done");
                lExpecter.Expect(eResponseTextContext.success, eResponseTextCode.none, "LIST command completed");
                lExpecter.Expect(eResponseTextContext.bye, eResponseTextCode.none, "logging out");
                lExpecter.Expect(eResponseTextContext.success, eResponseTextCode.none, "logged out");

                lClient.Connect();
                lClient.Disconnect();

                lServer.ThrowAnyErrors();
                lExpecter.Done();

                if (lClient.Namespaces.Personal[0].NamespaceName.Delimiter != '/' || lClient.Namespaces.Personal[0].Prefix != "") throw new cTestsException("namespace problem");
            }
        }

        private static void ZTestPreauthAtStartup3(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestPreauthAtStartup3));

            using (cServer lServer = new cServer(lContext, nameof(ZTestPreauthAtStartup3)))
            using (cIMAPClient lClient = lServer.NewClient())
            {
                lServer.AddSendData("* PREAUTH [CAPABILITY ENABLE IDLE LITERAL+ IMAP4rev1 AUTH=PLAIN XSOMEFEATURE XSOMEOTHERFEATURE LOGINDISABLED] this is the text\r\n");
                lServer.AddExpectTagged("LIST \"\" \"\"\r\n");
                lServer.AddSendData("* LIST () \"/\" \"\"\r\n");
                lServer.AddSendTagged("OK LIST command completed\r\n");
                lServer.AddExpectTagged("LOGOUT\r\n");
                lServer.AddSendData("* BYE logging out\r\n");
                lServer.AddSendTagged("OK logged out\r\n");
                lServer.AddClose();

                lClient.SetPlainAuthenticationParameters("fred", "angus");

                cResponseTextExpecter lExpecter = new cResponseTextExpecter(lClient, lContext);
                lExpecter.Expect(eResponseTextContext.greetingpreauth, eResponseTextCode.other, "this is the text");
                lExpecter.Expect(eResponseTextContext.success, eResponseTextCode.none, "LIST command completed");
                lExpecter.Expect(eResponseTextContext.bye, eResponseTextCode.none, "logging out");
                lExpecter.Expect(eResponseTextContext.success, eResponseTextCode.none, "logged out");

                lClient.Connect();
                lClient.Disconnect();

                lServer.ThrowAnyErrors();
                lExpecter.Done();
            }
        }

        private static void ZTestAuthAtStartup1(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestAuthAtStartup1));

            using (cServer lServer = new cServer(lContext, nameof(ZTestAuthAtStartup1)))
            using (cIMAPClient lClient = lServer.NewClient())
            {
                lServer.AddSendData("* OK [CAPABILITY ENABLE IDLE LITERAL+ IMAP4rev1 XSOMEFEATURE XSOMEOTHERFEATURE LOGINDISABLED] this is the text\r\n");
                lServer.AddExpectTagged("LOGOUT\r\n");
                lServer.AddSendData("* BYE logging out\r\n");
                lServer.AddSendTagged("OK logged out\r\n");
                lServer.AddClose();

                lClient.SetPlainAuthenticationParameters("fred", "angus");

                cResponseTextExpecter lExpecter = new cResponseTextExpecter(lClient, lContext);
                lExpecter.Expect(eResponseTextContext.greetingok, eResponseTextCode.other, "this is the text");
                lExpecter.Expect(eResponseTextContext.bye, eResponseTextCode.none, "logging out");
                lExpecter.Expect(eResponseTextContext.success, eResponseTextCode.none, "logged out");


                bool lFailed = false;
                try { lClient.Connect(); }
                catch (cAuthenticationMechanismsException) { lFailed = true; }
                if (!lFailed) throw new cTestsException("expected connect to fail");

                lServer.ThrowAnyErrors();
                lExpecter.Done();
            }
        }

        private static void ZTestAuthAtStartup2(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestAuthAtStartup2));

            using (cServer lServer = new cServer(lContext, nameof(ZTestAuthAtStartup2)))
            using (cIMAPClient lClient = lServer.NewClient())
            {
                lServer.AddSendData("* OK [CAPABILITY ENABLE IDLE LITERAL+ IMAP4rev1 XSOMEFEATURE XSOMEOTHERFEATURE] this is the text\r\n");
                lServer.AddExpectTagged("LOGIN {4+}\r\nfred {5+}\r\nangus\r\n");
                lServer.AddSendTagged("OK [CAPABILITY ENABLE IDLE LITERAL+ IMAP4rev1 XSOMEFEATURE XSOMEOTHERFEATURE] logged in\r\n");

                lServer.AddExpectTagged("LIST \"\" \"\"\r\n");
                lServer.AddSendData("* LIST () \"/\" \"\"\r\n");
                lServer.AddSendTagged("OK LIST command completed\r\n");

                lServer.AddExpectTagged("LOGOUT\r\n");
                lServer.AddSendData("* BYE logging out\r\n");
                lServer.AddSendTagged("OK logged out\r\n");
                lServer.AddClose();

                lClient.SetPlainAuthenticationParameters("fred", "angus", eTLSRequirement.indifferent);

                cResponseTextExpecter lExpecter = new cResponseTextExpecter(lClient, lContext);
                lExpecter.Expect(eResponseTextContext.greetingok, eResponseTextCode.other, "this is the text");
                lExpecter.Expect(eResponseTextContext.success, eResponseTextCode.other, "logged in");
                lExpecter.Expect(eResponseTextContext.success, eResponseTextCode.none, "LIST command completed");
                lExpecter.Expect(eResponseTextContext.bye, eResponseTextCode.none, "logging out");
                lExpecter.Expect(eResponseTextContext.success, eResponseTextCode.none, "logged out");

                lClient.Connect();

                if (lClient.HomeServerReferral != null) throw new cTestsException("referral should be null", lContext);

                lClient.Disconnect();

                lServer.ThrowAnyErrors();
                lExpecter.Done();
            }
        }

        private static void ZTestAuthAtStartup3(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestAuthAtStartup3));

            using (cServer lServer = new cServer(lContext, nameof(ZTestAuthAtStartup3)))
            using (cIMAPClient lClient = lServer.NewClient())
            {
                lServer.AddSendData("* OK [CAPABILITY ENABLE IDLE LITERAL+ IMAP4rev1 XSOMEFEATURE AUTH=PLAIN XSOMEOTHERFEATURE] this is the text\r\n");
                lServer.AddExpectTagged("AUTHENTICATE PLAIN\r\n");
                lServer.AddSendData("+ \r\n");
                lServer.AddExpectData("AGZyZWQAYW5ndXM=\r\n");
                lServer.AddSendTagged("OK [CAPABILITY ENABLE IDLE LITERAL+ IMAP4rev1 XSOMEFEATURE XSOMEOTHERFEATURE] logged in\r\n");
                lServer.AddExpectTagged("LIST \"\" \"\"\r\n");
                lServer.AddSendData("* LIST () \"/\" \"\"\r\n");
                lServer.AddSendTagged("OK LIST command completed\r\n");
                lServer.AddExpectTagged("LOGOUT\r\n");
                lServer.AddSendData("* BYE logging out\r\n");
                lServer.AddSendTagged("OK logged out\r\n");
                lServer.AddClose();

                lClient.SetPlainAuthenticationParameters("fred", "angus", eTLSRequirement.indifferent);

                cResponseTextExpecter lExpecter = new cResponseTextExpecter(lClient, lContext);
                lExpecter.Expect(eResponseTextContext.greetingok, eResponseTextCode.other, "this is the text");
                lExpecter.Expect(eResponseTextContext.success, eResponseTextCode.other, "logged in");
                lExpecter.Expect(eResponseTextContext.success, eResponseTextCode.none, "LIST command completed");
                lExpecter.Expect(eResponseTextContext.bye, eResponseTextCode.none, "logging out");
                lExpecter.Expect(eResponseTextContext.success, eResponseTextCode.none, "logged out");

                lClient.Connect();

                if (lClient.HomeServerReferral != null) throw new cTestsException("referral should be null", lContext);

                lClient.Disconnect();

                lServer.ThrowAnyErrors();
                lExpecter.Done();
            }
        }

        private static void ZTestAuthAtStartup4(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestAuthAtStartup4));

            using (cServer lServer = new cServer(lContext, nameof(ZTestAuthAtStartup4)))
            using (cIMAPClient lClient = lServer.NewClient())
            {
                lServer.AddSendData("* OK [CAPABILITY ENABLE IDLE IMAP4rev1 XSOMEFEATURE AUTH=PLAIN XSOMEOTHERFEATURE] this is the text\r\n");

                lServer.AddExpectTagged("AUTHENTICATE PLAIN\r\n");
                lServer.AddSendData("+ \r\n");
                lServer.AddExpectData("AGZyZWQAYW5ndXM=\r\n");
                lServer.AddSendTagged("NO incorrect password\r\n");

                lServer.AddExpectTagged("LOGIN {4}\r\n");
                lServer.AddSendData("+ ready\r\n");
                lServer.AddExpectData("fred {5}\r\n");
                lServer.AddSendData("+ ready\r\n");
                lServer.AddExpectData("angus\r\n");
                lServer.AddSendTagged("NO [CAPABILITY ENABLE IDLE LITERAL+ IMAP4rev1 XSOMEFEATURE XSOMEOTHERFEATURE] incorrect password again\r\n");

                lServer.AddExpectTagged("LOGOUT\r\n");
                lServer.AddSendData("* BYE logging out\r\n");
                lServer.AddSendTagged("OK logged out\r\n");
                lServer.AddClose();

                lClient.SetPlainAuthenticationParameters("fred", "angus", eTLSRequirement.indifferent);

                cResponseTextExpecter lExpecter = new cResponseTextExpecter(lClient, lContext);
                lExpecter.Expect(eResponseTextContext.greetingok, eResponseTextCode.other, "this is the text");
                lExpecter.Expect(eResponseTextContext.failure, eResponseTextCode.none, "incorrect password");
                lExpecter.Expect(eResponseTextContext.continuerequest, eResponseTextCode.none, "ready");
                lExpecter.Expect(eResponseTextContext.continuerequest, eResponseTextCode.none, "ready");
                lExpecter.Expect(eResponseTextContext.failure, eResponseTextCode.other, "incorrect password again"); // the CAPABILITY on a NO is not allowed by the base spec
                lExpecter.Expect(eResponseTextContext.bye, eResponseTextCode.none, "logging out");
                lExpecter.Expect(eResponseTextContext.success, eResponseTextCode.none, "logged out");

                bool lFailed = false;
                try { lClient.Connect(); }
                catch (cCredentialsException) { lFailed = true; }
                if (!lFailed) throw new cTestsException("expected connect to fail");

                lServer.ThrowAnyErrors();
                lExpecter.Done();
            }
        }

        private static void ZTestAuthAtStartup4_1(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestAuthAtStartup4_1));

            using (cServer lServer = new cServer(lContext, nameof(ZTestAuthAtStartup4_1)))
            using (cIMAPClient lClient = lServer.NewClient())
            {
                lServer.AddSendData("* OK [CAPABILITY ENABLE IDLE IMAP4rev1 XSOMEFEATURE AUTH=PLAIN XSOMEOTHERFEATURE LOGIN-REFERRALS] this is the text\r\n");

                lServer.AddExpectTagged("AUTHENTICATE PLAIN\r\n");
                lServer.AddSendData("+ \r\n");
                lServer.AddExpectData("AGZyZWQAYW5ndXM=\r\n");
                lServer.AddSendTagged("NO [REFERRAL IMAP://user;AUTH=GSSAPI@SERVER2/] Specified user is invalid on this server.Try SERVER2.\r\n");

                lServer.AddExpectTagged("LOGOUT\r\n");
                lServer.AddSendData("* BYE logging out\r\n");
                lServer.AddSendTagged("OK logged out\r\n");
                lServer.AddClose();

                lClient.SetPlainAuthenticationParameters("fred", "angus", eTLSRequirement.indifferent);

                cResponseTextExpecter lExpecter = new cResponseTextExpecter(lClient, lContext);
                lExpecter.Expect(eResponseTextContext.greetingok, eResponseTextCode.other, "this is the text");
                lExpecter.Expect(eResponseTextContext.failure, eResponseTextCode.referral, "Specified user is invalid on this server.Try SERVER2.");
                lExpecter.Expect(eResponseTextContext.bye, eResponseTextCode.none, "logging out");
                lExpecter.Expect(eResponseTextContext.success, eResponseTextCode.none, "logged out");

                bool lFailed = false;
                try { lClient.Connect(); }
                catch (cHomeServerReferralException)
                {
                    if (lClient.HomeServerReferral.Host != "SERVER2" || lClient.HomeServerReferral.UserId != "user" || lClient.HomeServerReferral.MechanismName != "GSSAPI") throw new cTestsException("unexpected URL properties", lContext);
                    lFailed = true;
                }
                if (!lFailed) throw new cTestsException("expected connect to fail");

                if (lClient.HomeServerReferral == null) throw new cTestsException("referral should be set", lContext);
                if (lClient.HomeServerReferral.MustUseAnonymous || lClient.HomeServerReferral.UserId != "user" || lClient.HomeServerReferral.MechanismName != "GSSAPI" || lClient.HomeServerReferral.Host != "SERVER2" || lClient.HomeServerReferral.Port != 143) throw new cTestsException("referral isn't what is expected", lContext);

                lServer.ThrowAnyErrors();
                lExpecter.Done();
            }
        }

        private static void ZTestAuthAtStartup4_2(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestAuthAtStartup4_2));

            using (cServer lServer = new cServer(lContext, nameof(ZTestAuthAtStartup4_2)))
            using (cIMAPClient lClient = lServer.NewClient())
            {
                lServer.AddSendData("* OK [CAPABILITY ENABLE IDLE IMAP4rev1 XSOMEFEATURE AUTH=PLAIN XSOMEOTHERFEATURE] this is the text\r\n");

                lServer.AddExpectTagged("AUTHENTICATE PLAIN\r\n");
                lServer.AddSendData("+ \r\n");
                lServer.AddExpectData("AGZyZWQAYW5ndXM=\r\n");
                lServer.AddSendTagged("NO [AUTHENTICATIONFAILED] incorrect password\r\n");

                lServer.AddExpectTagged("LOGOUT\r\n");
                lServer.AddSendData("* BYE logging out\r\n");
                lServer.AddSendTagged("OK logged out\r\n");
                lServer.AddClose();

                lClient.SetPlainAuthenticationParameters("fred", "angus", eTLSRequirement.indifferent);

                cResponseTextExpecter lExpecter = new cResponseTextExpecter(lClient, lContext);
                lExpecter.Expect(eResponseTextContext.greetingok, eResponseTextCode.other, "this is the text");
                lExpecter.Expect(eResponseTextContext.failure, eResponseTextCode.authenticationfailed, "incorrect password");
                lExpecter.Expect(eResponseTextContext.bye, eResponseTextCode.none, "logging out");
                lExpecter.Expect(eResponseTextContext.success, eResponseTextCode.none, "logged out");

                bool lFailed = false;
                try { lClient.Connect(); }
                catch (cCredentialsException) { lFailed = true; }
                if (!lFailed) throw new cTestsException("expected connect to fail");

                lServer.ThrowAnyErrors();
                lExpecter.Done();
            }
        }

        private static void ZTestAuthAtStartup5(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestAuthAtStartup5));

            using (cServer lServer = new cServer(lContext, nameof(ZTestAuthAtStartup5), 60000))
            using (cIMAPClient lClient = lServer.NewClient())
            {
                lServer.AddSendData("* OK [CAPABILITY ENABLE IDLE LITERAL+ IMAP4rev1 XSOMEFEATURE AUTH=PLAIN XSOMEOTHERFEATURE] this is the text\r\n");

                lServer.AddExpectTagged("AUTHENTICATE PLAIN\r\n");
                lServer.AddSendData("+ \r\n");
                lServer.AddExpectData("AGZyZWQAYW5ndXM=\r\n");
                lServer.AddSendTagged("OK logged in\r\n");

                lServer.AddExpectTagged("CAPABILITY\r\n");
                lServer.AddSendData("* CAPABILITY ENABLE IDLE LITERAL+ IMAP4rev1 XSOMEFEATURE XSOMEOTHERFEATURE\r\n");
                lServer.AddSendTagged("OK capability done\r\n");

                lServer.AddExpectTagged("LIST \"\" \"\"\r\n");
                lServer.AddSendData("* LIST () \"/\" \"\"\r\n");
                lServer.AddSendTagged("OK LIST command completed\r\n");


                lServer.AddExpectTagged("IDLE\r\n");
                lServer.AddSendData("+ idling\r\n");
                lServer.AddDelay(1000);
                lServer.AddSendData("* OK information message\r\n");
                lServer.AddDelay(1000);
                lServer.AddSendData("* NO warning message\r\n");
                lServer.AddDelay(1000);
                lServer.AddSendData("* BAD error message\r\n");
                lServer.AddExpectData("DONE\r\n");
                lServer.AddSendTagged("OK idle terminated\r\n");

                lServer.AddExpectTagged("IDLE\r\n");
                lServer.AddSendData("+ idling\r\n");
                lServer.AddDelay(1000);
                lServer.AddSendData("* BYE unilateral bye\r\n");

                lServer.AddExpectClose();

                lClient.SetPlainAuthenticationParameters("fred", "angus", eTLSRequirement.indifferent);
                lClient.IdleConfiguration = new cIdleConfiguration(2000, 10000);

                cResponseTextExpecter lExpecter = new cResponseTextExpecter(lClient, lContext);
                lExpecter.Expect(eResponseTextContext.greetingok, eResponseTextCode.other, "this is the text");
                lExpecter.Expect(eResponseTextContext.success, eResponseTextCode.none, "logged in");
                lExpecter.Expect(eResponseTextContext.success, eResponseTextCode.none, "capability done");
                lExpecter.Expect(eResponseTextContext.success, eResponseTextCode.none, "LIST command completed");
                lExpecter.Expect(eResponseTextContext.continuerequest, eResponseTextCode.none, "idling");
                lExpecter.Expect(eResponseTextContext.information, eResponseTextCode.none, "information message");
                lExpecter.Expect(eResponseTextContext.warning, eResponseTextCode.none, "warning message");
                lExpecter.Expect(eResponseTextContext.protocolerror, eResponseTextCode.none, "error message");
                lExpecter.Expect(eResponseTextContext.success, eResponseTextCode.none, "idle terminated");
                lExpecter.Expect(eResponseTextContext.continuerequest, eResponseTextCode.none, "idling");
                lExpecter.Expect(eResponseTextContext.bye, eResponseTextCode.none, "unilateral bye");

                lClient.Connect();

                lServer.GetSessionTask(0).Wait(60000);

                bool lFailed = false;
                try { lClient.Disconnect(); }
                catch
                {
                    lContext.TraceVerbose($"disconnect failed as expected:\n");
                    lFailed = true;
                }

                if (!lFailed) throw new cTestsException("disconnect should have failed");

                lServer.ThrowAnyErrors();
                lExpecter.Done();
            }
        }

        private static void ZTestAuthAtStartup5_1(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestAuthAtStartup5_1));

            using (cServer lServer = new cServer(lContext, nameof(ZTestAuthAtStartup5_1)))
            using (cIMAPClient lClient = lServer.NewClient())
            {
                lServer.AddSendData("* OK [CAPABILITY ENABLE IDLE LITERAL+ IMAP4rev1 XSOMEFEATURE AUTH=PLAIN XSOMEOTHERFEATURE LOGIN-REFERRALS] this is the text\r\n");

                lServer.AddExpectTagged("AUTHENTICATE PLAIN\r\n");
                lServer.AddSendData("+ \r\n");
                lServer.AddExpectData("AGZyZWQAYW5ndXM=\r\n");
                lServer.AddSendTagged("OK [REFERRAL IMAP://MATTHEW@SERVER2/] Specified user's personal mailboxes located on Server2, but public mailboxes are available.\r\n");

                lServer.AddExpectTagged("CAPABILITY\r\n");
                lServer.AddSendData("* CAPABILITY ENABLE IDLE LITERAL+ IMAP4rev1 XSOMEFEATURE XSOMEOTHERFEATURE\r\n");
                lServer.AddSendTagged("OK capability done\r\n");


                lServer.AddExpectTagged("LIST \"\" \"\"\r\n");



                lServer.AddSendData("* BYE unilateral bye\r\n");

                lServer.AddExpectClose();

                lClient.SetPlainAuthenticationParameters("fred", "angus", eTLSRequirement.indifferent);


                try { lClient.Connect(); }
                catch { }

                if (lClient.HomeServerReferral == null) throw new cTestsException("referral should be set", lContext);
                if (lClient.HomeServerReferral.MustUseAnonymous || lClient.HomeServerReferral.UserId != "MATTHEW" || lClient.HomeServerReferral.MechanismName != null || lClient.HomeServerReferral.Host != "SERVER2" || lClient.HomeServerReferral.Port != 143) throw new cTestsException("referral isn't what is expected", lContext);

                bool lFailed = false;
                try { lClient.Poll(); }
                catch (Exception e)
                {
                    lContext.TraceVerbose($"poll failed as expected:\n{e}");
                    lFailed = true;
                }
                if (!lFailed) throw new cTestsException("poll should have failed");

                lServer.ThrowAnyErrors();
            }
        }

        private static string ZString(int pLength)
        {
            StringBuilder lBuilder = new StringBuilder(pLength);
            for (int i = 0; i < pLength; i++) lBuilder.Append((char)mRandom.Next(32, 126));
            return lBuilder.ToString();
        }

        private static void ZTestLiteralMinus(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestLiteralMinus));
            ZTestLiteralMinusWorker(ZString(10), ZString(10), lContext);
            ZTestLiteralMinusWorker(ZString(4096), ZString(4096), lContext);
            ZTestLiteralMinusWorker(ZString(4097), ZString(4097), lContext);
            ZTestLiteralMinusWorker(ZString(10), ZString(4097), lContext);
            ZTestLiteralMinusWorker(ZString(4097), ZString(10), lContext);
        }

        private static void ZTestLiteralMinusWorker(string pUserId, string pPassword, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestLiteralMinusWorker), pUserId, pPassword);

            using (cServer lServer = new cServer(lContext, nameof(ZTestLiteralMinusWorker)))
            using (cIMAPClient lClient = lServer.NewClient())
            {
                lServer.AddSendData("* OK [CAPABILITY LITERAL- IMAP4rev1] this is the text\r\n");

                if (pUserId.Length < 4097 && pPassword.Length < 4097)
                {
                    lServer.AddExpectTagged("LOGIN {" + pUserId.Length.ToString() + "+}\r\n" + pUserId + " {" + pPassword.Length.ToString() + "+}\r\n" + pPassword + "\r\n");
                }
                else if (pUserId.Length > 4096 && pPassword.Length > 4096)
                {
                    lServer.AddExpectTagged("LOGIN {" + pUserId.Length.ToString() + "}\r\n");
                    lServer.AddSendData("+ ready\r\n");
                    lServer.AddExpectData(pUserId + " {" + pPassword.Length.ToString() + "}\r\n");
                    lServer.AddSendData("+ ready\r\n");
                    lServer.AddExpectData(pPassword + "\r\n");
                }
                else if (pUserId.Length < 4097)
                {
                    lServer.AddExpectTagged("LOGIN {" + pUserId.Length.ToString() + "+}\r\n" + pUserId + " {" + pPassword.Length.ToString() + "}\r\n");
                    lServer.AddSendData("+ ready\r\n");
                    lServer.AddExpectData(pPassword + "\r\n");
                }
                else
                {
                    lServer.AddExpectTagged("LOGIN {" + pUserId.Length.ToString() + "}\r\n");
                    lServer.AddSendData("+ ready\r\n");
                    lServer.AddExpectData(pUserId + " {" + pPassword.Length.ToString() + "+}\r\n" + pPassword + "\r\n");
                }

                lServer.AddSendTagged("OK [CAPABILITY ENABLE IDLE LITERAL+ IMAP4rev1 XSOMEFEATURE XSOMEOTHERFEATURE] logged in\r\n");


                lServer.AddExpectTagged("LIST \"\" \"\"\r\n");
                lServer.AddSendData("* LIST () \"/\" \"\"\r\n");
                lServer.AddSendTagged("OK LIST command completed\r\n");


                lServer.AddExpectTagged("LOGOUT\r\n");
                lServer.AddSendData("* BYE logging out\r\n");
                lServer.AddSendTagged("OK logged out\r\n");
                lServer.AddExpectClose();

                lClient.SetPlainAuthenticationParameters(pUserId, pPassword, eTLSRequirement.indifferent);

                lClient.Connect();
                lClient.Disconnect();

                lServer.ThrowAnyErrors();
            }
        }

        private static void ZTestNonIdlePolling(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestNonIdlePolling));

            using (cServer lServer = new cServer(lContext, nameof(ZTestNonIdlePolling), 60000))
            using (cIMAPClient lClient = lServer.NewClient())
            {
                lServer.AddSendData("* OK [CAPABILITY ENABLE LITERAL+ IMAP4rev1 XSOMEFEATURE AUTH=PLAIN XSOMEOTHERFEATURE] this is the text\r\n");

                lServer.AddExpectTagged("AUTHENTICATE PLAIN\r\n");
                lServer.AddSendData("+ \r\n");
                lServer.AddExpectData("AGZyZWQAYW5ndXM=\r\n");
                lServer.AddSendTagged("OK logged in\r\n");

                lServer.AddExpectTagged("CAPABILITY\r\n");
                lServer.AddSendData("* CAPABILITY ENABLE LITERAL+ IMAP4rev1 XSOMEFEATURE XSOMEOTHERFEATURE\r\n");
                lServer.AddSendTagged("OK capability done\r\n");

                lServer.AddExpectTagged("LIST \"\" \"\"\r\n");
                lServer.AddSendData("* LIST () \"/\" \"\"\r\n");
                lServer.AddSendTagged("OK LIST command completed\r\n");


                lServer.AddExpectTagged("NOOP\r\n");
                lServer.AddSendData("* OK information message\r\n");
                lServer.AddSendData("* NO warning message\r\n");
                lServer.AddSendData("* BAD error message\r\n");
                lServer.AddSendTagged("OK noop completed\r\n");

                lServer.AddExpectTagged("NOOP\r\n");
                lServer.AddSendData("* OK information message\r\n");
                lServer.AddSendData("* NO warning message\r\n");
                lServer.AddSendData("* BAD error message\r\n");
                lServer.AddSendTagged("OK noop completed\r\n");

                lServer.AddExpectTagged("NOOP\r\n");
                lServer.AddSendData("* BYE unilateral bye\r\n");
                lServer.AddExpectClose();


                lClient.SetPlainAuthenticationParameters("fred", "angus", eTLSRequirement.indifferent);
                lClient.IdleConfiguration = new cIdleConfiguration(2000, 1200000, 7000);

                cResponseTextExpecter lExpecter = new cResponseTextExpecter(lClient, lContext);
                lExpecter.Expect(eResponseTextContext.greetingok, eResponseTextCode.other, "this is the text");
                lExpecter.Expect(eResponseTextContext.success, eResponseTextCode.none, "logged in");
                lExpecter.Expect(eResponseTextContext.success, eResponseTextCode.none, "capability done");
                lExpecter.Expect(eResponseTextContext.success, eResponseTextCode.none, "LIST command completed");
                lExpecter.Expect(eResponseTextContext.information, eResponseTextCode.none, "information message");
                lExpecter.Expect(eResponseTextContext.warning, eResponseTextCode.none, "warning message");
                lExpecter.Expect(eResponseTextContext.protocolerror, eResponseTextCode.none, "error message");
                lExpecter.Expect(eResponseTextContext.success, eResponseTextCode.none, "noop completed");
                lExpecter.Expect(eResponseTextContext.information, eResponseTextCode.none, "information message");
                lExpecter.Expect(eResponseTextContext.warning, eResponseTextCode.none, "warning message");
                lExpecter.Expect(eResponseTextContext.protocolerror, eResponseTextCode.none, "error message");
                lExpecter.Expect(eResponseTextContext.success, eResponseTextCode.none, "noop completed");
                lExpecter.Expect(eResponseTextContext.bye, eResponseTextCode.none, "unilateral bye");


                lClient.Connect();

                lServer.GetSessionTask(0).Wait(60000);

                bool lFailed = false;
                try { lClient.Disconnect(); }
                catch (Exception e)
                {
                    lContext.TraceVerbose($"disconnect failed as expected:\n{e}");
                    lFailed = true;
                }
                if (!lFailed) throw new cTestsException("disconnect should have failed");

                lExpecter.Done();
                lServer.ThrowAnyErrors();
            }
        }

        private class cTestAuth1AuthenticationParameters : cAuthenticationParameters
        {
            public cTestAuth1AuthenticationParameters(object pPreAuthCredId, bool pTryAllSASLs) :
                base(pPreAuthCredId, new cSASL[] { new cSASLPlain("fred", "angus", eTLSRequirement.indifferent), new cSASLAnonymous("fr€d", eTLSRequirement.indifferent) }, pTryAllSASLs, new cLogin("fred", "angus", eTLSRequirement.indifferent)) { }
        }

        private static void ZTestAuth1(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestAuth1));

            var lPreAuthCredId = new object();

            var lCredsNoPreAuthDontTryAll = new cTestAuth1AuthenticationParameters(null, false);
            var lCredsNoPreAuthTryAll = new cTestAuth1AuthenticationParameters(null, true);
            var lCredsPreAuthTryAll = new cTestAuth1AuthenticationParameters(lPreAuthCredId, true);

            bool lFailed;

            // 1 - mechanisms not advertised
            //
            using (cServer lServer = new cServer(lContext, nameof(ZTestAuth1) + "1"))
            using (cIMAPClient lClient = lServer.NewClient())
            {
                lServer.AddSendData("* OK [CAPABILITY ENABLE IDLE LITERAL+ IMAP4rev1 XSOMEFEATURE XSOMEOTHERFEATURE LOGINDISABLED] this is the text\r\n");
                lServer.AddExpectTagged("LOGOUT\r\n");
                lServer.AddSendData("* BYE logging out\r\n");
                lServer.AddSendTagged("OK logged out\r\n");
                lServer.AddClose();

                lClient.AuthenticationParameters = lCredsNoPreAuthDontTryAll;

                lFailed = false;
                try { lClient.Connect(); }
                catch (cAuthenticationMechanismsException) { lFailed = true; }
                if (!lFailed) throw new cTestsException("should have failed to connect", lContext);

                lServer.ThrowAnyErrors();
            }

            cAccountId lAnon = new cAccountId("localhost", cSASLAnonymous.AnonymousCredentialId);

            // 2 - just anon advertised
            //
            using (cServer lServer = new cServer(lContext, nameof(ZTestAuth1) + "2"))
            using (cIMAPClient lClient = lServer.NewClient())
            {
                lServer.AddSendData("* OK [CAPABILITY ENABLE IDLE LITERAL+ IMAP4rev1 XSOMEFEATURE AUTH=ANONYMOUS XSOMEOTHERFEATURE] this is the text\r\n");

                lServer.AddExpectTagged("AUTHENTICATE ANONYMOUS\r\n");
                lServer.AddSendData("+ \r\n");
                lServer.AddExpectData("ZnLigqxk\r\n");
                lServer.AddSendTagged("OK [CAPABILITY ENABLE IDLE LITERAL+ IMAP4rev1 XSOMEFEATURE XSOMEOTHERFEATURE] logged in\r\n");

                lServer.AddExpectTagged("LIST \"\" \"\"\r\n");
                lServer.AddSendData("* LIST () \"/\" \"\"\r\n");
                lServer.AddSendTagged("OK LIST command completed\r\n");


                lServer.AddExpectTagged("LOGOUT\r\n");
                lServer.AddSendData("* BYE logging out\r\n");
                lServer.AddSendTagged("OK logged out\r\n");
                lServer.AddClose();

                lClient.AuthenticationParameters = lCredsNoPreAuthDontTryAll;

                lClient.Connect();

                if (lClient.ConnectedAccountId != lAnon) throw new cTestsException($"{nameof(ZTestAuth1)}.2");

                lClient.Disconnect();

                lServer.ThrowAnyErrors();
            }



            // 3 - both advertised
            //
            using (cServer lServer = new cServer(lContext, nameof(ZTestAuth1) + "3"))
            using (cIMAPClient lClient = lServer.NewClient())
            {
                lServer.AddSendData("* OK [CAPABILITY ENABLE IDLE LITERAL+ IMAP4rev1 XSOMEFEATURE AUTH=ANONYMOUS AUTH=PLAIN XSOMEOTHERFEATURE] this is the text\r\n");

                lServer.AddExpectTagged("AUTHENTICATE PLAIN\r\n");
                lServer.AddSendData("+ \r\n");
                lServer.AddExpectData("AGZyZWQAYW5ndXM=\r\n");
                lServer.AddSendTagged("NO why not try anonymous\r\n");

                lServer.AddExpectTagged("AUTHENTICATE ANONYMOUS\r\n");
                lServer.AddSendData("+ \r\n");
                lServer.AddExpectData("ZnLigqxk\r\n");
                lServer.AddSendTagged("OK [CAPABILITY ENABLE IDLE LITERAL+ IMAP4rev1 XSOMEFEATURE XSOMEOTHERFEATURE] logged in\r\n");

                lServer.AddExpectTagged("LIST \"\" \"\"\r\n");
                lServer.AddSendData("* LIST () \"/\" \"\"\r\n");
                lServer.AddSendTagged("OK LIST command completed\r\n");


                lServer.AddExpectTagged("LOGOUT\r\n");
                lServer.AddSendData("* BYE logging out\r\n");
                lServer.AddSendTagged("OK logged out\r\n");
                lServer.AddClose();

                lClient.AuthenticationParameters = lCredsNoPreAuthDontTryAll;

                lClient.Connect();

                if (lClient.ConnectedAccountId != lAnon) throw new cTestsException($"{nameof(ZTestAuth1)}.3");

                lClient.Disconnect();

                lServer.ThrowAnyErrors();
            }


            // 4 - mechanisms not advertised but force try on
            //
            using (cServer lServer = new cServer(lContext, nameof(ZTestAuth1) + "4"))
            using (cIMAPClient lClient = lServer.NewClient())
            {
                lServer.AddSendData("* OK [CAPABILITY ENABLE IDLE LITERAL+ IMAP4rev1 XSOMEFEATURE XSOMEOTHERFEATURE] this is the text\r\n");

                lServer.AddExpectTagged("AUTHENTICATE PLAIN\r\n");
                lServer.AddSendData("+ \r\n");
                lServer.AddExpectData("AGZyZWQAYW5ndXM=\r\n");
                lServer.AddSendTagged("NO why not try anonymous\r\n");

                lServer.AddExpectTagged("AUTHENTICATE ANONYMOUS\r\n");
                lServer.AddSendData("+ \r\n");
                lServer.AddExpectData("ZnLigqxk\r\n");
                lServer.AddSendTagged("OK [CAPABILITY ENABLE IDLE LITERAL+ IMAP4rev1 XSOMEFEATURE XSOMEOTHERFEATURE] logged in\r\n");

                lServer.AddExpectTagged("LIST \"\" \"\"\r\n");
                lServer.AddSendData("* LIST () \"/\" \"\"\r\n");
                lServer.AddSendTagged("OK LIST command completed\r\n");

                lServer.AddExpectTagged("LOGOUT\r\n");
                lServer.AddSendData("* BYE logging out\r\n");
                lServer.AddSendTagged("OK logged out\r\n");
                lServer.AddClose();

                lClient.AuthenticationParameters = lCredsNoPreAuthTryAll;

                lClient.Connect();

                if (lClient.ConnectedAccountId != lAnon) throw new cTestsException($"{nameof(ZTestAuth1)}.4");

                lClient.Disconnect();

                lServer.ThrowAnyErrors();
            }



            // 4.1 - mechanisms not advertised but force try on (pre-auth)
            //
            using (cServer lServer = new cServer(lContext, nameof(ZTestAuth1) + "4"))
            using (cIMAPClient lClient = lServer.NewClient())
            {
                lServer.AddSendData("* OK [CAPABILITY ENABLE IDLE LITERAL+ IMAP4rev1 XSOMEFEATURE XSOMEOTHERFEATURE] this is the text\r\n");

                lServer.AddExpectTagged("AUTHENTICATE PLAIN\r\n");
                lServer.AddSendData("+ \r\n");
                lServer.AddExpectData("AGZyZWQAYW5ndXM=\r\n");
                lServer.AddSendTagged("NO why not try anonymous\r\n");

                lServer.AddExpectTagged("AUTHENTICATE ANONYMOUS\r\n");
                lServer.AddSendData("+ \r\n");
                lServer.AddExpectData("ZnLigqxk\r\n");
                lServer.AddSendTagged("OK [CAPABILITY ENABLE IDLE LITERAL+ IMAP4rev1 XSOMEFEATURE XSOMEOTHERFEATURE] logged in\r\n");

                lServer.AddExpectTagged("LIST \"\" \"\"\r\n");
                lServer.AddSendData("* LIST () \"/\" \"\"\r\n");
                lServer.AddSendTagged("OK LIST command completed\r\n");

                lServer.AddExpectTagged("LOGOUT\r\n");
                lServer.AddSendData("* BYE logging out\r\n");
                lServer.AddSendTagged("OK logged out\r\n");
                lServer.AddClose();

                lClient.AuthenticationParameters = lCredsPreAuthTryAll;

                lClient.Connect();

                if (lClient.ConnectedAccountId != lAnon) throw new cTestsException($"{nameof(ZTestAuth1)}.4.1");

                lClient.Disconnect();

                lServer.ThrowAnyErrors();
            }


            cAccountId lFred = new cAccountId("localhost", "fred");

            // 5 - mechanisms not advertised but force try on and plain succeeds
            //
            using (cServer lServer = new cServer(lContext, nameof(ZTestAuth1) + "5"))
            using (cIMAPClient lClient = lServer.NewClient())
            {
                lServer.AddSendData("* OK [CAPABILITY ENABLE IDLE LITERAL+ IMAP4rev1 XSOMEFEATURE XSOMEOTHERFEATURE] this is the text\r\n");

                lServer.AddExpectTagged("AUTHENTICATE PLAIN\r\n");
                lServer.AddSendData("+ \r\n");
                lServer.AddExpectData("AGZyZWQAYW5ndXM=\r\n");
                lServer.AddSendTagged("OK [CAPABILITY ENABLE IDLE LITERAL+ IMAP4rev1 XSOMEFEATURE XSOMEOTHERFEATURE] why not try anonymous\r\n");

                lServer.AddExpectTagged("LIST \"\" \"\"\r\n");
                lServer.AddSendData("* LIST () \"/\" \"\"\r\n");
                lServer.AddSendTagged("OK LIST command completed\r\n");

                lServer.AddExpectTagged("LOGOUT\r\n");
                lServer.AddSendData("* BYE logging out\r\n");
                lServer.AddSendTagged("OK logged out\r\n");
                lServer.AddClose();

                lClient.AuthenticationParameters = lCredsNoPreAuthTryAll;

                lClient.Connect();

                if (lClient.ConnectedAccountId != lFred) throw new cTestsException($"{nameof(ZTestAuth1)}.5");

                lClient.Disconnect();

                lServer.ThrowAnyErrors();
            }

            // 6 - mechanisms not advertised but force try on and login succeeds
            //
            using (cServer lServer = new cServer(lContext, nameof(ZTestAuth1) + "5"))
            using (cIMAPClient lClient = lServer.NewClient())
            {
                lServer.AddSendData("* OK [CAPABILITY ENABLE IDLE LITERAL+ IMAP4rev1 XSOMEFEATURE XSOMEOTHERFEATURE] this is the text\r\n");

                lServer.AddExpectTagged("AUTHENTICATE PLAIN\r\n");
                lServer.AddSendData("+ \r\n");
                lServer.AddExpectData("AGZyZWQAYW5ndXM=\r\n");
                lServer.AddSendTagged("NO why not try anonymous\r\n");

                lServer.AddExpectTagged("AUTHENTICATE ANONYMOUS\r\n");
                lServer.AddSendData("+ \r\n");
                lServer.AddExpectData("ZnLigqxk\r\n");
                lServer.AddSendTagged("NO why not try login\r\n");

                lServer.AddExpectTagged("LOGIN {4+}\r\nfred {5+}\r\nangus\r\n");
                lServer.AddSendTagged("OK [CAPABILITY ENABLE IDLE LITERAL+ IMAP4rev1 XSOMEFEATURE XSOMEOTHERFEATURE] logged in\r\n");

                lServer.AddExpectTagged("LIST \"\" \"\"\r\n");
                lServer.AddSendData("* LIST () \"/\" \"\"\r\n");
                lServer.AddSendTagged("OK LIST command completed\r\n");

                lServer.AddExpectTagged("LOGOUT\r\n");
                lServer.AddSendData("* BYE logging out\r\n");
                lServer.AddSendTagged("OK logged out\r\n");
                lServer.AddClose();

                lClient.AuthenticationParameters = lCredsNoPreAuthTryAll;

                lClient.Connect();

                if (lClient.ConnectedAccountId != lFred) throw new cTestsException($"{nameof(ZTestAuth1)}.5");

                lClient.Disconnect();

                lServer.ThrowAnyErrors();
            }

            cAccountId lAnon2 = new cAccountId("localhost", cLogin.Anonymous);

            // 7 - anon not advertised 
            //
            using (cServer lServer = new cServer(lContext, nameof(ZTestAuth1) + "6"))
            using (cIMAPClient lClient = lServer.NewClient())
            {
                lServer.AddSendData("* OK [CAPABILITY ENABLE IDLE LITERAL+ IMAP4rev1 XSOMEFEATURE XSOMEOTHERFEATURE] this is the text\r\n");

                lServer.AddExpectTagged("LOGIN {9+}\r\nanonymous {4+}\r\nfred\r\n");
                lServer.AddSendTagged("OK [CAPABILITY ENABLE IDLE LITERAL+ IMAP4rev1 XSOMEFEATURE XSOMEOTHERFEATURE] logged in\r\n");

                lServer.AddExpectTagged("LIST \"\" \"\"\r\n");
                lServer.AddSendData("* LIST () \"/\" \"\"\r\n");
                lServer.AddSendTagged("OK LIST command completed\r\n");

                lServer.AddExpectTagged("LOGOUT\r\n");
                lServer.AddSendData("* BYE logging out\r\n");
                lServer.AddSendTagged("OK logged out\r\n");
                lServer.AddClose();

                lClient.AuthenticationParameters = cAuthenticationParameters.Anonymous("fred");

                lClient.Connect();

                if (lClient.ConnectedAccountId != lAnon2) throw new cTestsException($"{nameof(ZTestAuth1)}.7");

                lClient.Disconnect();

                lServer.ThrowAnyErrors();
            }

            // 8 - anon advertised 
            //
            using (cServer lServer = new cServer(lContext, nameof(ZTestAuth1) + "6"))
            using (cIMAPClient lClient = lServer.NewClient())
            {
                lServer.AddSendData("* OK [CAPABILITY ENABLE IDLE LITERAL+ IMAP4rev1 XSOMEFEATURE AUTH=ANONYMOUS AUTH=PLAIN XSOMEOTHERFEATURE] this is the text\r\n");

                lServer.AddExpectTagged("AUTHENTICATE ANONYMOUS\r\n");
                lServer.AddSendData("+ \r\n");
                lServer.AddExpectData("ZnLigqxk\r\n");
                lServer.AddSendTagged("OK [CAPABILITY ENABLE IDLE LITERAL+ IMAP4rev1 XSOMEFEATURE XSOMEOTHERFEATURE] logged in\r\n");

                lServer.AddExpectTagged("LIST \"\" \"\"\r\n");
                lServer.AddSendData("* LIST () \"/\" \"\"\r\n");
                lServer.AddSendTagged("OK LIST command completed\r\n");

                lServer.AddExpectTagged("LOGOUT\r\n");
                lServer.AddSendData("* BYE logging out\r\n");
                lServer.AddSendTagged("OK logged out\r\n");
                lServer.AddClose();

                lClient.AuthenticationParameters = cAuthenticationParameters.Anonymous("fred");

                lClient.Connect();

                if (lClient.ConnectedAccountId != lAnon) throw new cTestsException($"{nameof(ZTestAuth1)}.8");

                lClient.Disconnect();

                lServer.ThrowAnyErrors();
            }

            cAccountId lPreAuth = new cAccountId("localhost", lPreAuthCredId);

            // 9 - pre-auth, expected 
            //
            using (cServer lServer = new cServer(lContext, nameof(ZTestAuth1) + "6"))
            using (cIMAPClient lClient = lServer.NewClient())
            {
                lServer.AddSendData("* PREAUTH [CAPABILITY IMAP4rev1] this is the text\r\n");
                lServer.AddExpectTagged("LIST \"\" \"\"\r\n");
                lServer.AddSendData("* LIST () \"/\" \"\"\r\n");
                lServer.AddSendTagged("OK LIST command completed\r\n");

                lServer.AddExpectTagged("LOGOUT\r\n");
                lServer.AddSendData("* BYE logging out\r\n");
                lServer.AddSendTagged("OK logged out\r\n");
                lServer.AddClose();

                lClient.AuthenticationParameters = lCredsPreAuthTryAll;

                lClient.Connect();

                if (lClient.ConnectedAccountId != lPreAuth) throw new cTestsException($"{nameof(ZTestAuth1)}.9");

                lClient.Disconnect();

                lServer.ThrowAnyErrors();
            }

            // 10 - pre-auth, unexpected 
            //
            using (cServer lServer = new cServer(lContext, nameof(ZTestAuth1) + "6"))
            using (cIMAPClient lClient = lServer.NewClient())
            {
                lServer.AddSendData("* PREAUTH [CAPABILITY IMAP4rev1] this is the text\r\n");

                lServer.AddExpectTagged("LOGOUT\r\n");
                lServer.AddSendData("* BYE logging out\r\n");
                lServer.AddSendTagged("OK logged out\r\n");
                lServer.AddClose();

                lClient.AuthenticationParameters = lCredsNoPreAuthTryAll;

                lFailed = false;
                try { lClient.Connect(); }
                catch (cUnexpectedPreAuthenticatedConnectionException) { lFailed = true; }

                if (!lFailed) throw new cTestsException($"{nameof(ZTestAuth1)}.10");

                lServer.ThrowAnyErrors();
            }



            //???; // try continuing twice

            //// 5 - mechanisms not advertised but force try on and plain succeeds

            //lServer = new cServer();
            //lServer.AddSendData("* OK [CAPABILITY ENABLE IDLE LITERAL+ IMAP4rev1 XSOMEFEATURE XSOMEOTHERFEATURE] this is the text\r\n");

            //lServer.AddExpectTagged("AUTHENTICATE PLAIN\r\n");
            //lServer.AddSendData("+ \r\n");
            //lServer.AddExpectData("AGZyZWQAYW5ndXM=\r\n");
            //lServer.AddSendData("+ \r\n");
            //lServer.AddSendTagged("OK [CAPABILITY ENABLE IDLE LITERAL+ IMAP4rev1 XSOMEFEATURE XSOMEOTHERFEATURE] why not try anonymous\r\n");

            //lServer.AddExpectTagged("LOGOUT\r\n");
            //lServer.AddSendData("* BYE logging out\r\n");
            //lServer.AddSendTagged("OK logged out\r\n");
            //lServer.AddClose();

            //lClient = new cIMAPClient("ZTestAuth1_5_cIMAPClient");

            //lTask = null;

            //try
            //{
            //    lTask = lServer.RunAsync(lContext);

            //    lClient.Connect("localhost", 143, false, lCredsTrue);
            //    lClient.Disconnect();

            //    if (!lTask.Wait(1000)) throw new cTestsException("session should be complete", lContext);
            //    if (lTask.IsFaulted) throw new cTestsException("server failed", lTask.Exception, lContext);
            //}
            //finally
            //{
            //    ZFinally(lServer, lClient, lTask);
            //}



        }

        private static void ZTestSASLIR(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestSASLIR));

            using (cServer lServer = new cServer(lContext, nameof(ZTestSASLIR)))
            using (cIMAPClient lClient = lServer.NewClient())
            {
                lServer.AddSendData("* OK [CAPABILITY IMAP4rev1 SASL-IR AUTH=PLAIN] this is the text\r\n");
                lServer.AddExpectTagged("AUTHENTICATE PLAIN AGZyZWQAYW5ndXM=\r\n");
                lServer.AddSendTagged("OK [CAPABILITY IMAP4rev1 XSOMEFEATURE XSOMEOTHERFEATURE] logged in\r\n");

                lServer.AddExpectTagged("LIST \"\" \"\"\r\n");
                lServer.AddSendData("* LIST () \"/\" \"\"\r\n");
                lServer.AddSendTagged("OK LIST command completed\r\n");

                lServer.AddExpectTagged("LOGOUT\r\n");
                lServer.AddSendData("* BYE logging out\r\n");
                lServer.AddSendTagged("OK logged out\r\n");
                lServer.AddClose();

                lClient.SetPlainAuthenticationParameters("fred", "angus", eTLSRequirement.indifferent);

                lClient.Connect();
                lClient.Disconnect();

                lServer.ThrowAnyErrors();
            }
        }

        /*
        private static void ZTestLoginReferrals(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestLoginReferrals));

            cServer lServer;
            cIMAPClient lClient;
            Task lTask;

            // 1

            lServer = new cServer();
            lServer.AddSendData("* BYE this is the text\r\n");




            lServer.AddSendData("* OK [CAPABILITY IMAP4rev1 SASL-IR AUTH=PLAIN] this is the text\r\n");
            lServer.AddExpectTagged("AUTHENTICATE PLAIN AGZyZWQAYW5ndXM=\r\n");
            lServer.AddSendTagged("OK [CAPABILITY IMAP4rev1 XSOMEFEATURE XSOMEOTHERFEATURE] logged in\r\n");
            lServer.AddExpectTagged("LOGOUT\r\n");
            lServer.AddSendData("* BYE logging out\r\n");
            lServer.AddSendTagged("OK logged out\r\n");
            lServer.AddClose();

            lClient = new cIMAPClient("ZTestAuth2_1_cIMAPClient");
            lClient.SetServer("localhost");
            lClient.SetPlainCredentials("fred", "angus", eTLSRequirement.indifferent);

            lTask = null;

            try
            {
                lTask = lServer.RunAsync(lContext);

                lClient.Connect();
                lClient.Disconnect();

                if (!lTask.Wait(1000)) throw new cTestsException("session should be complete", lContext);
                if (lTask.IsFaulted) throw new cTestsException("server failed", lTask.Exception, lContext);
            }
            finally
            {
                ZFinally(lServer, lClient, lTask);
            }


        } */

        private static void ZTestSearch1(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestSearch1));

            using (cServer lServer = new cServer(lContext, nameof(ZTestSearch1)))
            using (cIMAPClient lClient = lServer.NewClient())
            {
                lServer.AddSendData("* PREAUTH [CAPABILITY IMAP4rev1] this is the text\r\n");
                lServer.AddExpectTagged("LIST \"\" \"\"\r\n");
                lServer.AddSendData("* LIST () nil \"\"\r\n");
                lServer.AddSendTagged("OK LIST command completed\r\n");

                lServer.AddExpectTagged("SELECT INBOX\r\n");
                lServer.AddSendData("* 172 EXISTS\r\n");
                lServer.AddSendData("* 1 RECENT\r\n");
                lServer.AddSendData("* OK [UNSEEN 12] Message 12 is first unseen\r\n");
                lServer.AddSendData("* OK [UIDVALIDITY 3857529045] UIDs valid\r\n");
                lServer.AddSendData("* OK [UIDNEXT 4392] Predicted next UID\r\n");
                lServer.AddSendData("* FLAGS (\\Answered \\Flagged \\Deleted \\Seen \\Draft)\r\n");
                lServer.AddSendData("* OK [PERMANENTFLAGS (\\Deleted \\Seen \\*)] Limited\r\n");
                lServer.AddSendTagged("OK [READ-WRITE] SELECT completed\r\n");

                lServer.AddExpectTagged("SEARCH UNSEEN\r\n");
                lServer.AddSendData("* OK [UIDVALIDITY 3857529046] clear cache\r\n");
                lServer.AddSendData("* SEARCH 2 84 172\r\n");
                lServer.AddSendTagged("OK SEARCH completed\r\n");

                lServer.AddExpectTagged("SEARCH UNSEEN\r\n");
                lServer.AddSendData("* SEARCH 2 84 172\r\n");
                lServer.AddSendTagged("OK SEARCH completed\r\n");

                lServer.AddExpectTagged("STATUS blurdybloop (MESSAGES UIDNEXT UNSEEN)\r\n");
                lServer.AddSendData("* STATUS blurdybloop (MESSAGES 231 UIDNEXT 44292)\r\n");
                lServer.AddSendTagged("OK STATUS completed\r\n");

                lServer.AddExpectTagged("STATUS blurdybloop (MESSAGES UIDNEXT UNSEEN)\r\n");
                lServer.AddSendData("* STATUS blurdybloop (MESSAGES 232 UNSEEN 3)\r\n");
                lServer.AddSendData("* STATUS blurdybloop (UIDNEXT 44293)\r\n");
                lServer.AddSendTagged("OK STATUS completed\r\n");


                lServer.AddExpectTagged("EXAMINE blurdybloop\r\n");
                lServer.AddSendData("* 17 EXISTS\r\n");
                lServer.AddSendData("* 2 RECENT\r\n");
                lServer.AddSendData("* OK [UNSEEN 8] Message 8 is first unseen\r\n");
                lServer.AddSendData("* OK [UIDVALIDITY 3857529045] UIDs valid\r\n");
                lServer.AddSendData("* OK [UIDNEXT 4392] Predicted next UID\r\n");
                lServer.AddSendData("* FLAGS (\\Answered \\Flagged \\Deleted \\Seen \\Draft)\r\n");
                lServer.AddSendData("* OK [PERMANENTFLAGS ()] No permanent flags permitted\r\n");
                //lServer.AddSendData("* CAPABILITY IMAP4rev1 ESEARCH\r\n");
                lServer.AddSendTagged("OK [READ-ONLY] EXAMINE completed\r\n");


                lServer.AddExpectTagged("SEARCH UNSEEN\r\n");
                lServer.AddSendData("* SEARCH 2 10 11\r\n");
                lServer.AddSendTagged("OK SEARCH completed\r\n");

                lServer.AddExpectTagged("SEARCH SINCE 8-JUN-2017\r\n");
                lServer.AddSendData("* SEARCH 15 16 17\r\n");
                lServer.AddSendTagged("OK SEARCH completed\r\n");

                lServer.AddExpectTagged("SEARCH SINCE 8-JUN-2017\r\n");
                lServer.AddSendData("* SEARCH 15 16 17\r\n");
                lServer.AddSendData("* 16 FETCH (INTERNALDATE \"08-JUN-2017 08:09:16 -1200\")\r\n");
                lServer.AddSendTagged("OK SEARCH completed\r\n");

                lServer.AddExpectTagged("FETCH 15 INTERNALDATE\r\n");
                lServer.AddSendData("* 15 FETCH (INTERNALDATE \"08-JUN-2017 08:09:15 -1200\")\r\n");
                lServer.AddSendTagged("OK FETCH completed\r\n");

                lServer.AddExpectTagged("FETCH 17 INTERNALDATE\r\n");
                lServer.AddSendData("* 17 FETCH (INTERNALDATE \"08-JUN-2017 08:09:17 -1200\")\r\n");
                lServer.AddSendTagged("OK FETCH completed\r\n");

                lServer.AddExpectTagged("SEARCH SINCE 7-JUN-2017\r\n");
                lServer.AddSendData("* SEARCH 15 16 17 14 13 12\r\n");
                lServer.AddSendTagged("OK SEARCH completed\r\n");

                lServer.AddExpectTagged("FETCH 12:14 INTERNALDATE\r\n");
                lServer.AddSendData("* 12 FETCH (INTERNALDATE \"08-JUN-2017 08:09:12 -1200\")\r\n");
                lServer.AddSendData("* 14 FETCH (INTERNALDATE \"08-JUN-2017 08:09:14 -1200\")\r\n");
                lServer.AddSendData("* 13 FETCH (INTERNALDATE \"08-JUN-2017 08:09:14 -1200\")\r\n");
                //lServer.AddSendData("* CAPABILITY IMAP4rev1 SORT\r\n");
                lServer.AddSendTagged("OK FETCH completed\r\n");

                //lServer.AddExpectTagged("SORT (REVERSE ARRIVAL) US-ASCII SINCE 7-JUN-2017\r\n");
                //lServer.AddSendData("* SORT 17 16 15 13 14 12\r\n");
                //lServer.AddSendTagged("OK SORT completed\r\n"); 


                //lServer.AddExpectTagged("SEARCH SINCE 7-JUN-2017\r\n");
                //lServer.AddExpectTagged("SORT (REVERSE ARRIVAL) US-ASCII SINCE 8-JUN-2017\r\n");
                //lServer.AddSendData("* SORT 17 16 15\r\n");
                //lServer.AddSendTagged("OK SORT completed\r\n");
                //lServer.AddSendData("* SEARCH 15 16 17 14 13 12\r\n");
                //lServer.AddSendTagged("OK SEARCH completed\r\n");


                //lServer.AddExpectTagged("SEARCH SINCE 7-JUN-2017\r\n");
                //lServer.AddExpectTagged("SORT (REVERSE ARRIVAL) US-ASCII SINCE 8-JUN-2017\r\n");
                //lServer.AddSendData("* SORT 17 16 15\r\n");
                //lServer.AddSendTagged("OK SORT completed\r\n");
                //lServer.AddSendData("* SEARCH 15 16 17 14 13 12\r\n");
                //lServer.AddSendTagged("OK SEARCH completed\r\n");
                //lServer.AddExpectTagged("SEARCH SINCE 8-JUN-2017\r\n");
                //lServer.AddSendData("* SEARCH 15 16 17\r\n");
                //lServer.AddSendTagged("OK SEARCH completed\r\n"); 



                lServer.AddExpectTagged("LOGOUT\r\n");
                lServer.AddSendData("* BYE logging out\r\n");
                lServer.AddSendTagged("OK logged out\r\n");
                lServer.AddExpectClose();


                lClient.AuthenticationParameters = new cAuthenticationParameters(new object());
                lClient.IdleConfiguration = null;

                cMessageFlags lFlags;
                cMailbox lMailbox;
                List<cMessage> lMessageList;
                cMessage lMessage;
                //Task<List<cMessage>> lTask1;
                //Task<List<cMessage>> lTask2;
                //Task<List<cMessage>> lTask3; 


                lClient.MailboxCacheDataItems = fMailboxCacheDataItems.messagecount | fMailboxCacheDataItems.unseencount | fMailboxCacheDataItems.uidnext;

                lClient.Connect();

                if (lClient.Inbox.IsSelected) throw new cTestsException("ZTestSearch1.1");

                lClient.Inbox.Select(true);

                if (!lClient.Inbox.IsSelected) throw new cTestsException("ZTestSearch1.2");

                if (lClient.Inbox.MessageCount != 172 || lClient.Inbox.RecentCount != 1 || lClient.Inbox.UIDNext != 4392 || lClient.Inbox.UIDValidity != 3857529045 || lClient.Inbox.UnseenCount != 0 || lClient.Inbox.UnseenUnknownCount != 172) throw new cTestsException("ZTestSearch1.3");

                lFlags = lClient.Inbox.MessageFlags;
                if (lFlags.Count != 5 || !lFlags.Contains(kMessageFlag.Answered) || !lFlags.Contains(kMessageFlag.Flagged) || !lFlags.Contains(kMessageFlag.Deleted) || !lFlags.Contains(kMessageFlag.Seen) || !lFlags.Contains(kMessageFlag.Draft)) throw new cTestsException("ZTestSearch1.4");

                lFlags = lClient.Inbox.ForUpdatePermanentFlags;
                if (lFlags.Count != 3 || !lFlags.Contains(kMessageFlag.Deleted) || !lFlags.Contains(kMessageFlag.Seen) || !lFlags.Contains(kMessageFlag.CreateNewIsPossible) || lFlags.Contains(kMessageFlag.Draft) || lFlags.Contains(kMessageFlag.Flagged)) throw new cTestsException("ZTestSearch1.5");

                if (!lClient.Inbox.IsSelectedForUpdate) throw new cTestsException("ZTestSearch1.6");

                bool lFailed = false;
                try { lClient.Inbox.SetUnseenCount(); }
                catch (cUIDValidityException) { lFailed = true; }
                if (!lFailed) throw new cTestsException("ZTestSearch1.7");

                lClient.Inbox.SetUnseenCount();
                if (lClient.Inbox.MessageCount != 172 || lClient.Inbox.RecentCount != 1 || lClient.Inbox.UIDNext != 0 || lClient.Inbox.UIDNextUnknownCount != 172 || lClient.Inbox.UIDValidity != 3857529046 || lClient.Inbox.UnseenCount != 3 || lClient.Inbox.UnseenUnknownCount != 0) throw new cTestsException("ZTestSearch1.8");

                lMailbox = lClient.Mailbox(new cMailboxName("blurdybloop", null));
                if (lMailbox.IsSelected) throw new cTestsException("ZTestSearch2.1");

                if (lMailbox.MessageCount != 231 || lMailbox.UIDNext != 44292) throw new cTestsException("ZTestSearch2.2");

                lMailbox.Refresh(fMailboxCacheDataSets.status);
                if (lMailbox.MessageCount != 232 || lMailbox.UnseenCount != 3 || lMailbox.UIDNext != 44293) throw new cTestsException("ZTestSearch2.3");

                lMailbox.Select();
                if (lClient.Inbox.IsSelected || !lMailbox.IsSelected) throw new cTestsException("ZTestSearch3.1");

                if (lMailbox.MessageCount != 17 || lMailbox.RecentCount != 2 || lMailbox.UIDNext != 4392 || lMailbox.UIDValidity != 3857529045 || lMailbox.UnseenCount != 0 || lMailbox.UnseenUnknownCount != 17) throw new cTestsException("ZTestSearch3.2");

                lFlags = lMailbox.MessageFlags;
                if (lFlags.Count != 5 || !lFlags.Contains(kMessageFlag.Answered) || !lFlags.Contains(kMessageFlag.Flagged) || !lFlags.Contains(kMessageFlag.Deleted) || !lFlags.Contains(kMessageFlag.Seen) || !lFlags.Contains(kMessageFlag.Draft)) throw new cTestsException("ZTestSearch3.3");

                lFlags = lMailbox.ReadOnlyPermanentFlags;
                if (lFlags.Count != 0) throw new cTestsException("ZTestSearch3.4");

                if (lMailbox.IsSelectedForUpdate) throw new cTestsException("ZTestSearch3.5");

                lMailbox.SetUnseenCount();
                if (lMailbox.UnseenCount != 3 || lMailbox.UnseenUnknownCount != 0) throw new cTestsException("ZTestSearch3.7");

                lMessageList = lMailbox.Messages(cFilter.Received >= new DateTime(2017, 6, 8));
                if (lMessageList.Count != 3) throw new cTestsException("ZTestSearch4.1");

                lMessageList = lMailbox.Messages(cFilter.Received >= new DateTime(2017, 6, 8), null, fMessageCacheAttributes.received);
                if (lMessageList.Count != 3) throw new cTestsException("ZTestSearch4.2");

                lMessage = lMessageList[0];

                if (lMessage.Expunged || lMessage.MessageHandle.Attributes != (fMessageCacheAttributes.received | fMessageCacheAttributes.modseq)) throw new cTestsException("ZTestSearch4.4");
                if (lMessage.ReceivedDateTime.ToUniversalTime() != new DateTime(2017, 6, 8, 20, 09, 15, DateTimeKind.Utc)) throw new cTestsException("ZTestSearch4.5");




                lMessageList = lMailbox.Messages(cFilter.Received >= new DateTime(2017, 6, 7), new cSort(cSortItem.ReceivedDesc));

                if (lMessageList.Count != 6) throw new cTestsException("ZTestSearch5.1");
                if (lMessageList[0].MessageHandle.CacheSequence != 16 || lMessageList[1].MessageHandle.CacheSequence != 15 || lMessageList[2].MessageHandle.CacheSequence != 14 ||
                    lMessageList[3].MessageHandle.CacheSequence != 12 || lMessageList[4].MessageHandle.CacheSequence != 13 || lMessageList[5].MessageHandle.CacheSequence != 11) throw new cTestsException("ZTestSearch5.2");

                //lMessageList = lMailbox.Messages(cFilter.Received >= new DateTime(2017, 6, 7), new cSort(cSortItem.ReceivedDesc));

                //if (lMessageList.Count != 6) throw new cTestsException("ZTestSearch6.1");
                //if (lMessageList[0].Handle.CacheSequence != 16 || lMessageList[1].Handle.CacheSequence != 15 || lMessageList[2].Handle.CacheSequence != 14 ||
                //    lMessageList[3].Handle.CacheSequence != 12 || lMessageList[4].Handle.CacheSequence != 13 || lMessageList[5].Handle.CacheSequence != 11) throw new cTestsException("ZTestSearch6.2");


                //lTask1 = lMailbox.MessagesAsync(cFilter.Received >= new DateTime(2017, 6, 7));
                //lTask2 = lMailbox.MessagesAsync(cFilter.Received >= new DateTime(2017, 6, 8), new cSort(cSortItem.ReceivedDesc));

                //Task.WaitAll(lTask1, lTask2);

                //lMessageList = lTask1.Result;
                //if (lMessageList.Count != 6) throw new cTestsException("ZTestSearch7.1");

                //lMessageList = lTask2.Result;
                //if (lMessageList.Count != 3) throw new cTestsException("ZTestSearch7.2"); 


                //// this checks that the search commands lock one another out ...

                //lTask1 = lMailbox.MessagesAsync(cFilter.Received >= new DateTime(2017, 6, 7));
                //lTask2 = lMailbox.MessagesAsync(cFilter.Received >= new DateTime(2017, 6, 8));
                //lTask3 = lMailbox.MessagesAsync(cFilter.Received >= new DateTime(2017, 6, 8), new cSort(cSortItem.ReceivedDesc));

                //Task.WaitAll(lTask1, lTask2, lTask3); 






                lClient.Disconnect();

                lServer.ThrowAnyErrors();
            }
        }

        private static void ZTestSearch2(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestSearch2));

            using (cServer lServer = new cServer(lContext, nameof(ZTestSearch2)))
            using (cIMAPClient lClient = lServer.NewClient())
            {
                lServer.AddSendData("* PREAUTH [CAPABILITY IMAP4rev1 ESEARCH] this is the text\r\n");
                lServer.AddExpectTagged("LIST \"\" \"\"\r\n");
                lServer.AddSendData("* LIST () nil \"\"\r\n");
                lServer.AddSendTagged("OK LIST command completed\r\n");

                lServer.AddExpectTagged("SELECT INBOX\r\n");
                lServer.AddSendData("* 172 EXISTS\r\n");
                lServer.AddSendData("* 1 RECENT\r\n");
                lServer.AddSendData("* OK [UNSEEN 12] Message 12 is first unseen\r\n");
                lServer.AddSendData("* OK [UIDVALIDITY 3857529045] UIDs valid\r\n");
                lServer.AddSendData("* OK [UIDNEXT 4392] Predicted next UID\r\n");
                lServer.AddSendData("* FLAGS (\\Answered \\Flagged \\Deleted \\Seen \\Draft)\r\n");
                lServer.AddSendData("* OK [PERMANENTFLAGS (\\Deleted \\Seen \\*)] Limited\r\n");
                lServer.AddSendTagged("OK [READ-WRITE] SELECT completed\r\n");

                lServer.AddExpectTagged("SEARCH RETURN () UNSEEN\r\n");
                lServer.AddSendData("* ESEARCH (TAG \"\t\") ALL 2,10:11\r\n");
                lServer.AddSendData("* OK [UIDVALIDITY 3857529046] clear cache\r\n");
                lServer.AddSendTagged("OK SEARCH completed\r\n");

                lServer.AddExpectTagged("SEARCH RETURN () UNSEEN\r\n");
                lServer.AddSendData("* ESEARCH (TAG \"\t\") ALL 2,10:11\r\n");
                lServer.AddSendTagged("OK SEARCH completed\r\n");

                lServer.AddExpectTagged("STATUS blurdybloop (MESSAGES UIDNEXT UNSEEN)\r\n");
                lServer.AddSendData("* STATUS blurdybloop (MESSAGES 231 UIDNEXT 44292)\r\n");
                lServer.AddSendTagged("OK STATUS completed\r\n");

                lServer.AddExpectTagged("STATUS blurdybloop (MESSAGES UIDNEXT UNSEEN)\r\n");
                lServer.AddSendData("* STATUS blurdybloop (MESSAGES 232 UNSEEN 3)\r\n");
                lServer.AddSendData("* STATUS blurdybloop (UIDNEXT 44293)\r\n");
                lServer.AddSendTagged("OK STATUS completed\r\n");


                lServer.AddExpectTagged("EXAMINE blurdybloop\r\n");
                lServer.AddSendData("* 17 EXISTS\r\n");
                lServer.AddSendData("* 2 RECENT\r\n");
                lServer.AddSendData("* OK [UNSEEN 8] Message 8 is first unseen\r\n");
                lServer.AddSendData("* OK [UIDVALIDITY 3857529045] UIDs valid\r\n");
                lServer.AddSendData("* OK [UIDNEXT 4392] Predicted next UID\r\n");
                lServer.AddSendData("* FLAGS (\\Answered \\Flagged \\Deleted \\Seen \\Draft)\r\n");
                lServer.AddSendData("* OK [PERMANENTFLAGS ()] No permanent flags permitted\r\n");
                //lServer.AddSendData("* CAPABILITY IMAP4rev1 ESEARCH\r\n");
                lServer.AddSendTagged("OK [READ-ONLY] EXAMINE completed\r\n");


                lServer.AddExpectTagged("SEARCH RETURN () UNSEEN\r\n");
                lServer.AddSendData("* ESEARCH (TAG \"\t\") ALL 2,10:11\r\n");
                lServer.AddSendTagged("OK SEARCH completed\r\n");

                lServer.AddExpectTagged("SEARCH RETURN () SINCE 8-JUN-2017\r\n");
                lServer.AddSendData("* ESEARCH (TAG \"\t\") ALL 15:17\r\n");
                //lServer.AddSendData("* CAPABILITY IMAP4rev1 ESEARCH\r\n");
                lServer.AddSendTagged("OK SEARCH completed\r\n");

                lServer.AddExpectTagged("SEARCH RETURN () SINCE 8-JUN-2017\r\n");
                lServer.AddSendData("* ESEARCH (TAG \"\t\") ALL 15:17\r\n");
                lServer.AddSendData("* 16 FETCH (INTERNALDATE \"08-JUN-2017 08:09:16 -1200\")\r\n");
                lServer.AddSendTagged("OK SEARCH completed\r\n");

                lServer.AddExpectTagged("FETCH 15 INTERNALDATE\r\n");
                lServer.AddSendData("* 15 FETCH (INTERNALDATE \"08-JUN-2017 08:09:15 -1200\")\r\n");
                lServer.AddSendTagged("OK FETCH completed\r\n");

                lServer.AddExpectTagged("FETCH 17 INTERNALDATE\r\n");
                lServer.AddSendData("* 17 FETCH (INTERNALDATE \"08-JUN-2017 08:09:17 -1200\")\r\n");
                //lServer.AddSendData("* CAPABILITY IMAP4rev1\r\n");
                lServer.AddSendTagged("OK FETCH completed\r\n");

                lServer.AddExpectTagged("SEARCH RETURN () SINCE 7-JUN-2017\r\n");
                lServer.AddSendData("* ESEARCH (TAG \"\t\") ALL 12:17\r\n");
                lServer.AddSendTagged("OK SEARCH completed\r\n");

                lServer.AddExpectTagged("FETCH 12:14 INTERNALDATE\r\n");
                lServer.AddSendData("* 12 FETCH (INTERNALDATE \"08-JUN-2017 08:09:12 -1200\")\r\n");
                lServer.AddSendData("* 14 FETCH (INTERNALDATE \"08-JUN-2017 08:09:14 -1200\")\r\n");
                lServer.AddSendData("* 13 FETCH (INTERNALDATE \"08-JUN-2017 08:09:14 -1200\")\r\n");
                //lServer.AddSendData("* CAPABILITY IMAP4rev1 SORT\r\n");
                lServer.AddSendTagged("OK FETCH completed\r\n");

                //lServer.AddExpectTagged("SORT (REVERSE ARRIVAL) US-ASCII SINCE 7-JUN-2017\r\n");
                //lServer.AddSendData("* SORT 17 16 15 13 14 12\r\n");
                //lServer.AddSendTagged("OK SORT completed\r\n"); 


                //lServer.AddExpectTagged("SEARCH SINCE 7-JUN-2017\r\n");
                //lServer.AddExpectTagged("SORT (REVERSE ARRIVAL) US-ASCII SINCE 8-JUN-2017\r\n");
                //lServer.AddSendData("* SORT 17 16 15\r\n");
                //lServer.AddSendTagged("OK SORT completed\r\n");
                //lServer.AddSendData("* SEARCH 15 16 17 14 13 12\r\n");
                //lServer.AddSendTagged("OK SEARCH completed\r\n");


                //lServer.AddExpectTagged("SEARCH SINCE 7-JUN-2017\r\n");
                //lServer.AddExpectTagged("SORT (REVERSE ARRIVAL) US-ASCII SINCE 8-JUN-2017\r\n");
                //lServer.AddSendData("* SORT 17 16 15\r\n");
                //lServer.AddSendTagged("OK SORT completed\r\n");
                //lServer.AddSendData("* SEARCH 15 16 17 14 13 12\r\n");
                //lServer.AddSendTagged("OK SEARCH completed\r\n");
                //lServer.AddExpectTagged("SEARCH SINCE 8-JUN-2017\r\n");
                //lServer.AddSendData("* SEARCH 15 16 17\r\n");
                //lServer.AddSendTagged("OK SEARCH completed\r\n");



                lServer.AddExpectTagged("LOGOUT\r\n");
                lServer.AddSendData("* BYE logging out\r\n");
                lServer.AddSendTagged("OK logged out\r\n");
                lServer.AddExpectClose();


                lClient.AuthenticationParameters = new cAuthenticationParameters(new object());
                lClient.IdleConfiguration = null;

                cMessageFlags lFlags;
                cMailbox lMailbox;
                List<cMessage> lMessageList;
                cMessage lMessage;
                //Task<List<cMessage>> lTask1;
                //Task<List<cMessage>> lTask2;
                //Task<List<cMessage>> lTask3; 


                lClient.MailboxCacheDataItems = fMailboxCacheDataItems.messagecount | fMailboxCacheDataItems.unseencount | fMailboxCacheDataItems.uidnext;

                lClient.Connect();

                if (lClient.Inbox.IsSelected) throw new cTestsException("ZTestSearch2_1.1");

                lClient.Inbox.Select(true);

                if (!lClient.Inbox.IsSelected) throw new cTestsException("ZTestSearch2_1.2");

                if (lClient.Inbox.MessageCount != 172 || lClient.Inbox.RecentCount != 1 || lClient.Inbox.UIDNext != 4392 || lClient.Inbox.UIDValidity != 3857529045 || lClient.Inbox.UnseenCount != 0 || lClient.Inbox.UnseenUnknownCount != 172) throw new cTestsException("ZTestSearch1.3");

                lFlags = lClient.Inbox.MessageFlags;
                if (lFlags.Count != 5 || !lFlags.Contains(kMessageFlag.Answered) || !lFlags.Contains(kMessageFlag.Flagged) || !lFlags.Contains(kMessageFlag.Deleted) || !lFlags.Contains(kMessageFlag.Seen) || !lFlags.Contains(kMessageFlag.Draft)) throw new cTestsException("ZTestSearch2_1.4");

                lFlags = lClient.Inbox.ForUpdatePermanentFlags;
                if (lFlags.Count != 3 || !lFlags.Contains(kMessageFlag.Deleted) || !lFlags.Contains(kMessageFlag.Seen) || !lFlags.Contains(kMessageFlag.CreateNewIsPossible) || lFlags.Contains(kMessageFlag.Draft) || lFlags.Contains(kMessageFlag.Flagged)) throw new cTestsException("ZTestSearch2_1.5");

                if (!lClient.Inbox.IsSelectedForUpdate) throw new cTestsException("ZTestSearch2_1.6");

                bool lFailed = false;
                try { lClient.Inbox.SetUnseenCount(); }
                catch (cUIDValidityException) { lFailed = true; }
                if (!lFailed) throw new cTestsException("ZTestSearch2_1.7");

                lClient.Inbox.SetUnseenCount();
                if (lClient.Inbox.MessageCount != 172 || lClient.Inbox.RecentCount != 1 || lClient.Inbox.UIDNext != 0 || lClient.Inbox.UIDNextUnknownCount != 172 || lClient.Inbox.UIDValidity != 3857529046 || lClient.Inbox.UnseenCount != 3 || lClient.Inbox.UnseenUnknownCount != 0) throw new cTestsException("ZTestSearch2_1.8");

                lMailbox = lClient.Mailbox(new cMailboxName("blurdybloop", null));
                if (lMailbox.IsSelected) throw new cTestsException("ZTestSearch2_2.1");

                if (lMailbox.MessageCount != 231 || lMailbox.UIDNext != 44292) throw new cTestsException("ZTestSearch2_2.2");

                lMailbox.Refresh(fMailboxCacheDataSets.status);
                if (lMailbox.MessageCount != 232 || lMailbox.UnseenCount != 3 || lMailbox.UIDNext != 44293) throw new cTestsException("ZTestSearch2_2.3");

                lMailbox.Select();
                if (lClient.Inbox.IsSelected || !lMailbox.IsSelected) throw new cTestsException("ZTestSearch2_3.1");

                if (lMailbox.MessageCount != 17 || lMailbox.RecentCount != 2 || lMailbox.UIDNext != 4392 || lMailbox.UIDValidity != 3857529045 || lMailbox.UnseenCount != 0 || lMailbox.UnseenUnknownCount != 17) throw new cTestsException("ZTestSearch2_3.2");

                lFlags = lMailbox.MessageFlags;
                if (lFlags.Count != 5 || !lFlags.Contains(kMessageFlag.Answered) || !lFlags.Contains(kMessageFlag.Flagged) || !lFlags.Contains(kMessageFlag.Deleted) || !lFlags.Contains(kMessageFlag.Seen) || !lFlags.Contains(kMessageFlag.Draft)) throw new cTestsException("ZTestSearch2_3.3");

                lFlags = lMailbox.ReadOnlyPermanentFlags;
                if (lFlags.Count != 0) throw new cTestsException("ZTestSearch2_3.4");

                if (lMailbox.IsSelectedForUpdate) throw new cTestsException("ZTestSearch2_3.5");

                lMailbox.SetUnseenCount();
                if (lMailbox.UnseenCount != 3 || lMailbox.UnseenUnknownCount != 0) throw new cTestsException("ZTestSearch2_3.7");

                lMessageList = lMailbox.Messages(cFilter.Received >= new DateTime(2017, 6, 8));
                if (lMessageList.Count != 3) throw new cTestsException("ZTestSearch2_4.1");

                lMessageList = lMailbox.Messages(cFilter.Received >= new DateTime(2017, 6, 8), null, fMessageCacheAttributes.received);
                if (lMessageList.Count != 3) throw new cTestsException("ZTestSearch2_4.2");

                lMessage = lMessageList[0];

                if (lMessage.Expunged || lMessage.MessageHandle.Attributes != (fMessageCacheAttributes.received | fMessageCacheAttributes.modseq)) throw new cTestsException("ZTestSearch2_4.4");
                if (lMessage.ReceivedDateTime.ToUniversalTime() != new DateTime(2017, 6, 8, 20, 09, 15, DateTimeKind.Utc)) throw new cTestsException("ZTestSearch2_4.5");




                lMessageList = lMailbox.Messages(cFilter.Received >= new DateTime(2017, 6, 7), new cSort(cSortItem.ReceivedDesc));

                if (lMessageList.Count != 6) throw new cTestsException("ZTestSearch2_5.1");
                if (lMessageList[0].MessageHandle.CacheSequence != 16 || lMessageList[1].MessageHandle.CacheSequence != 15 || lMessageList[2].MessageHandle.CacheSequence != 14 ||
                    lMessageList[3].MessageHandle.CacheSequence != 12 || lMessageList[4].MessageHandle.CacheSequence != 13 || lMessageList[5].MessageHandle.CacheSequence != 11) throw new cTestsException("ZTestSearch2_5.2");

                //lMessageList = lMailbox.Messages(cFilter.Received >= new DateTime(2017, 6, 7), new cSort(cSortItem.ReceivedDesc));

                //if (lMessageList.Count != 6) throw new cTestsException("ZTestSearch6.1");
                //if (lMessageList[0].Handle.CacheSequence != 16 || lMessageList[1].Handle.CacheSequence != 15 || lMessageList[2].Handle.CacheSequence != 14 ||
                //    lMessageList[3].Handle.CacheSequence != 12 || lMessageList[4].Handle.CacheSequence != 13 || lMessageList[5].Handle.CacheSequence != 11) throw new cTestsException("ZTestSearch6.2");


                //lTask1 = lMailbox.MessagesAsync(cFilter.Received >= new DateTime(2017, 6, 7));
                //lTask2 = lMailbox.MessagesAsync(cFilter.Received >= new DateTime(2017, 6, 8), new cSort(cSortItem.ReceivedDesc));

                //Task.WaitAll(lTask1, lTask2);

                //lMessageList = lTask1.Result;
                //if (lMessageList.Count != 6) throw new cTestsException("ZTestSearch7.1");

                //lMessageList = lTask2.Result;
                //if (lMessageList.Count != 3) throw new cTestsException("ZTestSearch7.2");


                //// this checks that the search commands lock one another out ...

                //lTask1 = lMailbox.MessagesAsync(cFilter.Received >= new DateTime(2017, 6, 7));
                //lTask2 = lMailbox.MessagesAsync(cFilter.Received >= new DateTime(2017, 6, 8));
                //lTask3 = lMailbox.MessagesAsync(cFilter.Received >= new DateTime(2017, 6, 8), new cSort(cSortItem.ReceivedDesc));

                //Task.WaitAll(lTask1, lTask2, lTask3);






                lClient.Disconnect();

                lServer.ThrowAnyErrors();
            }
        }

        private static void ZTestSearch3(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestSearch3));

            using (cServer lServer = new cServer(lContext, nameof(ZTestSearch3)))
            using (cIMAPClient lClient = lServer.NewClient())
            {
                lServer.AddSendData("* PREAUTH [CAPABILITY IMAP4rev1 SORT] this is the text\r\n");
                lServer.AddExpectTagged("LIST \"\" \"\"\r\n");
                lServer.AddSendData("* LIST () nil \"\"\r\n");
                lServer.AddSendData("* STATUS blurdybloop (MESSAGES 231 UNSEEN 0)\r\n");
                lServer.AddSendData("* STATUS blurdybloop (UIDNEXT 44292)\r\n");
                lServer.AddSendTagged("OK LIST command completed\r\n");

                lServer.AddExpectTagged("STATUS blurdybloop (MESSAGES UIDNEXT UNSEEN)\r\n");
                lServer.AddSendData("* STATUS blurdybloop (MESSAGES 232 UNSEEN 3)\r\n");
                lServer.AddSendData("* STATUS blurdybloop (UIDNEXT 44293)\r\n");
                lServer.AddSendTagged("OK STATUS completed\r\n");


                lServer.AddExpectTagged("EXAMINE blurdybloop\r\n");
                lServer.AddSendData("* 17 EXISTS\r\n");
                lServer.AddSendData("* 2 RECENT\r\n");
                lServer.AddSendData("* OK [UNSEEN 8] Message 8 is first unseen\r\n");
                lServer.AddSendData("* OK [UIDVALIDITY 3857529045] UIDs valid\r\n");
                lServer.AddSendData("* OK [UIDNEXT 4392] Predicted next UID\r\n");
                lServer.AddSendData("* FLAGS (\\Answered \\Flagged \\Deleted \\Seen \\Draft)\r\n");
                lServer.AddSendData("* OK [PERMANENTFLAGS ()] No permanent flags permitted\r\n");
                lServer.AddSendTagged("OK [READ-ONLY] EXAMINE completed\r\n");

                lServer.AddExpectTagged("SORT (REVERSE ARRIVAL) US-ASCII SINCE 7-JUN-2017\r\n");
                lServer.AddSendData("* SORT 17 16 15 13 14 12\r\n");
                lServer.AddSendTagged("OK SORT completed\r\n");


                lServer.AddExpectTagged("SEARCH SINCE 7-JUN-2017\r\n");
                lServer.AddExpectTagged("SORT (REVERSE ARRIVAL) US-ASCII SINCE 8-JUN-2017\r\n");
                lServer.AddSendData("* SORT 17 16 15\r\n");
                lServer.AddSendTagged("OK SORT completed\r\n");
                lServer.AddSendData("* SEARCH 15 16 17 14 13 12\r\n");
                lServer.AddSendTagged("OK SEARCH completed\r\n");


                lServer.AddExpectTagged("SEARCH SINCE 7-JUN-2017\r\n");
                lServer.AddExpectTagged("SORT (REVERSE ARRIVAL) US-ASCII SINCE 8-JUN-2017\r\n");
                lServer.AddSendData("* SORT 17 16 15\r\n");
                lServer.AddSendTagged("OK SORT completed\r\n");
                lServer.AddSendData("* SEARCH 15 16 17 14 13 12\r\n");
                lServer.AddSendTagged("OK SEARCH completed\r\n");
                lServer.AddExpectTagged("SEARCH SINCE 8-JUN-2017\r\n");
                lServer.AddSendData("* SEARCH 15 16 17\r\n");
                lServer.AddSendTagged("OK SEARCH completed\r\n");



                lServer.AddExpectTagged("LOGOUT\r\n");
                lServer.AddSendData("* BYE logging out\r\n");
                lServer.AddSendTagged("OK logged out\r\n");
                lServer.AddExpectClose();


                lClient.AuthenticationParameters = new cAuthenticationParameters(new object());
                lClient.IdleConfiguration = null;

                cMessageFlags lFlags;
                cMailbox lMailbox;
                List<cMessage> lMessageList;
                Task<List<cMessage>> lTask1;
                Task<List<cMessage>> lTask2;
                Task<List<cMessage>> lTask3;


                lClient.MailboxCacheDataItems = fMailboxCacheDataItems.messagecount | fMailboxCacheDataItems.unseencount | fMailboxCacheDataItems.uidnext;

                lClient.Connect();

                lMailbox = lClient.Mailbox(new cMailboxName("blurdybloop", null));
                if (lMailbox.IsSelected) throw new cTestsException("ZTestSearch3_2.1");

                if (lMailbox.MessageCount != 231 || lMailbox.UIDNext != 44292) throw new cTestsException("ZTestSearch3_2.2");

                lMailbox.Refresh(fMailboxCacheDataSets.status);
                if (lMailbox.MessageCount != 232 || lMailbox.UnseenCount != 3 || lMailbox.UIDNext != 44293 || lMailbox.UIDNotSticky != null) throw new cTestsException("ZTestSearch3_2.3");

                lMailbox.Select();
                if (lClient.Inbox.IsSelected || !lMailbox.IsSelected || lMailbox.UIDNotSticky != false) throw new cTestsException("ZTestSearch3_3.1");

                if (lMailbox.MessageCount != 17 || lMailbox.RecentCount != 2 || lMailbox.UIDNext != 4392 || lMailbox.UIDValidity != 3857529045 || lMailbox.UnseenCount != 0 || lMailbox.UnseenUnknownCount != 17) throw new cTestsException("ZTestSearch3_3.2");

                lFlags = lMailbox.MessageFlags;
                if (lFlags.Count != 5 || !lFlags.Contains(kMessageFlag.Answered) || !lFlags.Contains(kMessageFlag.Flagged) || !lFlags.Contains(kMessageFlag.Deleted) || !lFlags.Contains(kMessageFlag.Seen) || !lFlags.Contains(kMessageFlag.Draft)) throw new cTestsException("ZTestSearch3_3.3");

                lFlags = lMailbox.ReadOnlyPermanentFlags;
                if (lFlags.Count != 0) throw new cTestsException("ZTestSearch3_3.4");

                if (lMailbox.IsSelectedForUpdate) throw new cTestsException("ZTestSearch3_3.5");

                lMessageList = lMailbox.Messages(cFilter.Received >= new DateTime(2017, 6, 7), new cSort(cSortItem.ReceivedDesc));

                if (lMessageList.Count != 6) throw new cTestsException("ZTestSearch3_6.1");
                if (lMessageList[0].MessageHandle.CacheSequence != 16 || lMessageList[1].MessageHandle.CacheSequence != 15 || lMessageList[2].MessageHandle.CacheSequence != 14 ||
                    lMessageList[3].MessageHandle.CacheSequence != 12 || lMessageList[4].MessageHandle.CacheSequence != 13 || lMessageList[5].MessageHandle.CacheSequence != 11) throw new cTestsException("ZTestSearch3_6.2");


                lTask1 = lMailbox.MessagesAsync(cFilter.Received >= new DateTime(2017, 6, 7));
                lTask2 = lMailbox.MessagesAsync(cFilter.Received >= new DateTime(2017, 6, 8), new cSort(cSortItem.ReceivedDesc));

                Task.WaitAll(lTask1, lTask2);

                lMessageList = lTask1.Result;
                if (lMessageList.Count != 6) throw new cTestsException("ZTestSearch3_7.1");

                lMessageList = lTask2.Result;
                if (lMessageList.Count != 3) throw new cTestsException("ZTestSearch3_7.2");


                // this checks that the search commands lock one another out ...

                lTask1 = lMailbox.MessagesAsync(cFilter.Received >= new DateTime(2017, 6, 7));
                lTask2 = lMailbox.MessagesAsync(cFilter.Received >= new DateTime(2017, 6, 8));
                lTask3 = lMailbox.MessagesAsync(cFilter.Received >= new DateTime(2017, 6, 8), new cSort(cSortItem.ReceivedDesc));

                Task.WaitAll(lTask1, lTask2, lTask3);






                lClient.Disconnect();

                lServer.ThrowAnyErrors();
            }
        }

        private static void ZTestBadCharsetUIDNotSticky(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestBadCharsetUIDNotSticky));

            using (cServer lServer = new cServer(lContext, nameof(ZTestBadCharsetUIDNotSticky)))
            using (cIMAPClient lClient = lServer.NewClient())
            {
                lServer.AddSendData("* PREAUTH [CAPABILITY IMAP4rev1 LITERAL+] this is the text\r\n");
                lServer.AddExpectTagged("LIST \"\" \"\"\r\n");
                lServer.AddSendData("* LIST () nil \"\"\r\n");
                lServer.AddSendTagged("OK LIST command completed\r\n");
                lServer.AddExpectTagged("EXAMINE INBOX\r\n");
                lServer.AddSendData("* 17 EXISTS\r\n");
                lServer.AddSendData("* 2 RECENT\r\n");
                lServer.AddSendData("* OK [UNSEEN 8] Message 8 is first unseen\r\n");
                lServer.AddSendData("* OK [UIDVALIDITY 3857529045] UIDs valid\r\n");
                lServer.AddSendData("* OK [UIDNEXT 4392] Predicted next UID\r\n");
                lServer.AddSendData("* FLAGS (\\Answered \\Flagged \\Deleted \\Seen \\Draft)\r\n");
                lServer.AddSendData("* OK [PERMANENTFLAGS ()] No permanent flags permitted\r\n");
                lServer.AddSendData("* NO [UIDNOTSTICKY] Non-persistent UIDs\r\n");
                lServer.AddSendTagged("OK [READ-ONLY] EXAMINE completed\r\n");





                lServer.AddExpectTagged(Encoding.UTF8.GetBytes("SEARCH CHARSET utf-8 BODY {6+}\r\nfr€d\r\n"));
                lServer.AddSendTagged("NO [BADCHARSET] invalid charset\r\n");

                lServer.AddExpectTagged(Encoding.UTF8.GetBytes("SEARCH CHARSET utf-8 BODY {6+}\r\nfr€d\r\n"));
                lServer.AddSendTagged("NO [BADCHARSET (x1 x2 \"a nother 1\")] invalid charset, use one of the ones I support\r\n");


                lServer.AddExpectTagged("LOGOUT\r\n");
                lServer.AddSendData("* BYE logging out\r\n");
                lServer.AddSendTagged("OK logged out\r\n");
                lServer.AddExpectClose();


                lClient.AuthenticationParameters = new cAuthenticationParameters(new object());
                lClient.IdleConfiguration = null;

                lClient.Connect();
                lClient.Inbox.Select();

                if (lClient.Inbox.UIDNotSticky != true) throw new cTestsException("ZTestBadCharsetUIDNotSticky.2");

                try
                {
                    var lMessageList = lClient.Inbox.Messages(cFilter.Body.Contains("fr€d"));
                }
                catch (cUnsuccessfulCompletionException e)
                {
                    if (e.ResponseText.Code != eResponseTextCode.badcharset || e.ResponseText.Arguments != null) throw new cTestsException("ZTestBadCharsetUIDNotSticky.3");
                }

                try
                {
                    var lMessageList = lClient.Inbox.Messages(cFilter.Body.Contains("fr€d"));
                }
                catch (cUnsuccessfulCompletionException e)
                {
                    if (e.ResponseText.Code != eResponseTextCode.badcharset || e.ResponseText.Arguments == null || e.ResponseText.Arguments.Count != 3) throw new cTestsException("ZTestBadCharsetUIDNotSticky.4");
                    if (e.ResponseText.Arguments[0] != "x1" || e.ResponseText.Arguments[2] != "a nother 1") throw new cTestsException("ZTestBadCharsetUIDNotSticky.5");
                }




                lClient.Disconnect();

                lServer.ThrowAnyErrors();
            }
        }

        private static void ZTestIdleRestart(cTrace.cContext pParentContext)
        {
            // test that idle stops on a fetch and restarts after the fetch
            //  and that UID fetch is used when appropriate
            //  and that expunge decreases the message numbers appropriately

            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestIdleRestart));

            using (cServer lServer = new cServer(lContext, nameof(ZTestIdleRestart)))
            using (cIMAPClient lClient = lServer.NewClient())
            {
                lServer.AddSendData("* PREAUTH [CAPABILITY IMAP4rev1 IDLE] this is the text\r\n");
                lServer.AddExpectTagged("LIST \"\" \"\"\r\n");
                lServer.AddSendData("* LIST () nil \"\"\r\n");
                lServer.AddSendTagged("OK LIST command completed\r\n");

                lServer.AddExpectTagged("SELECT INBOX\r\n");
                lServer.AddSendData("* 172 EXISTS\r\n");
                lServer.AddSendData("* 1 RECENT\r\n");
                lServer.AddSendData("* OK [UNSEEN 12] Message 12 is first unseen\r\n");
                lServer.AddSendData("* OK [UIDVALIDITY 3857529045] UIDs valid\r\n");
                lServer.AddSendData("* OK [UIDNEXT 4392] Predicted next UID\r\n");
                lServer.AddSendData("* FLAGS (\\Answered \\Flagged \\Deleted \\Seen \\Draft)\r\n");
                lServer.AddSendData("* OK [PERMANENTFLAGS (\\Deleted \\Seen \\*)] Limited\r\n");
                lServer.AddSendTagged("OK [READ-WRITE] SELECT completed\r\n");

                lServer.AddExpectTagged("SEARCH UNSEEN\r\n");
                lServer.AddSendData("* SEARCH 167 168 170\r\n");
                lServer.AddSendTagged("OK SEARCH completed\r\n");

                lServer.AddExpectTagged("IDLE\r\n");
                lServer.AddSendData("+ idling\r\n");
                lServer.AddSendData("* 168 EXPUNGE\r\n");
                lServer.AddSendData("* 167 FETCH (UID 10167)\r\n");
                lServer.AddExpectData("DONE\r\n");
                lServer.AddSendTagged("OK idle terminated\r\n");

                lServer.AddExpectTagged("IDLE\r\n");
                lServer.AddSendData("+ idling\r\n");
                lServer.AddExpectData("DONE\r\n");
                lServer.AddSendTagged("OK idle terminated\r\n");

                lServer.AddExpectTagged("FETCH 169 INTERNALDATE\r\n");
                lServer.AddSendData("* 169 FETCH (INTERNALDATE \"08-JUN-2017 01:06:09 -1200\")\r\n");
                lServer.AddSendTagged("OK FETCH completed\r\n");

                lServer.AddExpectTagged("UID FETCH 10167 INTERNALDATE\r\n");
                lServer.AddSendData("* 167 FETCH (INTERNALDATE \"08-JUN-2017 01:06:07 -1200\")\r\n");
                lServer.AddSendTagged("OK UID FETCH completed\r\n");

                lServer.AddExpectTagged("IDLE\r\n");
                lServer.AddSendData("+ idling\r\n");
                lServer.AddExpectData("DONE\r\n");
                lServer.AddSendTagged("OK idle terminated\r\n");

                lServer.AddExpectTagged("FETCH 167,169 FLAGS\r\n");
                lServer.AddSendData("* 169 FETCH (FLAGS ())\r\n");
                lServer.AddSendData("* 167 FETCH (FLAGS ())\r\n");
                lServer.AddSendTagged("OK FETCH completed\r\n");


                lServer.AddExpectTagged("EXAMINE blurdybloop\r\n");
                lServer.AddSendData("* 17 EXISTS\r\n");
                lServer.AddSendData("* 2 RECENT\r\n");
                lServer.AddSendData("* OK [UNSEEN 8] Message 8 is first unseen\r\n");
                lServer.AddSendData("* OK [UIDVALIDITY 3857529045] UIDs valid\r\n");
                lServer.AddSendData("* OK [UIDNEXT 4392] Predicted next UID\r\n");
                lServer.AddSendData("* FLAGS (\\Answered \\Flagged \\Deleted \\Seen \\Draft)\r\n");
                lServer.AddSendData("* OK [PERMANENTFLAGS ()] No permanent flags permitted\r\n");
                //lServer.AddSendData("* CAPABILITY IMAP4rev1 ESEARCH\r\n");
                lServer.AddSendTagged("OK [READ-ONLY] EXAMINE completed\r\n");

                lServer.AddExpectTagged("SEARCH UID 4392:4294967295\r\n");
                lServer.AddSendData("* SEARCH\r\n");
                lServer.AddSendTagged("OK SEARCH completed\r\n");



                lServer.AddExpectTagged("LOGOUT\r\n");
                lServer.AddSendData("* BYE logging out\r\n");
                lServer.AddSendTagged("OK logged out\r\n");
                lServer.AddExpectClose();




                lClient.AuthenticationParameters = new cAuthenticationParameters(new object());

                lClient.Connect();

                lClient.Inbox.Select(true);

                if (lClient.Inbox.MessageCount != 172) throw new cTestsException("ZTestIdleRestart1.1");

                var lMessages = lClient.Inbox.Messages(!cFilter.Seen);

                Thread.Sleep(3000); // idle should start, message 168 should get deleted, and message 167 should get a UID during this wait

                if (lClient.Inbox.MessageCount != 171) throw new cTestsException("ZTestIdleRestart1.2");


                var lLM = new List<cMessage>();
                lLM.Add(lMessages[1]);
                var lFB = lClient.Store(lLM, eStoreOperation.add, cStorableFlags.Empty);
                if (lFB.Summary().ExpungedCount != 1) throw new cTestsException("ZTestIdleRestart1.3.1"); // this should do nothing (as the message has been expunged), but idle should stop

                //if (lMessages[1].Fetch(fMessageCacheAttributes.uid)) throw new cTestsException("ZTestIdleRestart1.3.1"); // this should retrieve nothing (as the message has been deleted), but idle should stop
                Thread.Sleep(3000); // idle should restart in this wait

                var lList = new cMessage[] { lMessages[0], lMessages[1], lMessages[2] };

                List<cMessage> lUnfetched;

                // only message 1 and 3 should be fetched by this, as message 2 was 168 which should now be gone
                //  1 should be UID fetched, 3 should be a normal fetch
                lUnfetched = lClient.Fetch(lList, fMessageCacheAttributes.received, null);
                if (lUnfetched.Count != 1 || !ReferenceEquals(lUnfetched[0].MessageHandle, lMessages[1].MessageHandle)) throw new cTestsException("ZTestIdleRestart1.3.2");

                Thread.Sleep(3000); // idle should restart in this wait

                // only message 1 and 3 should be fetched, however this time (due to getting fast responses the last time) they should both be normal fetch
                lUnfetched = lClient.Fetch(lMessages, fMessageCacheAttributes.flags, null);
                if (lUnfetched.Count != 1 || !ReferenceEquals(lUnfetched[0].MessageHandle, lMessages[1].MessageHandle)) throw new cTestsException("ZTestIdleRestart1.3.3");


                cMailbox lMailbox;
                cFilter lFilter;
                bool lFailed;


                lFilter = cFilter.UID > new cUID(3857529044, 4391);

                // test that there is a throw if the mailbox isn't selected
                lMailbox = lClient.Mailbox(new cMailboxName("blurdybloop", null));

                lFailed = false;
                try { lMessages = lMailbox.Messages(lFilter); }
                catch (InvalidOperationException) { lFailed = true; }
                if (!lFailed) throw new cTestsException("ZTestIdleRestart1.4");

                lMailbox.Select(false);

                lFailed = false;
                try { lMessages = lMailbox.Messages(lFilter); }
                catch (cUIDValidityException) { lFailed = true; }
                if (!lFailed) throw new cTestsException("ZTestIdleRestart1.5");

                lFilter = cFilter.UID > new cUID(3857529045, 4391);

                lMessages = lMailbox.Messages(lFilter);





                lClient.Disconnect();

                lServer.ThrowAnyErrors();
            }
        }

        private static void ZTestUIDFetch1(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestUIDFetch1));

            using (cServer lServer = new cServer(lContext, nameof(ZTestUIDFetch1)))
            using (cIMAPClient lClient = lServer.NewClient())
            {
                lServer.AddSendData("* PREAUTH [CAPABILITY IMAP4rev1] this is the text\r\n");
                lServer.AddExpectTagged("LIST \"\" \"\"\r\n");
                lServer.AddSendData("* LIST () nil \"\"\r\n");
                lServer.AddSendTagged("OK LIST command completed\r\n");

                // open inbox
                lServer.AddExpectTagged("SELECT INBOX\r\n");
                lServer.AddSendData("* 172 EXISTS\r\n");
                lServer.AddSendData("* 1 RECENT\r\n");
                lServer.AddSendData("* OK [UNSEEN 12] Message 12 is first unseen\r\n");
                lServer.AddSendData("* OK [UIDVALIDITY 3857529045] UIDs valid\r\n");
                lServer.AddSendData("* OK [UIDNEXT 4392] Predicted next UID\r\n");
                lServer.AddSendData("* FLAGS (\\Answered \\Flagged \\Deleted \\Seen \\Draft)\r\n");
                lServer.AddSendData("* OK [PERMANENTFLAGS (\\Deleted \\Seen \\*)] Limited\r\n");
                lServer.AddSendTagged("OK [READ-WRITE] SELECT completed\r\n");

                // poll
                lServer.AddExpectTagged("CHECK\r\n");
                lServer.AddSendTagged("OK CHECK completed\r\n");
                lServer.AddExpectTagged("NOOP\r\n");
                lServer.AddSendData("* 2 FETCH (FLAGS ())\r\n");
                lServer.AddSendData("* 3 FETCH (UID 103)\r\n");
                lServer.AddSendData("* 4 FETCH (FLAGS () UID 105)\r\n");
                lServer.AddSendTagged("OK NOOP completed\r\n");


                //the mailbox looks like this in the client;
                //MSN     UID     UID known   flags known     internal date known
                //1       101     
                //2       102                 y
                //3       103     y
                //4       105     y           y

                //(104 has been expunged)


                // fetch
                lServer.AddExpectTagged("UID FETCH 105 INTERNALDATE\r\n");
                lServer.AddSendData("* 4 FETCH (UID 105 INTERNALDATE \"08-JUN-2017 01:05:00 -1200\")\r\n");
                lServer.AddSendTagged("OK FETCH completed\r\n");
                lServer.AddExpectTagged("UID FETCH 101:102 (FLAGS INTERNALDATE)\r\n");
                lServer.AddSendData("* 1 FETCH (UID 101 FLAGS () INTERNALDATE \"08-JUN-2017 01:01:00 -1200\")\r\n");
                lServer.AddSendData("* 2 FETCH (UID 102 FLAGS () INTERNALDATE \"08-JUN-2017 01:02:00 -1200\")\r\n");
                lServer.AddSendTagged("OK UID FETCH completed\r\n");
                lServer.AddExpectTagged("UID FETCH 103:104 (FLAGS INTERNALDATE)\r\n");
                lServer.AddSendData("* 3 FETCH (UID 103 FLAGS () INTERNALDATE \"08-JUN-2017 01:03:00 -1200\")\r\n");
                lServer.AddSendTagged("OK UID FETCH completed\r\n");

                lServer.AddExpectTagged("LOGOUT\r\n");
                lServer.AddSendData("* BYE logging out\r\n");
                lServer.AddSendTagged("OK logged out\r\n");
                lServer.AddExpectClose();

                lClient.AuthenticationParameters = new cAuthenticationParameters(new object());
                lClient.IdleConfiguration = null;

                lClient.Connect();

                // open inbox
                lClient.Inbox.Select(true);

                // give the server a chance to send some fetches to set up the test case
                lClient.Poll();

                // pretend that we know 5 UIDs; 104 has been expunged
                cUID[] lUIDs = new cUID[] { new cUID(3857529045, 105), new cUID(3857529045, 104), new cUID(3857529045, 103), new cUID(3857529045, 102), new cUID(3857529045, 101) };

                // fetch flags
                var lMessages = lClient.Inbox.Messages(lUIDs, fMessageCacheAttributes.flags | fMessageCacheAttributes.received);
                if (lMessages.Count != 4) throw new cTestsException($"{nameof(ZTestUIDFetch1)}.1");


                lClient.Disconnect();

                lServer.ThrowAnyErrors();
            }
        }










        private static void ZTestPipelineCancellation(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestPipelineCancellation)); // CHANGE THE NAME HERE

            using (cServer lServer = new cServer(lContext, nameof(ZTestPipelineCancellation)))
            using (cIMAPClient lClient = lServer.NewClient())
            {
                lServer.AddSendData("* PREAUTH [CAPABILITY IMAP4rev1 ESEARCH] this is the text\r\n");
                lServer.AddExpectTagged("LIST \"\" \"\"\r\n");
                lServer.AddSendData("* LIST () nil \"\"\r\n");
                lServer.AddSendTagged("OK LIST command completed\r\n");

                // add stuff here

                lServer.AddExpectTagged("SELECT INBOX\r\n");
                lServer.AddSendData("* 172 EXISTS\r\n");
                lServer.AddSendData("* 1 RECENT\r\n");
                lServer.AddSendData("* OK [UNSEEN 12] Message 12 is first unseen\r\n");
                lServer.AddSendData("* OK [UIDVALIDITY 3857529045] UIDs valid\r\n");
                lServer.AddSendData("* OK [UIDNEXT 4392] Predicted next UID\r\n");
                lServer.AddSendData("* FLAGS (\\Answered \\Flagged \\Deleted \\Seen \\Draft)\r\n");
                lServer.AddSendData("* OK [PERMANENTFLAGS (\\Deleted \\Seen \\*)] Limited\r\n");
                lServer.AddSendTagged("OK [READ-WRITE] SELECT completed\r\n");

                lServer.AddExpectTagged("SEARCH RETURN () BODY {6}\r\n");
                lServer.AddSendData("+ ready\r\n");
                lServer.AddExpectData("stu\rff\r\n");

                lServer.AddExpectTagged("SEARCH RETURN () BODY {5}\r\n");
                lServer.AddSendData("+ ready\r\n");
                lServer.AddExpectData("stu\rf\r\n");

                lServer.AddSendData("* ESEARCH (TAG \"\t\")\r\n");
                lServer.AddSendTagged("OK SEARCH completed\r\n");
                lServer.AddSendData("* ESEARCH (TAG \"\t\")\r\n");
                lServer.AddSendTagged("OK SEARCH completed\r\n");


                lServer.AddExpectTagged("SEARCH RETURN () BODY {6}\r\n");
                lServer.AddDelay(3000);
                lServer.AddSendData("+ ready\r\n");
                lServer.AddExpectData("stu\rff\r\n");

                lServer.AddSendData("* ESEARCH (TAG \"\t\")\r\n");
                lServer.AddSendTagged("OK SEARCH completed\r\n");





                //

                lServer.AddExpectTagged("LOGOUT\r\n");
                lServer.AddSendData("* BYE logging out\r\n");
                lServer.AddSendTagged("OK logged out\r\n");
                lServer.AddExpectClose();

                lClient.AuthenticationParameters = new cAuthenticationParameters(new object());
                lClient.IdleConfiguration = null;

                Task<List<cMessage>> lTask1;
                Task<List<cMessage>> lTask2;
                List<cMessage> lMessages;
                bool lFailed;


                lClient.Connect();
                lClient.Inbox.Select(true);

                lTask1 = lClient.Inbox.MessagesAsync(cFilter.Body.Contains("stu\rff"));

                using (var lCTS = new CancellationTokenSource(1000))
                {
                    lTask2 = lClient.Inbox.MessagesAsync(cFilter.Body.Contains("stu\rf"), null, null, new cMessageFetchConfiguration(lCTS.Token, null, null));

                    lMessages = lTask1.Result;
                    lMessages = lTask2.Result;
                }

                lTask1 = lClient.Inbox.MessagesAsync(cFilter.Body.Contains("stu\rff"));

                using (var lCTS = new CancellationTokenSource(1000))
                {
                    lTask2 = lClient.Inbox.MessagesAsync(cFilter.Body.Contains("stu\rf"), null, null, new cMessageFetchConfiguration(lCTS.Token, null, null));

                    lMessages = lTask1.Result;

                    lFailed = false;
                    try { lMessages = lTask2.Result; }
                    catch { lFailed = true; }
                    if (lFailed != true) throw new cTestsException($"{nameof(ZTestPipelineCancellation)}.1");
                }



                lClient.Disconnect();

                lServer.ThrowAnyErrors();
            }
        }

        private static void ZTestEarlyTermination1(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestEarlyTermination1));

            using (cServer lServer = new cServer(lContext, nameof(ZTestEarlyTermination1)))
            using (cIMAPClient lClient = lServer.NewClient())
            {
                lServer.AddSendData("* OK [CAPABILITY IMAP4rev1] this is the text\r\n");

                lServer.AddExpectTagged("LOGIN {4}\r\n");
                lServer.AddSendData("+ ready\r\n");
                lServer.AddExpectData("fred {5}\r\n");
                lServer.AddSendTagged("NO we don't like you fred\r\n");

                lServer.AddExpectTagged("LOGOUT\r\n");
                lServer.AddSendData("* BYE logging out\r\n");
                lServer.AddSendTagged("OK logged out\r\n");
                lServer.AddClose();

                lClient.SetPlainAuthenticationParameters("fred", "angus", eTLSRequirement.indifferent);

                bool lFailed = false;
                try { lClient.Connect(); }
                catch (cCredentialsException) { lFailed = true; }
                if (!lFailed) throw new cTestsException("expected connect to fail");

                lServer.ThrowAnyErrors();
            }
        }

        private static void ZTestEarlyTermination2(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestEarlyTermination2));

            // this test has a race condition, so run it 10 times hoping for a success ...

            for (int i = 0; i < 10; i++)
            {
                bool lFailed = false;
                try { ZTestEarlyTermination2Worker(lContext); }
                catch { lFailed = true; }
                if (!lFailed) return;
            }

            throw new cTestsException("ZTestEarlyTermination2");
        }

        private static void ZTestEarlyTermination2Worker(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestEarlyTermination2Worker));

            using (cServer lServer = new cServer(lContext, nameof(ZTestEarlyTermination2Worker)))
            using (cIMAPClient lClient = lServer.NewClient())
            {
                lServer.AddSendData("* OK [CAPABILITY LITERAL- IMAP4rev1] this is the text\r\n");

                lServer.AddExpectTagged("LOGIN {4+}\r\n");
                lServer.AddSendTagged("NO shutting down\r\n");
                lServer.AddExpectData("fred\r\n");

                lServer.AddExpectTagged("LOGOUT\r\n");
                lServer.AddSendData("* BYE logging out\r\n");
                lServer.AddSendTagged("OK logged out\r\n");
                lServer.AddClose();

                lClient.SetPlainAuthenticationParameters("fred", "angus", eTLSRequirement.indifferent);
                lClient.NetworkWriteConfiguration = new cBatchSizerConfiguration(1, 1, 10000, 1);


                bool lFailed = false;
                try { lClient.Connect(); }
                catch (cCredentialsException) { lFailed = true; }
                if (!lFailed) throw new cTestsException("expected connect to fail");

                lServer.ThrowAnyErrors();
            }
        }

        private static void ZTestAppendNoCatenateNoBinaryNoUTF8(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestAppendNoCatenateNoBinaryNoUTF8));

            using (var lTempFileCollection = new TempFileCollection())
            {
                using (cServer lServer = new cServer(lContext, nameof(ZTestAppendNoCatenateNoBinaryNoUTF8)))
                using (cIMAPClient lClient1 = lServer.NewClient(1))
                using (cIMAPClient lClient2 = lServer.NewClient(2))
                {
                    lServer.AddSendData("* PREAUTH [CAPABILITY IMAP4rev1 LITERAL+] this is the text\r\n");
                    lServer.AddExpectTagged("LIST \"\" \"\"\r\n");
                    lServer.AddSendData("* LIST () nil \"\"\r\n");
                    lServer.AddSendTagged("OK LIST command completed\r\n");

                    // select inbox
                    lServer.AddExpectTagged("EXAMINE INBOX\r\n");
                    lServer.AddSendData("* 1 EXISTS\r\n");
                    lServer.AddSendData("* 1 RECENT\r\n");
                    lServer.AddSendData("* OK [UNSEEN 1] Message 1 is first unseen\r\n");
                    lServer.AddSendData("* OK [UIDVALIDITY 3857529045] UIDs valid\r\n");
                    lServer.AddSendData("* OK [UIDNEXT 4392] Predicted next UID\r\n");
                    lServer.AddSendData("* FLAGS (\\Answered \\Flagged \\Deleted \\Seen \\Draft)\r\n");
                    lServer.AddSendData("* OK [PERMANENTFLAGS ()] No permanent flags permitted\r\n");
                    lServer.AddSendTagged("OK [READ-ONLY] EXAMINE completed\r\n");

                    // the second client connects
                    var lServer2 = lServer.NewJobList();
                    lServer2.AddSendData("* PREAUTH [CAPABILITY IMAP4rev1 LITERAL+] this is the text\r\n");
                    lServer2.AddExpectTagged("LIST \"\" \"\"\r\n");
                    lServer2.AddSendData("* LIST () nil \"\"\r\n");
                    lServer2.AddSendTagged("OK LIST command completed\r\n");

                    // the second client requests the message details from the first client
                    lServer.AddExpectTagged("FETCH 1 (FLAGS INTERNALDATE RFC822.SIZE)\r\n");
                    lServer.AddSendData("* 1 FETCH (FLAGS (\\seen) INTERNALDATE \"22-feb-2018 14:48:49 +1300\" RFC822.SIZE 226)\r\n");
                    lServer.AddSendTagged("OK FETCH completed\r\n");


                    // the second client requests the message text from the first client
                    lServer.AddExpectTagged("FETCH 1 BODY.PEEK[]<0.1000>\r\n");
                    //                                              12345678901234567 8 90123456789012345678901234567890123456789 012345678 9 0 1 2 34567890123 4 5678901234567890123456789 0 1 2 345678 9 01234567890 1 23456789012345678901234567890 1 2 3 45678901234567890123456789012345678901 2 3 4 567890 1 2345678901234 5 6
                    lServer.AddSendData("* 1 FETCH (BODY[] {226}\r\nmime-version: 1.0\r\ncontent-type: multipart/mixed; boundary=\"boundary\"\r\n\r\n--boundary\r\ncontent-type: text/plain\r\n\r\nhello\r\n--boundary\r\ncontent-type: message/rfc822\r\n\r\ndate: thu, 22 feb 2018 15:49:50 +1300\r\n\r\nhello\r\n--boundary--\r\n)\r\n");
                    lServer.AddSendTagged("OK FETCH completed\r\n");

                    // the second client appends; no rfc 4315 response
                    lServer2.AddExpectTagged("APPEND INBOX (\\seen) \"22-FEB-2018 14:48:49 +1300\" {226+}\r\nmime-version: 1.0\r\ncontent-type: multipart/mixed; boundary=\"boundary\"\r\n\r\n--boundary\r\ncontent-type: text/plain\r\n\r\nhello\r\n--boundary\r\ncontent-type: message/rfc822\r\n\r\ndate: thu, 22 feb 2018 15:49:50 +1300\r\n\r\nhello\r\n--boundary--\r\n\r\n");
                    lServer2.AddSendTagged("OK APPENDed\r\n");

                    // the second client requests the message text from the first client again (message data is not cached)
                    lServer.AddExpectTagged("FETCH 1 BODY.PEEK[]<0.1000>\r\n");
                    //                                              12345678901234567 8 90123456789012345678901234567890123456789 012345678 9 0 1 2 34567890123 4 5678901234567890123456789 0 1 2 345678 9 01234567890 1 23456789012345678901234567890 1 2 3 45678901234567890123456789012345678901 2 3 4 567890 1 2345678901234 5 6
                    lServer.AddSendData("* 1 FETCH (BODY[] {226}\r\nmime-version: 1.0\r\ncontent-type: multipart/mixed; boundary=\"boundary\"\r\n\r\n--boundary\r\ncontent-type: text/plain\r\n\r\nhello\r\n--boundary\r\ncontent-type: message/rfc822\r\n\r\ndate: thu, 22 feb 2018 15:49:50 +1300\r\n\r\nhello\r\n--boundary--\r\n)\r\n");
                    lServer.AddSendTagged("OK FETCH completed\r\n");

                    // the second client appends; rfc 4315 response
                    lServer2.AddExpectTagged("APPEND INBOX (\\seen) \"22-FEB-2018 14:48:49 +1300\" {226+}\r\nmime-version: 1.0\r\ncontent-type: multipart/mixed; boundary=\"boundary\"\r\n\r\n--boundary\r\ncontent-type: text/plain\r\n\r\nhello\r\n--boundary\r\ncontent-type: message/rfc822\r\n\r\ndate: thu, 22 feb 2018 15:49:50 +1300\r\n\r\nhello\r\n--boundary--\r\n\r\n");
                    lServer2.AddSendTagged("OK [APPENDUID 2 1] APPENDed\r\n");

                    // the second client requests the body structure from the first client
                    lServer.AddExpectTagged("FETCH 1 BODYSTRUCTURE\r\n");
                    lServer.AddSendData("* 1 FETCH (RFC822.SIZE 226 BODYSTRUCTURE ((\"text\" \"plain\" NIL NIL NIL \"7bit\" 5 0 NIL NIL NIL NIL)(\"message\" \"rfc822\" NIL NIL NIL \"7bit\" 46 (\"thu, 22 feb 2018 15:49:50 +1300\" NIL NIL NIL NIL NIL NIL NIL NIL NIL) (\"text\" \"plain\" NIL NIL NIL \"7bit\" 5 0 NIL NIL NIL NIL) 2) \"mixed\" (\"boundary\" \"boundary\")))\r\n");
                    lServer.AddSendTagged("OK FETCH completed\r\n");

                    // the second client requests the encapsulated message text from the first client
                    lServer.AddExpectTagged("FETCH 1 BODY.PEEK[2]<0.1000>\r\n");
                    //                                             1234567890123456789012345678901234567 8 9 0 123456
                    lServer.AddSendData("* 1 FETCH (BODY[2] {46}\r\ndate: thu, 22 feb 2018 15:49:50 +1300\r\n\r\nhello)\r\n");
                    lServer.AddSendTagged("OK FETCH completed\r\n");

                    // the second client appends the encapsulated message
                    lServer2.AddExpectTagged("APPEND INBOX \"22-FEB-2018 14:48:49 +1300\" {46+}\r\ndate: thu, 22 feb 2018 15:49:50 +1300\r\n\r\nhello\r\n");
                    lServer2.AddSendTagged("OK [APPENDUID 2 3] APPENDed\r\n");

                    // the second client appends a literal message
                    lServer2.AddExpectTagged("APPEND INBOX {46+}\r\ndate: thu, 22 feb 2018 15:49:50 +1300\r\n\r\nhello\r\n");
                    lServer2.AddSendTagged("OK [APPENDUID 2 4] APPENDed\r\n");

                    // the second client appends a file message with different options
                    lServer2.AddExpectTagged("APPEND INBOX {48+}\r\ndate: thu, 22 feb 2018 15:49:52 +1300\r\n\r\nhello\r\n\r\n");
                    lServer2.AddSendTagged("OK [APPENDUID 2 5] APPENDed\r\n");
                    lServer2.AddExpectTagged("APPEND INBOX (\\Draft) {48+}\r\ndate: thu, 22 feb 2018 15:49:52 +1300\r\n\r\nhello\r\n\r\n");
                    lServer2.AddSendTagged("OK [APPENDUID 2 6] APPENDed\r\n");
                    lServer2.AddExpectTagged("APPEND INBOX (\\Flagged) \"28-FEB-2018 13:40:50 +1300\" {48+}\r\ndate: thu, 22 feb 2018 15:49:52 +1300\r\n\r\nhello\r\n\r\n");
                    lServer2.AddSendTagged("OK [APPENDUID 2 7] APPENDed\r\n");
                    lServer2.AddExpectTagged("APPEND INBOX \"28-FEB-2018 13:40:51 +0000\" {48+}\r\ndate: thu, 22 feb 2018 15:49:52 +1300\r\n\r\nhello\r\n\r\n");
                    lServer2.AddSendTagged("OK [APPENDUID 2 8] APPENDed\r\n");
                    lServer2.AddExpectTagged("APPEND INBOX \"28-FEB-2018 13:40:52 -0000\" {48+}\r\ndate: thu, 22 feb 2018 15:49:52 +1300\r\n\r\nhello\r\n\r\n");
                    lServer2.AddSendTagged("OK [APPENDUID 2 9] APPENDed\r\n");

                    // from a stream
                    lServer2.AddExpectTagged("APPEND INBOX {48+}\r\ndate: thu, 22 feb 2018 15:49:52 +1300\r\n\r\nhello\r\n\r\n");
                    lServer2.AddSendTagged("OK [APPENDUID 2 10] APPENDed\r\n");

                    // client emulates multi-append
                    lServer2.AddExpectTagged("APPEND INBOX {46+}\r\ndate: thu, 22 feb 2018 15:49:51 +1300\r\n\r\nhello\r\n");
                    lServer2.AddExpectTagged("APPEND INBOX {48+}\r\ndate: thu, 22 feb 2018 15:49:50 +1300\r\n\r\nhello\r\n\r\n");
                    lServer2.AddSendTagged("OK [APPENDUID 2 12] APPENDed\r\n");
                    lServer2.AddSendTagged("OK [APPENDUID 2 11] APPENDed\r\n");

                    // fail/ success
                    lServer2.AddExpectTagged("APPEND INBOX {46+}\r\ndate: thu, 22 feb 2018 15:49:51 +1300\r\n\r\nhello\r\n");
                    lServer2.AddExpectTagged("APPEND INBOX {48+}\r\ndate: thu, 22 feb 2018 15:49:50 +1300\r\n\r\nhello\r\n\r\n");
                    lServer2.AddSendTagged("OK [APPENDUID 2 13] APPENDed\r\n");
                    lServer2.AddSendTagged("NO I don't like the look of that message\r\n");

                    /*
                    // multi-part
                    
                    // the second client requests the message text from the first client
                    lServer.AddExpectTagged("FETCH 1 BODY.PEEK[]<0.1000>\r\n");
                    lServer.AddSendData("* 1 FETCH (BODY[] {226}\r\nmime-version: 1.0\r\ncontent-type: multipart/mixed; boundary=\"boundary\"\r\n\r\n--boundary\r\ncontent-type: text/plain\r\n\r\nhello\r\n--boundary\r\ncontent-type: message/rfc822\r\n\r\ndate: thu, 22 feb 2018 15:49:50 +1300\r\n\r\nhello\r\n--boundary--\r\n)\r\n");
                    lServer.AddSendTagged("OK FETCH completed\r\n");

                    // the second client requests the encapsulated message text from the first client
                    lServer.AddExpectTagged("FETCH 1 BODY.PEEK[2]<0.1000>\r\n");
                    lServer.AddSendData("* 1 FETCH (BODY[2] {46}\r\ndate: thu, 22 feb 2018 15:49:50 +1300\r\n\r\nhello)\r\n");
                    lServer.AddSendTagged("OK FETCH completed\r\n");

                    ;?;
                    // the second client appends; rfc 4315 response
                    lServer2.AddExpectTagged("APPEND INBOX {226+}\r\nmime-version: 1.0\r\ncontent-type: multipart/mixed; boundary=\"boundary\"\r\n\r\n--boundary\r\ncontent-type: text/plain\r\n\r\nhello\r\n--boundary\r\ncontent-type: message/rfc822\r\n\r\ndate: thu, 22 feb 2018 15:49:50 +1300\r\n\r\nhello\r\n--boundary--\r\n\r\n");
                    lServer2.AddSendTagged("OK [APPENDUID 2 14] APPENDed\r\n");
                    */


                    // the second client disconnects
                    lServer2.AddExpectTagged("LOGOUT\r\n");
                    lServer2.AddSendData("* BYE logging out\r\n");
                    lServer2.AddSendTagged("OK logged out\r\n");
                    lServer2.AddExpectClose();

                    // the first client disconnects
                    lServer.AddExpectTagged("LOGOUT\r\n");
                    lServer.AddSendData("* BYE logging out\r\n");
                    lServer.AddSendTagged("OK logged out\r\n");
                    lServer.AddExpectClose();

                    lClient1.AuthenticationParameters = new cAuthenticationParameters(new object());
                    lClient1.IdleConfiguration = null;
                    lClient1.FetchBodyReadConfiguration = new cBatchSizerConfiguration(1000, 1000, 1000, 1000);

                    lClient2.AuthenticationParameters = lClient1.AuthenticationParameters;
                    lClient2.IdleConfiguration = null;

                    lClient1.Connect();
                    lClient1.Inbox.Select();
                    var lMessage = lClient1.Inbox.Messages()[0];

                    lClient2.Connect();

                    cUID lUID;

                    // append whole message with no rfc 4315 feedback
                    lUID = lClient2.Inbox.Append(lMessage);
                    if (lUID != null) throw new cTestsException($"{nameof(ZTestAppendNoCatenateNoBinaryNoUTF8)}.1");

                    // append whole message with rfc 4315 feedback
                    lUID = lClient2.Inbox.Append(lMessage);
                    if (lUID != new cUID(2, 1)) throw new cTestsException($"{nameof(ZTestAppendNoCatenateNoBinaryNoUTF8)}.2");

                    // append message part with rfc 4315 feedback
                    var lBS = lMessage.BodyStructure as cMultiPartBody;
                    lUID = lClient2.Inbox.Append(new cMessagePartAppendData(lMessage, lBS.Parts[1] as cMessageBodyPart));
                    if (lUID != new cUID(2, 3)) throw new cTestsException($"{nameof(ZTestAppendNoCatenateNoBinaryNoUTF8)}.3");

                    // append literal message
                    lUID = lClient2.Inbox.Append("date: thu, 22 feb 2018 15:49:50 +1300\r\n\r\nhello");
                    if (lUID != new cUID(2, 4)) throw new cTestsException($"{nameof(ZTestAppendNoCatenateNoBinaryNoUTF8)}.4");

                    // append from file

                    var lTempFileName = Path.GetTempFileName();
                    lTempFileCollection.AddFile(lTempFileName, false);
                    File.WriteAllLines(lTempFileName, new string[] { "date: thu, 22 feb 2018 15:49:52 +1300", "", "hello" });

                    // default flags, no received date
                    lUID = lClient2.Inbox.Append(new cFileAppendData(lTempFileName));
                    if (lUID != new cUID(2, 5)) throw new cTestsException($"{nameof(ZTestAppendNoCatenateNoBinaryNoUTF8)}.5");

                    // default flags, no received date
                    lClient2.DefaultAppendFlags = cStorableFlags.Draft;
                    lUID = lClient2.Inbox.Append(new cFileAppendData(lTempFileName));
                    if (lUID != new cUID(2, 6)) throw new cTestsException($"{nameof(ZTestAppendNoCatenateNoBinaryNoUTF8)}.6");

                    // override flags and a date
                    lUID = lClient2.Inbox.Append(new cFileAppendData(lTempFileName, cStorableFlags.Flagged, new DateTime(2018, 02, 28, 13, 40, 50, DateTimeKind.Local)));
                    if (lUID != new cUID(2, 7)) throw new cTestsException($"{nameof(ZTestAppendNoCatenateNoBinaryNoUTF8)}.7");

                    // no flags and a gmt date
                    lUID = lClient2.Inbox.Append(new cFileAppendData(lTempFileName, cStorableFlags.Empty, new DateTime(2018, 02, 28, 13, 40, 51, DateTimeKind.Utc)));
                    if (lUID != new cUID(2, 8)) throw new cTestsException($"{nameof(ZTestAppendNoCatenateNoBinaryNoUTF8)}.8");

                    // no flags and an unspecified date kind
                    lUID = lClient2.Inbox.Append(new cFileAppendData(lTempFileName, cStorableFlags.Empty, new DateTime(2018, 02, 28, 13, 40, 52, DateTimeKind.Unspecified)));
                    if (lUID != new cUID(2, 9)) throw new cTestsException($"{nameof(ZTestAppendNoCatenateNoBinaryNoUTF8)}.9");

                    // append from a stream
                    lClient2.DefaultAppendFlags = null;

                    using (var lStream = new FileStream(lTempFileName, FileMode.Open, FileAccess.Read))
                    {
                        lUID = lClient2.Inbox.Append(lStream);
                        if (lUID != new cUID(2, 10)) throw new cTestsException($"{nameof(ZTestAppendNoCatenateNoBinaryNoUTF8)}.10");
                    }

                    cAppendFeedback lFeedback;

                    // success/ success
                    lFeedback = lClient2.Inbox.Append(new cAppendData[] { "date: thu, 22 feb 2018 15:49:51 +1300\r\n\r\nhello", new cFileAppendData(lTempFileName) });
                    if (lFeedback.Count != 2) throw new cTestsException($"{nameof(ZTestAppendNoCatenateNoBinaryNoUTF8)}.11.1");
                    if (lFeedback.SucceededCount != 2) throw new cTestsException($"{nameof(ZTestAppendNoCatenateNoBinaryNoUTF8)}.11.2");
                    if (lFeedback[0].UID != new cUID(2, 11)) throw new cTestsException($"{nameof(ZTestAppendNoCatenateNoBinaryNoUTF8)}.11.3");
                    if (lFeedback[1].UID != new cUID(2, 12)) throw new cTestsException($"{nameof(ZTestAppendNoCatenateNoBinaryNoUTF8)}.11.4");

                    // fail/ success
                    lFeedback = lClient2.Inbox.Append(new cAppendData[] { "date: thu, 22 feb 2018 15:49:51 +1300\r\n\r\nhello", new cFileAppendData(lTempFileName) });
                    if (lFeedback.Count != 2) throw new cTestsException($"{nameof(ZTestAppendNoCatenateNoBinaryNoUTF8)}.12.1");
                    if (lFeedback.SucceededCount != 1) throw new cTestsException($"{nameof(ZTestAppendNoCatenateNoBinaryNoUTF8)}.12.2");
                    if (lFeedback[0].UID != null) throw new cTestsException($"{nameof(ZTestAppendNoCatenateNoBinaryNoUTF8)}.12.3");
                    if (lFeedback[1].UID != new cUID(2, 13)) throw new cTestsException($"{nameof(ZTestAppendNoCatenateNoBinaryNoUTF8)}.12.4");

                    /*
                    var lParts = new List<cAppendDataPart>();

                    // literals and message and message part and file and stream
                    lParts.Add(new cHeaderFieldAppendDataPart("mime-version", "1.0"));
                    lParts.Add(new cHeaderFieldAppendDataPart("content-type", new cHeaderFieldValuePart[] { "multipart/mixed", new cHeaderFieldMIMEParameter("boundary", "boundary2") }));

                    // text
                    lParts.Add("\r\n--boundary2\r\n");
                    lParts.Add(new cHeaderFieldAppendDataPart("content-type", "text/plain"));
                    lParts.Add("\r\n");
                    lParts.Add("hello");

                    // message
                    lParts.Add("\r\n--boundary2\r\n");
                    lParts.Add(new cHeaderFieldAppendDataPart("content-type", "message/rfc822"));
                    lParts.Add("\r\n");
                    lParts.Add(new cMessageAppendDataPart(lMessage));

                    // message part
                    lParts.Add("\r\n--boundary2\r\n");
                    lParts.Add(new cHeaderFieldAppendDataPart("content-type", "message/rfc822"));
                    lParts.Add("\r\n");
                    lParts.Add(new cMessagePartAppendDataPart(lMessage, lBS.Parts[1] as cMessageBodyPart));

                    // file
                    lParts.Add("\r\n--boundary2\r\n");
                    lParts.Add(new cHeaderFieldAppendDataPart("content-type", "message/rfc822"));
                    lParts.Add("\r\n");
                    lParts.Add(new cFileAppendDataPart(lTempFileName));

                    // end
                    lParts.Add("\r\n--boundary2--\r\n");

                    lUID = lClient2.Inbox.Append(lParts);
                    if (lUID != new cUID(2, 14)) throw new cTestsException($"{nameof(ZTestAppendNoCatenateNoBinaryNoUTF8)}.14");
                    */


                    lClient2.Disconnect();

                    lClient1.Disconnect();

                    lServer.ThrowAnyErrors();
                }
            }
        }




        private static void ZTestBlank(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestBlank)); // CHANGE THE NAME HERE

            using (cServer lServer = new cServer(lContext, nameof(ZTestBlank)))// CHANGE THE NAME HERE
            using (cIMAPClient lClient = lServer.NewClient()) 
            {

                lServer.AddSendData("* PREAUTH [CAPABILITY IMAP4rev1] this is the text\r\n");
                lServer.AddExpectTagged("LIST \"\" \"\"\r\n");
                lServer.AddSendData("* LIST () nil \"\"\r\n");
                lServer.AddSendTagged("OK LIST command completed\r\n");

                // add stuff here

                lServer.AddExpectTagged("LOGOUT\r\n");
                lServer.AddSendData("* BYE logging out\r\n");
                lServer.AddSendTagged("OK logged out\r\n");
                lServer.AddExpectClose();

                lClient.SetServer("localhost");
                lClient.AuthenticationParameters = new cAuthenticationParameters(new object());
                lClient.IdleConfiguration = null;

                lClient.Connect();

                // add stuff here

                lClient.Disconnect();

                lServer.ThrowAnyErrors();
            }
        }

        // disable warning about my listener
#pragma warning disable 618

        private sealed class cServer : IDisposable
        {
            private readonly string mNameOfTest;
            private readonly TcpListener mListener = new TcpListener(143);
            private readonly CancellationTokenSource mCancellationTokenSource = new CancellationTokenSource();
            private readonly List<cJobList> mJobLists = new List<cJobList>();
            private readonly List<cSession> mSessions = new List<cSession>();
            private readonly List<Task> mSessionTasks = new List<Task>();
            private readonly Task mServerTask;
            private readonly Task mTimeoutTask;

            public cServer(cTrace.cContext pContext, string pNameOfTest, int pMaxRunTime = 10000)
            {
                mNameOfTest = pNameOfTest;

                mJobLists.Add(new cJobList());

                mServerTask = ZServer(pContext);
                mTimeoutTask = ZTimeout(pMaxRunTime);
            }

            public void AddExpectData(string pData) => mJobLists[0].AddExpectData(pData);
            public void AddExpectData(byte[] pData) => mJobLists[0].AddExpectData(pData);
            public void AddExpectTagged(string pData) => mJobLists[0].AddExpectTagged(pData);
            public void AddExpectTagged(byte[] pData) => mJobLists[0].AddExpectTagged(pData);
            public void AddExpectClose() => mJobLists[0].AddExpectClose();
            public void AddDelay(int pDelay) => mJobLists[0].AddDelay(pDelay);
            public void AddSendData(string pData) => mJobLists[0].AddSendData(pData);
            public void AddSendData(byte[] pData) => mJobLists[0].AddSendData(pData);
            public void AddSendTagged(string pData) => mJobLists[0].AddSendTagged(pData);
            public void AddClose() => mJobLists[0].AddClose();

            public cIMAPClient NewClient(int pInstance = 0)
            {
                cIMAPClient lClient = new cIMAPClient(mNameOfTest + "." + pInstance);
                lClient.SetServer("localhost");
                lClient.CancellationToken = mCancellationTokenSource.Token;
                return lClient;
            }

            public cJobList NewJobList()
            {
                var lJobList = new cJobList();
                mJobLists.Add(lJobList);
                return lJobList;
            }

            public Task GetSessionTask(int i) => mSessionTasks[i];

            public void ThrowAnyErrors()
            {
                if (mServerTask == null) throw new InvalidOperationException();
                mCancellationTokenSource.Cancel();
                Task.WaitAll(mSessionTasks.ToArray());
            }

            private async Task ZServer(cTrace.cContext pContext)
            { 
                mListener.Start();

                try
                {
                    while (true)
                    {
                        var lClient = await mListener.AcceptTcpClientAsync().ConfigureAwait(false);
                        var lJobList = mJobLists[mSessions.Count];
                        var lSession = new cSession(lClient, lJobList, mCancellationTokenSource);
                        mSessions.Add(lSession);
                        mSessionTasks.Add(lSession.RunAsync(pContext));
                    }
                }
                finally
                {
                    mListener.Stop();
                    mCancellationTokenSource.Cancel();
                }
            }

            public async Task ZTimeout(int pMaxRunTime)
            {
                try { await Task.Delay(pMaxRunTime, mCancellationTokenSource.Token).ConfigureAwait(false); }
                finally
                {
                    mListener.Stop();
                    mCancellationTokenSource.Cancel();
                }
            }

            public void Dispose()
            {
                try { mListener.Stop(); }
                catch { }

                if (mCancellationTokenSource != null)
                {
                    try { mCancellationTokenSource.Cancel(); }
                    catch { }
                }

                if (mServerTask != null)
                {
                    try
                    {
                        mServerTask.Wait();
                        mServerTask.Dispose();
                    }
                    catch { }
                }

                if (mTimeoutTask != null)
                {
                    try
                    {
                        mTimeoutTask.Wait();
                        mTimeoutTask.Dispose();
                    }
                    catch { }
                }

                foreach (var lSession in mSessions) lSession.Dispose();

                foreach (var lSessionTask in mSessionTasks)
                {
                    try
                    {
                        lSessionTask.Wait();
                        lSessionTask.Dispose();
                    }
                    catch { }
                }

                if (mCancellationTokenSource != null)
                {
                    try { mCancellationTokenSource.Dispose(); }
                    catch { }
                }
            }

            public class cJob
            {
                public enum eType { expectdata, expecttagged, expectclose, delay, senddata, sendtagged, close }

                public readonly eType Type;
                public readonly byte[] Data;
                public readonly int Delay;

                public cJob(eType pType)
                {
                    Type = pType; // expectclose, close
                    Data = null;
                    Delay = 0;
                }

                public cJob(eType pType, string pData)
                {
                    Type = pType; // expectdata, senddata
                    Data = new byte[pData.Length];
                    for (int i = 0; i < pData.Length; i++) Data[i] = (byte)pData[i];
                    Delay = 0;
                }

                public cJob(eType pType, byte[] pData)
                {
                    Type = pType; // expectdata, senddata
                    Data = pData;
                    Delay = 0;
                }

                public cJob(int pDelay)
                {
                    Type = eType.delay; // expectclose, close
                    Data = null;
                    Delay = pDelay;
                }

                public override string ToString()
                {
                    StringBuilder lBuilder = new StringBuilder();

                    lBuilder.Append(Type);

                    if (Data != null)
                    {
                        lBuilder.Append(",");
                        foreach (var lByte in Data) lBuilder.Append((char)lByte);
                    }

                    if (Delay > 0)
                    {
                        lBuilder.Append(",");
                        lBuilder.Append(Delay);
                    }

                    return lBuilder.ToString();
                }
            }

            public class cJobList : IReadOnlyCollection<cJob>
            {
                private List<cJob> mJobs = new List<cJob>();

                public cJobList() { }

                public void AddExpectData(string pData) => mJobs.Add(new cJob(cJob.eType.expectdata, pData));
                public void AddExpectData(byte[] pData) => mJobs.Add(new cJob(cJob.eType.expectdata, pData));
                public void AddExpectTagged(string pData) => mJobs.Add(new cJob(cJob.eType.expecttagged, pData));
                public void AddExpectTagged(byte[] pData) => mJobs.Add(new cJob(cJob.eType.expecttagged, pData));
                public void AddExpectClose() => mJobs.Add(new cJob(cJob.eType.expectclose));
                public void AddDelay(int pDelay) => mJobs.Add(new cJob(pDelay));
                public void AddSendData(string pData) => mJobs.Add(new cJob(cJob.eType.senddata, pData));
                public void AddSendData(byte[] pData) => mJobs.Add(new cJob(cJob.eType.senddata, pData));
                public void AddSendTagged(string pData) => mJobs.Add(new cJob(cJob.eType.sendtagged, pData));
                public void AddClose() => mJobs.Add(new cJob(cJob.eType.close));

                public int Count => mJobs.Count;
                public IEnumerator<cJob> GetEnumerator() => mJobs.GetEnumerator();
                IEnumerator IEnumerable.GetEnumerator() => mJobs.GetEnumerator();

                public cJob this[int i] => mJobs[i];
            }

            private class cSession : IDisposable
            {
                private readonly TcpClient mClient;
                private readonly cJobList mJobs;
                private readonly CancellationTokenSource mCancellationTokenSource;
                private readonly NetworkStream mStream;

                public cSession(TcpClient pClient, cJobList pJobs, CancellationTokenSource pCancellationTokenSource)
                {
                    mClient = pClient;
                    mJobs = pJobs;
                    mCancellationTokenSource = pCancellationTokenSource;
                    mStream = pClient.GetStream();
                }

                public async Task RunAsync(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewRootMethod(nameof(cSession), nameof(RunAsync));

                    {
                        lContext.TraceVerbose("begin job list");
                        foreach (var lJob in mJobs) lContext.TraceVerbose(" {0}", lJob);
                        lContext.TraceVerbose("end job list");
                    }

                    try
                    {
                        int lJobNo = 0;
                        int lByteInLine = 0;
                        bool lReadTag = false;
                        List<byte> lTagBuilder = new List<byte>();
                        Stack<byte[]> lTagStack = new Stack<byte[]>();

                        byte[] lBuffer = new byte[1000];
                        int lBytesRead = 0;
                        int lByteInBuffer = 0;

                        cJob lJob = mJobs[lJobNo++];

                        while (true)
                        {
                            lContext.TraceVerbose("{0} {1}", lJobNo, lJob.Type);

                            if (lJob.Type == cJob.eType.close) break;

                            if (lJob.Type == cJob.eType.delay)
                            {
                                await Task.Delay(lJob.Delay, mCancellationTokenSource.Token).ConfigureAwait(false);

                                lJob = mJobs[lJobNo++];
                                goto next_task;
                            }

                            if (lJob.Type == cJob.eType.senddata)
                            {
                                List<byte> lBytes = new List<byte>();

                                foreach (var lByte in lJob.Data)
                                {
                                    if (lByte == 9) lBytes.AddRange(lTagStack.Peek());
                                    else lBytes.Add(lByte);
                                }

                                byte[] lData = lBytes.ToArray();

                                //lContext.TraceVerbose($"sending {new cBytes(lBytes)}");
                                await mStream.WriteAsync(lData, 0, lData.Length, mCancellationTokenSource.Token).ConfigureAwait(false);

                                lJob = mJobs[lJobNo++];
                                goto next_task;
                            }

                            if (lJob.Type == cJob.eType.sendtagged)
                            {
                                byte[] lTag = lTagStack.Pop();
                                //lContext.TraceVerbose($"sending {new cBytes(lTag)} {new cBytes(lTask.Data)}");
                                await mStream.WriteAsync(lTag, 0, lTag.Length, mCancellationTokenSource.Token).ConfigureAwait(false);
                                await mStream.WriteAsync(new byte[1] { 32 }, 0, 1, mCancellationTokenSource.Token).ConfigureAwait(false);
                                await mStream.WriteAsync(lJob.Data, 0, lJob.Data.Length, mCancellationTokenSource.Token).ConfigureAwait(false);
                                lJob = mJobs[lJobNo++];
                                goto next_task;
                            }

                            if (lJob.Type == cJob.eType.expectclose)
                            {
                                lBytesRead = await mStream.ReadAsync(lBuffer, 0, 1000).ConfigureAwait(false);

                                if (lBytesRead != 0)
                                {
                                    List<byte> lBytes = new List<byte>(lBytesRead);
                                    for (int i = 0; i < lBytesRead; i++) lBytes.Add(lBuffer[i]);
                                    throw new cTestsException($"expected close, got data", lContext);
                                }

                                break;
                            }

                            if (lJob.Type == cJob.eType.expecttagged && !lReadTag)
                            {
                                while (lByteInBuffer < lBytesRead)
                                {
                                    byte lByte = lBuffer[lByteInBuffer++];

                                    if (lByte < 48 || lByte > 57)
                                    {
                                        if (lTagBuilder.Count == 0) throw new cTestsException("expected tag", lContext);
                                        if (lByte != 32) throw new cTestsException("expected space to terminate tag", lContext);

                                        lTagStack.Push(lTagBuilder.ToArray());
                                        lTagBuilder.Clear();
                                        lReadTag = true;

                                        break;
                                    }

                                    lTagBuilder.Add(lByte);
                                }
                            }

                            while (lByteInBuffer < lBytesRead)
                            {
                                // this is case sensitive
                                if (lBuffer[lByteInBuffer++] != lJob.Data[lByteInLine++])
                                    throw new cTestsException($"received bytes don't match expectation, job {lJobNo}, position {lByteInLine}, text {lJob}", lContext);

                                if (lByteInLine == lJob.Data.Length)
                                {
                                    lJob = mJobs[lJobNo++];
                                    lByteInLine = 0;
                                    lReadTag = false;
                                    goto next_task;
                                }
                            }

                            lBytesRead = await mStream.ReadAsync(lBuffer, 0, 1000).ConfigureAwait(false);

                            if (lBytesRead == 0) throw new cTestsException("connection closed", lContext);

                            {
                                List<byte> lBytes = new List<byte>(lBytesRead);
                                for (int i = 0; i < lBytesRead; i++) lBytes.Add(lBuffer[i]);
                                //lContext.TraceVerbose($"read {new cBytes(lBytes)}");
                            }

                            lByteInBuffer = 0;

                            next_task:;
                        }
                    }
                    catch (Exception)
                    {
                        mCancellationTokenSource.Cancel();
                        throw;
                    }
                }

                public void Dispose()
                {
                    if (mStream != null)
                    {
                        try { mStream.Dispose(); }
                        catch { }
                    }

                    if (mClient != null)
                    {
                        try { mClient.Close(); }
                        catch { }
                    }
                }
            }
        }

        private class cResponseTextExpecter
        {
            private cTrace.cContext mContext;
            private List<cExpected> mExpected = new List<cExpected>();
            private int mCurrent = 0;
            private List<string> mUnexpected = new List<string>();

            public cResponseTextExpecter(cIMAPClient pClient, cTrace.cContext pParentContext)
            {
                mContext = pParentContext.NewObject(nameof(cResponseTextExpecter));
                pClient.ResponseText += ResponseText;
            }

            public void Expect(eResponseTextContext pType, eResponseTextCode pCode, string pText)
            {
                mExpected.Add(new cExpected(pType, pCode, pText));
            }

            public void Done()
            {
                if (mCurrent != mExpected.Count || mUnexpected.Count != 0) throw new cTestsException($"response text problem: {mCurrent}, {mUnexpected.Count}");
            }

            private void ResponseText(object sender, cResponseTextEventArgs e)
            {
                mContext.TraceVerbose("got responsetext: {0}", e);

                if (mCurrent == mExpected.Count)
                {
                    mUnexpected.Add(e.ToString());
                    return;
                }

                var lExpected = mExpected[mCurrent++];

                if (e.Context == lExpected.Context && e.Text.Code == lExpected.Code && e.Text.Text == lExpected.Text) return;

                mUnexpected.Add(e.ToString());
            }

            private class cExpected
            {
                public readonly eResponseTextContext Context;
                public readonly eResponseTextCode Code;
                public readonly string Text;

                public cExpected(eResponseTextContext pContext, eResponseTextCode pCode, string pText)
                {
                    Context = pContext;
                    Code = pCode;
                    Text = pText;
                }
            }
        }
    }

    internal class cTestsException : Exception
    {
        internal cTestsException() { }
        internal cTestsException(string pMessage) : base(pMessage) { }
        internal cTestsException(string pMessage, Exception pInner) : base(pMessage, pInner) { }
        internal cTestsException(string pMessage, cTrace.cContext pContext) : base(pMessage) => pContext.TraceError(pMessage);
        internal cTestsException(string pMessage, Exception pInner, cTrace.cContext pContext) : base(pMessage, pInner) => pContext.TraceError("{0}\n{1}", pMessage, pInner);
    }
}
