using System;
using System.Collections.Generic;
using System.Text;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
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
        private readonly string mFieldName;
        private readonly List<cHeaderFieldValuePart> mParts = new List<cHeaderFieldValuePart>();

        public cHeaderFieldAppendDataPart(string pFieldName)
        {
            if (pFieldName == null) throw new ArgumentNullException(nameof(pFieldName));
            if (pFieldName.Length == 0) throw new ArgumentOutOfRangeException(nameof(pFieldName));
            if (!cCharset.FText.ContainsAll(pFieldName)) throw new ArgumentOutOfRangeException(nameof(pFieldName));
            mFieldName = pFieldName;
        }

        public void AddUnstructured(string pText)
        {
            if (pText == null) throw new ArgumentNullException(nameof(pText));
            mParts.Add(new cHeaderFieldUnstructuredPart(pText));
        }

        public void AddComment(string pText)
        {
            cHeaderFieldComment lComment = new cHeaderFieldComment();
            lComment.Add(pText);
            mParts.Add(lComment);
        }

        public void AddPhrase(string pText)
        {
            cHeaderFieldPhrase lPhrase = new cHeaderFieldPhrase();
            lPhrase.Add(pText);
            mParts.Add(lPhrase);
        }

        public void Add(cHeaderFieldComment pComment)
        {
            mParts.Add(pComment);
        }

        public void Add(cHeaderFieldPhrase pPhrase)
        {
            mParts.Add(pPhrase);
        }

        public void AddParameter(string pAttribute, string pValue)
        {
            ;?; // validate that the attribute name is a token
            ;?; // validate that value is printable-ascii or WSP OR utf-8 (regardless of the utf8 setting)

            //  note utf-8 can be placed in the quoted string if utf8 is on 
            // output a single word of the form ';ATTR={value as token or value as quoted-string}' IFF VALUE is all printable ascii WSP (or utf8 is on) AND ";ATTR=value" (including all quoting (both the " and the \ quoting)) is 77 chars or less
            //  else
            //   if the value is all printable ascii and WSP (or utf8 is on)
            //    output multiple words all starting with ;ATTR*<n>={value as token or q-s} where n = 0.. and each word is 77 chars or less (quoting or not quoting depends on the data in the part being output in the word
            //   else
            //    <here only if utf8 is off and there are utf8 chars>
            //    output 
            //     a single word of the form ";ATTR*=<charset>''valuepart" where valuepart is in PEform (% encode bytes that are <= ' ', del, *, ', %, tspecials from rfc2045)
            //     then other parts of the value in either ;ATTR*<n>={value as token or q-s} if all chars are printable ascii and WSP OR
            //                                             ;ATTR*<n>*=valuepart in PEform otherwise 
            //                (PEform should have the whole char rule)

            ;?;
        }


        internal override bool HasContent => true;

        internal override IList<byte> GetBytes(Encoding pEncoding)
        {
            cHeaderFieldBytes lBytes = new cHeaderFieldBytes(mFieldName, pEncoding);
            foreach (var lPart in mParts) lPart.GetBytes(lBytes);
            lBytes.AddNewLine();
            return lBytes.Bytes;
        }

        public override string ToString()
        {
            cListBuilder lBuilder = new cListBuilder(nameof(cHeaderFieldAppendDataPart));
            lBuilder.Append(mFieldName);
            foreach (var lPart in mParts) lBuilder.Append(lPart);
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

            lPart.AddUnstructured("\r\n\tx\r\n x\r\n ");

            try
            {
                lPart.AddUnstructured("\r\nx\r\n x\r\n ");
                throw new cTestsException($"{nameof(cHeaderFieldAppendDataPart)}.1.4");
            }
            catch (ArgumentOutOfRangeException) { }

            try
            {
                lPart.AddUnstructured("\r\n\tx\r\nx\r\n ");
                throw new cTestsException($"{nameof(cHeaderFieldAppendDataPart)}.1.5");
            }
            catch (ArgumentOutOfRangeException) { }

            try
            {
                lPart.AddUnstructured("\r\n\tx\r x\r\n ");
                throw new cTestsException($"{nameof(cHeaderFieldAppendDataPart)}.1.6");
            }
            catch (ArgumentOutOfRangeException) { }

            try
            {
                lPart.AddUnstructured("\r\n\tx\n x\r\n ");
                throw new cTestsException($"{nameof(cHeaderFieldAppendDataPart)}.1.7");
            }
            catch (ArgumentOutOfRangeException) { }

            try
            {
                lPart.AddUnstructured("\r\n\t\0\r\n x\r\n ");
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
            ZTestUnstructured("joins.1", "    A𠈓C A𠈓C fred fr€d fr€d fred  fr€d    fr€d    fred    fred ", "    A𠈓C A𠈓C fred\r\n fr€d fr€d fred  fr€d fr€d\r\n fred    fred", "utf-8" , "    =?utf-8?b?QfCgiJNDIEHwoIiTQw==?= fred\r\n =?utf-8?b?ZnLigqxkIGZy4oKsZA==?= fred  =?utf-8?b?ZnLigqxkIGZy4oKsZA==?=\r\n fred    fred");

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
            lPart = new cHeaderFieldAppendDataPart("x");
            lPart.AddComment("Keld J\"#$%&'(),.:;<>@[\\]^`{|}~ørn Simonsen");
            ZTest("q.2", lPart, null, "ISO-8859-1", "x:(Keld =?iso-8859-1?q?J\"#$%&'=28=29,.:;<>@[=5C]^`{|}~=F8rn?= Simonsen)");

            // phrase
            lPart = new cHeaderFieldAppendDataPart("x");
            //lPart.AddPhrase("Keld J\"#$%&'(),.:;<>@[\\]^`{|}~orn J\"#$%&'(),.^`{|}~ørn12345678901234567890123456 J#$%&':;<>@^`{|}~ørn12345678901234567890123456 J#$%&'[\\]^`{|}~ørn12345678901234567890123456 Simonsen");
            lPart.AddPhrase("Keld J\"#$%&'(),.:;<>@[\\]^`{|}~orn J\"#$%&ørn4567890123456789012345678901234567890 J'(),.ørn4567890123456789012345678901234567 J:;<>@ørn4567890123456789012345678901234567 J[\\]^`ørn4567890123456789012345678901234567 J{|}~ørn4567890123456789012345678901234567 Simonsen");
            //                                                                                        1234567890123456789012345678901234567890123456789012345678901234567890123456
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
            lPart = new cHeaderFieldAppendDataPart("x");

            cHeaderFieldPhrase lPhrase = new cHeaderFieldPhrase();
            lPhrase.Add("phrase is only ");
            cHeaderFieldComment lComment = new cHeaderFieldComment();
            lComment.Add("not really");
            lComment.AddComment("sorry");
            lPhrase.Add(lComment);
            lPhrase.Add("used in display name");

            lPart.Add(lPhrase);
            lPart.AddUnstructured("<Muhammed.");
            lPart.AddComment("I am the greatest");
            lPart.AddUnstructured(" Ali @");
            lPart.AddComment("the");
            lPart.AddUnstructured("Vegas.WBA>");

            //                     123456789012345678901234567890123456789012345678901234567890123456789012345678
            ZTest("ccp.1", lPart, "x:phrase is only(not really(sorry))used in display name <Muhammed.(I am the\r\n greatest) Ali @(the)Vegas.WBA>");




            lPart = new cHeaderFieldAppendDataPart("x");
            lPart.AddComment("ts is a really long comment with one utf8 wørd in it (the word is the only one that should be \"treated\" differently if utf8 is on or off)");
            //                                                                                                                                                                                     12345678901234567890123456789012345678901234567890123456 7890123456789012345678    12345678901234567890 12345678 90123456789012345678901234567890123456789012345678
            ZTest("cc.1", lPart, "x:(ts is a really long comment with one utf8 wørd in it \\(the word is the only\r\n one that should be \"treated\" differently if utf8 is on or off\\))", null, "x:(ts is a really long comment with one utf8 wørd in it \\(the word is the only\r\n one that should be \"treated\" differently if utf8 is on or off\\))");
            ZTest("cc.2", lPart, "x:(ts is a really long comment with one utf8 wørd in it\r\n \\(the word is the only one that should be \"treated\" differently if utf8 is on\r\n or off\\))", "utf-8",
            //   1234567890123456789012345678901234567890123456789012345678901234567890123451 234567890123456789012345678901234567890123 45678901 234567890123456789012345678
                "x:(ts is a really long comment with one utf8 =?utf-8?b?d8O4cmQ=?= in it\r\n \\(the word is the only one that should be \"treated\" differently if utf8 is on\r\n or off\\))");

            lPart = new cHeaderFieldAppendDataPart("x");
            lPart.AddComment("tis is a really long comment with one utf8 wørd in it (the word is the only one that should be \"treated\" differently if utf8 is on or off)");
            //                                                                                                                                                                                      12345678901234567890123456789012345678901234567890123456 789012345678901234    1234567890123456789012345 67890123 456789012345678901234567890123456789012345678
            ZTest("cc.3", lPart, "x:(tis is a really long comment with one utf8 wørd in it \\(the word is the\r\n only one that should be \"treated\" differently if utf8 is on or off\\))", null, "x:(tis is a really long comment with one utf8 wørd in it \\(the word is the\r\n only one that should be \"treated\" differently if utf8 is on or off\\))");



            // test blank line suppression
            ZTestUnstructured("b.1", "here is\r\n \r\n a test\r\n \r\n ", "here is\r\n a test");

            // test insertion of space

            lPart = new cHeaderFieldAppendDataPart("x");
            lPart.AddUnstructured("here is");
            lPart.AddUnstructured("a test");
            lPart.AddComment("this is a comment");
            lPart.AddUnstructured("followed by text");
            lPart.AddPhrase("an.d a ph.rase");

            ZTest("sp.1", lPart, "x:here is a test(this is a comment)followed by text \"an.d\" a \"ph.rase\"");


            lPart = new cHeaderFieldAppendDataPart("12345678901234567890123456789012345678901234567890123456789012345678901234567890");
            lPart.AddUnstructured(".");

            ZTest("long.1", lPart, "12345678901234567890123456789012345678901234567890123456789012345678901234567890:\r\n .");

        }

        private static void ZTestUnstructured(string pTestName, string pString, string pExpectedF = null, string pCharsetName = null, string pExpectedI = null)
        {
            cHeaderFieldAppendDataPart lPart = new cHeaderFieldAppendDataPart("fred");
            lPart.AddUnstructured(pString);

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
            cHeaderFieldAppendDataPart lPart = new cHeaderFieldAppendDataPart("x");
            lPart.AddPhrase(pString);

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

    }
}