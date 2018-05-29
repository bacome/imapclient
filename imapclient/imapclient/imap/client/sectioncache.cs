using System;
using System.Threading.Tasks;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        ;?; // here are callbacks that are passed to the session for

        // receiving a host/cred$/mailboxname/uidvalidity [to delete entries that have a different uidvalidity] 

        // receiving a host/cred$/mailboxname/uid [to delete entries for]
        //  this one called by subscribing to the event

    }
}