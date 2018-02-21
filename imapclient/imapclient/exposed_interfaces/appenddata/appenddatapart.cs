using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Mail;
using System.Text;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public abstract class cAppendDataPart
    {
        public static readonly cAppendDataPart CRLF = new cLiteralAppendDataPart("\r\n");

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

        public cMessageAppendDataPart(cMessage pMessage)
        {
            if (pMessage == null) throw new ArgumentNullException(nameof(pMessage));

            // check that the source message is in a selected mailbox (in case we have to stream it)
            //  (note that this is just a sanity check; the mailbox could become un-selected before we get a chance to get the message data which will cause a throw)
            if (!pMessage.IsValid()) throw new ArgumentOutOfRangeException(nameof(pMessage), kArgumentOutOfRangeExceptionMessage.IsInvalid);

            Client = pMessage.Client;
            MessageHandle = pMessage.MessageHandle;
        }

        internal override bool HasContent => true;

        public override string ToString() => $"{nameof(cMessageAppendDataPart)}({MessageHandle})";
    }

    public class cMessagePartAppendDataPart : cAppendDataPart
    {
        public readonly cIMAPClient Client;
        public readonly iMessageHandle MessageHandle;
        public readonly cSinglePartBody Part;

        public cMessagePartAppendDataPart(cMessage pMessage, cSinglePartBody pPart)
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
        }

        public cMessagePartAppendDataPart(cAttachment pAttachment)
        {
            if (pAttachment == null) throw new ArgumentNullException(nameof(pAttachment));

            Client = pAttachment.Client;
            MessageHandle = pAttachment.MessageHandle;
            Part = pAttachment.Part;
        }

        internal override bool HasContent => Part.SizeInBytes != 0;

        public override string ToString() => $"{nameof(cMessagePartAppendDataPart)}({MessageHandle},{Part})";
    }

    public class cUIDSectionAppendDataPart : cAppendDataPart
    {
        public readonly cIMAPClient Client;
        public readonly iMailboxHandle MailboxHandle;
        public readonly cUID UID;
        public readonly cSection Section;
        public readonly uint Length;

        public cUIDSectionAppendDataPart(cMailbox pMailbox, cUID pUID, cSection pSection, uint pLength)
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
        }

        internal override bool HasContent => Length != 0;

        public override string ToString() => $"{nameof(cUIDSectionAppendDataPart)}({MailboxHandle},{UID},{Section},{Length})";
    }

    public abstract class cLiteralAppendDataPartBase : cAppendDataPart
    {
        internal cLiteralAppendDataPartBase() { }
        internal abstract IList<byte> GetBytes(Encoding pEncoding);
    }

    public class cLiteralAppendDataPart : cLiteralAppendDataPartBase
    {
        private cBytes mBytes;

        internal cLiteralAppendDataPart(cBytes pBytes)
        {
            mBytes = pBytes ?? throw new ArgumentNullException(nameof(pBytes));
        }

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
        private static readonly ReadOnlyCollection<cHeaderFieldValuePart> kNoValue = new ReadOnlyCollection<cHeaderFieldValuePart>(new cHeaderFieldValuePart[] { });

        private readonly string mName;
        private readonly ReadOnlyCollection<cHeaderFieldValuePart> mValueParts;
        private readonly ReadOnlyCollection<cHeaderFieldValuePart> mUTF8ValueParts;

        public cHeaderFieldAppendDataPart(string pName)
        {
            if (pName == null) throw new ArgumentNullException(nameof(pName));
            if (pName.Length == 0) throw new ArgumentOutOfRangeException(nameof(pName));
            if (!cCharset.FText.ContainsAll(pName)) throw new ArgumentOutOfRangeException(nameof(pName));
            mName = pName;
            mValueParts = kNoValue;
            mUTF8ValueParts = null;
        }

        public cHeaderFieldAppendDataPart(string pName, string pText, string pUTF8Text = null)
        {
            if (pName == null) throw new ArgumentNullException(nameof(pName));
            if (pText == null) throw new ArgumentNullException(nameof(pText));
            if (pName.Length == 0) throw new ArgumentOutOfRangeException(nameof(pName));
            if (!cCharset.FText.ContainsAll(pName)) throw new ArgumentOutOfRangeException(nameof(pName));
            mName = pName;

            var lValueParts = new List<cHeaderFieldValuePart>();
            lValueParts.Add(new cHeaderFieldTextValuePart(pText));
            mValueParts = lValueParts.AsReadOnly();

            if (pUTF8Text == null) mUTF8ValueParts = null;
            else
            {
                var lUTF8ValueParts = new List<cHeaderFieldValuePart>();
                lUTF8ValueParts.Add(new cHeaderFieldTextValuePart(pUTF8Text));
                mUTF8ValueParts = lUTF8ValueParts.AsReadOnly();
            }
        }

        public cHeaderFieldAppendDataPart(string pName, DateTime pDateTime)
        {
            if (pName == null) throw new ArgumentNullException(nameof(pName));
            if (pName.Length == 0) throw new ArgumentOutOfRangeException(nameof(pName));
            if (!cCharset.FText.ContainsAll(pName)) throw new ArgumentOutOfRangeException(nameof(pName));
            mName = pName;

            var lValueParts = new List<cHeaderFieldValuePart>();
            lValueParts.Add(pDateTime);
            mValueParts = lValueParts.AsReadOnly();

            mUTF8ValueParts = null;
        }

        public cHeaderFieldAppendDataPart(string pName, DateTimeOffset pDateTimeOffset)
        {
            if (pName == null) throw new ArgumentNullException(nameof(pName));
            if (pName.Length == 0) throw new ArgumentOutOfRangeException(nameof(pName));
            if (!cCharset.FText.ContainsAll(pName)) throw new ArgumentOutOfRangeException(nameof(pName));
            mName = pName;

            var lValueParts = new List<cHeaderFieldValuePart>();
            lValueParts.Add(pDateTimeOffset);
            mValueParts = lValueParts.AsReadOnly();

            mUTF8ValueParts = null;
        }

        public cHeaderFieldAppendDataPart(string pName, MailAddress pAddress)
        {
            // encoding in pAddress is ignored

            if (pName == null) throw new ArgumentNullException(nameof(pName));
            if (pName.Length == 0) throw new ArgumentOutOfRangeException(nameof(pName));
            if (!cCharset.FText.ContainsAll(pName)) throw new ArgumentOutOfRangeException(nameof(pName));
            mName = pName;

            if (pAddress == null) throw new ArgumentNullException(nameof(pAddress));

            bool lOK;
            cHeaderFieldValuePart lMailbox;

            if (string.IsNullOrWhiteSpace(pAddress.DisplayName)) lOK = cHeaderFieldValuePart.TryAsAddrSpec(ZRemoveQuotes(pAddress.User), pAddress.Host, out lMailbox);
            else lOK = cHeaderFieldValuePart.TryAsNameAddr(pAddress.DisplayName, ZRemoveQuotes(pAddress.User), pAddress.Host, out lMailbox);

            if (!lOK) throw new ArgumentOutOfRangeException(nameof(pAddress));

            var lValueParts = new List<cHeaderFieldValuePart>();
            lValueParts.Add(lMailbox);
            mValueParts = lValueParts.AsReadOnly();

            mUTF8ValueParts = null;
        }

        public cHeaderFieldAppendDataPart(string pName, IEnumerable<MailAddress> pAddresses)
        {
            // encoding in pAddress is ignored

            if (pName == null) throw new ArgumentNullException(nameof(pName));
            if (pName.Length == 0) throw new ArgumentOutOfRangeException(nameof(pName));
            if (!cCharset.FText.ContainsAll(pName)) throw new ArgumentOutOfRangeException(nameof(pName));
            mName = pName;

            if (pAddresses == null) throw new ArgumentNullException(nameof(pAddresses));

            bool lFirst = true;
            var lMailboxList = new List<cHeaderFieldValuePart>();

            foreach (var lAddress in pAddresses)
            {
                if (lFirst) lFirst = false;
                else lMailboxList.Add(cHeaderFieldValuePart.COMMA);

                if (lAddress == null) throw new ArgumentOutOfRangeException(nameof(pAddresses), kArgumentOutOfRangeExceptionMessage.ContainsNulls);

                bool lOK;
                cHeaderFieldValuePart lMailbox;

                if (string.IsNullOrWhiteSpace(lAddress.DisplayName)) lOK = cHeaderFieldValuePart.TryAsAddrSpec(ZRemoveQuotes(lAddress.User), lAddress.Host, out lMailbox);
                else lOK = cHeaderFieldValuePart.TryAsNameAddr(lAddress.DisplayName, ZRemoveQuotes(lAddress.User), lAddress.Host, out lMailbox);

                if (!lOK) throw new ArgumentOutOfRangeException(nameof(pAddresses), kArgumentOutOfRangeExceptionMessage.CantConvert + lAddress.ToString());

                lMailboxList.Add(lMailbox);
            }

            mValueParts = lMailboxList.AsReadOnly();

            mUTF8ValueParts = null;
        }

        public cHeaderFieldAppendDataPart(string pName, IEnumerable<cHeaderFieldValuePart> pValueParts, IEnumerable<cHeaderFieldValuePart> pUTF8ValueParts = null)
        {
            if (pName == null) throw new ArgumentNullException(nameof(pName));
            if (pValueParts == null) throw new ArgumentNullException(nameof(pValueParts));
            if (pName.Length == 0) throw new ArgumentOutOfRangeException(nameof(pName));
            if (!cCharset.FText.ContainsAll(pName)) throw new ArgumentOutOfRangeException(nameof(pName));
            mName = pName;

            mValueParts = new List<cHeaderFieldValuePart>(pValueParts).AsReadOnly();

            if (pUTF8ValueParts == null) mUTF8ValueParts = null;
            else mUTF8ValueParts = new List<cHeaderFieldValuePart>(pUTF8ValueParts).AsReadOnly();
        }

        internal override bool HasContent => true;

        internal override IList<byte> GetBytes(Encoding pEncoding)
        {
            cHeaderFieldBytes lBytes = new cHeaderFieldBytes(mName, pEncoding);

            ReadOnlyCollection<cHeaderFieldValuePart> lValueParts;
            if (lBytes.UTF8Allowed && mUTF8ValueParts != null) lValueParts = mUTF8ValueParts;
            else lValueParts = mValueParts;

            foreach (var lPart in lValueParts) lPart.GetBytes(lBytes, eHeaderFieldValuePartContext.unstructured);

            lBytes.AddNewLine();
            return lBytes.Bytes;
        }

        public override string ToString()
        {
            cListBuilder lBuilder = new cListBuilder(nameof(cHeaderFieldAppendDataPart));

            lBuilder.Append(mName);

            cListBuilder lVP = new cListBuilder(nameof(mValueParts));
            foreach (var lPart in mValueParts) lVP.Append(lPart);
            lBuilder.Append(lVP.ToString());

            if (mUTF8ValueParts != null)
            {
                cListBuilder lUTF8VP = new cListBuilder(nameof(mUTF8ValueParts));
                foreach (var lPart in mUTF8ValueParts) lUTF8VP.Append(lPart);
                lBuilder.Append(lUTF8VP.ToString());
            }

            return lBuilder.ToString();
        }

        private string ZRemoveQuotes(string pPossiblyQuotedString)
        {
            if (pPossiblyQuotedString == null) return null;
            if (pPossiblyQuotedString.Length < 2) return pPossiblyQuotedString;
            if (pPossiblyQuotedString[0] != '"' || pPossiblyQuotedString[pPossiblyQuotedString.Length - 1] != '"') return pPossiblyQuotedString;
            return pPossiblyQuotedString.Substring(1, pPossiblyQuotedString.Length - 2);
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
            ZTestUnstructured("joins.1", "    A𠈓C A𠈓C fred fr€d fr€d fred  fr€d    fr€d    fred    fred ", "    A𠈓C A𠈓C fred\r\n fr€d fr€d fred  fr€d fr€d\r\n    fred    fred", "utf-8", "    =?utf-8?b?QfCgiJNDIEHwoIiTQw==?= fred\r\n =?utf-8?b?ZnLigqxkIGZy4oKsZA==?= fred  =?utf-8?b?ZnLigqxkIGZy4oKsZA==?=\r\n    fred    fred");

            // if a line ends with an encoded word and the next line begins with an encoded word, a space is added to the beginning of the second encoded word to prevent them being joined on decoding
            ZTestUnstructured("spaces.1", "    A𠈓C\r\n A𠈓C\r\n fred\r\n fr€d fr€d fred  fr€d fr€d    fred \r\n   fred ", "    A𠈓C A𠈓C\r\n fred\r\n fr€d fr€d fred  fr€d fr€d\r\n    fred\r\n   fred", "utf-8");

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

            lPart = new cHeaderFieldAppendDataPart("x", new cHeaderFieldValuePart[] { new cHeaderFieldCommentValuePart("Keld J\"#$%&'(),.:;<>@[\\]^`{|}~ørn Simonsen") });
            ZTest("q.2", lPart, null, "ISO-8859-1", "x:(Keld =?iso-8859-1?q?J\"#$%&'=28=29,.:;<>@[=5C]^`{|}~=F8rn?= Simonsen)");

            // phrase
            lPart = new cHeaderFieldAppendDataPart("x",
                new cHeaderFieldValuePart[] {
                    new cHeaderFieldPhraseValuePart("Keld J\"#$%&'(),.:;<>@[\\]^`{|}~orn J\"#$%&ørn4567890123456789012345678901234567890 J'(),.ørn4567890123456789012345678901234567 J:;<>@ørn4567890123456789012345678901234567 J[\\]^`ørn4567890123456789012345678901234567 J{|}~ørn4567890123456789012345678901234567 Simonsen")
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
                    new cHeaderFieldPhraseValuePart(
                        new cHeaderFieldCommentOrTextValuePart[]
                        {
                            new cHeaderFieldTextValuePart("phrase is only "),
                            new cHeaderFieldCommentValuePart(
                                new cHeaderFieldCommentOrTextValuePart[]
                                {
                                    new cHeaderFieldTextValuePart("not really"),
                                    new cHeaderFieldCommentValuePart("sorry")
                                }),
                            new cHeaderFieldTextValuePart("used in display name")
                        }
                        ),
                    new cHeaderFieldTextValuePart("<Muhammed."),
                    new cHeaderFieldCommentValuePart("I am the greatest"),
                    new cHeaderFieldTextValuePart(" Ali @"),
                    new cHeaderFieldCommentValuePart("the"),
                    new cHeaderFieldTextValuePart("Vegas.WBA>")
                }
                );

            ZTest("ccp.1", lPart, "x:phrase is only(not really(sorry))used in display name <Muhammed.(I am the\r\n greatest) Ali @(the)Vegas.WBA>");




            lPart = new cHeaderFieldAppendDataPart("x", new cHeaderFieldValuePart[] { new cHeaderFieldCommentValuePart("ts is a really long comment with one utf8 wørd in it (the word is the only one that should be \"treated\" differently if utf8 is on or off)") });
            //                                                                                                                                                                                     12345678901234567890123456789012345678901234567890123456 7890123456789012345678    12345678901234567890 12345678 90123456789012345678901234567890123456789012345678
            ZTest("cc.1", lPart, "x:(ts is a really long comment with one utf8 wørd in it \\(the word is the only\r\n one that should be \"treated\" differently if utf8 is on or off\\))", null, "x:(ts is a really long comment with one utf8 wørd in it \\(the word is the only\r\n one that should be \"treated\" differently if utf8 is on or off\\))");
            ZTest("cc.2", lPart, "x:(ts is a really long comment with one utf8 wørd in it\r\n \\(the word is the only one that should be \"treated\" differently if utf8 is on\r\n or off\\))", "utf-8",
            //   1234567890123456789012345678901234567890123456789012345678901234567890123451 234567890123456789012345678901234567890123 45678901 234567890123456789012345678
                "x:(ts is a really long comment with one utf8 =?utf-8?b?d8O4cmQ=?= in it\r\n \\(the word is the only one that should be \"treated\" differently if utf8 is on\r\n or off\\))");

            lPart = new cHeaderFieldAppendDataPart("x", new cHeaderFieldValuePart[] { new cHeaderFieldCommentValuePart("tis is a really long comment with one utf8 wørd in it (the word is the only one that should be \"treated\" differently if utf8 is on or off)") });
            //                                                                                                                                                                                      12345678901234567890123456789012345678901234567890123456 789012345678901234    1234567890123456789012345 67890123 456789012345678901234567890123456789012345678
            ZTest("cc.3", lPart, "x:(tis is a really long comment with one utf8 wørd in it \\(the word is the\r\n only one that should be \"treated\" differently if utf8 is on or off\\))", null, "x:(tis is a really long comment with one utf8 wørd in it \\(the word is the\r\n only one that should be \"treated\" differently if utf8 is on or off\\))");



            // test blank line suppression
            ZTestUnstructured("b.1", "here is\r\n \r\n a test\r\n \r\n ", "here is\r\n a test");

            // test insertion of space

            lPart = new cHeaderFieldAppendDataPart("x",
                new cHeaderFieldValuePart[]
                {
                    new cHeaderFieldTextValuePart("here is"),
                    new cHeaderFieldTextValuePart("a test"),
                    new cHeaderFieldCommentValuePart("this is a comment"),
                    new cHeaderFieldTextValuePart("followed by text"),
                    new cHeaderFieldPhraseValuePart("an.d a ph.rase")
                }
                );

            ZTest("sp.1", lPart, "x:here is a test(this is a comment)followed by text \"an.d\" a \"ph.rase\"");


            lPart = new cHeaderFieldAppendDataPart("12345678901234567890123456789012345678901234567890123456789012345678901234567890", ".");

            ZTest("long.1", lPart, "12345678901234567890123456789012345678901234567890123456789012345678901234567890:\r\n .");


            // parameters

            lPart = new cHeaderFieldAppendDataPart("content-type",
                new cHeaderFieldValuePart[]
                {
                    new cHeaderFieldTextValuePart("message/external-body"),
                    new cHeaderFieldMIMEParameter("access-type", "URL"),
                    new cHeaderFieldMIMEParameter("URL", "ftp://cs.utk.edu/pub/moore/bulk-mailer/bulk-mailer.tar")
                }
                );

            //                     12345678901234567890123456789012345678901234567890123456789012345678901234567890
            ZTestMime("1", lPart, "content-type:message/external-body ;access-type=URL\r\n ;URL=\"ftp://cs.utk.edu/pub/moore/bulk-mailer/bulk-mailer.tar\"");

            lPart = new cHeaderFieldAppendDataPart("content-type",
                new cHeaderFieldValuePart[]
                {
                    new cHeaderFieldTextValuePart("message/external-body"),
                    new cHeaderFieldMIMEParameter("access-type", "URL"),
                    new cHeaderFieldMIMEParameter("URL", "ftp://cs.utk.edu/pub/moore/bulk-mailer/bulk-mailer/bulk-mailer/bulk-mailer.tar")
                }
                );

            //                                                                            12345678 901234567890123456789012345678901234567890123456789012345678901234567890
            ZTestMime("2", lPart, "content-type:message/external-body ;access-type=URL\r\n ;URL*0=\"ftp://cs.utk.edu/pub/moore/bulk-mailer/bulk-mailer/bulk-mailer/bulk-\"\r\n ;URL*1=mailer.tar");

            lPart = new cHeaderFieldAppendDataPart("content-type",
                new cHeaderFieldValuePart[]
                {
                    new cHeaderFieldTextValuePart("message/external-body"),
                    new cHeaderFieldMIMEParameter("access-type", "URL"),
                    new cHeaderFieldMIMEParameter("URL", "ftp://cs.utk.€du/pub/moore/bulk-mailer/bulk-mailer/bulk-mailer/bulk-mail€r.tar")
                }
                );

            //                                                                            12345678 901234567890123456789012345678901234567890123456789012345678901234567890
            ZTestMime("2.1", lPart, "content-type:message/external-body ;access-type=URL\r\n ;URL*0=\"ftp://cs.utk.€du/pub/moore/bulk-mailer/bulk-mailer/bulk-mailer/bulk-\"\r\n ;URL*1=\"mail€r.tar\"");

            lPart = new cHeaderFieldAppendDataPart("content-type",
                new cHeaderFieldValuePart[]
                {
                    new cHeaderFieldTextValuePart("message/external-body"),
                    new cHeaderFieldMIMEParameter("access-type", "URL"),
                    new cHeaderFieldMIMEParameter("URL", "ftp://cs.utk.€du/pub/moore/bulk-mailer/bulk-mailer.tar")
                }
                );

            //                     12345678901234567890123456789012345678901234567890123456789012345678901234567890
            ZTestMime("3", lPart, "content-type:message/external-body ;access-type=URL\r\n ;URL=\"ftp://cs.utk.€du/pub/moore/bulk-mailer/bulk-mailer.tar\"");

            lPart = new cHeaderFieldAppendDataPart("content-type",
                new cHeaderFieldValuePart[]
                {
                    new cHeaderFieldTextValuePart("message/external-body"),
                    new cHeaderFieldMIMEParameter("access-type", "URL"),
                    new cHeaderFieldMIMEParameter("URL", "moøre")
                }
                );

            //                       12345678901234567890123456789012345678901234567890123456789012345678901234567890
            ZTestMime("3.1", lPart, "content-type:message/external-body ;access-type=URL ;URL*=iso-8859-1''mo%F8re", "ISO-8859-1");

            lPart = new cHeaderFieldAppendDataPart("content-type",
                new cHeaderFieldValuePart[]
                {
                    new cHeaderFieldTextValuePart("message/external-body"),
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
                    new cHeaderFieldTextValuePart("message/external-body"),
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

            ZTestDateTimeOffset("-3", new DateTimeOffset(2018, 02, 07, 01, 02, 03, new TimeSpan(-3, 0, 0)), "x:07 FEB 2018 01:02:03 -0300\r\n");
            ZTestDateTimeOffset("0", new DateTimeOffset(2018, 02, 07, 01, 02, 03, new TimeSpan(0, 0, 0)), "x:07 FEB 2018 01:02:03 +0000\r\n");
            ZTestDateTimeOffset("3", new DateTimeOffset(2018, 02, 07, 01, 02, 03, new TimeSpan(3, 0, 0)), "x:07 FEB 2018 01:02:03 +0300\r\n");


            ZTestAddress("1", new MailAddress("user@host"), "x:user@host\r\n");
            ZTestAddress("2", new MailAddress("\"display name\" <user@host>"), "x:display name<user@host>\r\n");
            ZTestAddress("3", new MailAddress("\"display name\" user@host"), "x:display name<user@host>\r\n");
            ZTestAddress("4", new MailAddress("display name <user@host>"), "x:display name<user@host>\r\n");
            ZTestAddress("5", new MailAddress("fr€d <user@host>"), "x:=?utf-8?b?ZnLigqxk?=<user@host>\r\n");
            ZTestAddress("6", new MailAddress("\"user name\"@host"), "x:\"user name\"@host\r\n");
            ZTestAddress("7", new MailAddress("user...name..@host"), "x:\"user...name..\"@host\r\n");
            ZTestAddress("8", new MailAddress("<user@[my domain]>"), null); // illegal according to rfc 5322 (dtext does not include space)
            ZTestAddress("9", new MailAddress("(comment)\"display name\"(comment)<(comment)user(comment)@(comment)domain(comment)>(comment)"), "x:display name<user@domain>\r\n");
            ZTestAddress("10", new MailAddress("<user@[my_domain]>"), "x:user@[my_domain]\r\n");

            //                                                                       123456789012345678901234567890123456789012345678901234567890123456789012345678
            ZTestAddresses(
                "1", 
                new MailAddress[]
                {
                    new MailAddress("user@host"), new MailAddress("user@host"), new MailAddress("user@host"), new MailAddress("user@host"),
                    new MailAddress("user@host"), new MailAddress("user@host"), new MailAddress("user@host"), new MailAddress("user@host")
                },
                "x:user@host,user@host,user@host,user@host,user@host,user@host,user@host,user@\r\n host\r\n");

            ZTestAddresses(
                "2",
                new MailAddress[]
                {
                },
                "x:\r\n");

            ZTestAddresses(
                "3",
                new MailAddress[]
                {
                    new MailAddress("user@host")
                },
                "x:user@host\r\n");


            lPart = new cHeaderFieldAppendDataPart("content-transfer-encoding", "7bit", "8bit");

            ZTest("utf8.1", lPart, "content-transfer-encoding:7bit", "utf-8");
            ZTest("utf8.1", lPart, "content-transfer-encoding:8bit");
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

            if (pDateTime.Kind != DateTimeKind.Local) if (lDateTimeOffset.Offset.CompareTo(TimeSpan.Zero) != 0) throw new cTestsException($"{nameof(cHeaderFieldAppendDataPart)}.DateTime({pTestName}.o)");
        }

        private static void ZTestDateTimeOffset(string pTestName, DateTimeOffset pDateTimeOffset, string pExpected)
        {
            var lPart = new cHeaderFieldAppendDataPart("x", pDateTimeOffset);
            var lBytes = lPart.GetBytes(null);

            var lCursor = new cBytesCursor(lBytes);

            if (!lCursor.SkipByte(cASCII.x) || !lCursor.SkipByte(cASCII.COLON) || !lCursor.GetRFC822DateTime(out var lDateTimeOffset, out var lDateTime) || !lCursor.SkipByte(cASCII.CR) || !lCursor.SkipByte(cASCII.LF) || !lCursor.Position.AtEnd) throw new cTestsException($"{nameof(cHeaderFieldAppendDataPart)}.DateTime({pTestName}.i)");

            if (lDateTimeOffset != pDateTimeOffset) throw new cTestsException($"{nameof(cHeaderFieldAppendDataPart)}.DateTimeOffset({pTestName}.r)");
            string lString = cTools.ASCIIBytesToString(lBytes);
            if (lString != pExpected) throw new cTestsException($"{nameof(cHeaderFieldAppendDataPart)}.DateTimeOffset({pTestName}.e,{lString})");
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
            cHeaderFieldAppendDataPart lPart = new cHeaderFieldAppendDataPart("x", new cHeaderFieldValuePart[] { new cHeaderFieldPhraseValuePart(pString) });

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

        private static void ZTestAddress(string pTestName, MailAddress pAddress, string pExpected)
        {
            cHeaderFieldAppendDataPart lPart;

            try { lPart = new cHeaderFieldAppendDataPart("x", pAddress); }
            catch { lPart = null; }

            if (lPart == null)
            {
                if (pExpected == null) return;
                throw new cTestsException($"{nameof(cHeaderFieldAppendDataPart)}.Address({pTestName}.f)");
            }

            var lString = cTools.ASCIIBytesToString(lPart.GetBytes(Encoding.UTF8));
            if (lString != pExpected) throw new cTestsException($"{nameof(cHeaderFieldAppendDataPart)}.Address({pTestName}: {lString})");
        }

        private static void ZTestAddresses(string pTestName, IEnumerable<MailAddress> pAddresses, string pExpected)
        {
            cHeaderFieldAppendDataPart lPart;

            try { lPart = new cHeaderFieldAppendDataPart("x", pAddresses); }
            catch { lPart = null; }

            if (lPart == null)
            {
                if (pExpected == null) return;
                throw new cTestsException($"{nameof(cHeaderFieldAppendDataPart)}.Addresses({pTestName}.f)");
            }

            var lString = cTools.ASCIIBytesToString(lPart.GetBytes(Encoding.UTF8));
            if (lString != pExpected) throw new cTestsException($"{nameof(cHeaderFieldAppendDataPart)}.Addresses({pTestName}: {lString})");
        }
    }

    public class cFileAppendDataPart : cAppendDataPart
    {
        public readonly string Path;
        public readonly uint Length; // if encoding, this is the encoded length
        public readonly bool Base64Encode;
        public readonly cBatchSizerConfiguration ReadConfiguration; // optional

        public cFileAppendDataPart(string pPath, bool pBase64Encode = false, cBatchSizerConfiguration pReadConfiguration = null)
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

        public cStreamAppendDataPart(Stream pStream, bool pBase64Encode = false, cBatchSizerConfiguration pReadConfiguration = null)
        {
            Stream = pStream ?? throw new ArgumentNullException(nameof(pStream));
            if (!pStream.CanRead || !pStream.CanSeek) throw new ArgumentOutOfRangeException(nameof(pStream));

            long lLength;
            if (pBase64Encode) lLength = cBase64Encoder.EncodedLength(pStream.Length);
            else lLength = pStream.Length;

            if (lLength > uint.MaxValue) throw new ArgumentOutOfRangeException(nameof(pStream));
            Length = (uint)lLength;

            Base64Encode = pBase64Encode;
            ReadConfiguration = pReadConfiguration;
        }

        public cStreamAppendDataPart(Stream pStream, uint pLength, bool pBase64Encode = false, cBatchSizerConfiguration pReadConfiguration = null)
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