using System;
using work.bacome.imapclient;

namespace usageexamples
{
    class Program
    {
        static void Main(string[] args)
        {
            string lHost = "192.168.56.101"; // my test server on my internal network - you'll need to change this
            string lUserId = "imaptest1"; // a valid user on my test server - you'll need to change this
            string lPassword = "imaptest1"; // the correct password on my test server - you'll need to change this

            try
            {
                using (cIMAPClient lClient = new cIMAPClient())
                {
                    // connect
                    //
                    lClient.SetServer(lHost); // if you are using this against a production server you'll likely need to specify SSL and maybe the port 
                    lClient.SetPlainCredentials(lUserId, lPassword);
                    lClient.Connect();

                    // list out the mailboxes that the user has at the top level of their first personal namespace
                    //
                    Console.WriteLine("a list of mailboxes");
                    foreach (var lMailbox in lClient.Namespaces.Personal[0].List()) Console.WriteLine(lMailbox.Name);

                    // get a reference to the inbox
                    var lInbox = lClient.Inbox;

                    // show some information about the status of the inbox
                    //
                    var lStatus = lInbox.Status(fStatusAttributes.all);
                    Console.WriteLine($"{lStatus.Unseen} unseen messages out of {lStatus.Messages} in the inbox");

                    // select the inbox so we can look at the messages in it
                    lInbox.Select();

                    // list out some details of the messages that have arrived in the last 100 days in the order that the messages were received
                    foreach (var lMessage in lInbox.Search(cFilter.Received >= DateTime.Today.AddDays(-100), new cSort(cSortItem.Received), fMessageProperties.envelope))
                    {
                        Console.WriteLine($"{lMessage.Envelope.Sent}\t{lMessage.Envelope.From[0].DisplayName}\t{lMessage.Envelope.Subject}");
                    }

                    // done
                    lClient.Disconnect();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"something bad happened\n{e}");
            }

            Console.WriteLine("press enter to continue");
            Console.Read();
        }
    }
}
