using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    public partial class cHeaderFieldFactory
    {
        //        /// When generating header fields and RFC 2047 encoded words or RFC 2231 MIME parameters are , then this encoding is used.


        private readonly bool mUTF8Headers;
        private readonly Encoding mEncoding;
        private readonly cBytes mCharsetNameBytes;

        internal cHeaderFieldFactory(bool pUTF8Headers, Encoding pEncoding)
        {
            mUTF8Headers = pUTF8Headers;
            mEncoding = pEncoding ?? throw new ArgumentNullException(nameof(pEncoding));
            mCharsetNameBytes = new cBytes(cTools.GetCharsetNameBytes(pEncoding));
        }

        public cLiteralMessageDataPart Unstructured(string pFieldName, string pText)
        {
            var lBytes = new cHeaderFieldBytes(mUTF8Headers, mEncoding, mCharsetNameBytes, pFieldName);
            if (!lBytes.TryAdd(pText, eHeaderFieldTextContext.unstructured)) return null;
            return lBytes.GetMessageDataPart();
        }

        public cLiteralMessageDataPart Phrases(string pFieldName, params string[] pPhrases) => Phrases(pFieldName, pPhrases as IEnumerable<string>);

        public cLiteralMessageDataPart Phrases(string pFieldName, IEnumerable<string> pPhrases)
        {
            var lBytes = new cHeaderFieldBytes(mUTF8Headers, mEncoding, mCharsetNameBytes, pFieldName);

            if (pPhrases == null) throw new ArgumentNullException(nameof(pPhrases));

            bool lFirst = false;

            foreach (var lPhrase in pPhrases)
            {
                if (lPhrase == null) throw new ArgumentOutOfRangeException(nameof(pPhrases), kArgumentOutOfRangeExceptionMessage.ContainsNulls);

                if (lFirst) lFirst = false;
                else if (!lBytes.TryAdd(",", eHeaderFieldTextContext.structured)) throw new cInternalErrorException($"{nameof(cHeaderFieldFactory)}.{nameof(Phrases)}.1");

                if (!lBytes.TryAdd(lPhrase, eHeaderFieldTextContext.phrase)) return null;
            }

            return lBytes.GetMessageDataPart();
        }

        public cLiteralMessageDataPart Phrases(string pFieldName, params cHeaderPhraseValue[] pPhrases) => Phrases(pFieldName, pPhrases as IEnumerable<cHeaderPhraseValue>);

        public cLiteralMessageDataPart Phrases(string pFieldName, IEnumerable<cHeaderPhraseValue> pPhrases)
        {
            var lBytes = new cHeaderFieldBytes(mUTF8Headers, mEncoding, mCharsetNameBytes, pFieldName);

            if (pPhrases == null) throw new ArgumentNullException(nameof(pPhrases));

            bool lFirst = false;

            foreach (var lPhrase in pPhrases)
            {
                if (lPhrase == null) throw new ArgumentOutOfRangeException(nameof(pPhrases), kArgumentOutOfRangeExceptionMessage.ContainsNulls);

                if (lFirst) lFirst = false;
                else if (!lBytes.TryAdd(",", eHeaderFieldTextContext.structured)) throw new cInternalErrorException($"{nameof(cHeaderFieldFactory)}.{nameof(Phrases)}.2");

                if (!ZTryAddPhrase(lPhrase, lBytes)) return null;
            }

            return lBytes.GetMessageDataPart();
        }

        public cLiteralMessageDataPart Structured(string pFieldName, string pText)
        {
            var lBytes = new cHeaderFieldBytes(mUTF8Headers, mEncoding, mCharsetNameBytes, pFieldName);
            if (!lBytes.TryAdd(pText, eHeaderFieldTextContext.structured)) return null;
            return lBytes.GetMessageDataPart();
        }

        public cLiteralMessageDataPart Structured(string pFieldName, cHeaderStructuredValue pValue)
        {
            var lBytes = new cHeaderFieldBytes(mUTF8Headers, mEncoding, mCharsetNameBytes, pFieldName);

            if (pValue == null) throw new ArgumentNullException(nameof(pValue));

            foreach (var lPart in pValue.Parts)
            {
                switch (lPart)
                {
                    case cHeaderTextValue lText:

                        if (!lBytes.TryAdd(lText.Text, eHeaderFieldTextContext.structured)) return null;
                        break;

                    case cHeaderCommentValue lComment:

                        if (!ZTryAddComment(lComment, lBytes)) return null;
                        break;

                    case cHeaderPhraseValue lPhrase:

                        if (!ZTryAddPhrase(lPhrase, lBytes)) return null;
                        break;

                    default:

                        throw new cInternalErrorException($"{nameof(cHeaderFieldFactory)}.{nameof(Structured)}");
                }
            }

            return lBytes.GetMessageDataPart();
        }

        public cLiteralMessageDataPart DateTimeValue(string pFieldName, DateTime pDateTime)
        {
            var lBytes = new cHeaderFieldBytes(mUTF8Headers, mEncoding, mCharsetNameBytes, pFieldName);
            if (!lBytes.TryAdd(cTools.GetRFC822DateTimeString(pDateTime), eHeaderFieldTextContext.structured)) throw new cInternalErrorException($"{nameof(cHeaderFieldFactory)}.{nameof(DateTimeValue)}.1");
            return lBytes.GetMessageDataPart();
        }

        public cLiteralMessageDataPart DateTimeValue(string pFieldName, DateTimeOffset pDateTimeOffset)
        {
            var lBytes = new cHeaderFieldBytes(mUTF8Headers, mEncoding, mCharsetNameBytes, pFieldName);
            if (!lBytes.TryAdd(cTools.GetRFC822DateTimeString(pDateTimeOffset), eHeaderFieldTextContext.structured)) throw new cInternalErrorException($"{nameof(cHeaderFieldFactory)}.{nameof(DateTimeValue)}.2");
            return lBytes.GetMessageDataPart();
        }

        public cLiteralMessageDataPart Mailboxes(string pFieldName, params MailAddress[] pAddresses) => Mailboxes(pFieldName, pAddresses as IEnumerable<MailAddress>);

        public cLiteralMessageDataPart Mailboxes(string pFieldName, IEnumerable<MailAddress> pAddresses)
        {
            if (pAddresses == null) throw new ArgumentNullException(nameof(pAddresses));

            var lAddresses = new List<cEmailAddress>();

            foreach (var lAddress in pAddresses)
            {
                if (lAddress == null) throw new ArgumentOutOfRangeException(nameof(pAddresses), kArgumentOutOfRangeExceptionMessage.ContainsNulls);
                if (!cEmailAddress.TryConstruct(lAddress, out var lEmailAddress)) return null;
                lAddresses.Add(lEmailAddress);
            }

            return Mailboxes(pFieldName, lAddresses);
        }

        public cLiteralMessageDataPart Mailboxes(string pFieldName, params cEmailAddress[] pAddresses) => Mailboxes(pFieldName, pAddresses as IEnumerable<cEmailAddress>);

        public cLiteralMessageDataPart Mailboxes(string pFieldName, IEnumerable<cEmailAddress> pAddresses)
        {
            var lBytes = new cHeaderFieldBytes(mUTF8Headers, mEncoding, mCharsetNameBytes, pFieldName);
            if (!ZTryAddEmailAddresses(pAddresses, lBytes)) return null;
            return lBytes.GetMessageDataPart();
        }

        public cLiteralMessageDataPart Addresses(string pFieldName, params cAddress[] pAddresses) => Addresses(pFieldName, pAddresses as IEnumerable<cAddress>);

        public cLiteralMessageDataPart Addresses(string pFieldName, IEnumerable<cAddress> pAddresses)
        {
            var lBytes = new cHeaderFieldBytes(mUTF8Headers, mEncoding, mCharsetNameBytes, pFieldName);

            if (pAddresses == null) throw new ArgumentNullException(nameof(pAddresses));

            bool lFirst = false;

            foreach (var lAddress in pAddresses)
            {
                if (lAddress == null) throw new ArgumentOutOfRangeException(nameof(pAddresses), kArgumentOutOfRangeExceptionMessage.ContainsNulls);

                if (lFirst) lFirst = false;
                else if (!lBytes.TryAdd(",", eHeaderFieldTextContext.structured)) throw new cInternalErrorException($"{nameof(cHeaderFieldFactory)}.{nameof(Addresses)}.1");

                switch (lAddress)
                {
                    case cGroupAddress lGroup:

                        if (!ZTryAddGroupAddress(lGroup, lBytes)) return null;
                        break;

                    case cEmailAddress lEmail:

                        if (!ZTryAddEmailAddress(lEmail, lBytes)) return null;
                        break;

                    default:

                        throw new cInternalErrorException($"{nameof(cHeaderFieldFactory)}.{nameof(Addresses)}.2");
                }
            }

            return lBytes.GetMessageDataPart();
        }

        public cLiteralMessageDataPart MessageId(string pFieldName, string pMessageId)
        {
            var lBytes = new cHeaderFieldBytes(mUTF8Headers, mEncoding, mCharsetNameBytes, pFieldName);
            ;?;
            return lBytes.GetMessageDataPart();
        }

        public cLiteralMessageDataPart MessageIds(string pFieldName, string pMessageIds)
        {
            var lBytes = new cHeaderFieldBytes(mUTF8Headers, mEncoding, mCharsetNameBytes, pFieldName);
            ;?;
            return lBytes.GetMessageDataPart();
        }

        public cLiteralMessageDataPart MessageIds(string pFieldName, params string[] pMessageIds) => MessageIds(pFieldName, pMessageIds as IEnumerable<string>);

        public cLiteralMessageDataPart MessageIds(string pFieldName, IEnumerable<string> pMessageIds)
        {
            var lBytes = new cHeaderFieldBytes(mUTF8Headers, mEncoding, mCharsetNameBytes, pFieldName);
            ;?;
            return lBytes.GetMessageDataPart();
        }

        public cLiteralMessageDataPart MIME(string pFieldName, string pValue, params cHeaderFieldMIMEParameter[] pParameters)
        {

        }

        public cLiteralMessageDataPart MIME(string pFieldName, string pValue, IEnumerable<cHeaderFieldMIMEParameter> pParameters)
        {

        }

        private bool ZTryAddPhrase(cHeaderPhraseValue pPhrase, cHeaderFieldBytes pBytes)
        {
            foreach (var lPart in pPhrase.Parts)
            {
                switch (lPart)
                {
                    case cHeaderTextValue lText:

                        if (!pBytes.TryAdd(lText.Text, eHeaderFieldTextContext.phrase)) return false;
                        break;

                    case cHeaderCommentValue lComment:

                        if (!ZTryAddComment(lComment, pBytes)) return false;
                        break;

                    default:

                        throw new cInternalErrorException($"{nameof(cHeaderFieldFactory)}.{nameof(ZTryAddPhrase)}");
                }
            }

            return true;
        }

        private bool ZTryAddComment(cHeaderCommentValue pComment, cHeaderFieldBytes pBytes)
        {
            if (!pBytes.TryAdd("(", eHeaderFieldTextContext.structured)) throw new cInternalErrorException($"{nameof(cHeaderFieldFactory)}.{nameof(ZTryAddComment)}.1");

            foreach (var lPart in pComment.Parts)
            {
                switch (lPart)
                {
                    case cHeaderTextValue lText:

                        if (!pBytes.TryAdd(lText.Text, eHeaderFieldTextContext.comment)) return false;
                        break;

                    case cHeaderCommentValue lComment:

                        if (!ZTryAddComment(lComment, pBytes)) return false;
                        break;

                    default:

                        throw new cInternalErrorException($"{nameof(cHeaderFieldFactory)}.{nameof(ZTryAddComment)}.2");
                }
            }

            if (!pBytes.TryAdd(")", eHeaderFieldTextContext.structured)) throw new cInternalErrorException($"{nameof(cHeaderFieldFactory)}.{nameof(ZTryAddComment)}.3");

            return true;
        }

        private bool ZTryAddGroupAddress(cGroupAddress pAddress, cHeaderFieldBytes pBytes)
        {
            if (!pBytes.TryAdd(pAddress.DisplayName.ToString(), eHeaderFieldTextContext.phrase)) return false;
            if (!pBytes.TryAdd(":", eHeaderFieldTextContext.structured)) throw new cInternalErrorException($"{nameof(cHeaderFieldFactory)}.{nameof(ZTryAddGroupAddress)}.1");
            if (!ZTryAddEmailAddresses(pAddress.EmailAddresses, pBytes)) return false;
            if (!pBytes.TryAdd(";", eHeaderFieldTextContext.structured)) throw new cInternalErrorException($"{nameof(cHeaderFieldFactory)}.{nameof(ZTryAddGroupAddress)}.2");
            return true;
        }

        private bool ZTryAddEmailAddresses(IEnumerable<cEmailAddress> pAddresses, cHeaderFieldBytes pBytes)
        {
            if (pAddresses == null) throw new ArgumentNullException(nameof(pAddresses));

            bool lFirst = false;

            foreach (var lAddress in pAddresses)
            {
                if (lAddress == null) throw new ArgumentOutOfRangeException(nameof(pAddresses), kArgumentOutOfRangeExceptionMessage.ContainsNulls);

                if (lFirst) lFirst = false;
                else if (!pBytes.TryAdd(",", eHeaderFieldTextContext.structured)) throw new cInternalErrorException($"{nameof(cHeaderFieldFactory)}.{nameof(ZTryAddEmailAddresses)}");

                if (!ZTryAddEmailAddress(lAddress, pBytes)) return false;
            }

            return true;
        }

        private bool ZTryAddEmailAddress(cEmailAddress pAddress, cHeaderFieldBytes pBytes)
        {
            if (pAddress.DisplayName == null) return ZTryAddAddrSpec(pAddress, pBytes);

            if (!pBytes.TryAdd(pAddress.DisplayName.ToString(), eHeaderFieldTextContext.phrase)) return false;
            if (!pBytes.TryAdd("<", eHeaderFieldTextContext.structured)) throw new cInternalErrorException($"{nameof(cHeaderFieldFactory)}.{nameof(ZTryAddEmailAddress)}.1");
            if (!ZTryAddAddrSpec(pAddress, pBytes)) return false;
            if (!pBytes.TryAdd(">", eHeaderFieldTextContext.structured)) throw new cInternalErrorException($"{nameof(cHeaderFieldFactory)}.{nameof(ZTryAddEmailAddress)}.2");

            return true;
        }

        private bool ZTryAddAddrSpec(cEmailAddress pAddress, cHeaderFieldBytes pBytes)
        {
            // note that the cEmailAddress may have come from the server, so the content isn't necessarily sendable ...
            if (!cCharset.WSPVChar.ContainsAll(pAddress.LocalPart) || !cValidation.IsDomain(pAddress.Domain)) return false;

            string lLocalPart;
            if (cValidation.IsDotAtom(pAddress.LocalPart)) lLocalPart = pAddress.LocalPart;
            else lLocalPart = cTools.Enquote(pAddress.LocalPart);

            // it is done this way because of the "should" in rfc 5322 3.4.1
            return pBytes.TryAdd(lLocalPart + "@" + pAddress.Domain, eHeaderFieldTextContext.structured);
        }


        ;?; // mime header - field, value, parameters ...













        internal static void _Tests(cTrace.cContext pParentContext)
        {
            if (TryAsDotAtom("", out _)) throw new cTestsException("dotatom.1", pParentContext);
            if (TryAsDotAtom(".fred", out _)) throw new cTestsException("dotatom.2", pParentContext);
            if (TryAsDotAtom("fred..fred", out _)) throw new cTestsException("dotatom.3", pParentContext);
            if (TryAsDotAtom("fred.", out _)) throw new cTestsException("dotatom.4", pParentContext);
            if (!TryAsDotAtom("fred.fred", out _)) throw new cTestsException("dotatom.5", pParentContext);

            ZTestAddrSpec("1", "non.existant", "bacome.work", "x:non.existant@bacome.work");
            ZTestAddrSpec("2", "non,existant", "bacome.work", "x:\"non,existant\"@bacome.work");
            ZTestAddrSpec("3", "non\0existant", "bacome.work", null);
            ZTestAddrSpec("4", "non.existant", "[bacome.work]", "x:non.existant@[bacome.work]");
            ZTestAddrSpec("5", "non.existant", "[bacome]work", null);

            ZTestNameAddr("1", null, "non.existant", "bacome.work", "x:<non.existant@bacome.work>");
            ZTestNameAddr("2", "", "non.existant", "bacome.work", "x:<non.existant@bacome.work>");
            ZTestNameAddr("3", " ", "non.existant", "bacome.work", "x:<non.existant@bacome.work>");
            ZTestNameAddr("4", "Keld Jørn Simonsen", "non.existant", "bacome.work", "x:Keld =?utf-8?b?SsO4cm4=?= Simonsen<non.existant@bacome.work>");

            TryAsMsgId("left", "right", out var lPart);
            cHeaderFieldBytes lBytes = new cHeaderFieldBytes("x", null);

            for (int i = 0; i < 6; i++)
            {
                if (i != 0) COMMA.GetBytes(lBytes, eHeaderFieldValuePartContext.unstructured);
                lPart.GetBytes(lBytes, eHeaderFieldValuePartContext.unstructured);
            }

            string lString = cTools.ASCIIBytesToString(lBytes.Bytes);
            if (lString != "x:<left@right>,<left@right>,<left@right>,<left@right>,<left@right>,\r\n <left@right>") throw new cTestsException("msgid");

            //  12345678901234567890123456789012345678901234567890123456789012345678901234567890
            // "x:<left@right>,<left@right>,<left@right>,<left@right>,<left@right>,<left@right>"
        }

        private static void ZTestAddrSpec(string pTestName, string pLocalPart, string pDomain, string pExpected)
        {
            cHeaderFieldBytes lBytes = new cHeaderFieldBytes("x", null);

            if (!TryAsAddrSpec(pLocalPart, pDomain, out var lPart))
            {
                if (pExpected == null) return;
                throw new cTestsException($"addrspec.{pTestName}.f");
            }

            if (pExpected == null) throw new cTestsException($"addrspec.{pTestName}.s");

            lPart.GetBytes(lBytes, eHeaderFieldValuePartContext.unstructured);
            string lString = cTools.ASCIIBytesToString(lBytes.Bytes);
            if (lString != pExpected) throw new cTestsException($"addrspec.{pTestName}.e({lString})");
        }

        private static void ZTestNameAddr(string pTestName, string pDisplayName, string pLocalPart, string pDomain, string pExpected)
        {
            cHeaderFieldBytes lBytes = new cHeaderFieldBytes("x", Encoding.UTF8);

            if (!TryAsNameAddr(pDisplayName, pLocalPart, pDomain, out var lPart))
            {
                if (pExpected == null) return;
                throw new cTestsException($"nameaddr.{pTestName}.f");
            }

            if (pExpected == null) throw new cTestsException($"nameaddr.{pTestName}.s");

            lPart.GetBytes(lBytes, eHeaderFieldValuePartContext.unstructured);
            string lString = cTools.ASCIIBytesToString(lBytes.Bytes);
            if (lString != pExpected) throw new cTestsException($"nameaddr.{pTestName}.e({lString})");
        }
    }
}