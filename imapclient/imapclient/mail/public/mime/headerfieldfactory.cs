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
            mCharsetNameBytes = new cBytes(cTools.CharsetNameBytes(pEncoding));
        }

        public cLiteralMessageDataPart UnstructuredValue(string pFieldName, string pFieldValue)
        {
            var lBytes = new cHeaderFieldBytes(mUTF8Headers, mEncoding, mCharsetNameBytes, pFieldName);
            if (pFieldValue == null) throw new ArgumentNullException(nameof(pFieldValue));
            if (!cTools.IsValidHeaderFieldText(pFieldValue)) throw new ArgumentOutOfRangeException(nameof(pFieldValue));
            lBytes.AddEncodableText(pFieldValue, eHeaderFieldTextContext.unstructured);
            lBytes.AddNewLine();
            return new cLiteralMessageDataPart(lBytes.Bytes, lBytes.Format);
        }

        public cLiteralMessageDataPart PhraseValue(string pFieldName, params string[] pPhrases) => PhraseValue(pFieldName, pPhrases as IEnumerable<string>);

        public cLiteralMessageDataPart PhraseValue(string pFieldName, IEnumerable<string> pPhrases)
        {
            var lBytes = new cHeaderFieldBytes(mUTF8Headers, mEncoding, mCharsetNameBytes, pFieldName);

            if (pPhrases == null) throw new ArgumentNullException(nameof(pPhrases));

            bool lFirst = false;

            foreach (var lPhrase in pPhrases)
            {
                if (lPhrase == null) throw new ArgumentOutOfRangeException(nameof(pPhrases), kArgumentOutOfRangeExceptionMessage.ContainsNulls);
                if (!cTools.IsValidHeaderFieldText(lPhrase)) throw new ArgumentOutOfRangeException(nameof(pPhrases), kArgumentOutOfRangeExceptionMessage.IsInvalid);

                if (lFirst) lFirst = false;
                else lBytes.AddSpecial(cASCII.COMMA);

                lBytes.AddEncodableText(lPhrase, eHeaderFieldTextContext.phrase);
            }

            lBytes.AddNewLine();

            return new cLiteralMessageDataPart(lBytes.Bytes, lBytes.Format);
        }

        public cLiteralMessageDataPart PhraseValue(string pFieldName, params cHeaderFieldPhrase[] pPhrases) => PhraseValue(pFieldName, pPhrases as IEnumerable<cHeaderFieldPhrase>);

        public cLiteralMessageDataPart PhraseValue(string pFieldName, IEnumerable<cHeaderFieldPhrase> pPhrases)
        {
            var lBytes = new cHeaderFieldBytes(mUTF8Headers, mEncoding, mCharsetNameBytes, pFieldName);

            if (pPhrases == null) throw new ArgumentNullException(nameof(pPhrases));

            bool lFirst = false;

            foreach (var lPhrase in pPhrases)
            {
                if (lPhrase == null) throw new ArgumentOutOfRangeException(nameof(pPhrases), kArgumentOutOfRangeExceptionMessage.ContainsNulls);

                if (lFirst) lFirst = false;
                else lBytes.AddSpecial(cASCII.COMMA);

                lPhrase.AddTo(lBytes);
            }

            lBytes.AddNewLine();

            return new cLiteralMessageDataPart(lBytes.Bytes, lBytes.Format);
        }

        public cLiteralMessageDataPart DateTimeValue(string pFieldName, DateTime pDateTime)
        {
            var lBytes = new cHeaderFieldBytes(mUTF8Headers, mEncoding, mCharsetNameBytes, pFieldName);
            if (!lBytes.TryAddFoldableText(cTools.RFC822DateTimeString(pDateTime))) throw new cInternalErrorException($"{nameof(cHeaderFieldFactory)}.{nameof(DateTimeValue)}.1");
            lBytes.AddNewLine();
            return new cLiteralMessageDataPart(lBytes.Bytes, lBytes.Format);
        }

        public cLiteralMessageDataPart DateTimeValue(string pFieldName, DateTimeOffset pDateTimeOffset)
        {
            var lBytes = new cHeaderFieldBytes(mUTF8Headers, mEncoding, mCharsetNameBytes, pFieldName);
            if (!lBytes.TryAddFoldableText(cTools.RFC822DateTimeString(pDateTimeOffset))) throw new cInternalErrorException($"{nameof(cHeaderFieldFactory)}.{nameof(DateTimeValue)}.2");
            lBytes.AddNewLine();
            return new cLiteralMessageDataPart(lBytes.Bytes, lBytes.Format);
        }

        ;?; // common for emailaddress conversion
        public cLiteralMessageDataPart MailboxValue(string pFieldName, params MailAddress[] pMailAddresses) => MailboxValue(pFieldName, ZEmailAddresses(pMailAddresses));

        public cLiteralMessageDataPart MailboxValue(string pFieldName, IEnumerable<MailAddress> pMailAddresses) => MailboxValue(pFieldName, ZAddresses(pMailAddresses));

        public cLiteralMessageDataPart MailboxValue(string pFieldName, params cEmailAddress[] pEmailAddresses) => MailboxValue(pFieldName, pEmailAddresses as IEnumerable<cEmailAddress>);

        public cLiteralMessageDataPart MailboxValue(string pFieldName, IEnumerable<cEmailAddress> pEmailAddresses)
        {
            var lBytes = new cHeaderFieldBytes(mUTF8Headers, mEncoding, mCharsetNameBytes, pFieldName);

            if (pEmailAddresses == null) throw new ArgumentNullException(nameof(pEmailAddresses));

            bool lFirst = false;

            foreach (var lEmailAddress in pEmailAddresses)
            {
                if (lEmailAddress == null) throw new ArgumentOutOfRangeException(nameof(pEmailAddresses), kArgumentOutOfRangeExceptionMessage.ContainsNulls);

                if (lFirst) lFirst = false;
                else lBytes.AddSpecial(cASCII.COMMA);

                ZAddEmailAddress(lEmailAddress, lBytes);
            }

            lBytes.AddNewLine();

            return new cLiteralMessageDataPart(lBytes.Bytes, lBytes.Format);
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
                else lBytes.AddSpecial(cASCII.COMMA);

                ;?;
                ZAddAddress(lAddress, lBytes);
            }

            lBytes.AddNewLine();

            return new cLiteralMessageDataPart(lBytes.Bytes, lBytes.Format);
        }

        public cLiteralMessageDataPart MsgIdValue(string pFieldName, cMsgId pMsgId)
        {
            var lBytes = new cHeaderFieldBytes(mUTF8Headers, mEncoding, mCharsetNameBytes, pFieldName);
            if (pMsgId == null) throw new ArgumentNullException(nameof(pMsgId));
            if (!lBytes.TryAddFoldableText(pMsgId.MessageId)) throw new ArgumentOutOfRangeException();
            lBytes.AddNewLine();
            return new cLiteralMessageDataPart(lBytes.Bytes, lBytes.Format);
        }

        public cLiteralMessageDataPart MsgIdValue(string pFieldName, params cMsgId[] pMsgIds) => MsgIdValue(pFieldName, pMsgIds as IEnumerable<cMsgId>);

        public cLiteralMessageDataPart MsgIdValue(string pFieldName, IEnumerable<cMsgId> pMsgIds)
        {
            var lBytes = new cHeaderFieldBytes(mUTF8Headers, mEncoding, mCharsetNameBytes, pFieldName);

            if (pMsgIds == null) throw new ArgumentNullException(nameof(pMsgIds));

            bool lFirst = true;

            foreach (var lMsgId in pMsgIds)
            {
                if (lMsgId == null) throw new ArgumentOutOfRangeException(nameof(pMsgIds), kArgumentOutOfRangeExceptionMessage.ContainsNulls);
                lFirst = false;
                if (!lBytes.TryAddFoldableText(lMsgId.MessageId)) throw new ArgumentOutOfRangeException(nameof(pMsgIds));
            }

            if (lFirst) throw new ArgumentOutOfRangeException(nameof(pMsgIds), kArgumentOutOfRangeExceptionMessage.HasNoContent);

            lBytes.AddNewLine();

            return new cLiteralMessageDataPart(lBytes.Bytes, lBytes.Format);
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