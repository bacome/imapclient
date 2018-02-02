using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public abstract class cAppendDataPart
    {
        internal cAppendDataPart() { }
        internal abstract bool HasContent { get; }
        public static implicit operator cAppendDataPart(cMessage pMessage) => new cMessageAppendDataPart(pMessage);
        public static implicit operator cAppendDataPart(cAttachment pAttachment) => new cMessagePartAppendDataPart(pAttachment);
        public static implicit operator cAppendDataPart(string pString) => new cLiteralAppendDataPart(pString);
        public static implicit operator cAppendDataPart(Stream pStream) => new cStreamAppendDataPart(pStream);
    }

    public class cMessageAppendDataPart : cAppendDataPart
    {
        public readonly cIMAPClient Client;
        public readonly iMessageHandle MessageHandle;
        public readonly bool AllowCatenate;

        public cMessageAppendDataPart(cMessage pMessage, bool pAllowCatenate = true)
        {
            if (pMessage == null) throw new ArgumentNullException(nameof(pMessage));

            // check that the source message is in a selected mailbox (in case we have to stream it)
            //  (note that this is just a sanity check; the mailbox could become un-selected before we get a chance to get the message data which will cause a throw)
            if (!pMessage.IsValid()) throw new ArgumentOutOfRangeException(nameof(pMessage), kArgumentOutOfRangeExceptionMessage.IsInvalid);

            Client = pMessage.Client;
            MessageHandle = pMessage.MessageHandle;
            AllowCatenate = pAllowCatenate;
        }

        internal override bool HasContent => true;

        public override string ToString() => $"{nameof(cMessageAppendDataPart)}({MessageHandle},{AllowCatenate})";
    }

    public class cMessagePartAppendDataPart : cAppendDataPart
    {
        public readonly cIMAPClient Client;
        public readonly iMessageHandle MessageHandle;
        public readonly cSinglePartBody Part;
        public readonly bool AllowCatenate;

        public cMessagePartAppendDataPart(cMessage pMessage, cSinglePartBody pPart, bool pAllowCatenate = true)
        {
            if (pMessage == null) throw new ArgumentNullException(nameof(pMessage));

            // check that the source message is in a selected mailbox (in case we have to stream it)
            //  (note that this is just a sanity check; the mailbox could become un-selected before we get a chance to get the message data which will cause a throw)
            if (!pMessage.IsValid()) throw new ArgumentOutOfRangeException(nameof(pMessage), kArgumentOutOfRangeExceptionMessage.IsInvalid);

            Client = pMessage.Client;
            MessageHandle = pMessage.MessageHandle;

            Part = pPart ?? throw new ArgumentNullException(nameof(pPart));

            // check that the part is part of the message
            if (!pMessage.Contains(pPart)) throw new ArgumentOutOfRangeException(nameof(pPart));

            AllowCatenate = pAllowCatenate;
        }

        public cMessagePartAppendDataPart(cAttachment pAttachment, bool pAllowCatenate = true)
        {
            if (pAttachment == null) throw new ArgumentNullException(nameof(pAttachment));

            Client = pAttachment.Client;
            MessageHandle = pAttachment.MessageHandle;
            Part = pAttachment.Part;
            AllowCatenate = pAllowCatenate;
        }

        internal override bool HasContent => Part.SizeInBytes != 0;

        public override string ToString() => $"{nameof(cMessagePartAppendDataPart)}({MessageHandle},{Part},{AllowCatenate})";
    }

    public class cUIDSectionAppendDataPart : cAppendDataPart
    {
        public readonly cIMAPClient Client;
        public readonly iMailboxHandle MailboxHandle;
        public readonly cUID UID;
        public readonly cSection Section;
        public readonly uint Length;
        public readonly bool AllowCatenate;

        public cUIDSectionAppendDataPart(cMailbox pMailbox, cUID pUID, cSection pSection, uint pLength, bool pAllowCatenate = true)
        {
            if (pMailbox == null) throw new ArgumentNullException(nameof(pMailbox));

            // check that the mailbox is selected (in case we have to stream it)
            //  (note that this is just a sanity check; the mailbox could become un-selected before we get a chance to get the message data which will cause a throw)
            if (!pMailbox.IsSelected) throw new ArgumentOutOfRangeException(nameof(pMailbox), kArgumentOutOfRangeExceptionMessage.MailboxMustBeSelected);

            Client = pMailbox.Client;
            MailboxHandle = pMailbox.MailboxHandle;

            UID = pUID ?? throw new ArgumentNullException(nameof(pUID));
            Section = pSection ?? throw new ArgumentNullException(nameof(pSection));

            Length = pLength;

            AllowCatenate = pAllowCatenate;
        }

        internal override bool HasContent => Length != 0;

        public override string ToString() => $"{nameof(cUIDSectionAppendDataPart)}({MailboxHandle},{UID},{Section},{Length},{AllowCatenate})";
    }

    public abstract class cLiteralAppendDataPartBase : cAppendDataPart
    {
        internal cLiteralAppendDataPartBase() { }
        internal abstract IList<byte> GetBytes(Encoding pEncoding);
    }

    public class cLiteralAppendDataPart : cLiteralAppendDataPartBase
    {
        private cBytes mBytes;

        public cLiteralAppendDataPart(IEnumerable<byte> pBytes)
        {
            if (pBytes == null) throw new ArgumentNullException(nameof(pBytes));
            mBytes = new cBytes(new List<byte>(pBytes));
        }

        public cLiteralAppendDataPart(string pString)
        {
            if (pString == null) throw new ArgumentNullException(nameof(pString));
            mBytes = new cBytes(Encoding.UTF8.GetBytes(pString));
        }

        internal override bool HasContent => mBytes.Count > 0;

        internal override IList<byte> GetBytes(Encoding pEncoding) => mBytes;

        public override string ToString() => $"{nameof(cLiteralAppendDataPart)}({mBytes})";
    }

    public class cHeaderFieldAppendDataPart : cLiteralAppendDataPartBase
    {
        private static readonly ReadOnlyCollection<cHeaderFieldValuePart> kEmpty = new ReadOnlyCollection<cHeaderFieldValuePart>(new cHeaderFieldValuePart[] { });

        private readonly string mFieldName;
        private readonly ReadOnlyCollection<cHeaderFieldValuePart> mValue;

        public cHeaderFieldAppendDataPart(string pFieldName)
        {
            if (pFieldName == null) throw new ArgumentNullException(nameof(pFieldName));
            if (pFieldName.Length == 0) throw new ArgumentOutOfRangeException(nameof(pFieldName));
            if (!cCharset.FText.ContainsAll(pFieldName)) throw new ArgumentOutOfRangeException(nameof(pFieldName));
            mFieldName = pFieldName;
            mValue = kEmpty;
        }

        public cHeaderFieldAppendDataPart(string pFieldName, string pText)
        {
            if (pFieldName == null) throw new ArgumentNullException(nameof(pFieldName));
            if (pText == null) throw new ArgumentNullException(nameof(pText));
            if (pFieldName.Length == 0) throw new ArgumentOutOfRangeException(nameof(pFieldName));
            if (!cCharset.FText.ContainsAll(pFieldName)) throw new ArgumentOutOfRangeException(nameof(pFieldName));
            mFieldName = pFieldName;

            var lValue = new List<cHeaderFieldValuePart>();
            lValue.Add(new cHeaderFieldText(pText));
            mValue = new ReadOnlyCollection<cHeaderFieldValuePart>(lValue);
        }

        public cHeaderFieldAppendDataPart(string pFieldName, DateTime pDateTime)
        {
            if (pFieldName == null) throw new ArgumentNullException(nameof(pFieldName));
            if (pFieldName.Length == 0) throw new ArgumentOutOfRangeException(nameof(pFieldName));
            if (!cCharset.FText.ContainsAll(pFieldName)) throw new ArgumentOutOfRangeException(nameof(pFieldName));
            mFieldName = pFieldName;

            string lSign;
            string lZone;

            if (pDateTime.Kind == DateTimeKind.Local)
            {
                var lOffset = TimeZoneInfo.Local.GetUtcOffset(pDateTime);

                if (lOffset < TimeSpan.Zero)
                {
                    lSign = "-";
                    lOffset = -lOffset;
                }
                else lSign = "+";

                lZone = lOffset.ToString("hhmm");
            }
            else if (pDateTime.Kind == DateTimeKind.Utc)
            {
                lSign = "+";
                lZone = "0000";
            }
            else
            {
                lSign = "-";
                lZone = "0000";
            }

            var lMonth = cRFCMonth.cName[pDateTime.Month - 1];

            string l5322DateTime = string.Format("{0:dd} {1} {0:yyyy} {0:HH}:{0:mm}:{0:ss} {2}{3}", pDateTime, lMonth, lSign, lZone);

            var lValue = new List<cHeaderFieldValuePart>();
            lValue.Add(new cHeaderFieldText(l5322DateTime));
            mValue = new ReadOnlyCollection<cHeaderFieldValuePart>(lValue);
        }

        public cHeaderFieldAppendDataPart(string pFieldName, IEnumerable<cHeaderFieldValuePart> pValue)
        {
            if (pFieldName == null) throw new ArgumentNullException(nameof(pFieldName));
            if (pValue == null) throw new ArgumentNullException(nameof(pValue));
            if (pFieldName.Length == 0) throw new ArgumentOutOfRangeException(nameof(pFieldName));
            if (!cCharset.FText.ContainsAll(pFieldName)) throw new ArgumentOutOfRangeException(nameof(pFieldName));
            mFieldName = pFieldName;
            mValue = new ReadOnlyCollection<cHeaderFieldValuePart>(new List<cHeaderFieldValuePart>(pValue));
        }

        internal override bool HasContent => true;

        internal override IList<byte> GetBytes(Encoding pEncoding)
        {
            cHeaderFieldBytes lBytes = new cHeaderFieldBytes(mFieldName, pEncoding);
            foreach (var lPart in mValue) lPart.GetBytes(lBytes, eHeaderValuePartContext.unstructured);
            lBytes.AddNewLine();
            return lBytes.Bytes;
        }

        public override string ToString()
        {
            cListBuilder lBuilder = new cListBuilder(nameof(cHeaderFieldAppendDataPart));
            lBuilder.Append(mFieldName);
            foreach (var lPart in mValue) lBuilder.Append(lPart);
            return lBuilder.ToString();
        }



















        internal static void _Tests(cTrace.cContext pParentContext)
        {
            // check construction validation

            cHeaderFieldAppendDataPart lPart = new cHeaderFieldAppendDataPart("fred");

            try
            {
                cHeaderFieldAppendDataPart x = new cHeaderFieldAppendDataPart("fr€d");
                throw new cTestsException($"{nameof(cHeaderFieldAppendDataPart)}.1.1");
            }
            catch (ArgumentOutOfRangeException) { }

            try
            {
                cHeaderFieldAppendDataPart x = new cHeaderFieldAppendDataPart("fr:d");
                throw new cTestsException($"{nameof(cHeaderFieldAppendDataPart)}.1.2");
            }
            catch (ArgumentOutOfRangeException) { }

            try
            {
                cHeaderFieldAppendDataPart x = new cHeaderFieldAppendDataPart("fr d");
                throw new cTestsException($"{nameof(cHeaderFieldAppendDataPart)}.1.3");
            }
            catch (ArgumentOutOfRangeException) { }

            // check validation of crlf

            lPart = new cHeaderFieldAppendDataPart("fred", "\r\n\tx\r\n x\r\n ");

            try
            {
                lPart = new cHeaderFieldAppendDataPart("fred", "\r\nx\r\n x\r\n ");
                throw new cTestsException($"{nameof(cHeaderFieldAppendDataPart)}.1.4");
            }
            catch (ArgumentOutOfRangeException) { }

            try
            {
                lPart = new cHeaderFieldAppendDataPart("fred", "\r\n\tx\r\nx\r\n ");
                throw new cTestsException($"{nameof(cHeaderFieldAppendDataPart)}.1.5");
            }
            catch (ArgumentOutOfRangeException) { }

            try
            {
                lPart = new cHeaderFieldAppendDataPart("fred", "\r\n\tx\r x\r\n ");
                throw new cTestsException($"{nameof(cHeaderFieldAppendDataPart)}.1.6");
            }
            catch (ArgumentOutOfRangeException) { }

            try
            {
                lPart = new cHeaderFieldAppendDataPart("fred", "\r\n\tx\n x\r\n ");
                throw new cTestsException($"{nameof(cHeaderFieldAppendDataPart)}.1.7");
            }
            catch (ArgumentOutOfRangeException) { }

            try
            {
                lPart = new cHeaderFieldAppendDataPart("fred", "\r\n\t\0\r\n x\r\n ");
                throw new cTestsException($"{nameof(cHeaderFieldAppendDataPart)}.1.8");
            }
            catch (ArgumentOutOfRangeException) { }




            ZTestUnstructured("8.1.1", "Keld Jørn Simonsen", null, "ISO-8859-1", "Keld =?iso-8859-1?q?J=F8rn?= Simonsen");
            ZTestUnstructured("8.1.2", "Keld Jøørn Simonsen", null, "ISO-8859-1", "Keld =?iso-8859-1?b?Svj4cm4=?= Simonsen"); // should switch to base64
            ZTestUnstructured("8.1.3", "Keld Jørn Simonsen", null, null, "Keld Jørn Simonsen"); // should use utf8

            // adjacent words that need to be encoded are encoded together with one space between them, 76 char line length limit on encoded word lines, trailing spaces are removed
            //                                                                                                                                                                       fred: 7890123456789012345678901234567890123456789012345678901234567890123456
            //                                                                                                                                                                                                                          1234567890123456789012345678901234567890123456789012345678901234567890123456
            //                                                                                                                                                                                                                                          
            ZTestUnstructured("joins.1", "    A𠈓C A𠈓C fred fr€d fr€d fred  fr€d    fr€d    fred    fred ", "    A𠈓C A𠈓C fred\r\n fr€d fr€d fred  fr€d fr€d\r\n fred    fred", "utf-8", "    =?utf-8?b?QfCgiJNDIEHwoIiTQw==?= fred\r\n =?utf-8?b?ZnLigqxkIGZy4oKsZA==?= fred  =?utf-8?b?ZnLigqxkIGZy4oKsZA==?=\r\n fred    fred");

            // if a line ends with an encoded word and the next line begins with an encoded word, a space is added to the beginning of the second encoded word to prevent them being joined on decoding
            ZTestUnstructured("spaces.1", "    A𠈓C\r\n A𠈓C\r\n fred\r\n fr€d fr€d fred  fr€d fr€d    fred \r\n   fred ", "    A𠈓C A𠈓C\r\n fred\r\n fr€d fr€d fred  fr€d fr€d\r\n fred\r\n   fred", "utf-8");

            // check that adjacent encoded words are in fact joined
            ZTestUnstructured("long.1",
                " 12345678901234567890123456789012345678901234567890123456789012345678901234567890\r\n 1234567890123456789012345678901234567890€12345678901234\r\n 1234567890123456789012345678901234567890€12345678901\r\n 1234567890123456789012345678901234567890€123456789012",
                "\r\n 12345678901234567890123456789012345678901234567890123456789012345678901234567890\r\n 1234567890123456789012345678901234567890€12345678901234 1234567890123456789012345678901234567890€12345678901 1234567890123456789012345678901234567890€123456789012",
                "utf-8"
                );

            // check that each encoded word is a whole number of characters                                                                                                     fred: 1234567890123456789012345678901234567890123456789012345678901234567890123456
            ZTestUnstructured("charcounting.1", " 𠈓𠈓𠈓𠈓𠈓a𠈓𠈓𠈓𠈓𠈓𠈓 fred 𠈓𠈓𠈓𠈓𠈓ab𠈓𠈓𠈓𠈓𠈓𠈓\r\n \r\n ", "\r\n 𠈓𠈓𠈓𠈓𠈓a𠈓𠈓𠈓𠈓𠈓𠈓\r\n fred 𠈓𠈓𠈓𠈓𠈓ab𠈓𠈓𠈓𠈓𠈓𠈓", "utf-8", "\r\n =?utf-8?b?8KCIk/CgiJPwoIiT8KCIk/CgiJNh8KCIk/CgiJPwoIiT8KCIk/CgiJPwoIiT?=\r\n fred =?utf-8?b?8KCIk/CgiJPwoIiT8KCIk/CgiJNhYvCgiJPwoIiT8KCIk/CgiJPwoIiT?=\r\n =?utf-8?b?8KCIkw==?=");

            // q-encoding rule checks

            //  unstructured - e.g. subject
            ZTestUnstructured("q.1", "Keld J\"#$%&'(),.:;<>@[\\]^`{|}~ørn Simonsen", null, "ISO-8859-1", "Keld =?iso-8859-1?q?J\"#$%&'(),.:;<>@[\\]^`{|}~=F8rn?= Simonsen");

            // comment

            lPart = new cHeaderFieldAppendDataPart("x", new cHeaderFieldValuePart[] { new cHeaderFieldComment("Keld J\"#$%&'(),.:;<>@[\\]^`{|}~ørn Simonsen") });
            ZTest("q.2", lPart, null, "ISO-8859-1", "x:(Keld =?iso-8859-1?q?J\"#$%&'=28=29,.:;<>@[=5C]^`{|}~=F8rn?= Simonsen)");

            // phrase
            lPart = new cHeaderFieldAppendDataPart("x",
                new cHeaderFieldValuePart[] {
                    new cHeaderFieldPhrase("Keld J\"#$%&'(),.:;<>@[\\]^`{|}~orn J\"#$%&ørn4567890123456789012345678901234567890 J'(),.ørn4567890123456789012345678901234567 J:;<>@ørn4567890123456789012345678901234567 J[\\]^`ørn4567890123456789012345678901234567 J{|}~ørn4567890123456789012345678901234567 Simonsen")
                }
                );

            ZTest("q.3", lPart, null,
                "ISO-8859-1", "x:Keld \"J\\\"#$%&'(),.:;<>@[\\\\]^`{|}~orn\"\r\n" +
                " =?iso-8859-1?q?J=22=23=24=25=26=F8rn4567890123456789012345678901234567890?=\r\n" +
                " =?iso-8859-1?q?=20J=27=28=29=2C=2E=F8rn4567890123456789012345678901234567?=\r\n" +
                " =?iso-8859-1?q?=20J=3A=3B=3C=3E=40=F8rn4567890123456789012345678901234567?=\r\n" +
                " =?iso-8859-1?q?=20J=5B=5C=5D=5E=60=F8rn4567890123456789012345678901234567?=\r\n" +
                " =?iso-8859-1?q?=20J=7B=7C=7D=7E=F8rn4567890123456789012345678901234567?=\r\n" +
                " Simonsen");

            // looks like an encoded word
            ZTestUnstructured("e.1", "mary had =?x?q?x?= little lamb", null, "utf-8", "mary had =?utf-8?b?PT94P3E/eD89?= little lamb");
            ZTestUnstructured("e.1", "mary had =?x?q?x!= little lamb", null, "utf-8", "mary had =?x?q?x!= little lamb");

            // phrase
            ZTestPhrase("p.1", "atom str.ng encod€d word", true, "atom \"str.ng\" encod€d word", "atom \"str.ng\" encod€d word");
            ZTestPhrase("p.2", "atom str.ng encod€d word", false, "atom \"str.ng\" encod€d word", "atom \"str.ng\" =?utf-8?b?ZW5jb2Tigqxk?= word");

            // 
            lPart = new cHeaderFieldAppendDataPart("x",
                new cHeaderFieldValuePart[]
                {
                    new cHeaderFieldPhrase(
                        new cHeaderFieldCommentOrText[]
                        {
                            new cHeaderFieldText("phrase is only "),
                            new cHeaderFieldComment(
                                new cHeaderFieldCommentOrText[]
                                {
                                    new cHeaderFieldText("not really"),
                                    new cHeaderFieldComment("sorry")
                                }),
                            new cHeaderFieldText("used in display name")
                        }
                        ),
                    new cHeaderFieldText("<Muhammed."),
                    new cHeaderFieldComment("I am the greatest"),
                    new cHeaderFieldText(" Ali @"),
                    new cHeaderFieldComment("the"),
                    new cHeaderFieldText("Vegas.WBA>")
                }
                );

            ZTest("ccp.1", lPart, "x:phrase is only(not really(sorry))used in display name <Muhammed.(I am the\r\n greatest) Ali @(the)Vegas.WBA>");




            lPart = new cHeaderFieldAppendDataPart("x", new cHeaderFieldValuePart[] { new cHeaderFieldComment("ts is a really long comment with one utf8 wørd in it (the word is the only one that should be \"treated\" differently if utf8 is on or off)") });
            //                                                                                                                                                                                     12345678901234567890123456789012345678901234567890123456 7890123456789012345678    12345678901234567890 12345678 90123456789012345678901234567890123456789012345678
            ZTest("cc.1", lPart, "x:(ts is a really long comment with one utf8 wørd in it \\(the word is the only\r\n one that should be \"treated\" differently if utf8 is on or off\\))", null, "x:(ts is a really long comment with one utf8 wørd in it \\(the word is the only\r\n one that should be \"treated\" differently if utf8 is on or off\\))");
            ZTest("cc.2", lPart, "x:(ts is a really long comment with one utf8 wørd in it\r\n \\(the word is the only one that should be \"treated\" differently if utf8 is on\r\n or off\\))", "utf-8",
            //   1234567890123456789012345678901234567890123456789012345678901234567890123451 234567890123456789012345678901234567890123 45678901 234567890123456789012345678
                "x:(ts is a really long comment with one utf8 =?utf-8?b?d8O4cmQ=?= in it\r\n \\(the word is the only one that should be \"treated\" differently if utf8 is on\r\n or off\\))");

            lPart = new cHeaderFieldAppendDataPart("x", new cHeaderFieldValuePart[] { new cHeaderFieldComment("tis is a really long comment with one utf8 wørd in it (the word is the only one that should be \"treated\" differently if utf8 is on or off)") });
            //                                                                                                                                                                                      12345678901234567890123456789012345678901234567890123456 789012345678901234    1234567890123456789012345 67890123 456789012345678901234567890123456789012345678
            ZTest("cc.3", lPart, "x:(tis is a really long comment with one utf8 wørd in it \\(the word is the\r\n only one that should be \"treated\" differently if utf8 is on or off\\))", null, "x:(tis is a really long comment with one utf8 wørd in it \\(the word is the\r\n only one that should be \"treated\" differently if utf8 is on or off\\))");



            // test blank line suppression
            ZTestUnstructured("b.1", "here is\r\n \r\n a test\r\n \r\n ", "here is\r\n a test");

            // test insertion of space

            lPart = new cHeaderFieldAppendDataPart("x",
                new cHeaderFieldValuePart[]
                {
                    new cHeaderFieldText("here is"),
                    new cHeaderFieldText("a test"),
                    new cHeaderFieldComment("this is a comment"),
                    new cHeaderFieldText("followed by text"),
                    new cHeaderFieldPhrase("an.d a ph.rase")
                }
                );

            ZTest("sp.1", lPart, "x:here is a test(this is a comment)followed by text \"an.d\" a \"ph.rase\"");


            lPart = new cHeaderFieldAppendDataPart("12345678901234567890123456789012345678901234567890123456789012345678901234567890", ".");

            ZTest("long.1", lPart, "12345678901234567890123456789012345678901234567890123456789012345678901234567890:\r\n .");


            // parameters

            lPart = new cHeaderFieldAppendDataPart("content-type",
                new cHeaderFieldValuePart[]
                {
                    new cHeaderFieldText("message/external-body"),
                    new cHeaderFieldMIMEParameter("access-type", "URL"),
                    new cHeaderFieldMIMEParameter("URL", "ftp://cs.utk.edu/pub/moore/bulk-mailer/bulk-mailer.tar")
                }
                );

            //                     12345678901234567890123456789012345678901234567890123456789012345678901234567890
            ZTestMime("1", lPart, "content-type:message/external-body ;access-type=URL\r\n ;URL=\"ftp://cs.utk.edu/pub/moore/bulk-mailer/bulk-mailer.tar\"");

            lPart = new cHeaderFieldAppendDataPart("content-type",
                new cHeaderFieldValuePart[]
                {
                    new cHeaderFieldText("message/external-body"),
                    new cHeaderFieldMIMEParameter("access-type", "URL"),
                    new cHeaderFieldMIMEParameter("URL", "ftp://cs.utk.edu/pub/moore/bulk-mailer/bulk-mailer/bulk-mailer/bulk-mailer.tar")
                }
                );

            //                                                                            12345678 901234567890123456789012345678901234567890123456789012345678901234567890
            ZTestMime("2", lPart, "content-type:message/external-body ;access-type=URL\r\n ;URL*0=\"ftp://cs.utk.edu/pub/moore/bulk-mailer/bulk-mailer/bulk-mailer/bulk-\"\r\n ;URL*1=mailer.tar");

            lPart = new cHeaderFieldAppendDataPart("content-type",
                new cHeaderFieldValuePart[]
                {
                    new cHeaderFieldText("message/external-body"),
                    new cHeaderFieldMIMEParameter("access-type", "URL"),
                    new cHeaderFieldMIMEParameter("URL", "ftp://cs.utk.€du/pub/moore/bulk-mailer/bulk-mailer/bulk-mailer/bulk-mail€r.tar")
                }
                );

            //                                                                            12345678 901234567890123456789012345678901234567890123456789012345678901234567890
            ZTestMime("2.1", lPart, "content-type:message/external-body ;access-type=URL\r\n ;URL*0=\"ftp://cs.utk.€du/pub/moore/bulk-mailer/bulk-mailer/bulk-mailer/bulk-\"\r\n ;URL*1=\"mail€r.tar\"");

            lPart = new cHeaderFieldAppendDataPart("content-type",
                new cHeaderFieldValuePart[]
                {
                    new cHeaderFieldText("message/external-body"),
                    new cHeaderFieldMIMEParameter("access-type", "URL"),
                    new cHeaderFieldMIMEParameter("URL", "ftp://cs.utk.€du/pub/moore/bulk-mailer/bulk-mailer.tar")
                }
                );

            //                     12345678901234567890123456789012345678901234567890123456789012345678901234567890
            ZTestMime("3", lPart, "content-type:message/external-body ;access-type=URL\r\n ;URL=\"ftp://cs.utk.€du/pub/moore/bulk-mailer/bulk-mailer.tar\"");

            lPart = new cHeaderFieldAppendDataPart("content-type",
                new cHeaderFieldValuePart[]
                {
                    new cHeaderFieldText("message/external-body"),
                    new cHeaderFieldMIMEParameter("access-type", "URL"),
                    new cHeaderFieldMIMEParameter("URL", "moøre")
                }
                );

            //                       12345678901234567890123456789012345678901234567890123456789012345678901234567890
            ZTestMime("3.1", lPart, "content-type:message/external-body ;access-type=URL ;URL*=iso-8859-1''mo%F8re", "ISO-8859-1");

            lPart = new cHeaderFieldAppendDataPart("content-type",
                new cHeaderFieldValuePart[]
                {
                    new cHeaderFieldText("message/external-body"),
                    new cHeaderFieldMIMEParameter("access-type", "URL"),
                    new cHeaderFieldMIMEParameter("URL", "ftp://cs.utk.edu/pub/moøre/bulk-mailer/bulk-mailer.tar")
                }
                );

            //                                                                            12345678901234567890123456789012345678901234567890123456789012345678901234567890
            //                                                                                                            
            ZTestMime("4", lPart, "content-type:message/external-body ;access-type=URL\r\n ;URL*0*=iso-8859-1''ftp%3A%2F%2Fcs.utk.edu%2Fpub%2Fmo%F8re%2Fbulk-mailer%2Fbu\r\n ;URL*1=lk-mailer.tar", "ISO-8859-1");

            lPart = new cHeaderFieldAppendDataPart("content-type",
                new cHeaderFieldValuePart[]
                {
                    new cHeaderFieldText("message/external-body"),
                    new cHeaderFieldMIMEParameter("access-type", "URL"),
                    new cHeaderFieldMIMEParameter("URL", "ftp://cs.utk.edu/pub/moøre/bulk-mailer/bulk-mailer/bulk-mailer/bulk-mailer/bulk-mailer/bulk-mailer/bulk-maile9012345678901234567890123456789012345678901234567890123456789012345678moøre.tar")
                }
                );

            //                                                                            12345678901234567890123456789012345678901234567890123456789012345678901234567890  12345678 901234567890123456789012345678901234567890123456789012345678901234567 8    123456789012345678901234567890123456789012345678901234567890123456789012345678
            //                                                                                                            
            ZTestMime("4", lPart, "content-type:message/external-body ;access-type=URL\r\n ;URL*0*=iso-8859-1''ftp%3A%2F%2Fcs.utk.edu%2Fpub%2Fmo%F8re%2Fbulk-mailer%2Fbu\r\n ;URL*1=\"lk-mailer/bulk-mailer/bulk-mailer/bulk-mailer/bulk-mailer/bulk-maile\"\r\n ;URL*2=9012345678901234567890123456789012345678901234567890123456789012345678\r\n ;URL*3*=mo%F8re.tar", "ISO-8859-1");



            ZTestDateTime("local", new DateTime(2018, 02, 03, 15, 16, 17, DateTimeKind.Local));
            ZTestDateTime("unspecified", new DateTime(2018, 02, 03, 15, 16, 17, DateTimeKind.Unspecified));
            ZTestDateTime("utc", new DateTime(2018, 02, 03, 15, 16, 17, DateTimeKind.Utc));
        }

        private static void ZTestDateTime(string pTestName, DateTime pDateTime)
        {
            var lPart = new cHeaderFieldAppendDataPart("x", pDateTime);
            var lBytes = lPart.GetBytes(null);
            var lCursor = new cBytesCursor(lBytes);

            if (!lCursor.SkipByte(cASCII.x) || !lCursor.SkipByte(cASCII.COLON) || !lCursor.GetRFC822DateTime(out var lDateTimeOffset, out var lDateTime) || !lCursor.SkipByte(cASCII.CR) || !lCursor.SkipByte(cASCII.LF) || !lCursor.Position.AtEnd) throw new cTestsException($"{nameof(cHeaderFieldAppendDataPart)}.DateTime({pTestName}.i)");

            if (pDateTime.Kind == DateTimeKind.Utc)
            {
                if (lDateTime.ToUniversalTime() != pDateTime) throw new cTestsException($"{nameof(cHeaderFieldAppendDataPart)}.DateTime({pTestName}.r1)");
            }
            else
            {
                if (lDateTime != pDateTime) throw new cTestsException($"{nameof(cHeaderFieldAppendDataPart)}.DateTime({pTestName}.r2)");
            }

            if (pDateTime.Kind == DateTimeKind.Local)
            {
                int i = 7 + 4;
            }
            else
            {
                if (lDateTimeOffset.Offset.CompareTo(TimeSpan.Zero) != 0) throw new cTestsException($"{nameof(cHeaderFieldAppendDataPart)}.DateTime({pTestName}.o)");
            }
        }

        private static void ZTestUnstructured(string pTestName, string pString, string pExpectedF = null, string pCharsetName = null, string pExpectedI = null)
        {
            cHeaderFieldAppendDataPart lPart = new cHeaderFieldAppendDataPart("fred", pString);

            string lExpectedI;
            if (pExpectedI == null) lExpectedI = null;
            else lExpectedI = "fred:" + pExpectedI;

            string lExpectedF;
            if (pExpectedF == null) lExpectedF = "fred:" + pString;
            else lExpectedF = "fred:" + pExpectedF;

            ZTest("unstructured." + pTestName, lPart, lExpectedF, pCharsetName, lExpectedI);
        }


        private static void ZTestPhrase(string pTestName, string pString, bool pUTF8, string pExpectedF, string pExpectedI = null)
        {
            cHeaderFieldAppendDataPart lPart = new cHeaderFieldAppendDataPart("x", new cHeaderFieldValuePart[] { new cHeaderFieldPhrase(pString) });

            string lExpectedI;
            if (pExpectedI == null) lExpectedI = null;
            else lExpectedI = "x:" + pExpectedI;

            string lExpectedF;
            if (pExpectedF == null) lExpectedF = "x:" + pString;
            else lExpectedF = "x:" + pExpectedF;

            string lCharsetName;
            if (pUTF8) lCharsetName = null;
            else lCharsetName = "utf-8";

            ZTest("phrase." + pTestName, lPart, lExpectedF, lCharsetName, lExpectedI);
        }

        private static void ZTest(string pTestName, cHeaderFieldAppendDataPart pPart, string pExpectedF, string pCharsetName = null, string pExpectedI = null)
        {
            Encoding lEncoding;
            if (pCharsetName == null) lEncoding = null;
            else lEncoding = Encoding.GetEncoding(pCharsetName);

            var lTemp = pPart.GetBytes(lEncoding);
            byte[] lBytes = new byte[lTemp.Count - 2];
            for (int i = 0; i < lTemp.Count - 2; i++) lBytes[i] = lTemp[i];
            string lIString = new string(Encoding.UTF8.GetChars(lBytes));

            cCulturedString lCS = new cCulturedString(lBytes);
            string lFString = lCS.ToString();

            if (pExpectedI != null && lIString != pExpectedI) throw new cTestsException($"{nameof(cHeaderFieldAppendDataPart)}.Unstructured({pTestName}.i : {lIString})");
            if (pExpectedF != null && lFString != pExpectedF) throw new cTestsException($"{nameof(cHeaderFieldAppendDataPart)}.Unstructured({pTestName}.f : {lFString})");
        }

        private static void ZTestMime(string pTestName, cHeaderFieldAppendDataPart pPart, string pExpected, string pCharsetName = null)
        {
            Encoding lEncoding;
            if (pCharsetName == null) lEncoding = null;
            else lEncoding = Encoding.GetEncoding(pCharsetName);

            var lTemp = pPart.GetBytes(lEncoding);

            byte[] lBytes = new byte[lTemp.Count - 2];
            for (int i = 0; i < lTemp.Count - 2; i++) lBytes[i] = lTemp[i];

            string lString = new string(Encoding.UTF8.GetChars(lBytes));

            if (lString != pExpected) throw new cTestsException($"{nameof(cHeaderFieldAppendDataPart)}.mime.{pTestName} : {lString}");
        }
    }

    public class cFileAppendDataPart : cAppendDataPart
    {
        public readonly string Path;
        public readonly uint Length; // if encoding, this is the encoded length
        public readonly bool Base64Encode;
        public readonly cBatchSizerConfiguration ReadConfiguration; // optional

        public cFileAppendDataPart(string pPath, cBatchSizerConfiguration pReadConfiguration = null)
        {
            if (string.IsNullOrWhiteSpace(pPath)) throw new ArgumentOutOfRangeException(nameof(pPath));

            var lFileInfo = new FileInfo(pPath);
            if (!lFileInfo.Exists || (lFileInfo.Attributes & FileAttributes.Directory) != 0 || lFileInfo.Length > uint.MaxValue) throw new ArgumentOutOfRangeException(nameof(pPath));

            Path = lFileInfo.FullName;
            Length = (uint)lFileInfo.Length;
            Base64Encode = false;
            ReadConfiguration = pReadConfiguration;
        }

        public cFileAppendDataPart(string pPath, bool pBase64Encode, cBatchSizerConfiguration pReadConfiguration = null)
        {
            if (string.IsNullOrWhiteSpace(pPath)) throw new ArgumentOutOfRangeException(nameof(pPath));

            var lFileInfo = new FileInfo(pPath);
            if (!lFileInfo.Exists || (lFileInfo.Attributes & FileAttributes.Directory) != 0) throw new ArgumentOutOfRangeException(nameof(pPath));

            Path = lFileInfo.FullName;

            long lLength;
            if (pBase64Encode) lLength = cBase64Encoder.EncodedLength(lFileInfo.Length);
            else lLength = lFileInfo.Length;

            if (lLength > uint.MaxValue) throw new ArgumentOutOfRangeException(nameof(pPath));

            Length = (uint)lLength;
            Base64Encode = pBase64Encode;
            ReadConfiguration = pReadConfiguration;
        }

        internal override bool HasContent => Length != 0;

        public override string ToString() => $"{nameof(cFileAppendDataPart)}({Path},{Length},{Base64Encode},{ReadConfiguration})";
    }

    public class cStreamAppendDataPart : cAppendDataPart
    {
        public readonly Stream Stream;
        public readonly uint Length; // if encoding, this is the encoded length
        public readonly bool Base64Encode;
        public readonly cBatchSizerConfiguration ReadConfiguration; // optional

        public cStreamAppendDataPart(Stream pStream, cBatchSizerConfiguration pReadConfiguration = null)
        {
            Stream = pStream ?? throw new ArgumentNullException(nameof(pStream));
            if (!pStream.CanRead || !pStream.CanSeek) throw new ArgumentOutOfRangeException(nameof(pStream));

            long lLength = pStream.Length - pStream.Position;
            if (lLength < 0 || lLength > uint.MaxValue) throw new ArgumentOutOfRangeException(nameof(pStream));
            Length = (uint)lLength;

            Base64Encode = false;
            ReadConfiguration = pReadConfiguration;
        }

        public cStreamAppendDataPart(Stream pStream, uint pLength, cBatchSizerConfiguration pReadConfiguration = null)
        {
            Stream = pStream ?? throw new ArgumentNullException(nameof(pStream));
            if (!pStream.CanRead) throw new ArgumentOutOfRangeException(nameof(pStream));
            Length = pLength;
            Base64Encode = false;
            ReadConfiguration = pReadConfiguration;
        }

        public cStreamAppendDataPart(Stream pStream, bool pBase64Encode, cBatchSizerConfiguration pReadConfiguration = null)
        {
            Stream = pStream ?? throw new ArgumentNullException(nameof(pStream));
            if (!pStream.CanRead || !pStream.CanSeek) throw new ArgumentOutOfRangeException(nameof(pStream));

            long lLength = pStream.Length - pStream.Position;
            if (lLength < 0) throw new ArgumentOutOfRangeException(nameof(pStream));
            if (pBase64Encode) lLength = cBase64Encoder.EncodedLength(lLength);
            if (lLength > uint.MaxValue) throw new ArgumentOutOfRangeException(nameof(pStream));
            Length = (uint)lLength;

            Base64Encode = pBase64Encode;
            ReadConfiguration = pReadConfiguration;
        }

        public cStreamAppendDataPart(Stream pStream, uint pLength, bool pBase64Encode, cBatchSizerConfiguration pReadConfiguration = null)
        {
            Stream = pStream ?? throw new ArgumentNullException(nameof(pStream));
            if (!pStream.CanRead) throw new ArgumentOutOfRangeException(nameof(pStream));

            if (pBase64Encode)
            {
                long lLength = cBase64Encoder.EncodedLength(pLength);
                if (lLength > uint.MaxValue) throw new ArgumentOutOfRangeException(nameof(pLength));
                Length = (uint)lLength;
            }
            else Length = pLength;

            Base64Encode = pBase64Encode;
            ReadConfiguration = pReadConfiguration;
        }

        internal override bool HasContent => Length != 0;

        public override string ToString() => $"{nameof(cStreamAppendDataPart)}({Length},{Base64Encode},{ReadConfiguration})";
    }
}