using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public class cCredentials
    {
        public readonly eAccountType Type;
        public readonly string UserId; // may be null for anonymous and NONE; must not be null otherwise
        public readonly cLogin Login; // only set if the account and password supplied can be used with login
        public readonly bool TryAllSASLs; // indicates that all SASLs should be tried if the server doesn't advertise any mechanisms
        protected readonly List<cSASL> mSASLs = new List<cSASL>();

        private cCredentials(eAccountType pType, cLogin pLogin, bool pTryAllSASLs = false)
        {
            Type = pType;
            UserId = null;
            Login = pLogin;
            TryAllSASLs = pTryAllSASLs;
        }

        protected cCredentials(string pUserId, cLogin pLogin, bool pTryAllSASLs = false)
        {
            if (string.IsNullOrEmpty(pUserId)) throw new ArgumentOutOfRangeException(nameof(pUserId));

            Type = eAccountType.userid;
            UserId = pUserId;
            Login = pLogin;
            TryAllSASLs = pTryAllSASLs;
        }

        public ReadOnlyCollection<cSASL> SASLs => mSASLs.AsReadOnly();

        public static readonly cCredentials None = new cCredentials(eAccountType.none, null);

        public static cCredentials Anonymous(string pTrace, bool pRequireTLS, bool pTryAuthenticateEvenIfAuthAnonymousIsntAdvertised = false)
        {
            if (string.IsNullOrEmpty(pTrace)) throw new ArgumentOutOfRangeException(nameof(pTrace));

            cLogin.TryConstruct("anonymous", pTrace, pRequireTLS, out var lLogin);
            cSASLAnonymous.TryConstruct(pTrace, pRequireTLS, out var lSASL);
            if (lLogin == null && lSASL == null) throw new ArgumentOutOfRangeException(nameof(pTrace));

            var lCredentials = new cCredentials(eAccountType.anonymous, lLogin, pTryAuthenticateEvenIfAuthAnonymousIsntAdvertised);
            if (lSASL != null) lCredentials.mSASLs.Add(lSASL);
            return lCredentials;
        }

        public static cCredentials Plain(string pUserId, string pPassword, bool pRequireTLS, bool pTryAuthenticateEvenIfAuthPlainIsntAdvertised = false)
        {
            if (string.IsNullOrEmpty(pUserId)) throw new ArgumentOutOfRangeException(nameof(pUserId));
            if (string.IsNullOrEmpty(pPassword)) throw new ArgumentOutOfRangeException(nameof(pPassword));

            cLogin.TryConstruct(pUserId, pPassword, pRequireTLS, out var lLogin);
            cSASLPlain.TryConstruct(pUserId, pPassword, pRequireTLS, out var lPlain);
            if (lLogin == null && lPlain == null) throw new ArgumentOutOfRangeException(); // argument_s_outofrange

            var lCredentials = new cCredentials(pUserId, lLogin, pTryAuthenticateEvenIfAuthPlainIsntAdvertised);
            if (lPlain != null) lCredentials.mSASLs.Add(lPlain);
            return lCredentials;
        }

        [Conditional("DEBUG")]
        public static void _Tests(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cCredentials), nameof(_Tests));

            bool lFailed;
            cCredentials lCredentials;

            lCredentials = Anonymous("fred");
            if (lCredentials.Login == null || lCredentials.SASLs.Count != 1) throw new cTestsException("unexpected anon result");

            lFailed = false;
            try { lCredentials = Anonymous(""); }
            catch (ArgumentOutOfRangeException) { lFailed = true; }
            if (!lFailed) throw new cTestsException("unexpected anon result");

            lCredentials = Anonymous("fred@fred.com");
            if (lCredentials.Login == null || lCredentials.SASLs.Count != 1) throw new cTestsException("unexpected anon result");

            lCredentials = Anonymous("fred@fred@fred.com");
            if (lCredentials.Login == null || lCredentials.SASLs.Count != 0) throw new cTestsException("unexpected anon result");

            lCredentials = Anonymous("fred€fred.com");
            if (lCredentials.Login != null || lCredentials.SASLs.Count != 1) throw new cTestsException("unexpected anon result");

            lCredentials = Anonymous("fred€@fred.com");
            if (lCredentials.Login != null || lCredentials.SASLs.Count != 1) throw new cTestsException("unexpected anon result");

            lCredentials = Anonymous("123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890");
            if (lCredentials.Login == null || lCredentials.SASLs.Count != 0) throw new cTestsException("unexpected anon result");

            lFailed = false;
            try { lCredentials = Anonymous("fred€@fred@fred.com"); }
            catch (ArgumentOutOfRangeException) { lFailed = true; }
            if (!lFailed) throw new cTestsException("unexpected anon result");
        }
    }
}


        //public class cSASLOAuth2 : cSASLBase
        //{
        /* from https://www.nsoftware.com/kb/xml/10231401.rst the clientid and secret are got by registration of the app with google
            * how you are meant to get the other things I dont' know
            * what the settings are for yahoo I don't know
            * how you pass the token to the imap server I don't know
            * google oauth2 imap :)
            //Setting Gmail server settings
            imaps1.MailServer = "imap.gmail.com";
            imaps1.User = "youraddress@gmail.com";
            imaps1.SSLStartMode = IPWorksSSL.ImapsSSLStartModes.sslImplicit;
            imaps1.MailPort = 993;

            //Getting an authorization string
            oauth1.ClientId = "723966830965.apps.googleusercontent.com";
            oauth1.ClientSecret = "_bYMDLuvYkJeT_99Q-vkP1rh";
            oauth1.ServerAuthURL = "https://accounts.google.com/o/oauth2/auth";
            oauth1.ServerTokenURL = "https://accounts.google.com/o/oauth2/token";
            oauth1.AuthorizationScope = "https://mail.google.com/";
            string authorization = oauth1.GetAuthorization();

            //Authenticating using XOAuth2
            imaps1.AuthMechanism = IPWorksSSL.ImapsAuthMechanisms.amXOAUTH2;
            imaps1.Config("AuthorizationIdentity=" + authorization);
            imaps1.Connect();
            //Additional code here
            imaps1.Disconnect();
            MessageBox.Show("Connected.", "IMAP", MessageBoxButtons.OK);

            //Setting Gmail server settings
            htmlmailers1.MailServer = "smtp.gmail.com";
            htmlmailers1.SendTo = "sendto@email.com";
            htmlmailers1.Subject = "Testing OAuth";
            htmlmailers1.From = "youraddress@gmail.com";
            htmlmailers1.User = "youraddress@gmail.com";

            //Getting an authorization string
            oauth1.ClientId = "723966830965.apps.googleusercontent.com";
            oauth1.ClientSecret = "_bYMDLuvYkJeT_99Q-vkP1rh";
            oauth1.ServerAuthURL = "https://accounts.google.com/o/oauth2/auth";
            oauth1.ServerTokenURL = "https://accounts.google.com/o/oauth2/token";
            oauth1.AuthorizationScope = "https://mail.google.com/";
            string authorization = oauth1.GetAuthorization();

            //Authenticating using XOAuth2
            htmlmailers1.AuthMechanism = IPWorksSSL.HtmlmailersAuthMechanisms.amXOAUTH2;
            htmlmailers1.Config("AuthorizationIdentity=" + authorization);
            htmlmailers1.MessageHTML = "<p>Test Mail</p>";
            htmlmailers1.Connect();
            htmlmailers1.Send();
            htmlmailers1.Disconnect();
            MessageBox.Show("Message Sent.", "HTMLMailer", MessageBoxButtons.OK);
        */

        //}
