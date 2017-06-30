using System;
using work.bacome.imapclient;

namespace usageexample1
{
    class Program
    {
        static string mHost = "192.168.56.101"; // my test server on my internal network - you'll need to change this
        static string mUserId = "imaptest1"; // a valid user on my test server - you'll need to change this
        static string mPassword = "imaptest1"; // the correct password on my test server - you'll need to change this

        static void Main(string[] args)
        {
            try
            {
                using (cIMAPClient lClient = new cIMAPClient())
                {
                    // connect
                    //
                    lClient.SetServer(mHost); // if you are using this against a production server you'll likely need to specify SSL and maybe the port 
                    lClient.SetPlainCredentials(mUserId, mPassword);
                    lClient.Connect();

                    // list out the mailboxes that the user has at the top level of their first personal namespace
                    //
                    Console.WriteLine("a list of mailboxes");
                    foreach (var lMailbox in lClient.Namespaces.Personal[0].Mailboxes()) Console.WriteLine(lMailbox.Name);

                    // get a reference to the inbox
                    var lInbox = lClient.Inbox;

                    // show some information about the status of the inbox
                    //
                    var lStatus = lInbox.Status(fStatusAttributes.all);
                    Console.WriteLine($"{lStatus.Unseen} unseen messages out of {lStatus.Messages} in the inbox");

                    // select the inbox so we can look at the messages in it
                    lInbox.Select();

                    // list out some details of the messages that have arrived in the last 100 days in the order that the messages were received
                    foreach (var lMessage in lInbox.Messages(cFilter.Received >= DateTime.Today.AddDays(-100), new cSort(cSortItem.Received)))
                    {
                        Console.WriteLine($"{lMessage.Sent}\t{lMessage.From[0].DisplayName}\t{lMessage.Subject}");
                    }

                    // note - the loop above is not very efficient because each message has its envelope fetched separately: use this make it more efficient ...
                    //  foreach (var lMessage in lInbox.Messages(cFilter.Received >= DateTime.Today.AddDays(-100), new cSort(cSortItem.Received), fMessageProperties.envelope))

                    // done
                    lClient.Disconnect();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"something bad happened\n{e}");
            }

            WebVersion();

            Console.WriteLine("press enter to continue");
            Console.Read();
        }

        static void WebVersion()
        {
cIMAPClient lClient = new cIMAPClient();

// connect
lClient.SetServer(mHost);
lClient.SetPlainCredentials(mUserId, mPassword);
lClient.Connect();

// list mailboxes in the first personal namespace
            
Console.WriteLine("a list of mailboxes");

var lNamespace = lClient.Namespaces.Personal[0];

foreach (var lMailbox in lNamespace.Mailboxes())
    Console.WriteLine(lMailbox.Name);

// list status of inbox

var lStatus = lClient.Inbox.Status(fStatusAttributes.all);

Console.WriteLine(
    "{0} unseen messages out of {1} in the inbox",
    lStatus.Unseen,
    lStatus.Messages);

// select the inbox so we can look at the messages in it
lClient.Inbox.Select();

// list some details messages in the inbox

foreach (var lMessage in lClient.Inbox.Messages())
    Console.WriteLine("{0}\t{1}\t{2}",
        lMessage.Sent,
        lMessage.From[0].DisplayName,
        lMessage.Subject);

// disconnect
lClient.Disconnect();
        }
    }
}
