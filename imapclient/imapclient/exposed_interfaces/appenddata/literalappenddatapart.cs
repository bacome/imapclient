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
            ////                      x_____xxxxxxxxxx xx______   ^  x     xxxx                                   ^      xxxxx                                   ^      x xx
            lPart.AddPhrase("Keld J\"#$%&'(),.:;<>@[\\]^`{|}~orn J#$%&'^`{|}~ørn12345678901234567890123456 Simonsen");
            //                      x_____xxxxxxxxxx xx______   ^                                          ^      xxxxx                                   ^      x xx
            //ZTest("q.3", lPart, null, "ISO-8859-1", "x:Keld \"J\\\"#$%&'(),.:;<>@[\\\\]^`{|}~orn\" =?iso-8859-1?q?J\"#$%&'=28=29,.:;<>@[=5C]^`{|}~=F8rn?= Simonsen)");



            //  ccontent - in a comment
            //ZTest("q.2", eEncodedWordsLocation.ccontent, "Keld J\"#$%&'(),.:;<>@[\\\\]^`{|}~ørn Simonsen", "Keld =?iso-8859-1?q?J\"#$%&'=28=29,.:;<>@[=5C]^`{|}~=F8rn?= Simonsen", "Keld J\"#$%&'(),.:;<>@[\\]^`{|}~ørn Simonsen", "ISO-8859-1");

            //  qcontent - in a quoted string
            //ZTest("q.3", eEncodedWordsLocation.qcontent, "Keld J\"#$%&'(),.:;<>@[\\\\]^`{|}~ørn Simonsen", "Keld =?iso-8859-1?q?J=22#$%&'(),.:;<>@[=5C]^`{|}~=F8rn?= Simonsen", "Keld J\"#$%&'(),.:;<>@[\\]^`{|}~ørn Simonsen", "ISO-8859-1");

            // check that a word that looks like an encoded word gets encoded
            //;?;


            // properly check 76 char limit and 78 char limit

            //ZTest("8.1.1", eEncodedWordsLocation.qcontent, "Keld Jørn Simonsen", "Keld =?iso-8859-1?q?J=F8rn?= Simonsen", null, "ISO-8859-1");


            // utf-8 checks ;?;

            // check the phrase 



            //;?; // more tests to do
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

    /*
    public class cMIMEParameterAppendDataPart : cLiteralAppendDataPartBase
    {


        public override string ToString()
    } */
}