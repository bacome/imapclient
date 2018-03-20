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

        public cLiteralMessageDataPart UnstructuredValue(string pFieldName, string pText)
        {
            var lBytes = new cHeaderFieldBytes(mUTF8Headers, mEncoding, mCharsetNameBytes, pFieldName);
            if (pText == null) throw new ArgumentNullException(nameof(pText));
            if (!cCharset.WSPVChar.ContainsAll(pText)) throw new ArgumentOutOfRangeException(nameof(pText));
            if (!lBytes.TryAdd(pText, eHeaderFieldTextContext.unstructured)) throw new ArgumentOutOfRangeException(nameof(pText));
            if (!lBytes.TryGetMessageDataPart(out var lResult)) throw new ArgumentOutOfRangeException(nameof(pText));
            return lResult;
        }

        public cLiteralMessageDataPart PhraseValue(string pFieldName, params cHeaderPhraseValue[] pPhrases) => PhraseValue(pFieldName, pPhrases as IEnumerable<cHeaderPhraseValue>);

        public cLiteralMessageDataPart PhraseValue(string pFieldName, IEnumerable<cHeaderPhraseValue> pPhrases)
        {
            var lBytes = new cHeaderFieldBytes(mUTF8Headers, mEncoding, mCharsetNameBytes, pFieldName);

            if (pPhrases == null) throw new ArgumentNullException(nameof(pPhrases));

            bool lFirst = false;

            foreach (var lPhrase in pPhrases)
            {
                if (lPhrase == null) throw new ArgumentOutOfRangeException(nameof(pPhrases), kArgumentOutOfRangeExceptionMessage.ContainsNulls);

                if (lFirst) lFirst = false;
                else if (!lBytes.TryAdd(",", eHeaderFieldTextContext.structured)) throw new ArgumentOutOfRangeException(nameof(pPhrases));

                if (!ZAddPhrase(lPhrase, lBytes)) throw new ArgumentOutOfRangeException(nameof(pPhrases));
            }

            if (!lBytes.TryGetMessageDataPart(out var lResult)) throw new ArgumentOutOfRangeException(nameof(pPhrases));
            return lResult;
        }

        public cLiteralMessageDataPart StructuredValue(string pFieldName, cHeaderStructuredValue pValue)
        {
            var lBytes = new cHeaderFieldBytes(mUTF8Headers, mEncoding, mCharsetNameBytes, pFieldName);

            if (pValue == null) throw new ArgumentNullException(nameof(pValue));

            foreach (var lPart in pValue.Parts)
            {
                switch (lPart)
                {
                    case cHeaderTextValue lText:

                        if (!lBytes.TryAdd(lText.Text, eHeaderFieldTextContext.structured)) throw new ArgumentOutOfRangeException(nameof(pValue));
                        break;

                    case cHeaderCommentValue lComment:

                        if (!ZAddComment(lComment, lBytes)) throw new ArgumentOutOfRangeException(nameof(pValue));
                        break;

                    case cHeaderPhraseValue lPhrase:

                        if (!ZAddPhrase(lPhrase, lBytes)) throw new ArgumentOutOfRangeException(nameof(pValue));
                        break;

                    default:

                        throw new cInternalErrorException($"{nameof(cHeaderFieldFactory)},{nameof(StructuredValue)}");
                }
            }

            if (!lBytes.TryGetMessageDataPart(out var lResult)) throw new ArgumentOutOfRangeException(nameof(pValue));
            return lResult;
        }

        public cLiteralMessageDataPart DateTimeValue(string pFieldName, DateTime pDateTime)
        {
            var lBytes = new cHeaderFieldBytes(mUTF8Headers, mEncoding, mCharsetNameBytes, pFieldName);
            if (!lBytes.TryAdd(cTools.GetRFC822DateTimeString(pDateTime), eHeaderFieldTextContext.structured)) throw new cInternalErrorException($"{nameof(cHeaderFieldFactory)}.{nameof(DateTimeValue)}.1.1");
            if (!lBytes.TryGetMessageDataPart(out var lResult)) throw new cInternalErrorException($"{nameof(cHeaderFieldFactory)}.{nameof(DateTimeValue)}.1.2");
            return lResult;
        }

        public cLiteralMessageDataPart DateTimeValue(string pFieldName, DateTimeOffset pDateTimeOffset)
        {
            var lBytes = new cHeaderFieldBytes(mUTF8Headers, mEncoding, mCharsetNameBytes, pFieldName);
            if (!lBytes.TryAdd(cTools.GetRFC822DateTimeString(pDateTimeOffset), eHeaderFieldTextContext.structured)) throw new cInternalErrorException($"{nameof(cHeaderFieldFactory)}.{nameof(DateTimeValue)}.2.1");
            if (!lBytes.TryGetMessageDataPart(out var lResult)) throw new cInternalErrorException($"{nameof(cHeaderFieldFactory)}.{nameof(DateTimeValue)}.2.2");
            return lResult;
        }

        public cLiteralMessageDataPart MailboxValue(string pFieldName, params MailAddress[] pAddresses) => MailboxValue(pFieldName, cTools.MailAddressesToEmailAddresses(pAddresses));

        public cLiteralMessageDataPart MailboxValue(string pFieldName, IEnumerable<MailAddress> pAddresses) => MailboxValue(pFieldName, cTools.MailAddressesToEmailAddresses(pAddresses));

        public cLiteralMessageDataPart MailboxValue(string pFieldName, params cEmailAddress[] pAddresses) => MailboxValue(pFieldName, pAddresses as IEnumerable<cEmailAddress>);

        public cLiteralMessageDataPart MailboxValue(string pFieldName, IEnumerable<cEmailAddress> pAddresses)
        {
            var lBytes = new cHeaderFieldBytes(mUTF8Headers, mEncoding, mCharsetNameBytes, pFieldName);
            ZAddAddresses(pAddresses, lBytes);
            if (!lBytes.TryGetMessageDataPart(out var lResult)) throw new ArgumentOutOfRangeException(nameof(pAddresses));
            return lResult;
        }

        public cLiteralMessageDataPart AddressValue(string pFieldName, params cAddress[] pAddresses) => AddressValue(pFieldName, pAddresses as IEnumerable<cAddress>);

        public cLiteralMessageDataPart AddressValue(string pFieldName, IEnumerable<cAddress> pAddresses)
        {
            var lBytes = new cHeaderFieldBytes(mUTF8Headers, mEncoding, mCharsetNameBytes, pFieldName);

            if (pAddresses == null) throw new ArgumentNullException(nameof(pAddresses));

            bool lFirst = false;

            foreach (var lAddress in pAddresses)
            {
                if (lAddress == null) throw new ArgumentOutOfRangeException(nameof(pAddresses), kArgumentOutOfRangeExceptionMessage.ContainsNulls);

                if (lFirst) lFirst = false;
                else if (!lBytes.TryAdd(",", eHeaderFieldTextContext.structured)) throw new ArgumentOutOfRangeException(nameof(pAddresses));

                ZAddAddress(lAddress, lBytes);
            }

            if (!lBytes.TryGetMessageDataPart(out var lResult)) throw new ArgumentOutOfRangeException(nameof(pAddresses));
            return lResult;
        }

        public cLiteralMessageDataPart MIME(string pFieldName, string pValue, params cHeaderFieldMIMEParameter[] pParameters)
        {

        }

        public cLiteralMessageDataPart MIME(string pFieldName, string pValue, IEnumerable<cHeaderFieldMIMEParameter> pParameters)
        {

        }

        private bool ZAddPhrase(cHeaderPhraseValue pPhrase, cHeaderFieldBytes pBytes)
        {
            foreach (var lPart in pPhrase.Parts)
            {
                switch (lPart)
                {
                    case cHeaderTextValue lText:

                        if (!pBytes.TryAdd(lText.Text, eHeaderFieldTextContext.phrase)) return false;
                        break;

                    case cHeaderCommentValue lComment:

                        if (!ZAddComment(lComment, pBytes)) return false;
                        break;

                    default:

                        throw new cInternalErrorException($"{nameof(cHeaderFieldFactory)},{nameof(ZAddPhrase)}");
                }
            }

            return true;
        }

        private bool ZAddComment(cHeaderCommentValue pComment, cHeaderFieldBytes pBytes)
        {
            if (!pBytes.TryAdd("(", eHeaderFieldTextContext.structured)) return false;

            foreach (var lPart in pComment.Parts)
            {
                switch (lPart)
                {
                    case cHeaderTextValue lText:

                        if (!pBytes.TryAdd(lText.Text, eHeaderFieldTextContext.comment)) return false;
                        break;

                    case cHeaderCommentValue lComment:

                        if (!ZAddComment(lComment, pBytes)) return false;
                        break;

                    default:

                        throw new cInternalErrorException($"{nameof(cHeaderFieldFactory)},{nameof(ZAddComment)}");
                }
            }

            return pBytes.TryAdd(")", eHeaderFieldTextContext.structured);
        }

        private void ZAddAddress(cAddress pAddress, cHeaderFieldBytes pBytes)
        {
            switch (pAddress)
            {
                case cGroupAddress lGroup:

                    ZAddGroupAddress(lGroup, pBytes);
                    break;

                case cEmailAddress lEmail:

                    ZAddEmailAddress(lEmail, pBytes);
                    break;

                default:

                    throw new cInternalErrorException($"{nameof(cHeaderFieldFactory)},{nameof(ZAddAddress)}");
            }
        }

        private void ZAddGroupAddress(cGroupAddress pAddress, cHeaderFieldBytes pBytes)
        {
            if (!pBytes.TryAdd(pAddress.DisplayName.ToString(), eHeaderFieldTextContext.phrase)) throw new cAddressFormException(pAddress);
            if (!pBytes.TryAdd(":", eHeaderFieldTextContext.structured)) throw new cAddressFormException(pAddress);
            ZAddAddresses(pAddress.EmailAddresses, pBytes);
            if (!pBytes.TryAdd(";", eHeaderFieldTextContext.structured)) throw new cAddressFormException(pAddress);
        }

        private void ZAddAddresses(IEnumerable<cEmailAddress> pAddresses, cHeaderFieldBytes pBytes)
        {
            if (pAddresses == null) throw new ArgumentNullException(nameof(pAddresses));

            bool lFirst = false;

            foreach (var lAddress in pAddresses)
            {
                if (lAddress == null) throw new ArgumentOutOfRangeException(nameof(pAddresses), kArgumentOutOfRangeExceptionMessage.ContainsNulls);

                if (lFirst) lFirst = false;
                else if (!pBytes.TryAdd(",", eHeaderFieldTextContext.structured)) throw new ArgumentOutOfRangeException(nameof(pAddresses));

                ZAddEmailAddress(lAddress, lBytes);
            }
        }

        private void ZAddEmailAddress(cEmailAddress pAddress, cHeaderFieldBytes pBytes)
        {
            if (pAddress.DisplayName == null) ZAddAddrSpec(pAddress);
            else
            {
                if (!pBytes.TryAdd(pAddress.DisplayName.ToString(), eHeaderFieldTextContext.phrase)) throw new cAddressFormException(pAddress);
            }
        }


        ;?; // mime header - field, value, parameters ...

        private void ZAddAddress(MailAddress pAddress, cHeaderFieldBytes pBytes)
        {
            if (string.IsNullOrWhiteSpace(pAddress.DisplayName))
            {
                ZAddAddrSpec(pAddress, pBytes);
                return;
            }

            if (!cTools.IsValidHeaderFieldText(pAddress.DisplayName)) throw new cAddressFormException(pAddress);

            pBytes.AddEncodableText(pAddress.DisplayName, eHeaderFieldTextContext.phrase);
            pBytes.AddSpecial(cASCII.LESSTHAN);
            ZAddAddrSpec(pAddress, pBytes);
            pBytes.AddSpecial(cASCII.GREATERTHAN);
        }

        private void ZAddAddrSpec(MailAddress pAddress, cHeaderFieldBytes pBytes)
        {
            if (pAddress.Host == null || pAddress.Host.Length == 0) throw new cMailAddressFormException(pAddress);

            ;?;
            string lUser = ZRemoveQuoting(pAddress.User);
            if (!pBytes.TryAddDotAtom(lUser) && !pBytes.TryAddQuotedString(lUser)) throw new cMailAddressFormException(pAddress);

            pBytes.AddSpecial(cASCII.AT);

            if (ZIsDomainLiteral(pAddress.Host, out var lDText))
            {
                pBytes.AddSpecial(cASCII.LBRACKET);
                // add text/ structured
                if (!pBytes.TryAddFoldableText(lDText)) throw new cMailAddressFormException(pAddress);
                pBytes.AddSpecial(cASCII.RBRACKET);
            }
            else
            {
                string lHost;
                if (mUTF8Headers) lHost = pAddress.Host;
                else lHost = cTools.GetDNSHost(pAddress.Host);

                if (!pBytes.TryAddDotAtom(lHost)) throw new cMailAddressFormException(pAddress);
            }
        }

        private void ZAddAddress(cEmailAddress pAddress, cHeaderFieldBytes pBytes)
        {
            if (string.IsNullOrWhiteSpace(pAddress.DisplayName))
            {
                ZAddAddrSpec(pAddress, pBytes);
                return;
            }

            string lDisplayName = pAddress.DisplayName.ToString();

            ;?;
            if (!cTools.IsValidHeaderFieldText(lDisplayName)) throw new cAddressFormException((cAddress)pAddress);

            pBytes.AddEncodableText(pAddress.DisplayName, eHeaderFieldTextContext.phrase);
            pBytes.AddSpecial(cASCII.LESSTHAN);
            ZAddAddrSpec(pAddress, pBytes);
            pBytes.AddSpecial(cASCII.GREATERTHAN);
        }

        private void ZAddAddrSpec(cEmailAddress pAddress, cHeaderFieldBytes pBytes)
        {
            if (pAddress.Host == null || pAddress.Host.Length == 0) throw new cMailAddressFormException(pAddress);

            string lUser = ZRemoveQuoting(pAddress.User);
            if (!pBytes.TryAddDotAtom(lUser) && !pBytes.TryAddQuotedString(lUser)) throw new cMailAddressFormException(pAddress);

            pBytes.AddSpecial(cASCII.AT);

            if (ZIsDomainLiteral(pAddress.Host, out var lDText))
            {
                pBytes.AddSpecial(cASCII.LBRACKET);
                if (!pBytes.TryAddFoldableText(lDText)) throw new cMailAddressFormException(pAddress);
                pBytes.AddSpecial(cASCII.RBRACKET);
            }
            else
            {
                string lHost;
                if (mUTF8Headers) lHost = pAddress.Host;
                else lHost = cTools.GetDNSHost(pAddress.Host);

                if (!pBytes.TryAddDotAtom(lHost)) throw new cMailAddressFormException(pAddress);
            }
        }











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