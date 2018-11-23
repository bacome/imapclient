using System;
using System.Net.Mail;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using work.bacome.imapclient;
using work.bacome.imapsupport;

namespace work.bacome.imapclienttests
{
    [TestClass]
    public class Test_Address
    {
        [TestMethod]
        public void cEMailAddress_Tests()
        {
            // normal email with display name
            /////////////////////////////////

            var lEMailAddress11 = new cEmailAddress("fred.bloggs", "mydomain.com", "fred");
            var lEMailAddress12 = new cEmailAddress(" fred . bloggs ", "   mydomain  .  com  ", "fred");
            var lEMailAddress13 = new cEmailAddress(" \"fred\" . bloggs ", "   mydomain  .  com  ", "fred");
            var lEMailAddress14 = new cEmailAddress("\"fred.bloggs\"", "   mydomain  .  com  ", "fred");
            var lEMailAddress15 = new cEmailAddress("   \"fred.bloggs\"  \t ", "   mydomain  .  com  ", "fred");
            var lEMailAddress16 = new cEmailAddress(" fred (the man) . bloggs ", "   mydomain (how predicable (not really))  .  com  ", "fred");
            var lEMailAddress17 = new cEmailAddress(new cBytes("fred.bloggs"), new cBytes("mydomain.com"), new cBytes("fred"));

            Assert.AreEqual(lEMailAddress11, lEMailAddress12);
            Assert.AreEqual(lEMailAddress12, lEMailAddress13);
            Assert.AreEqual(lEMailAddress13, lEMailAddress14);
            Assert.AreEqual(lEMailAddress14, lEMailAddress15);
            Assert.AreEqual(lEMailAddress15, lEMailAddress16);
            Assert.AreEqual(lEMailAddress16, lEMailAddress17);

            Assert.IsTrue(lEMailAddress11.CanComposeHeaderFieldValue(false));
            Assert.IsTrue(lEMailAddress11.CanComposeHeaderFieldValue(true));

            Assert.AreEqual("fred", lEMailAddress11.DisplayText);
            Assert.AreEqual("fred.bloggs@mydomain.com", lEMailAddress11.Address);

            // should be different some variants
            var lEMailAddress1Dash1 = new cEmailAddress("   \"fred.bloggs \"  \t ", "   mydomain  .  com  ", "fred");
            var lEMailAddress1Dash2 = new cEmailAddress("fred bloggs", "   mydomain  .  com  ", "fred");
            var lEMailAddress1Dash3 = new cEmailAddress("fred.bloggs", "   mydomain  .  com  ", "Fred");
            var lEMailAddress1Dash4 = new cEmailAddress(new cBytes("fred.bloggs "), new cBytes("mydomain.com"), new cBytes("fred"));
            var lEMailAddress1Dash5 = new cEmailAddress(new cBytes("fred.bloggs"), new cBytes("mydomain.com "), new cBytes("fred"));
            var lEMailAddress1Dash6 = new cEmailAddress("fred.bloggs", "mydomain.com");

            Assert.AreNotEqual(lEMailAddress11, lEMailAddress1Dash1);
            Assert.AreNotEqual(lEMailAddress11, lEMailAddress1Dash2);
            Assert.AreNotEqual(lEMailAddress11, lEMailAddress1Dash3);
            Assert.AreNotEqual(lEMailAddress11, lEMailAddress1Dash4);
            Assert.AreNotEqual(lEMailAddress11, lEMailAddress1Dash5);
            Assert.AreNotEqual(lEMailAddress11, lEMailAddress1Dash6);

            Assert.AreEqual("\"fred.bloggs \"@mydomain.com", lEMailAddress1Dash4.Address);
            Assert.AreEqual("fred", lEMailAddress1Dash4.DisplayText);
            Assert.AreEqual("fred.bloggs@mydomain.com", lEMailAddress1Dash6.DisplayText);


            // encoded word
            var lEMailAddress21 = new cEmailAddress("fred.bloggs", "mydomain.com", "fr€d");
            var lEMailAddress22 = new cEmailAddress(new cBytes("fred.bloggs"), new cBytes("mydomain.com"), new cBytes("=?utf-8?b?ZnLigqxk?="));

            Assert.AreEqual(lEMailAddress21, lEMailAddress22);

            Assert.IsTrue(lEMailAddress21.CanComposeHeaderFieldValue(false));
            Assert.IsTrue(lEMailAddress21.CanComposeHeaderFieldValue(true));


            // utf8
            var lEMailAddress31 = new cEmailAddress("fr€d.bloggs", "mydomain.com", "fr€d");
            var lEMailAddress32 = new cEmailAddress(Encoding.UTF8.GetBytes("fr€d.bloggs"), new cBytes("mydomain.com"), new cBytes("=?utf-8?b?ZnLigqxk?="));

            Assert.AreEqual(lEMailAddress31, lEMailAddress32);

            Assert.IsFalse(lEMailAddress31.CanComposeHeaderFieldValue(false));
            Assert.IsTrue(lEMailAddress31.CanComposeHeaderFieldValue(true));

            /*

            // idn
            ;?;



            // try putting some illegal chars in and check cancompise
            ;?;


            // quoted string email
            //////////////////////

            ;?; // esp address should have the quotes


            // obsolete format email
            ////////////////////////




            // can



            /*


            var lCulturedString1 = new cCulturedString("fred");
            var lEMailAddress13 = new cEmailAddress(" fred . bloggs ", "   mydomain  .com", lCulturedString1);
            //var lEMailAddress14 = new cEmailAddress(" \"fred\" . bloggs ", "   mydomain  .com", lCulturedString1);
            var lMailAddress1 = new MailAddress("fred.blogs@mydomain.com", "fred");
            var lEMailAddress15 = new cEmailAddress(lMailAddress1);

            Assert.AreEqual(lEMailAddress11, lEMailAddress12);
            Assert.AreEqual(lEMailAddress12, lEMailAddress13);
            Assert.AreEqual(lEMailAddress13, lEMailAddress14);
            Assert.AreEqual(lEMailAddress14, lEMailAddress15);

            Assert.AreEqual("fred", lEMailAddress11.DisplayText);

            // same email without display name

            var lEMailAddress21 = new cEmailAddress(" fred . bloggs ", "   mydomain  .  com  ");
            var lMailAddress2 = new MailAddress("fred.blogs@mydomain.com");
            var lEMailAddress22 = new cEmailAddress(lMailAddress2);
            //cCulturedString lCulturedString2 = null;
            var lEMailAddress23 = new cEmailAddress(" fred . bloggs ", "   mydomain  .  com  ", lCulturedString2);

            Assert.AreNotEqual(lEMailAddress11, lEMailAddress21);
            Assert.AreEqual(lEMailAddress21, lEMailAddress22);
            Assert.AreEqual(lEMailAddress22, lEMailAddress23);

            Assert.AreEqual("fred.blogs@mydomain.com", lEMailAddress11.DisplayText);


            // quoted string email

            ;?;





            /*

            //var lCulturedString3 = new cCulturedString("fred");
            var lEMailAddress31 = new cEmailAddress(" \"fre\0\" . bloggs ", "   mydomain  .com", lCulturedString3);

            var lMailAddress4 = new MailAddress("fred.blogs@mydomain.com", "fr€d");
            ;?;


            var lEMailAddress51 = new cEmailAddress("fred bloggs", "   mydomain  .  com  ");

    */


        }
    }
}