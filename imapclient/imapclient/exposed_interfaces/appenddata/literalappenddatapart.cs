using System;
using System.Collections.Generic;
using System.Text;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
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
            mBytes = new cBytes(new List<byte>(pBytes));
        }

        public cLiteralAppendDataPart(string pString)
        {
            mBytes = new cBytes(pString);
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

        public void Add(string pText)
        {
            if (pText == null) throw new ArgumentNullException(nameof(pText));
            mParts.Add(new cHeaderFieldUnstructuredPart(pText));
        }

        public void Add(cHeaderFieldComment pComment)
        {
            mParts.Add(pComment);
        }

        public void Add(cHeaderFieldPhrase pPhrase)
        {
            mParts.Add(pPhrase);
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
            ZTest("8.1.1", "Keld Jørn Simonsen", "Keld =?iso-8859-1?q?J=F8rn?= Simonsen", null, "ISO-8859-1");
            ZTest("8.1.2", "Keld Jøørn Simonsen", "Keld =?iso-8859-1?b?Svj4cm4=?= Simonsen", null, "ISO-8859-1"); // should switch to base64
            ZTest("8.1.3", "Keld Jørn Simonsen", "Keld =?utf-8?b?SsO4cm4=?= Simonsen"); // should use utf8

            // adjacent words that need to be encoded are encoded together with one space between them
            ZTest("joins.1", "    A𠈓C A𠈓C fred fr€d fr€d fred  fr€d    fr€d    fred    fred ", "    =?utf-8?b?QfCgiJNDIEHwoIiTQw==?= fred =?utf-8?b?ZnLigqxkIGZy4oKsZA==?= fred  =?utf-8?b?ZnLigqxkIGZy4oKsZA==?=    fred    fred ", "    A𠈓C A𠈓C fred fr€d fr€d fred  fr€d fr€d    fred    fred ");

            // if a line ends with an encoded word and the next line begins with an encoded word, a space is added to the beginning of the second encoded word to prevent them being joined on decoding
            ZTest("spaces.1", "    A𠈓C\r\n A𠈓C\r\n fred\r\n fr€d fr€d fred  fr€d fr€d    fred \r\n   fred ", null, "    A𠈓C A𠈓C\r\n fred\r\n fr€d fr€d fred  fr€d fr€d    fred \r\n   fred ");

            // check that adjacent encoded words are in fact joined
            ZTest("long.1",
                " 12345678901234567890123456789012345678901234567890123456789012345678901234567890\r\n 1234567890123456789012345678901234567890€12345678901234\r\n 1234567890123456789012345678901234567890€12345678901\r\n 1234567890123456789012345678901234567890€123456789012",
                null,
                " 12345678901234567890123456789012345678901234567890123456789012345678901234567890\r\n 1234567890123456789012345678901234567890€12345678901234 1234567890123456789012345678901234567890€12345678901 1234567890123456789012345678901234567890€123456789012"
                );

            // check that each encoded word is a whole number of characters
            ZTest("charcounting.1", " 𠈓𠈓𠈓𠈓𠈓a𠈓𠈓𠈓𠈓𠈓𠈓 fred 𠈓𠈓𠈓𠈓𠈓ab𠈓𠈓𠈓𠈓𠈓𠈓\r\n \r\n", " =?utf-8?b?8KCIk/CgiJPwoIiT8KCIk/CgiJNh8KCIk/CgiJPwoIiT8KCIk/CgiJPwoIiT?= fred =?utf-8?b?8KCIk/CgiJPwoIiT8KCIk/CgiJNhYvCgiJPwoIiT8KCIk/CgiJPwoIiT?= =?utf-8?b?8KCIkw==?=\r\n \r\n");

            // q-encoding rule checks

            //  unstructured - e.g. subject
            ZTest("q.1", "Keld J\"#$%&'(),.:;<>@[\\]^`{|}~ørn Simonsen", "Keld =?iso-8859-1?q?J\"#$%&'(),.:;<>@[\\]^`{|}~=F8rn?= Simonsen", null, "ISO-8859-1");

            //  ccontent - in a comment
            //ZTest("q.2", eEncodedWordsLocation.ccontent, "Keld J\"#$%&'(),.:;<>@[\\\\]^`{|}~ørn Simonsen", "Keld =?iso-8859-1?q?J\"#$%&'=28=29,.:;<>@[=5C]^`{|}~=F8rn?= Simonsen", "Keld J\"#$%&'(),.:;<>@[\\]^`{|}~ørn Simonsen", "ISO-8859-1");

            //  qcontent - in a quoted string
            //ZTest("q.3", eEncodedWordsLocation.qcontent, "Keld J\"#$%&'(),.:;<>@[\\\\]^`{|}~ørn Simonsen", "Keld =?iso-8859-1?q?J=22#$%&'(),.:;<>@[=5C]^`{|}~=F8rn?= Simonsen", "Keld J\"#$%&'(),.:;<>@[\\]^`{|}~ørn Simonsen", "ISO-8859-1");

            // check that a word that looks like an encoded word gets encoded
            //;?;




            //ZTest("8.1.1", eEncodedWordsLocation.qcontent, "Keld Jørn Simonsen", "Keld =?iso-8859-1?q?J=F8rn?= Simonsen", null, "ISO-8859-1");


            //;?; // more tests to do
        }


        private static void ZTest(string pTestName, string pString, string pExpectedI = null, string pExpectedF = null, string pCharsetName = null)
        {
            Encoding lEncoding;
            if (pCharsetName == null) lEncoding = null;
            else lEncoding = Encoding.GetEncoding(pCharsetName);
            cUnstructuredTextAppendDataPart lEW = new cUnstructuredTextAppendDataPart(pString, lEncoding);
            var lBytes = lEW.GetBytes(Encoding.UTF8);

            cCulturedString lCS = new cCulturedString(lBytes);

            string lString;

            // for stepping through
            lString = cTools.ASCIIBytesToString(lBytes);

            lString = lCS.ToString();
            if (lString != (pExpectedF ?? pString)) throw new cTestsException($"{nameof(cUnstructuredTextAppendDataPart)}({pTestName}.f : {lString})");

            if (pExpectedI == null) return;

            lString = cTools.ASCIIBytesToString(lBytes);
            if (lString != pExpectedI) throw new cTestsException($"{nameof(cUnstructuredTextAppendDataPart)}({pTestName}.i : {lString})");
        }

    }

    /*
    public class cMIMEParameterAppendDataPart : cLiteralAppendDataPartBase
    {


        public override string ToString()
    } */
}