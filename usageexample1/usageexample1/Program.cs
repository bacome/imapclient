using System;
using work.bacome.imapclient;

namespace usageexample1
{
    class Program
    {
        static string mHost = "192.168.56.101"; // my test server on my internal network - you'll need to change this
        static string mUserId = "imapusageexample1"; // a valid user on my test server - you'll need to change this
        static string mPassword = "imapusageexample1"; // the correct password on my test server - you'll need to change this

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

                    Console.WriteLine(new string('-', 79));
                    Console.WriteLine();

                    // list the mailboxes that the user has at the top level of their first personal namespace
                    //
                    Console.WriteLine("A list of mailboxes;");
                    foreach (var lMailbox in lClient.Namespaces.Personal[0].Mailboxes()) Console.WriteLine(lMailbox.Name);

                    Console.WriteLine();
                    Console.WriteLine(new string('-', 79));
                    Console.WriteLine();

                    // get a reference to the inbox
                    var lInbox = lClient.Inbox;

                    // show some information about the status of the inbox
                    //
                    var lStatus = lInbox.Status(fStatusAttributes.all);
                    Console.WriteLine($"There are {lStatus.Unseen} unseen messages out of {lStatus.Messages} in the inbox");

                    Console.WriteLine();
                    Console.WriteLine(new string('-', 79));
                    Console.WriteLine();

                    // select the inbox so we can look at the messages in it
                    lInbox.Select();

                    // list some details of the unseen messages

                    foreach (var lMessage in lInbox.Messages(cFilter.IsNotFlagged(fMessageFlags.seen), new cSort(cSortItem.Received), fMessageProperties.envelope | fMessageProperties.bodystructure))
                    {
                        Console.WriteLine("Sent: " + lMessage.Sent);
                        Console.WriteLine("From: " + lMessage.From[0].DisplayName);
                        Console.WriteLine("Subject: " + lMessage.Subject);
                        Console.WriteLine();

                        var lAttachments = lMessage.Attachments;

                        if (lAttachments.Count > 0)
                        {
                            Console.WriteLine(lAttachments.Count + " attachments;");
                            foreach (var lAttachment in lAttachments) Console.WriteLine(lAttachment.FileName + "\t" + lAttachment.SizeInBytes + "b");
                            Console.WriteLine();
                        }

                        Console.WriteLine(lMessage.PlainText);

                        Console.WriteLine();
                        Console.WriteLine(new string('-', 79));
                        Console.WriteLine();
                    }

                    // done
                    lClient.Disconnect();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"something bad happened\n{e}");
            }

            NewWebVersion();

            Console.WriteLine("press enter to continue");
            Console.Read();
        }

        static void NewWebVersion()
        {
cIMAPClient lClient = new cIMAPClient();

// connect
lClient.SetServer(mHost);
lClient.SetPlainCredentials(mUserId, mPassword);
lClient.Connect();

Console.WriteLine(new string('-', 79));
Console.WriteLine();

// list mailboxes in the first personal namespace

Console.WriteLine("A list of mailboxes;");

var lNamespace = lClient.Namespaces.Personal[0];

foreach (var lMailbox in lNamespace.Mailboxes())
    Console.WriteLine(lMailbox.Name);

Console.WriteLine();
Console.WriteLine(new string('-', 79));
Console.WriteLine();

// list status of inbox

var lStatus = lClient.Inbox.Status(fStatusAttributes.all);

Console.WriteLine(
    "{0} unseen messages out of {1} in the inbox",
    lStatus.Unseen,
    lStatus.Messages);

Console.WriteLine();
Console.WriteLine(new string('-', 79));
Console.WriteLine();

// select the inbox so we can look at the messages in it
lClient.Inbox.Select();

// list unseen messages in the inbox

foreach (var lMessage in lClient.Inbox.Messages())
{
    Console.WriteLine("Sent: " + lMessage.Sent);
    Console.WriteLine("From: " + lMessage.From[0].DisplayName);
    Console.WriteLine("Subject: " + lMessage.Subject);
    Console.WriteLine();

    var lAttachments = lMessage.Attachments;

    if (lAttachments.Count > 0)
    {
        Console.WriteLine(lAttachments.Count + " attachments;");

        foreach (var lAttachment in lAttachments)
            Console.WriteLine(
                lAttachment.FileName + "\t" +
                lAttachment.SizeInBytes + "b");

        Console.WriteLine();
    }

    Console.WriteLine(lMessage.PlainText);

    Console.WriteLine();
    Console.WriteLine(new string('-', 79));
    Console.WriteLine();
}

// done
lClient.Disconnect();
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
