using System;
using System.Configuration;
using work.bacome.imapclient;

namespace usageexample2
{
    class Program
    {
        const string kUID = "UID";
        const string kUIDValidity = "UIDValidity";
        const string kHost = "192.168.56.101"; // my test server on my internal network - you'll need to change this
        const string kUserId = "imapusageexample2"; // a valid user on my test server - you'll need to change this
        const string kPassword = "imapusageexample2"; // the correct password on my test server - you'll need to change this

        static void Main(string[] args)
        {
            WebVersion();
            return;

            try
            {
                using (cIMAPClient lClient = new cIMAPClient())
                {
                    // get the settings

                    var lConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

                    var lSettings = lConfig.AppSettings.Settings;

                    var lUIDValiditySetting = lSettings[kUIDValidity];

                    if (lUIDValiditySetting == null)
                    {
                        lUIDValiditySetting = new KeyValueConfigurationElement(kUIDValidity, string.Empty);
                        lSettings.Add(lUIDValiditySetting);
                    }

                    var lUIDSetting = lSettings[kUID];

                    if (lUIDSetting == null)
                    {
                        lUIDSetting = new KeyValueConfigurationElement(kUID, string.Empty);
                        lSettings.Add(lUIDSetting);
                    }

                    // connect
                    //
                    lClient.SetServer(kHost); // if you are using this against a production server you'll likely need to specify SSL and maybe the port 
                    lClient.SetPlainCredentials(kUserId, kPassword, eTLSRequirement.indifferent); // if you are using this against a production server you will most likely want to require TLS (which is the default that I have overridden here)
                    lClient.Connect();

                    // select the inbox
                    lClient.Inbox.Select(true);

                    // get the UID we should inspect from
                    cUID lFromUID;

                    // check the UIDValidity to make sure it hasn't changed
                    if (uint.TryParse(lUIDValiditySetting.Value, out var lUIDValidity) &&
                        lUIDValidity == lClient.Inbox.UIDValidity.Value &&
                        uint.TryParse(lUIDSetting.Value, out var lUID)) lFromUID = new cUID(lUIDValidity, lUID);
                    else lFromUID = new cUID(lClient.Inbox.UIDValidity.Value, 1);

                    // note the UIDNext for the next run
                    uint lUIDNext = lClient.Inbox.UIDNext.Value;

                    // no point doing anything if there are no more messages than last time
                    if (lUIDNext > lFromUID.UID)
                    {
                        // this example is meant to demonstrate filtering, so here it is
                        var lFilter = cFilter.UID >= lFromUID & cFilter.From.Contains("imaptest2@dovecot.bacome.work") & !cFilter.Deleted;

                        // loop through the messages
                        foreach (var lMessage in lClient.Inbox.Messages(lFilter, cSort.None, fMessageProperties.attachments | fMessageProperties.uid))
                        {
                            // only process the message if it looks as expected
                            if (lMessage.Attachments.Count == 1 && lMessage.PlainText() == "FILE FOR PROCESSING")
                            {
                                // save the attachment
                                lMessage.Attachments[0].SaveAs($".\\SavedAttachment.{lMessage.UID.UIDValidity}.{lMessage.UID.UID}");

                                // mark the message as deleted
                                lMessage.Deleted = true;
                            }
                        }

                        // store the start point for the next run
                        lUIDValiditySetting.Value = lFromUID.UIDValidity.ToString();
                        lUIDSetting.Value = lUIDNext.ToString();
                        lConfig.Save(ConfigurationSaveMode.Modified);

                        // expunge the deleted messages
                        lClient.Inbox.Expunge(true);
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

        static void WebVersion()
        {


// the first part of the example is just getting settings
//  it isn't really what I want to show you

var lConfig = 
    ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

var lSettings = lConfig.AppSettings.Settings;

var lUIDValiditySetting = lSettings[kUIDValidity];

if (lUIDValiditySetting == null)
{
    lUIDValiditySetting = 
        new KeyValueConfigurationElement(kUIDValidity, string.Empty);

    lSettings.Add(lUIDValiditySetting);
}

var lUIDSetting = lSettings[kUID];

if (lUIDSetting == null)
{
    lUIDSetting = new KeyValueConfigurationElement(kUID, string.Empty);
    lSettings.Add(lUIDSetting);
}

// now what I want to show you

cIMAPClient lClient = new cIMAPClient();

// connect
//
lClient.SetServer(kHost);
lClient.SetPlainCredentials(kUserId, kPassword, eTLSRequirement.indifferent);
lClient.Connect();

// select the inbox for update
lClient.Inbox.Select(true);

cUID lFromUID;

// check the UIDValidity to make sure it hasn't changed
if (uint.TryParse(lUIDValiditySetting.Value, out var lUIDValidity) &&
    lUIDValidity == lClient.Inbox.UIDValidity.Value &&
    uint.TryParse(lUIDSetting.Value, out var lUID))
     lFromUID = new cUID(lUIDValidity, lUID);
else lFromUID = new cUID(lClient.Inbox.UIDValidity.Value, 1);

// note the UIDNext for the next run
uint lUIDNext = lClient.Inbox.UIDNext.Value;

// only process if there are more messages than at the last run
if (lUIDNext > lFromUID.UID)
{
    // this example is meant to demonstrate building a filter, so here it is
    var lFilter = 
        cFilter.UID >= lFromUID &
        cFilter.From.Contains("imaptest2@dovecot.bacome.work") &
        !cFilter.Deleted;

    // loop through the messages
    foreach (var lMessage in lClient.Inbox.Messages(lFilter))
    {
        // only process the message if it looks as expected
        if (lMessage.Attachments.Count == 1 &&
            lMessage.PlainText() == "FILE FOR PROCESSING")
        {
            // save the attachement
            lMessage.Attachments[0].SaveAs
                ($".\\SavedAttachment.{lMessage.UID.UIDValidity}.{lMessage.UID.UID}");

            // mark the message as deleted
            lMessage.Deleted = true;
        }
    }

    // store the start point for the next run
    lUIDValiditySetting.Value = lFromUID.UIDValidity.ToString();
    lUIDSetting.Value = lUIDNext.ToString();
    lConfig.Save(ConfigurationSaveMode.Modified);

    // expunge the deleted messages
    lClient.Inbox.Expunge(true);
}

// done
lClient.Disconnect();
lClient.Dispose();
            }
        }
}
