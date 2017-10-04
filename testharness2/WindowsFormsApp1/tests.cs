using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.imapclient;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace testharness2
{
    public static class cTests
    {
        private static Random mRandom = new Random();

        [Conditional("DEBUG")]
        public static void CurrentTest(cTrace.cContext pParentContext)
        {
            // quickly get to the test I'm working on
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(CurrentTest));
            //cIMAPClient._Tests(lContext);
            ZTestUIDFetch1(lContext);
        }

        [Conditional("DEBUG")]
        public static void Tests(bool pQuick, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(Tests));

            try
            {
                cIMAPClient._Tests(lContext);

                ZTestByeAtStartup1(cTrace.cContext.Null); // tests BYE at startup and ALERT
                ZTestByeAtStartup2(cTrace.cContext.Null); // tests BYE at startup and greeting
                ZTestByeAtStartup3(lContext); // tests BYE at startup with referral

                ZTestPreauthAtStartup1(lContext); // tests capability in greeting and logout
                ZTestPreauthAtStartup1_2(lContext); // tests utf8 id
                ZTestPreauthAtStartup1_3(lContext); // tests utf8 id 2
                ZTestPreauthAtStartup2(lContext); // tests capability command, enabling UTF8, information, warning and error messages, 
                ZTestPreauthAtStartup3(cTrace.cContext.Null); // tests that when there is nothing to enable that the enable isn't done

                ZTestAuthAtStartup1(cTrace.cContext.Null); // tests that connect fails when there are no credentials that can be used
                ZTestAuthAtStartup2(cTrace.cContext.Null); // tests login, literal+, capability response on successful login, disconnect
                ZTestAuthAtStartup3(cTrace.cContext.Null); // tests auth=plain, capability response on successful authenticate
                ZTestAuthAtStartup4(cTrace.cContext.Null); // tests auth failure falls back to login, synchronised literals, that failure to authenticate causes connect to fail, capability response where it shouldn't be, failure responses
                ZTestAuthAtStartup4_1(cTrace.cContext.Null); // tests referral terminates login sequence
                ZTestAuthAtStartup4_2(cTrace.cContext.Null); // tests authfailed terminates login sequence
                if (!pQuick) ZTestAuthAtStartup5(cTrace.cContext.Null); // tests capability is issued when there isn't one in the OK, tests IDLE
                ZTestAuthAtStartup5_1(cTrace.cContext.Null); // tests referral on an ok

                ZTestLiteralMinus(cTrace.cContext.Null); // tests literal-
                if (!pQuick) ZTestNonIdlePolling(cTrace.cContext.Null); // tests polling when idle is not available

                ZTestAuth1(lContext); // tests auth=anon and multiple auth methods with forced try
                ZTestSASLIR(lContext);



                //ZTestAuth3(lContext); // tests various weird conditions


                ZTestSearch1(lContext);
                ZTestSearch2(lContext);
                ZTestSearch3(lContext);
                if (!pQuick) ZTestIdleRestart(lContext);
                ZTestUIDFetch1(lContext);
            }
            catch (Exception e) when (lContext.TraceException(e)) { }
        }

        private static void ZTestByeAtStartup1(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestByeAtStartup1));

            cServer lServer = new cServer();
            lServer.AddSendData("* BYE [ALERT] this is the text\r\n");
            lServer.AddExpectClose();

            cIMAPClient lClient = new cIMAPClient("ZTestByeAtStartup1_cIMAPClient");
            lClient.SetServer("localhost");
            lClient.SetPlainCredentials("fred", "angus");

            cResponseTextExpecter lExpecter = new cResponseTextExpecter(lClient, lContext);
            lExpecter.Expect(eResponseTextType.greeting, eResponseTextCode.alert, "this is the text");

            Task lTask = null;

            try
            {
                lTask = lServer.RunAsync(lContext);

                try
                {
                    lClient.Connect();
                    throw new cTestsException("connect should have failed", lContext);
                }
                catch (cConnectByeException) { /* expected */ }

                if (lClient.HomeServerReferral != null) throw new cTestsException("referral should be null", lContext);

                if (!lTask.Wait(1000)) throw new cTestsException("session should be complete", lContext);
                if (lTask.IsFaulted) throw new cTestsException("server failed", lTask.Exception, lContext);
            }
            finally
            {
                ZFinally(lServer, lClient, lTask);
            }

            lExpecter.Done();
        }

        private static void ZTestByeAtStartup2(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestByeAtStartup2));

            cServer lServer = new cServer();
            lServer.AddSendData("* BYE this is the text\r\n");
            lServer.AddExpectClose();

            cIMAPClient lClient = new cIMAPClient("ZTestByeAtStartup2_cIMAPClient");
            lClient.SetServer("localhost");
            lClient.SetPlainCredentials("fred", "angus");

            cResponseTextExpecter lExpecter = new cResponseTextExpecter(lClient, lContext);
            lExpecter.Expect(eResponseTextType.greeting, eResponseTextCode.none, "this is the text");

            Task lTask = null;

            try
            {
                lTask = lServer.RunAsync(lContext);

                try
                {
                    lClient.Connect();
                    throw new cTestsException("connect should have failed", lContext);
                }
                catch (cConnectByeException) { /* expected */ }

                if (lClient.HomeServerReferral != null) throw new cTestsException("referral should be null", lContext);

                if (!lTask.Wait(1000)) throw new cTestsException("session should be complete", lContext);
                if (lTask.IsFaulted) throw new cTestsException("server failed", lTask.Exception, lContext);
            }
            finally
            {
                ZFinally(lServer, lClient, lTask);
            }

            lExpecter.Done();
        }

        private static void ZTestByeAtStartup3(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestByeAtStartup3));

            cServer lServer = new cServer();
            lServer.AddSendData("* BYE [REFERRAL IMAP://user;AUTH=*@SERVER2/] Server not accepting connections.Try SERVER2\r\n");
            lServer.AddExpectClose();

            cIMAPClient lClient = new cIMAPClient("ZTestByeAtStartup3_cIMAPClient");
            lClient.SetServer("localhost");
            lClient.SetPlainCredentials("fred", "angus");

            cResponseTextExpecter lExpecter = new cResponseTextExpecter(lClient, lContext);
            lExpecter.Expect(eResponseTextType.greeting, eResponseTextCode.referral, "Server not accepting connections.Try SERVER2");

            Task lTask = null;

            try
            {
                lTask = lServer.RunAsync(lContext);

                try
                {
                    lClient.Connect();
                    throw new cTestsException("connect should have failed", lContext);
                }
                catch (cHomeServerReferralException) { /* expected */ }

                if (lClient.HomeServerReferral == null) throw new cTestsException("referral should be set", lContext);
                if (lClient.HomeServerReferral.MustUseAnonymous || lClient.HomeServerReferral.UserId != "user" || lClient.HomeServerReferral.MechanismName != null || lClient.HomeServerReferral.Host != "SERVER2" || lClient.HomeServerReferral.Port != 143) throw new cTestsException("referral isn't what is expected", lContext);

                if (!lTask.Wait(1000)) throw new cTestsException("session should be complete", lContext);
                if (lTask.IsFaulted) throw new cTestsException("server failed", lTask.Exception, lContext);
            }
            finally
            {
                ZFinally(lServer, lClient, lTask);
            }

            lExpecter.Done();
        }

        private static void ZTestPreauthAtStartup1(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestPreauthAtStartup1));

            cServer lServer = new cServer();
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

            cIMAPClient lClient = new cIMAPClient("ZTestPreauthAtStartup1_cIMAPClient");
            lClient.SetServer("localhost");
            lClient.SetPlainCredentials("fred", "angus");

            cIdDictionary lIdDictionary = new cIdDictionary(false);
            lIdDictionary.Name = "fred";
            lClient.ClientId = lIdDictionary;

            cResponseTextExpecter lExpecter = new cResponseTextExpecter(lClient, lContext);
            lExpecter.Expect(eResponseTextType.greeting, eResponseTextCode.none, "this is the text");
            lExpecter.Expect(eResponseTextType.success, eResponseTextCode.none, "NAMESPACE command completed");
            lExpecter.Expect(eResponseTextType.success, eResponseTextCode.none, "ID command completed");
            lExpecter.Expect(eResponseTextType.bye, eResponseTextCode.none, "logging out");
            lExpecter.Expect(eResponseTextType.success, eResponseTextCode.none, "logged out");

            Task lTask = null;

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

            lExpecter.Done();

            if (lClient.ServerId.Name != "Cyrus") throw new cTestsException("serverid failure 1");
            if (lClient.ServerId.Count != 5) throw new cTestsException("serverid failure 2");

            if (lClient.Namespaces.Personal.Count != 1 || lClient.Namespaces.Personal[0].Prefix.Length != 0 || lClient.Namespaces.Personal[0].NamespaceName.Delimiter != '/') throw new cTestsException("namespace failure 1", lContext);
            if (lClient.Namespaces.OtherUsers.Count != 1 || lClient.Namespaces.OtherUsers[0].Prefix != "~" || lClient.Namespaces.OtherUsers[0].NamespaceName.Delimiter != '/') throw new cTestsException("namespace failure 1", lContext);
            if (lClient.Namespaces.Shared != null) throw new cTestsException("namespace failure 1", lContext);
        }

        private static void ZTestPreauthAtStartup1_2(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestPreauthAtStartup1_2));

            cServer lServer = new cServer();
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

            cIMAPClient lClient = new cIMAPClient("ZTestPreauthAtStartup1_2_cIMAPClient");
            lClient.SetServer("localhost");
            lClient.SetPlainCredentials("fred", "angus");

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
            lExpecter.Expect(eResponseTextType.greeting, eResponseTextCode.none, "this is the text");
            lExpecter.Expect(eResponseTextType.success, eResponseTextCode.none, "ID command completed");
            lExpecter.Expect(eResponseTextType.success, eResponseTextCode.none, "LIST command completed");
            lExpecter.Expect(eResponseTextType.bye, eResponseTextCode.none, "logging out");
            lExpecter.Expect(eResponseTextType.success, eResponseTextCode.none, "logged out");

            Task lTask = null;

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

            lExpecter.Done();

            if (lClient.ServerId.Name != "Cyrus") throw new cTestsException("serverid failure 1");
            if (lClient.ServerId.Count != 5) throw new cTestsException("serverid failure 2");
        }

        private static void ZTestPreauthAtStartup1_3(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestPreauthAtStartup1_3));

            cServer lServer = new cServer();
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

            cIMAPClient lClient = new cIMAPClient("ZTestPreauthAtStartup1_3_cIMAPClient");
            lClient.SetServer("localhost");
            lClient.SetPlainCredentials("fred", "angus", eTLSRequirement.indifferent);

            cIdDictionary lIdDictionary = new cIdDictionary(false);
            lIdDictionary.Name = "fr€d";
            lClient.ClientIdUTF8 = lIdDictionary;

            cResponseTextExpecter lExpecter = new cResponseTextExpecter(lClient, lContext);
            lExpecter.Expect(eResponseTextType.greeting, eResponseTextCode.none, "this is the text");
            //lExpecter.Expect(eResponseTextType.success, eResponseTextCode.none, "ID command completed");
            lExpecter.Expect(eResponseTextType.success, eResponseTextCode.none, "logged in");
            lExpecter.Expect(eResponseTextType.success, eResponseTextCode.none, "enable done");
            lExpecter.Expect(eResponseTextType.success, eResponseTextCode.none, "ID command completed");
            lExpecter.Expect(eResponseTextType.success, eResponseTextCode.none, "LIST command completed");
            lExpecter.Expect(eResponseTextType.bye, eResponseTextCode.none, "logging out");
            lExpecter.Expect(eResponseTextType.success, eResponseTextCode.none, "logged out");

            Task lTask = null;

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

            lExpecter.Done();

            if (lClient.ServerId.Count != 5) throw new cTestsException("expected 5 fields");
            if (lClient.ServerId.Name != "Cyrus") throw new cTestsException("expected cyrus");
            if (lClient.ServerId.SupportURL != "mailto:cyrus-bugs+@andr€w.cmu.€du") throw new cTestsException("expected UTF8 in the support URL");
            if (lClient.Namespaces.Personal.Count != 1 || lClient.Namespaces.OtherUsers != null || lClient.Namespaces.Shared != null || lClient.Namespaces.Personal[0].NamespaceName.Delimiter != null || lClient.Namespaces.Personal[0].Prefix != "") throw new cTestsException("namespace problem");
        }

        private static void ZTestPreauthAtStartup2(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestPreauthAtStartup2));

            cServer lServer = new cServer();

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

            cIMAPClient lClient = new cIMAPClient("ZTestPreauthAtStartup2_cIMAPClient");
            lClient.SetServer("localhost");
            lClient.SetPlainCredentials("fred", "angus");

            cResponseTextExpecter lExpecter = new cResponseTextExpecter(lClient, lContext);
            lExpecter.Expect(eResponseTextType.greeting, eResponseTextCode.none, "this is the text");
            lExpecter.Expect(eResponseTextType.success, eResponseTextCode.none, "capability done");
            lExpecter.Expect(eResponseTextType.information, eResponseTextCode.none, "information message");
            lExpecter.Expect(eResponseTextType.warning, eResponseTextCode.none, "warning message");
            lExpecter.Expect(eResponseTextType.protocolerror, eResponseTextCode.none, "error message");
            lExpecter.Expect(eResponseTextType.success, eResponseTextCode.none, "enable done");
            lExpecter.Expect(eResponseTextType.success, eResponseTextCode.none, "LIST command completed");
            lExpecter.Expect(eResponseTextType.bye, eResponseTextCode.none, "logging out");
            lExpecter.Expect(eResponseTextType.success, eResponseTextCode.none, "logged out");

            Task lTask = null;

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

            lExpecter.Done();

            if (lClient.Namespaces.Personal[0].NamespaceName.Delimiter != '/' || lClient.Namespaces.Personal[0].Prefix != "") throw new cTestsException("namespace problem");

        }

        private static void ZTestPreauthAtStartup3(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestPreauthAtStartup3));

            cServer lServer = new cServer();
            lServer.AddSendData("* PREAUTH [CAPABILITY ENABLE IDLE LITERAL+ IMAP4rev1 AUTH=PLAIN XSOMEFEATURE XSOMEOTHERFEATURE LOGINDISABLED] this is the text\r\n");
            lServer.AddExpectTagged("LIST \"\" \"\"\r\n");
            lServer.AddSendData("* LIST () \"/\" \"\"\r\n");
            lServer.AddSendTagged("OK LIST command completed\r\n");
            lServer.AddExpectTagged("LOGOUT\r\n");
            lServer.AddSendData("* BYE logging out\r\n");
            lServer.AddSendTagged("OK logged out\r\n");
            lServer.AddClose();

            cIMAPClient lClient = new cIMAPClient("ZTestPreauthAtStartup3_cIMAPClient");
            lClient.SetServer("localhost");
            lClient.SetPlainCredentials("fred", "angus");

            cResponseTextExpecter lExpecter = new cResponseTextExpecter(lClient, lContext);
            lExpecter.Expect(eResponseTextType.greeting, eResponseTextCode.none, "this is the text");
            lExpecter.Expect(eResponseTextType.success, eResponseTextCode.none, "LIST command completed");
            lExpecter.Expect(eResponseTextType.bye, eResponseTextCode.none, "logging out");
            lExpecter.Expect(eResponseTextType.success, eResponseTextCode.none, "logged out");

            Task lTask = null;

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

            lExpecter.Done();
        }

        private static void ZTestAuthAtStartup1(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestAuthAtStartup1));

            cServer lServer = new cServer();
            lServer.AddSendData("* OK [CAPABILITY ENABLE IDLE LITERAL+ IMAP4rev1 XSOMEFEATURE XSOMEOTHERFEATURE LOGINDISABLED] this is the text\r\n");
            lServer.AddExpectTagged("LOGOUT\r\n");
            lServer.AddSendData("* BYE logging out\r\n");
            lServer.AddSendTagged("OK logged out\r\n");
            lServer.AddClose();

            cIMAPClient lClient = new cIMAPClient("ZTestAuthAtStartup1_cIMAPClient");
            lClient.SetServer("localhost");
            lClient.SetPlainCredentials("fred", "angus");

            cResponseTextExpecter lExpecter = new cResponseTextExpecter(lClient, lContext);
            lExpecter.Expect(eResponseTextType.greeting, eResponseTextCode.none, "this is the text");
            lExpecter.Expect(eResponseTextType.bye, eResponseTextCode.none, "logging out");
            lExpecter.Expect(eResponseTextType.success, eResponseTextCode.none, "logged out");

            Task lTask = null;

            try
            {
                lTask = lServer.RunAsync(lContext);

                bool lFailed = false;
                try { lClient.Connect(); }
                catch (cAuthenticationMechanismsException) { lFailed = true; }
                if (!lFailed) throw new cTestsException("expected connect to fail");

                if (!lTask.Wait(1000)) throw new cTestsException("session should be complete", lContext);
                if (lTask.IsFaulted) throw new cTestsException("server failed", lTask.Exception, lContext);
            }
            finally
            {
                ZFinally(lServer, lClient, lTask);
            }

            lExpecter.Done();
        }

        private static void ZTestAuthAtStartup2(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestAuthAtStartup2));

            cServer lServer = new cServer();
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

            cIMAPClient lClient = new cIMAPClient("ZTestAuthAtStartup2_cIMAPClient");
            lClient.SetServer("localhost");
            lClient.SetPlainCredentials("fred", "angus", eTLSRequirement.indifferent);

            cResponseTextExpecter lExpecter = new cResponseTextExpecter(lClient, lContext);
            lExpecter.Expect(eResponseTextType.greeting, eResponseTextCode.none, "this is the text");
            lExpecter.Expect(eResponseTextType.success, eResponseTextCode.none, "logged in");
            lExpecter.Expect(eResponseTextType.success, eResponseTextCode.none, "LIST command completed");
            lExpecter.Expect(eResponseTextType.bye, eResponseTextCode.none, "logging out");
            lExpecter.Expect(eResponseTextType.success, eResponseTextCode.none, "logged out");

            Task lTask = null;

            try
            {
                lTask = lServer.RunAsync(lContext);

                lClient.Connect();

                if (lClient.HomeServerReferral != null) throw new cTestsException("referral should be null", lContext);

                lClient.Disconnect();

                if (!lTask.Wait(1000)) throw new cTestsException("session should be complete", lContext);
                if (lTask.IsFaulted) throw new cTestsException("server failed", lTask.Exception, lContext);
            }
            finally
            {
                ZFinally(lServer, lClient, lTask);
            }

            lExpecter.Done();
        }

        private static void ZTestAuthAtStartup3(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestAuthAtStartup3));

            cServer lServer = new cServer();
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

            cIMAPClient lClient = new cIMAPClient("ZTestAuthAtStartup3_cIMAPClient");
            lClient.SetServer("localhost");
            lClient.SetPlainCredentials("fred", "angus", eTLSRequirement.indifferent);

            cResponseTextExpecter lExpecter = new cResponseTextExpecter(lClient, lContext);
            lExpecter.Expect(eResponseTextType.greeting, eResponseTextCode.none, "this is the text");
            lExpecter.Expect(eResponseTextType.success, eResponseTextCode.none, "logged in");
            lExpecter.Expect(eResponseTextType.success, eResponseTextCode.none, "LIST command completed");
            lExpecter.Expect(eResponseTextType.bye, eResponseTextCode.none, "logging out");
            lExpecter.Expect(eResponseTextType.success, eResponseTextCode.none, "logged out");

            Task lTask = null;

            try
            {
                lTask = lServer.RunAsync(lContext);

                lClient.Connect();

                if (lClient.HomeServerReferral != null) throw new cTestsException("referral should be null", lContext);

                lClient.Disconnect();

                if (!lTask.Wait(1000)) throw new cTestsException("session should be complete", lContext);
                if (lTask.IsFaulted) throw new cTestsException("server failed", lTask.Exception, lContext);
            }
            finally
            {
                ZFinally(lServer, lClient, lTask);
            }

            lExpecter.Done();
        }

        private static void ZTestAuthAtStartup4(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestAuthAtStartup4));

            cServer lServer = new cServer();
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
            lServer.AddSendTagged("NO [CAPABILITY ENABLE IDLE LITERAL+ IMAP4rev1 XSOMEFEATURE XSOMEOTHERFEATURE] incorrect password again\r\n"); // should generate an error in the log file

            lServer.AddExpectTagged("LOGOUT\r\n");
            lServer.AddSendData("* BYE logging out\r\n");
            lServer.AddSendTagged("OK logged out\r\n");
            lServer.AddClose();

            cIMAPClient lClient = new cIMAPClient("ZTestAuthAtStartup4_cIMAPClient");
            lClient.SetServer("localhost");
            lClient.SetPlainCredentials("fred", "angus", eTLSRequirement.indifferent);

            cResponseTextExpecter lExpecter = new cResponseTextExpecter(lClient, lContext);
            lExpecter.Expect(eResponseTextType.greeting, eResponseTextCode.none, "this is the text");
            lExpecter.Expect(eResponseTextType.failure, eResponseTextCode.none, "incorrect password");
            lExpecter.Expect(eResponseTextType.continuerequest, eResponseTextCode.none, "ready");
            lExpecter.Expect(eResponseTextType.continuerequest, eResponseTextCode.none, "ready");
            lExpecter.Expect(eResponseTextType.failure, eResponseTextCode.none, "incorrect password again");
            lExpecter.Expect(eResponseTextType.bye, eResponseTextCode.none, "logging out");
            lExpecter.Expect(eResponseTextType.success, eResponseTextCode.none, "logged out");

            Task lTask = null;

            try
            {
                lTask = lServer.RunAsync(lContext);

                bool lFailed = false;
                try { lClient.Connect(); }
                catch (cCredentialsException) { lFailed = true; }
                if (!lFailed) throw new cTestsException("expected connect to fail");

                if (!lTask.Wait(1000)) throw new cTestsException("session should be complete", lContext);
                if (lTask.IsFaulted) throw new cTestsException("server failed", lTask.Exception, lContext);
            }
            finally
            {
                ZFinally(lServer, lClient, lTask);
            }

            lExpecter.Done();
        }

        private static void ZTestAuthAtStartup4_1(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestAuthAtStartup4_1));

            cServer lServer = new cServer();
            lServer.AddSendData("* OK [CAPABILITY ENABLE IDLE IMAP4rev1 XSOMEFEATURE AUTH=PLAIN XSOMEOTHERFEATURE LOGIN-REFERRALS] this is the text\r\n");

            lServer.AddExpectTagged("AUTHENTICATE PLAIN\r\n");
            lServer.AddSendData("+ \r\n");
            lServer.AddExpectData("AGZyZWQAYW5ndXM=\r\n");
            lServer.AddSendTagged("NO [REFERRAL IMAP://user;AUTH=GSSAPI@SERVER2/] Specified user is invalid on this server.Try SERVER2.\r\n");

            lServer.AddExpectTagged("LOGOUT\r\n");
            lServer.AddSendData("* BYE logging out\r\n");
            lServer.AddSendTagged("OK logged out\r\n");
            lServer.AddClose();

            cIMAPClient lClient = new cIMAPClient("ZTestAuthAtStartup4_1_cIMAPClient");
            lClient.SetServer("localhost");
            lClient.SetPlainCredentials("fred", "angus", eTLSRequirement.indifferent);

            cResponseTextExpecter lExpecter = new cResponseTextExpecter(lClient, lContext);
            lExpecter.Expect(eResponseTextType.greeting, eResponseTextCode.none, "this is the text");
            lExpecter.Expect(eResponseTextType.failure, eResponseTextCode.referral, "Specified user is invalid on this server.Try SERVER2.");
            lExpecter.Expect(eResponseTextType.bye, eResponseTextCode.none, "logging out");
            lExpecter.Expect(eResponseTextType.success, eResponseTextCode.none, "logged out");

            Task lTask = null;

            try
            {
                lTask = lServer.RunAsync(lContext);

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

                if (!lTask.Wait(1000)) throw new cTestsException("session should be complete", lContext);
                if (lTask.IsFaulted) throw new cTestsException("server failed", lTask.Exception, lContext);
            }
            finally
            {
                ZFinally(lServer, lClient, lTask);
            }

            lExpecter.Done();
        }

        private static void ZTestAuthAtStartup4_2(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestAuthAtStartup4_2));

            cServer lServer = new cServer();
            lServer.AddSendData("* OK [CAPABILITY ENABLE IDLE IMAP4rev1 XSOMEFEATURE AUTH=PLAIN XSOMEOTHERFEATURE] this is the text\r\n");

            lServer.AddExpectTagged("AUTHENTICATE PLAIN\r\n");
            lServer.AddSendData("+ \r\n");
            lServer.AddExpectData("AGZyZWQAYW5ndXM=\r\n");
            lServer.AddSendTagged("NO [AUTHENTICATIONFAILED] incorrect password\r\n");

            lServer.AddExpectTagged("LOGOUT\r\n");
            lServer.AddSendData("* BYE logging out\r\n");
            lServer.AddSendTagged("OK logged out\r\n");
            lServer.AddClose();

            cIMAPClient lClient = new cIMAPClient("ZTestAuthAtStartup4_2_cIMAPClient");
            lClient.SetServer("localhost");
            lClient.SetPlainCredentials("fred", "angus", eTLSRequirement.indifferent);

            cResponseTextExpecter lExpecter = new cResponseTextExpecter(lClient, lContext);
            lExpecter.Expect(eResponseTextType.greeting, eResponseTextCode.none, "this is the text");
            lExpecter.Expect(eResponseTextType.failure, eResponseTextCode.authenticationfailed, "incorrect password");
            lExpecter.Expect(eResponseTextType.bye, eResponseTextCode.none, "logging out");
            lExpecter.Expect(eResponseTextType.success, eResponseTextCode.none, "logged out");

            Task lTask = null;

            try
            {
                lTask = lServer.RunAsync(lContext);

                bool lFailed = false;
                try { lClient.Connect(); }
                catch (cCredentialsException) { lFailed = true; }
                if (!lFailed) throw new cTestsException("expected connect to fail");

                if (!lTask.Wait(1000)) throw new cTestsException("session should be complete", lContext);
                if (lTask.IsFaulted) throw new cTestsException("server failed", lTask.Exception, lContext);
            }
            finally
            {
                ZFinally(lServer, lClient, lTask);
            }

            lExpecter.Done();
        }

        private static void ZTestAuthAtStartup5(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestAuthAtStartup5));

            cServer lServer = new cServer();
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

            cIMAPClient lClient = new cIMAPClient("ZTestAuthAtStartup5_cIMAPClient");
            lClient.SetServer("localhost");
            lClient.SetPlainCredentials("fred", "angus", eTLSRequirement.indifferent);
            lClient.IdleConfiguration = new cIdleConfiguration(2000, 10000);

            cResponseTextExpecter lExpecter = new cResponseTextExpecter(lClient, lContext);
            lExpecter.Expect(eResponseTextType.greeting, eResponseTextCode.none, "this is the text");
            lExpecter.Expect(eResponseTextType.success, eResponseTextCode.none, "logged in");
            lExpecter.Expect(eResponseTextType.success, eResponseTextCode.none, "capability done");
            lExpecter.Expect(eResponseTextType.success, eResponseTextCode.none, "LIST command completed");
            lExpecter.Expect(eResponseTextType.continuerequest, eResponseTextCode.none, "idling");
            lExpecter.Expect(eResponseTextType.information, eResponseTextCode.none, "information message");
            lExpecter.Expect(eResponseTextType.warning, eResponseTextCode.none, "warning message");
            lExpecter.Expect(eResponseTextType.protocolerror, eResponseTextCode.none, "error message");
            lExpecter.Expect(eResponseTextType.success, eResponseTextCode.none, "idle terminated");
            lExpecter.Expect(eResponseTextType.continuerequest, eResponseTextCode.none, "idling");
            lExpecter.Expect(eResponseTextType.bye, eResponseTextCode.none, "unilateral bye");

            Task lTask = null;

            try
            {
                lTask = lServer.RunAsync(lContext);

                lClient.Connect();

                if (!lTask.Wait(60000)) throw new cTestsException("session should be complete", lContext);
                if (lTask.IsFaulted) throw new cTestsException("server failed", lTask.Exception, lContext);

                bool lFailed = false;
                try { lClient.Disconnect(); }
                catch
                {
                    lContext.TraceVerbose($"disconnect failed as expected:\n");
                    lFailed = true;
                }

                if (!lFailed) throw new cTestsException("disconnect should have failed");
            }
            finally
            {
                ZFinally(lServer, lClient, lTask);
            }

            lExpecter.Done();
        }

        private static void ZTestAuthAtStartup5_1(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestAuthAtStartup5_1));

            cServer lServer = new cServer();
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

            cIMAPClient lClient = new cIMAPClient("ZTestAuthAtStartup5_1_cIMAPClient");
            lClient.SetServer("localhost");
            lClient.SetPlainCredentials("fred", "angus", eTLSRequirement.indifferent);

            Task lTask = null;

            try
            {
                lTask = lServer.RunAsync(lContext);

                try { lClient.Connect(); }
                catch { }

                if (lClient.HomeServerReferral == null) throw new cTestsException("referral should be set", lContext);
                if (lClient.HomeServerReferral.MustUseAnonymous || lClient.HomeServerReferral.UserId != "MATTHEW" || lClient.HomeServerReferral.MechanismName != null || lClient.HomeServerReferral.Host != "SERVER2" || lClient.HomeServerReferral.Port != 143) throw new cTestsException("referral isn't what is expected", lContext);

                bool lFailed = false;
                try { lClient.Poll(); }
                catch (Exception e)
                {
                    lContext.TraceVerbose($"disconnect failed as expected:\n{e}");
                    lFailed = true;
                }
                if (!lFailed) throw new cTestsException("poll should have failed");

                if (!lTask.Wait(60000)) throw new cTestsException("session should be complete", lContext);
                if (lTask.IsFaulted) throw new cTestsException("server failed", lTask.Exception, lContext);

            }
            finally
            {
                ZFinally(lServer, lClient, lTask);
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

            cServer lServer = new cServer();
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
            lServer.AddClose();

            cIMAPClient lClient = new cIMAPClient("ZTestLiteralMinusWorker_cIMAPClient");
            lClient.SetServer("localhost");
            lClient.SetPlainCredentials(pUserId, pPassword, eTLSRequirement.indifferent);

            Task lTask = null;

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
        }

        private static void ZTestNonIdlePolling(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestNonIdlePolling));

            cServer lServer = new cServer();
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


            cIMAPClient lClient = new cIMAPClient("ZTestNonIdlePolling_cIMAPClient");
            lClient.SetServer("localhost");
            lClient.SetPlainCredentials("fred", "angus", eTLSRequirement.indifferent);
            lClient.IdleConfiguration = new cIdleConfiguration(2000, 1200000, 7000);

            cResponseTextExpecter lExpecter = new cResponseTextExpecter(lClient, lContext);
            lExpecter.Expect(eResponseTextType.greeting, eResponseTextCode.none, "this is the text");
            lExpecter.Expect(eResponseTextType.success, eResponseTextCode.none, "logged in");
            lExpecter.Expect(eResponseTextType.success, eResponseTextCode.none, "capability done");
            lExpecter.Expect(eResponseTextType.success, eResponseTextCode.none, "LIST command completed");
            lExpecter.Expect(eResponseTextType.information, eResponseTextCode.none, "information message");
            lExpecter.Expect(eResponseTextType.warning, eResponseTextCode.none, "warning message");
            lExpecter.Expect(eResponseTextType.protocolerror, eResponseTextCode.none, "error message");
            lExpecter.Expect(eResponseTextType.success, eResponseTextCode.none, "noop completed");
            lExpecter.Expect(eResponseTextType.information, eResponseTextCode.none, "information message");
            lExpecter.Expect(eResponseTextType.warning, eResponseTextCode.none, "warning message");
            lExpecter.Expect(eResponseTextType.protocolerror, eResponseTextCode.none, "error message");
            lExpecter.Expect(eResponseTextType.success, eResponseTextCode.none, "noop completed");
            lExpecter.Expect(eResponseTextType.bye, eResponseTextCode.none, "unilateral bye");

            Task lTask = null;

            try
            {
                lTask = lServer.RunAsync(lContext);

                lClient.Connect();

                if (!lTask.Wait(60000)) throw new cTestsException("session should be complete", lContext);
                if (lTask.IsFaulted) throw new cTestsException("server failed", lTask.Exception, lContext);

                bool lFailed = false;
                try { lClient.Disconnect(); }
                catch (Exception e)
                {
                    lContext.TraceVerbose($"disconnect failed as expected:\n{e}");
                    lFailed = true;
                }
                if (!lFailed) throw new cTestsException("disconnect should have failed");
            }
            finally
            {
                ZFinally(lServer, lClient, lTask);
            }

            lExpecter.Done();
        }

        private class cTestAuth1Creds : cCredentials
        {
            public cTestAuth1Creds(bool pTryAllSASLs) : base("fred", null, pTryAllSASLs)
            {
                mSASLs.Add(new cSASLPlain("fred", "angus", eTLSRequirement.indifferent));
                mSASLs.Add(new cSASLAnonymous("fr€d", eTLSRequirement.indifferent));
            }
        }

        private static void ZTestAuth1(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestAuth1));

            cTestAuth1Creds lCredsFalse = new cTestAuth1Creds(false);
            cTestAuth1Creds lCredsTrue = new cTestAuth1Creds(true);

            cServer lServer;
            cIMAPClient lClient;
            Task lTask;
            bool lFailed;



            // 1 - mechanisms not advertised

            lServer = new cServer();
            lServer.AddSendData("* OK [CAPABILITY ENABLE IDLE LITERAL+ IMAP4rev1 XSOMEFEATURE XSOMEOTHERFEATURE] this is the text\r\n");
            lServer.AddExpectTagged("LOGOUT\r\n");
            lServer.AddSendData("* BYE logging out\r\n");
            lServer.AddSendTagged("OK logged out\r\n");
            lServer.AddClose();

            lClient = new cIMAPClient("ZTestAuth1_1_cIMAPClient");
            lClient.SetServer("localhost");
            lClient.Credentials = lCredsFalse;

            lTask = null;

            try
            {
                lTask = lServer.RunAsync(lContext);

                lFailed = false;
                try { lClient.Connect(); }
                catch (cAuthenticationMechanismsException) { lFailed = true; }
                if (!lFailed) throw new cTestsException("should have failed to connect", lContext);

                if (!lTask.Wait(1000)) throw new cTestsException("session should be complete", lContext);
                if (lTask.IsFaulted) throw new cTestsException("server failed", lTask.Exception, lContext);
            }
            finally
            {
                ZFinally(lServer, lClient, lTask);
            }


            // 2 - just anon advertised

            lServer = new cServer();
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

            lClient = new cIMAPClient("ZTestAuth1_2_cIMAPClient");
            lClient.SetServer("localhost");
            lClient.Credentials = lCredsFalse;

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




            // 3 - both advertised

            lServer = new cServer();
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

            lClient = new cIMAPClient("ZTestAuth1_3_cIMAPClient");
            lClient.SetServer("localhost");
            lClient.Credentials = lCredsFalse;

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


            // 4 - mechanisms not advertised but force try on

            lServer = new cServer();
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

            lClient = new cIMAPClient("ZTestAuth1_4_cIMAPClient");
            lClient.SetServer("localhost");
            lClient.Credentials = lCredsTrue;

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


            // 5 - mechanisms not advertised but force try on and plain succeeds

            lServer = new cServer();
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

            lClient = new cIMAPClient("ZTestAuth1_5_cIMAPClient");
            lClient.SetServer("localhost");
            lClient.Credentials = lCredsTrue;

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








            /*

            ???; // try continuing twice

            // 5 - mechanisms not advertised but force try on and plain succeeds

            lServer = new cServer();
            lServer.AddSendData("* OK [CAPABILITY ENABLE IDLE LITERAL+ IMAP4rev1 XSOMEFEATURE XSOMEOTHERFEATURE] this is the text\r\n");

            lServer.AddExpectTagged("AUTHENTICATE PLAIN\r\n");
            lServer.AddSendData("+ \r\n");
            lServer.AddExpectData("AGZyZWQAYW5ndXM=\r\n");
            lServer.AddSendData("+ \r\n");
            lServer.AddSendTagged("OK [CAPABILITY ENABLE IDLE LITERAL+ IMAP4rev1 XSOMEFEATURE XSOMEOTHERFEATURE] why not try anonymous\r\n");

            lServer.AddExpectTagged("LOGOUT\r\n");
            lServer.AddSendData("* BYE logging out\r\n");
            lServer.AddSendTagged("OK logged out\r\n");
            lServer.AddClose();

            lClient = new cIMAPClient("ZTestAuth1_5_cIMAPClient");

            lTask = null;

            try
            {
                lTask = lServer.RunAsync(lContext);

                lClient.Connect("localhost", 143, false, lCredsTrue);
                lClient.Disconnect();

                if (!lTask.Wait(1000)) throw new cTestsException("session should be complete", lContext);
                if (lTask.IsFaulted) throw new cTestsException("server failed", lTask.Exception, lContext);
            }
            finally
            {
                ZFinally(lServer, lClient, lTask);
            }

*/


        }

        private static void ZTestSASLIR(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestSASLIR));

            cServer lServer;
            cIMAPClient lClient;
            Task lTask;

            // 1

            lServer = new cServer();
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


        }

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


        }

        private static void ZTestSearch1(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestSearch1));

            cServer lServer = new cServer();
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

            /*
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
            lServer.AddSendTagged("OK SEARCH completed\r\n"); */



            lServer.AddExpectTagged("LOGOUT\r\n");
            lServer.AddSendData("* BYE logging out\r\n");
            lServer.AddSendTagged("OK logged out\r\n");
            lServer.AddExpectClose();


            cIMAPClient lClient = new cIMAPClient("ZTestSearch1_cIMAPClient");
            lClient.SetServer("localhost");
            lClient.SetNoCredentials();
            lClient.IdleConfiguration = null;

            Task lTask = null;

            try
            {
                cMessageFlags lFlags;
                cMailbox lMailbox;
                List<cMessage> lMessageList;
                cMessage lMessage;
                /*Task<List<cMessage>> lTask1;
                Task<List<cMessage>> lTask2;
                Task<List<cMessage>> lTask3; */


                lTask = lServer.RunAsync(lContext);

                lClient.MailboxCacheData = fMailboxCacheData.messagecount | fMailboxCacheData.unseencount | fMailboxCacheData.uidnext;

                lClient.Connect();

                if (lClient.Inbox.IsSelected) throw new cTestsException("ZTestSearch1.1");

                lClient.Inbox.Select(true);

                if (!lClient.Inbox.IsSelected) throw new cTestsException("ZTestSearch1.2");

                if (lClient.Inbox.MessageCount != 172 || lClient.Inbox.RecentCount != 1 || lClient.Inbox.UIDNext != 4392 || lClient.Inbox.UIDValidity != 3857529045 || lClient.Inbox.UnseenCount != 0 || lClient.Inbox.UnseenUnknownCount != 172) throw new cTestsException("ZTestSearch1.3");

                lFlags = lClient.Inbox.MessageFlags;
                if (lFlags.Count != 5 || !lFlags.Contains(kMessageFlagName.Answered) || !lFlags.Contains(kMessageFlagName.Flagged) || !lFlags.Contains(kMessageFlagName.Deleted) || !lFlags.Contains(kMessageFlagName.Seen) || !lFlags.Contains(kMessageFlagName.Draft)) throw new cTestsException("ZTestSearch1.4");

                lFlags = lClient.Inbox.ForUpdatePermanentFlags;
                if (lFlags.Count != 3 || !lFlags.Contains(kMessageFlagName.Deleted) || !lFlags.Contains(kMessageFlagName.Seen) || !lFlags.Contains(kMessageFlagName.CreateNewIsPossible) || lFlags.Contains(kMessageFlagName.Draft) || lFlags.Contains(kMessageFlagName.Flagged)) throw new cTestsException("ZTestSearch1.5");

                if (!lClient.Inbox.IsSelectedForUpdate) throw new cTestsException("ZTestSearch1.6");

                bool lFailed = false;
                try { lClient.Inbox.SetUnseen(); }
                catch (cUIDValidityChangedException) { lFailed = true; }
                if (!lFailed) throw new cTestsException("ZTestSearch1.7");

                lClient.Inbox.SetUnseen();
                if (lClient.Inbox.MessageCount != 172 || lClient.Inbox.RecentCount != 1 || lClient.Inbox.UIDNext != 0 || lClient.Inbox.UIDNextUnknownCount != 172 || lClient.Inbox.UIDValidity != 3857529046 || lClient.Inbox.UnseenCount != 3 || lClient.Inbox.UnseenUnknownCount != 0) throw new cTestsException("ZTestSearch1.8");

                lMailbox = lClient.Mailbox(new cMailboxName("blurdybloop", null));
                if (lMailbox.IsSelected) throw new cTestsException("ZTestSearch2.1");

                if (lMailbox.MessageCount != 231 || lMailbox.UIDNext != 44292) throw new cTestsException("ZTestSearch2.2");

                lMailbox.Fetch(fMailboxCacheDataSets.status);
                if (lMailbox.MessageCount != 232 || lMailbox.UnseenCount != 3 || lMailbox.UIDNext != 44293) throw new cTestsException("ZTestSearch2.3");

                lMailbox.Select();
                if (lClient.Inbox.IsSelected || !lMailbox.IsSelected) throw new cTestsException("ZTestSearch3.1");

                if (lMailbox.MessageCount != 17 || lMailbox.RecentCount != 2 || lMailbox.UIDNext != 4392 || lMailbox.UIDValidity != 3857529045 || lMailbox.UnseenCount != 0 || lMailbox.UnseenUnknownCount != 17) throw new cTestsException("ZTestSearch3.2");

                lFlags = lMailbox.MessageFlags;
                if (lFlags.Count != 5 || !lFlags.Contains(kMessageFlagName.Answered) || !lFlags.Contains(kMessageFlagName.Flagged) || !lFlags.Contains(kMessageFlagName.Deleted) || !lFlags.Contains(kMessageFlagName.Seen) || !lFlags.Contains(kMessageFlagName.Draft)) throw new cTestsException("ZTestSearch3.3");

                lFlags = lMailbox.ReadOnlyPermanentFlags;
                if (lFlags.Count != 0) throw new cTestsException("ZTestSearch3.4");

                if (lMailbox.IsSelectedForUpdate) throw new cTestsException("ZTestSearch3.5");

                lMailbox.SetUnseen();
                if (lMailbox.UnseenCount != 3 || lMailbox.UnseenUnknownCount != 0) throw new cTestsException("ZTestSearch3.7");

                lMessageList = lMailbox.Messages(cFilter.Received >= new DateTime(2017, 6, 8));
                if (lMessageList.Count != 3) throw new cTestsException("ZTestSearch4.1");

                lMessageList = lMailbox.Messages(cFilter.Received >= new DateTime(2017, 6, 8), null, fCacheAttributes.received);
                if (lMessageList.Count != 3) throw new cTestsException("ZTestSearch4.2");
                foreach (var lItem in lMessageList) if (lItem.Indent != -1) throw new cTestsException("ZTestSearch4.3");

                lMessage = lMessageList[0];

                if (lMessage.IsExpunged || lMessage.Handle.Attributes != (fCacheAttributes.received | fCacheAttributes.modseq)) throw new cTestsException("ZTestSearch4.4");
                if (lMessage.Received != new DateTime(2017, 6, 8, 20, 09, 15)) throw new cTestsException("ZTestSearch4.5");




                lMessageList = lMailbox.Messages(cFilter.Received >= new DateTime(2017, 6, 7), new cSort(cSortItem.ReceivedDesc));

                if (lMessageList.Count != 6) throw new cTestsException("ZTestSearch5.1");
                if (lMessageList[0].Handle.CacheSequence != 16 || lMessageList[1].Handle.CacheSequence != 15 || lMessageList[2].Handle.CacheSequence != 14 ||
                    lMessageList[3].Handle.CacheSequence != 12 || lMessageList[4].Handle.CacheSequence != 13 || lMessageList[5].Handle.CacheSequence != 11) throw new cTestsException("ZTestSearch5.2");

                /*
                lMessageList = lMailbox.Messages(cFilter.Received >= new DateTime(2017, 6, 7), new cSort(cSortItem.ReceivedDesc));

                if (lMessageList.Count != 6) throw new cTestsException("ZTestSearch6.1");
                if (lMessageList[0].Handle.CacheSequence != 16 || lMessageList[1].Handle.CacheSequence != 15 || lMessageList[2].Handle.CacheSequence != 14 ||
                    lMessageList[3].Handle.CacheSequence != 12 || lMessageList[4].Handle.CacheSequence != 13 || lMessageList[5].Handle.CacheSequence != 11) throw new cTestsException("ZTestSearch6.2");
                    

                lTask1 = lMailbox.MessagesAsync(cFilter.Received >= new DateTime(2017, 6, 7));
                lTask2 = lMailbox.MessagesAsync(cFilter.Received >= new DateTime(2017, 6, 8), new cSort(cSortItem.ReceivedDesc));

                Task.WaitAll(lTask1, lTask2);

                lMessageList = lTask1.Result;
                if (lMessageList.Count != 6) throw new cTestsException("ZTestSearch7.1");

                lMessageList = lTask2.Result;
                if (lMessageList.Count != 3) throw new cTestsException("ZTestSearch7.2"); 


                // this checks that the search commands lock one another out ...

                lTask1 = lMailbox.MessagesAsync(cFilter.Received >= new DateTime(2017, 6, 7));
                lTask2 = lMailbox.MessagesAsync(cFilter.Received >= new DateTime(2017, 6, 8));
                lTask3 = lMailbox.MessagesAsync(cFilter.Received >= new DateTime(2017, 6, 8), new cSort(cSortItem.ReceivedDesc));

                Task.WaitAll(lTask1, lTask2, lTask3); */






                lClient.Disconnect();

                if (!lTask.Wait(1000)) throw new cTestsException("session should be complete", lContext);
                if (lTask.IsFaulted) throw new cTestsException("server failed", lTask.Exception, lContext);
            }
            finally
            {
                ZFinally(lServer, lClient, lTask);
            }

        }

        private static void ZTestSearch2(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestSearch2));

            cServer lServer = new cServer();
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

            /*
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
            lServer.AddSendTagged("OK SEARCH completed\r\n");*/



            lServer.AddExpectTagged("LOGOUT\r\n");
            lServer.AddSendData("* BYE logging out\r\n");
            lServer.AddSendTagged("OK logged out\r\n");
            lServer.AddExpectClose();


            cIMAPClient lClient = new cIMAPClient("ZTestSearch2_cIMAPClient");
            lClient.SetServer("localhost");
            lClient.SetNoCredentials();
            lClient.IdleConfiguration = null;

            Task lTask = null;

            try
            {
                cMessageFlags lFlags;
                cMailbox lMailbox;
                List<cMessage> lMessageList;
                cMessage lMessage;
                /*Task<List<cMessage>> lTask1;
                Task<List<cMessage>> lTask2;
                Task<List<cMessage>> lTask3; */


                lTask = lServer.RunAsync(lContext);

                lClient.MailboxCacheData = fMailboxCacheData.messagecount | fMailboxCacheData.unseencount | fMailboxCacheData.uidnext;

                lClient.Connect();

                if (lClient.Inbox.IsSelected) throw new cTestsException("ZTestSearch2_1.1");

                lClient.Inbox.Select(true);

                if (!lClient.Inbox.IsSelected) throw new cTestsException("ZTestSearch2_1.2");

                if (lClient.Inbox.MessageCount != 172 || lClient.Inbox.RecentCount != 1 || lClient.Inbox.UIDNext != 4392 || lClient.Inbox.UIDValidity != 3857529045 || lClient.Inbox.UnseenCount != 0 || lClient.Inbox.UnseenUnknownCount != 172) throw new cTestsException("ZTestSearch1.3");

                lFlags = lClient.Inbox.MessageFlags;
                if (lFlags.Count != 5 || !lFlags.Contains(kMessageFlagName.Answered) || !lFlags.Contains(kMessageFlagName.Flagged) || !lFlags.Contains(kMessageFlagName.Deleted) || !lFlags.Contains(kMessageFlagName.Seen) || !lFlags.Contains(kMessageFlagName.Draft)) throw new cTestsException("ZTestSearch2_1.4");

                lFlags = lClient.Inbox.ForUpdatePermanentFlags;
                if (lFlags.Count != 3 || !lFlags.Contains(kMessageFlagName.Deleted) || !lFlags.Contains(kMessageFlagName.Seen) || !lFlags.Contains(kMessageFlagName.CreateNewIsPossible) || lFlags.Contains(kMessageFlagName.Draft) || lFlags.Contains(kMessageFlagName.Flagged)) throw new cTestsException("ZTestSearch2_1.5");

                if (!lClient.Inbox.IsSelectedForUpdate) throw new cTestsException("ZTestSearch2_1.6");

                bool lFailed = false;
                try { lClient.Inbox.SetUnseen(); }
                catch (cUIDValidityChangedException) { lFailed = true; }
                if (!lFailed) throw new cTestsException("ZTestSearch2_1.7");

                lClient.Inbox.SetUnseen();
                if (lClient.Inbox.MessageCount != 172 || lClient.Inbox.RecentCount != 1 || lClient.Inbox.UIDNext != 0 || lClient.Inbox.UIDNextUnknownCount != 172 || lClient.Inbox.UIDValidity != 3857529046 || lClient.Inbox.UnseenCount != 3 || lClient.Inbox.UnseenUnknownCount != 0) throw new cTestsException("ZTestSearch2_1.8");

                lMailbox = lClient.Mailbox(new cMailboxName("blurdybloop", null));
                if (lMailbox.IsSelected) throw new cTestsException("ZTestSearch2_2.1");

                if (lMailbox.MessageCount != 231 || lMailbox.UIDNext != 44292) throw new cTestsException("ZTestSearch2_2.2");

                lMailbox.Fetch(fMailboxCacheDataSets.status);
                if (lMailbox.MessageCount != 232 || lMailbox.UnseenCount != 3 || lMailbox.UIDNext != 44293) throw new cTestsException("ZTestSearch2_2.3");

                lMailbox.Select();
                if (lClient.Inbox.IsSelected || !lMailbox.IsSelected) throw new cTestsException("ZTestSearch2_3.1");

                if (lMailbox.MessageCount != 17 || lMailbox.RecentCount != 2 || lMailbox.UIDNext != 4392 || lMailbox.UIDValidity != 3857529045 || lMailbox.UnseenCount != 0 || lMailbox.UnseenUnknownCount != 17) throw new cTestsException("ZTestSearch2_3.2");

                lFlags = lMailbox.MessageFlags;
                if (lFlags.Count != 5 || !lFlags.Contains(kMessageFlagName.Answered) || !lFlags.Contains(kMessageFlagName.Flagged) || !lFlags.Contains(kMessageFlagName.Deleted) || !lFlags.Contains(kMessageFlagName.Seen) || !lFlags.Contains(kMessageFlagName.Draft)) throw new cTestsException("ZTestSearch2_3.3");

                lFlags = lMailbox.ReadOnlyPermanentFlags;
                if (lFlags.Count != 0) throw new cTestsException("ZTestSearch2_3.4");

                if (lMailbox.IsSelectedForUpdate) throw new cTestsException("ZTestSearch2_3.5");

                lMailbox.SetUnseen();
                if (lMailbox.UnseenCount != 3 || lMailbox.UnseenUnknownCount != 0) throw new cTestsException("ZTestSearch2_3.7");

                lMessageList = lMailbox.Messages(cFilter.Received >= new DateTime(2017, 6, 8));
                if (lMessageList.Count != 3) throw new cTestsException("ZTestSearch2_4.1");

                lMessageList = lMailbox.Messages(cFilter.Received >= new DateTime(2017, 6, 8), null, fCacheAttributes.received);
                if (lMessageList.Count != 3) throw new cTestsException("ZTestSearch2_4.2");
                foreach (var lItem in lMessageList) if (lItem.Indent != -1) throw new cTestsException("ZTestSearch2_4.3");

                lMessage = lMessageList[0];

                if (lMessage.IsExpunged || lMessage.Handle.Attributes != (fCacheAttributes.received | fCacheAttributes.modseq)) throw new cTestsException("ZTestSearch2_4.4");
                if (lMessage.Received != new DateTime(2017, 6, 8, 20, 09, 15)) throw new cTestsException("ZTestSearch2_4.5");




                lMessageList = lMailbox.Messages(cFilter.Received >= new DateTime(2017, 6, 7), new cSort(cSortItem.ReceivedDesc));

                if (lMessageList.Count != 6) throw new cTestsException("ZTestSearch2_5.1");
                if (lMessageList[0].Handle.CacheSequence != 16 || lMessageList[1].Handle.CacheSequence != 15 || lMessageList[2].Handle.CacheSequence != 14 ||
                    lMessageList[3].Handle.CacheSequence != 12 || lMessageList[4].Handle.CacheSequence != 13 || lMessageList[5].Handle.CacheSequence != 11) throw new cTestsException("ZTestSearch2_5.2");

                /*
                lMessageList = lMailbox.Messages(cFilter.Received >= new DateTime(2017, 6, 7), new cSort(cSortItem.ReceivedDesc));

                if (lMessageList.Count != 6) throw new cTestsException("ZTestSearch6.1");
                if (lMessageList[0].Handle.CacheSequence != 16 || lMessageList[1].Handle.CacheSequence != 15 || lMessageList[2].Handle.CacheSequence != 14 ||
                    lMessageList[3].Handle.CacheSequence != 12 || lMessageList[4].Handle.CacheSequence != 13 || lMessageList[5].Handle.CacheSequence != 11) throw new cTestsException("ZTestSearch6.2");
                    

                lTask1 = lMailbox.MessagesAsync(cFilter.Received >= new DateTime(2017, 6, 7));
                lTask2 = lMailbox.MessagesAsync(cFilter.Received >= new DateTime(2017, 6, 8), new cSort(cSortItem.ReceivedDesc));

                Task.WaitAll(lTask1, lTask2);

                lMessageList = lTask1.Result;
                if (lMessageList.Count != 6) throw new cTestsException("ZTestSearch7.1");

                lMessageList = lTask2.Result;
                if (lMessageList.Count != 3) throw new cTestsException("ZTestSearch7.2");


                // this checks that the search commands lock one another out ...

                lTask1 = lMailbox.MessagesAsync(cFilter.Received >= new DateTime(2017, 6, 7));
                lTask2 = lMailbox.MessagesAsync(cFilter.Received >= new DateTime(2017, 6, 8));
                lTask3 = lMailbox.MessagesAsync(cFilter.Received >= new DateTime(2017, 6, 8), new cSort(cSortItem.ReceivedDesc));

                Task.WaitAll(lTask1, lTask2, lTask3);*/






                lClient.Disconnect();

                if (!lTask.Wait(1000)) throw new cTestsException("session should be complete", lContext);
                if (lTask.IsFaulted) throw new cTestsException("server failed", lTask.Exception, lContext);
            }
            finally
            {
                ZFinally(lServer, lClient, lTask);
            }

        }

        private static void ZTestSearch3(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestSearch3));

            cServer lServer = new cServer();
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


            cIMAPClient lClient = new cIMAPClient("ZTestSearch3_cIMAPClient");
            lClient.SetServer("localhost");
            lClient.SetNoCredentials();
            lClient.IdleConfiguration = null;

            Task lTask = null;

            try
            {
                cMessageFlags lFlags;
                cMailbox lMailbox;
                List<cMessage> lMessageList;
                Task<List<cMessage>> lTask1;
                Task<List<cMessage>> lTask2;
                Task<List<cMessage>> lTask3;


                lTask = lServer.RunAsync(lContext);

                lClient.MailboxCacheData = fMailboxCacheData.messagecount | fMailboxCacheData.unseencount | fMailboxCacheData.uidnext;

                lClient.Connect();

                lMailbox = lClient.Mailbox(new cMailboxName("blurdybloop", null));
                if (lMailbox.IsSelected) throw new cTestsException("ZTestSearch3_2.1");

                if (lMailbox.MessageCount != 231 || lMailbox.UIDNext != 44292) throw new cTestsException("ZTestSearch3_2.2");

                lMailbox.Fetch(fMailboxCacheDataSets.status);
                if (lMailbox.MessageCount != 232 || lMailbox.UnseenCount != 3 || lMailbox.UIDNext != 44293) throw new cTestsException("ZTestSearch3_2.3");

                lMailbox.Select();
                if (lClient.Inbox.IsSelected || !lMailbox.IsSelected) throw new cTestsException("ZTestSearch3_3.1");

                if (lMailbox.MessageCount != 17 || lMailbox.RecentCount != 2 || lMailbox.UIDNext != 4392 || lMailbox.UIDValidity != 3857529045 || lMailbox.UnseenCount != 0 || lMailbox.UnseenUnknownCount != 17) throw new cTestsException("ZTestSearch3_3.2");

                lFlags = lMailbox.MessageFlags;
                if (lFlags.Count != 5 || !lFlags.Contains(kMessageFlagName.Answered) || !lFlags.Contains(kMessageFlagName.Flagged) || !lFlags.Contains(kMessageFlagName.Deleted) || !lFlags.Contains(kMessageFlagName.Seen) || !lFlags.Contains(kMessageFlagName.Draft)) throw new cTestsException("ZTestSearch3_3.3");

                lFlags = lMailbox.ReadOnlyPermanentFlags;
                if (lFlags.Count != 0) throw new cTestsException("ZTestSearch3_3.4");

                if (lMailbox.IsSelectedForUpdate) throw new cTestsException("ZTestSearch3_3.5");

                lMessageList = lMailbox.Messages(cFilter.Received >= new DateTime(2017, 6, 7), new cSort(cSortItem.ReceivedDesc));

                if (lMessageList.Count != 6) throw new cTestsException("ZTestSearch3_6.1");
                if (lMessageList[0].Handle.CacheSequence != 16 || lMessageList[1].Handle.CacheSequence != 15 || lMessageList[2].Handle.CacheSequence != 14 ||
                    lMessageList[3].Handle.CacheSequence != 12 || lMessageList[4].Handle.CacheSequence != 13 || lMessageList[5].Handle.CacheSequence != 11) throw new cTestsException("ZTestSearch3_6.2");


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

                if (!lTask.Wait(1000)) throw new cTestsException("session should be complete", lContext);
                if (lTask.IsFaulted) throw new cTestsException("server failed", lTask.Exception, lContext);
            }
            finally
            {
                ZFinally(lServer, lClient, lTask);
            }

        }

        private static void ZTestIdleRestart(cTrace.cContext pParentContext)
        {
            // test that idle stops on a fetch and restarts after the fetch
            //  and that UID fetch is used when appropriate
            //  and that expunge decreases the message numbers appropriately

            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestIdleRestart));

            cServer lServer = new cServer();
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

            lServer.AddExpectTagged("SEARCH UID 4392:*\r\n");
            lServer.AddSendData("* SEARCH\r\n");
            lServer.AddSendTagged("OK SEARCH completed\r\n");



            lServer.AddExpectTagged("LOGOUT\r\n");
            lServer.AddSendData("* BYE logging out\r\n");
            lServer.AddSendTagged("OK logged out\r\n");
            lServer.AddExpectClose();




            cIMAPClient lClient = new cIMAPClient("ZTestIdleRestart_cIMAPClient");
            lClient.SetServer("localhost");
            lClient.SetNoCredentials();

            Task lTask = null;

            try
            {
                lTask = lServer.RunAsync(lContext);

                lClient.Connect();

                lClient.Inbox.Select(true);

                if (lClient.Inbox.MessageCount != 172) throw new cTestsException("ZTestIdleRestart1.1");

                var lMessages = lClient.Inbox.Messages(!cFilter.IsSeen);

                Thread.Sleep(3000); // idle should start, message 168 should get deleted, and message 167 should get a UID during this wait

                if (lClient.Inbox.MessageCount != 171) throw new cTestsException("ZTestIdleRestart1.2");

                if (lMessages[1].Fetch(fCacheAttributes.uid)) throw new cTestsException("ZTestIdleRestart1.3.1"); // this should retrieve nothing (as the message has been deleted), but idle should stop
                Thread.Sleep(3000); // idle should restart in this wait

                List<iMessageHandle> lList = new List<iMessageHandle>();
                lList.Add(lMessages[0].Handle);
                lList.Add(lMessages[1].Handle);
                lList.Add(lMessages[2].Handle);
                lList.Add(lMessages[0].Handle);
                lList.Add(lMessages[1].Handle);
                lList.Add(lMessages[2].Handle);
                lList.Add(lMessages[0].Handle);
                var lMHL = cMessageHandleList.FromHandles(lList);
                if (lMHL.Count != 3) throw new cTestsException("ZTestIdleRestart1.3.2.a");

                cMessageHandleList lUnfetched;

                // only message 1 and 3 should be fetched by this, as message 2 was 168 which should now be gone
                //  1 should be UID fetched, 3 should be a normal fetch
                lUnfetched = lClient.Fetch(lList, fCacheAttributes.received, null);
                if (lUnfetched.Count != 1 || !ReferenceEquals(lUnfetched[0], lMessages[1].Handle)) throw new cTestsException("ZTestIdleRestart1.3.2");

                Thread.Sleep(3000); // idle should restart in this wait

                // only message 1 and 3 should be fetched, however this time (due to getting fast responses the last time) they should both be normal fetch
                lUnfetched = lClient.Inbox.Fetch(lMessages, fCacheAttributes.flags, null);
                if (lUnfetched.Count != 1 || !ReferenceEquals(lUnfetched[0], lMessages[1].Handle)) throw new cTestsException("ZTestIdleRestart1.3.3");


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
                catch (cUIDValidityChangedException) { lFailed = true; }
                if (!lFailed) throw new cTestsException("ZTestIdleRestart1.5");

                lFilter = cFilter.UID > new cUID(3857529045, 4391);

                lMessages = lMailbox.Messages(lFilter);





                lClient.Disconnect();

                if (!lTask.Wait(1000)) throw new cTestsException("session should be complete", lContext);
                if (lTask.IsFaulted) throw new cTestsException("server failed", lTask.Exception, lContext);
                }
                finally
                {
                    ZFinally(lServer, lClient, lTask);
                }
        }

        private static void ZTestUIDFetch1(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestUIDFetch1));



            cServer lServer = new cServer();
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

            /*
                 
                the mailbox looks like this in the client;
                MSN     UID     UID known   flags known     internal date known
                1       101     
                2       102                 y
                3       103     y
                4       105     y           y

                (104 has been expunged)

                */

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

            cIMAPClient lClient = new cIMAPClient(nameof(ZTestUIDFetch1));
            lClient.SetServer("localhost");
            lClient.SetNoCredentials();
            lClient.IdleConfiguration = null;

            Task lTask = null;

            try
            {
                lTask = lServer.RunAsync(lContext);

                lClient.Connect();

                // open inbox
                lClient.Inbox.Select(true);

                // give the server a chance to send some fetches to set up the test case
                lClient.Poll();

                // pretend that we know 5 UIDs; 104 has been expunged
                cUID[] lUIDs = new cUID[] { new cUID(3857529045, 105), new cUID(3857529045, 104), new cUID(3857529045, 103), new cUID(3857529045, 102), new cUID(3857529045, 101) };

                // fetch flags
                var lMessages = lClient.Inbox.Messages(lUIDs, fCacheAttributes.flags | fCacheAttributes.received);
                if (lMessages.Count != 4) throw new cTestsException($"{nameof(ZTestUIDFetch1)}.1");


                lClient.Disconnect();

                if (!lTask.Wait(1000)) throw new cTestsException("session should be complete", lContext);
                if (lTask.IsFaulted) throw new cTestsException("server failed", lTask.Exception, lContext);
            }
            finally
            {
                ZFinally(lServer, lClient, lTask);
            }
        }

        private static void ZTestBlank(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cTests), nameof(ZTestBlank)); // CHANGE THE NAME HERE

            cServer lServer = new cServer();
            lServer.AddSendData("* PREAUTH [CAPABILITY IMAP4rev1] this is the text\r\n");
            lServer.AddExpectTagged("LIST \"\" \"\"\r\n");
            lServer.AddSendData("* LIST () nil \"\"\r\n");
            lServer.AddSendTagged("OK LIST command completed\r\n");

            // add stuff here

            lServer.AddExpectTagged("LOGOUT\r\n");
            lServer.AddSendData("* BYE logging out\r\n");
            lServer.AddSendTagged("OK logged out\r\n");
            lServer.AddExpectClose();

            cIMAPClient lClient = new cIMAPClient(nameof(ZTestBlank)); // CHANGE THE NAME HERE
            lClient.SetServer("localhost");
            lClient.SetNoCredentials();
            lClient.IdleConfiguration = null;

            Task lTask = null;

            try
            {
                lTask = lServer.RunAsync(lContext);

                lClient.Connect();

                // add stuff here

                lClient.Disconnect();

                if (!lTask.Wait(1000)) throw new cTestsException("session should be complete", lContext);
                if (lTask.IsFaulted) throw new cTestsException("server failed", lTask.Exception, lContext);
            }
            finally
            {
                ZFinally(lServer, lClient, lTask);
            }
        }

        private static void ZFinally(cServer pServer, cIMAPClient pClient, Task pTask)
        {
            if (pTask != null)
            {
                if (!pTask.IsCompleted)
                {
                    pServer.Cancel();
                    try { pTask.Wait(); }
                    catch { }
                }

                pTask.Dispose();
            }

            pClient.Dispose();
            pServer.Dispose();
        }

        // disable warning about my listener
#pragma warning disable 618

        private sealed class cServer : IDisposable
        {
            private List<cTask> mTasks = new List<cTask>();
            TcpListener mListener = new TcpListener(143);
            private TcpClient mClient = null;
            private NetworkStream mStream = null;
            private CancellationTokenSource mCancellationTokenSource = null;

            public cServer() { }

            public void AddExpectData(string pData) => mTasks.Add(new cTask(cTask.eType.expectdata, pData));
            public void AddExpectData(byte[] pData) => mTasks.Add(new cTask(cTask.eType.expectdata, pData));
            public void AddExpectTagged(string pData) => mTasks.Add(new cTask(cTask.eType.expecttagged, pData));
            public void AddExpectTagged(byte[] pData) => mTasks.Add(new cTask(cTask.eType.expecttagged, pData));
            public void AddExpectClose() => mTasks.Add(new cTask(cTask.eType.expectclose));
            public void AddDelay(int pDelay) => mTasks.Add(new cTask(pDelay));
            public void AddSendData(string pData) => mTasks.Add(new cTask(cTask.eType.senddata, pData));
            public void AddSendData(byte[] pData) => mTasks.Add(new cTask(cTask.eType.senddata, pData));
            public void AddSendTagged(string pData) => mTasks.Add(new cTask(cTask.eType.sendtagged, pData));
            public void AddClose() => mTasks.Add(new cTask(cTask.eType.close));

            public async Task RunAsync(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cServer), nameof(RunAsync));

                mListener.Start();
                mClient = await mListener.AcceptTcpClientAsync().ConfigureAwait(false);
                mStream = mClient.GetStream();
                mCancellationTokenSource = new CancellationTokenSource();

                int lTaskNo = 0;
                int lByteInLine = 0;
                bool lReadTag = false;
                List<byte> lTagBuilder = new List<byte>();
                Stack<byte[]> lTagStack = new Stack<byte[]>();

                byte[] lBuffer = new byte[1000];
                int lBytesRead = 0;
                int lByteInBuffer = 0;

                cTask lTask = mTasks[lTaskNo++];

                try
                {
                    while (true)
                    {
                        if (lTask.Type == cTask.eType.close) break;

                        if (lTask.Type == cTask.eType.delay)
                        {
                            lContext.TraceVerbose($"waiting for {lTask.Delay}ms");
                            await Task.Delay(lTask.Delay, mCancellationTokenSource.Token).ConfigureAwait(false);
                            lTask = mTasks[lTaskNo++];
                            goto next_task;
                        }

                        if (lTask.Type == cTask.eType.senddata)
                        {
                            List<byte> lBytes = new List<byte>();

                            foreach (var lByte in lTask.Data)
                            {
                                if (lByte == 9) lBytes.AddRange(lTagStack.Peek());
                                else lBytes.Add(lByte);
                            }

                            byte[] lData = lBytes.ToArray();

                            lContext.TraceVerbose($"sending {new cBytes(lBytes)}");
                            await mStream.WriteAsync(lData, 0, lData.Length, mCancellationTokenSource.Token).ConfigureAwait(false);
                            lTask = mTasks[lTaskNo++];
                            goto next_task;
                        }

                        if (lTask.Type == cTask.eType.sendtagged)
                        {
                            byte[] lTag = lTagStack.Pop();
                            lContext.TraceVerbose($"sending {new cBytes(lTag)} {new cBytes(lTask.Data)}");
                            await mStream.WriteAsync(lTag, 0, lTag.Length, mCancellationTokenSource.Token).ConfigureAwait(false);
                            await mStream.WriteAsync(new byte[1] { 32 }, 0, 1, mCancellationTokenSource.Token).ConfigureAwait(false);
                            await mStream.WriteAsync(lTask.Data, 0, lTask.Data.Length, mCancellationTokenSource.Token).ConfigureAwait(false);
                            lTask = mTasks[lTaskNo++];
                            goto next_task;
                        }

                        if (lTask.Type == cTask.eType.expectclose)
                        {
                            lContext.TraceVerbose("expecting close");
                            lBytesRead = await mStream.ReadAsync(lBuffer, 0, 1000).ConfigureAwait(false);

                            if (lBytesRead != 0)
                            {
                                List<byte> lBytes = new List<byte>(lBytesRead);
                                for (int i = 0; i < lBytesRead; i++) lBytes.Add(lBuffer[i]);
                                throw new cTestsException($"expected close, got {new cBytes(lBytes)}", lContext);
                            }

                            lContext.TraceVerbose("closed");
                            break;
                        }

                        if (lTask.Type == cTask.eType.expecttagged && !lReadTag)
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
                            if (lBuffer[lByteInBuffer++] != lTask.Data[lByteInLine++]) throw new cTestsException($"received bytes don't match expectation: {new cBytes(lTask.Data)} at position {lByteInLine}", lContext);

                            if (lByteInLine == lTask.Data.Length)
                            {
                                lTask = mTasks[lTaskNo++];
                                lByteInLine = 0;
                                lReadTag = false;
                                goto next_task;
                            }
                        }

                        lContext.TraceVerbose("waiting for data");

                        lBytesRead = await mStream.ReadAsync(lBuffer, 0, 1000).ConfigureAwait(false);

                        if (lBytesRead == 0) throw new cTestsException("connection closed", lContext);

                        {
                            List<byte> lBytes = new List<byte>(lBytesRead);
                            for (int i = 0; i < lBytesRead; i++) lBytes.Add(lBuffer[i]);
                            lContext.TraceVerbose($"read {new cBytes(lBytes)}");
                        }

                        lByteInBuffer = 0;

                        next_task:;
                    }
                }
                finally
                {
                    mListener.Stop();
                    mStream.Dispose();
                    mStream = null;
                    mClient.Close();
                    mClient = null;
                }
            }

            public void Cancel()
            {
                mListener.Stop();
                if (mCancellationTokenSource != null) mCancellationTokenSource.Cancel();
                if (mStream != null) { mStream.Dispose(); mStream = null; }
                if (mClient != null) { mClient.Close(); mClient = null; }
            }

            public void Dispose()
            {
                mListener.Stop();

                if (mCancellationTokenSource != null)
                {
                    try { mCancellationTokenSource.Dispose(); }
                    catch { }
                    mCancellationTokenSource = null;
                }

                if (mStream != null)
                {
                    try { mStream.Dispose(); }
                    catch { }
                    mStream = null;
                }

                if (mClient != null)
                {
                    try { mClient.Close(); }
                    catch { }
                    mClient = null;
                }
            }

            private class cTask
            {
                public enum eType { expectdata, expecttagged, expectclose, delay, senddata, sendtagged, close }

                public readonly eType Type;
                public readonly byte[] Data;
                public readonly int Delay;

                public cTask(eType pType)
                {
                    Type = pType; // expectclose, close
                    Data = null;
                    Delay = 0;
                }

                public cTask(eType pType, string pData)
                {
                    Type = pType; // expectdata, senddata
                    Data = new byte[pData.Length];
                    for (int i = 0; i < pData.Length; i++) Data[i] = (byte)pData[i];
                    Delay = 0;
                }

                public cTask(eType pType, byte[] pData)
                {
                    Type = pType; // expectdata, senddata
                    Data = pData;
                    Delay = 0;
                }

                public cTask(int pDelay)
                {
                    Type = eType.delay; // expectclose, close
                    Data = null;
                    Delay = pDelay;
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

            public void Expect(eResponseTextType pType, eResponseTextCode pCode, string pText)
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

                if (e.TextType == lExpected.Type && e.Text.Code == lExpected.Code && e.Text.Text == lExpected.Text) return;

                mUnexpected.Add(e.ToString());
            }

            private class cExpected
            {
                public readonly eResponseTextType Type;
                public readonly eResponseTextCode Code;
                public readonly string Text;

                public cExpected(eResponseTextType pType, eResponseTextCode pCode, string pText)
                {
                    Type = pType;
                    Code = pCode;
                    Text = pText;
                }
            }
        }
    }
}
