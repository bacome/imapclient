using System;
using work.bacome.mailclient;

namespace work.bacome.smtpclient
{
    // i didn't really want to do SMTP, however sending a message that is already in the IMAP store
    //  and using BURL really make it a requirement
    //
    // implementation to come

    public sealed partial class cSMTPClient : cMailClient, IDisposable
    {

    }
}