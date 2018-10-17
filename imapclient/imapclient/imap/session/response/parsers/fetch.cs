using System;
using System.Collections.Generic;
using System.Diagnostics;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cResponseDataParserFetch : iResponseDataParser
            {
                private static readonly cBytes kFetchSpace = new cBytes("FETCH ");

                private static readonly cBytes kFlagsSpace = new cBytes("FLAGS ");
                private static readonly cBytes kEnvelopeSpace = new cBytes("ENVELOPE ");
                private static readonly cBytes kInternalDateSpace = new cBytes("INTERNALDATE ");
                private static readonly cBytes kRFC822Space = new cBytes("RFC822 ");
                private static readonly cBytes kRFC822HeaderSpace = new cBytes("RFC822.HEADER ");
                private static readonly cBytes kRFC822TextSpace = new cBytes("RFC822.TEXT ");
                private static readonly cBytes kRFC822SizeSpace = new cBytes("RFC822.SIZE ");
                private static readonly cBytes kBodySpace = new cBytes("BODY ");
                private static readonly cBytes kBodyStructureSpace = new cBytes("BODYSTRUCTURE ");
                private static readonly cBytes kBodyLBracket = new cBytes("BODY[");
                private static readonly cBytes kUIDSpace = new cBytes("UID ");
                private static readonly cBytes kBinaryLBracket = new cBytes("BINARY[");
                private static readonly cBytes kBinarySizeLBracket = new cBytes("BINARY.SIZE[");
                private static readonly cBytes kModSeqSpaceLParen = new cBytes("MODSEQ (");

                private readonly bool mUTF8Enabled;

                public cResponseDataParserFetch(bool pUTF8Enabled)
                {
                    mUTF8Enabled = pUTF8Enabled;
                }

                public bool Process(cBytesCursor pCursor, out cResponseData rResponseData, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cResponseDataParserFetch), nameof(Process));

                    if (!pCursor.GetNZNumber(out _, out var lMSN) || !pCursor.SkipByte(cASCII.SPACE) || !pCursor.SkipBytes(kFetchSpace)) { rResponseData = null; return false; }

                    if (!pCursor.SkipByte(cASCII.LPAREN))
                    {
                        lContext.TraceWarning("likely malformed fetch response: {0}", pCursor);
                        rResponseData = null;
                        return true;
                    }

                    cFetchableFlags lFlags = null;
                    cEnvelope lEnvelope = null;
                    cTimestamp lReceived = null;
                    cBytes lRFC822 = null;
                    cBytes lRFC822Header = null;
                    cBytes lRFC822Text = null;
                    uint? lSize = null;
                    cBodyPart lBody = null;
                    cBodyPart lBodyStructure = null;
                    List<cBody> lBodies = new List<cBody>();
                    uint? lUID = null;
                    cHeaderFields lHeaderFields = null;
                    Dictionary<string, uint> lSectionPartToBinarySize = new Dictionary<string, uint>();
                    ulong? lModSeq = null;

                    while (true)
                    {
                        bool lOK;

                        if (pCursor.SkipBytes(kFlagsSpace)) lOK = pCursor.GetFlags(out var lRawFlags) && cFetchableFlags.TryConstruct(lRawFlags, out lFlags);
                        else if (pCursor.SkipBytes(kEnvelopeSpace)) lOK = ZProcessEnvelope(pCursor, out lEnvelope);
                        else if (pCursor.SkipBytes(kInternalDateSpace)) lOK = pCursor.GetDateTime(out lReceived);
                        else if (pCursor.SkipBytes(kRFC822Space))
                        {
                            if (lOK = pCursor.GetNString(out IList<byte> lTemp1))
                            {
                                lRFC822 = new cBytes(lTemp1);
                                if (cHeaderFields.TryConstruct(lTemp1, out var lTemp2)) lHeaderFields += lTemp2;
                            }
                        }
                        else if (pCursor.SkipBytes(kRFC822HeaderSpace))
                        {
                            if (lOK = pCursor.GetNString(out IList<byte> lTemp1))
                            {
                                lRFC822Header = new cBytes(lTemp1);
                                if (cHeaderFields.TryConstruct(lTemp1, out var lTemp2)) lHeaderFields += lTemp2;
                            }
                        }
                        else if (pCursor.SkipBytes(kRFC822TextSpace))
                        {
                            if (lOK = pCursor.GetNString(out IList<byte> lTemp)) lRFC822Text = new cBytes(lTemp);
                        }
                        else if (pCursor.SkipBytes(kRFC822SizeSpace))
                        {
                            lOK = pCursor.GetNumber(out _, out var lNumber);
                            if (lOK) lSize = lNumber;
                        }
                        else if (pCursor.SkipBytes(kBodySpace)) lOK = ZProcessBodyStructure(pCursor, cSection.Text, false, out lBody);
                        else if (pCursor.SkipBytes(kBodyStructureSpace)) lOK = ZProcessBodyStructure(pCursor, cSection.Text, true, out lBodyStructure);
                        else if (pCursor.SkipBytes(kBodyLBracket))
                        {
                            lOK = ZProcessBody(pCursor, false, out var lABody);

                            if (lOK)
                            {
                                lBodies.Add(lABody);

                                if (lABody.Section.Part == null && lABody.Origin == null)
                                {
                                    cHeaderFields lTemp;

                                    switch (lABody.Section.TextPart)
                                    {
                                        case eSectionTextPart.all:
                                        case eSectionTextPart.header:

                                            if (cHeaderFields.TryConstruct(lABody.Bytes, out lTemp)) lHeaderFields += lTemp;
                                            break;

                                        case eSectionTextPart.headerfields:

                                            if (cHeaderFields.TryConstruct(lABody.Section.Names, false, lABody.Bytes, out lTemp)) lHeaderFields += lTemp;
                                            break;

                                        case eSectionTextPart.headerfieldsnot:

                                            if (cHeaderFields.TryConstruct(lABody.Section.Names, true, lABody.Bytes, out lTemp)) lHeaderFields += lTemp;
                                            break;
                                    }
                                }
                            }
                        }
                        else if (pCursor.SkipBytes(kUIDSpace))
                        {
                            lOK = pCursor.GetNZNumber(out _, out var lNumber);
                            if (lOK) lUID = lNumber;
                        }
                        else if (pCursor.SkipBytes(kBinaryLBracket))
                        {
                            lOK = ZProcessBody(pCursor, true, out var lABody);
                            if (lOK) lBodies.Add(lABody);
                        }
                        else if (pCursor.SkipBytes(kBinarySizeLBracket))
                        {
                            lOK = ZProcessBinarySize(pCursor, out var lSectionPart, out var lBinarySize);
                            if (lOK) lSectionPartToBinarySize[lSectionPart] = lBinarySize;
                        }
                        else if (pCursor.SkipBytes(kModSeqSpaceLParen))
                        {
                            lOK = pCursor.GetNumber(out var lTemp) && pCursor.SkipByte(cASCII.RPAREN);
                            if (lOK) lModSeq = lTemp;
                        }
                        else break;

                        if (!lOK)
                        {
                            lContext.TraceWarning("likely malformed fetch response: {0}", pCursor);
                            rResponseData = null;
                            return true;
                        }

                        if (!pCursor.SkipByte(cASCII.SPACE)) break;
                    }

                    if (!pCursor.SkipByte(cASCII.RPAREN) || !pCursor.Position.AtEnd)
                    {
                        lContext.TraceWarning("likely malformed fetch response: {0}", pCursor);
                        rResponseData = null;
                        return true;
                    }

                    cModSeqFlags lModSeqFlags;
                    if (lFlags == null) lModSeqFlags = null;
                    else lModSeqFlags = new cModSeqFlags(lFlags, lModSeq ?? 0);

                    cBinarySizes lBinarySizes;
                    if (lSectionPartToBinarySize.Count == 0) lBinarySizes = null;
                    else lBinarySizes = new cBinarySizes(lSectionPartToBinarySize);

                    rResponseData =
                        new cResponseDataFetch(
                            lMSN,
                            lUID,
                            lModSeqFlags,
                            lBody,
                            lEnvelope, lReceived, lSize, lBodyStructure, lHeaderFields, lBinarySizes,
                            lRFC822, lRFC822Header, lRFC822Text,
                            lBodies);

                    return true;
                }

                private bool ZProcessEnvelope(cBytesCursor pCursor, out cEnvelope rEnvelope)
                {
                    //  NOTE: this routine does not return the cursor to its original position if it fails

                    if (!pCursor.SkipByte(cASCII.LPAREN)) { rEnvelope = null; return false; }

                    if (!pCursor.GetNString(out IList<byte> lDateBytes) ||
                        !pCursor.SkipByte(cASCII.SPACE) ||
                        !pCursor.GetNString(out IList<byte> lSubjectBytes) ||
                        !pCursor.SkipByte(cASCII.SPACE) ||
                        !ZProcessAddresses(pCursor, out var lFrom) ||
                        !pCursor.SkipByte(cASCII.SPACE) ||
                        !ZProcessAddresses(pCursor, out var lSender) ||
                        !pCursor.SkipByte(cASCII.SPACE) ||
                        !ZProcessAddresses(pCursor, out var lReplyTo) ||
                        !pCursor.SkipByte(cASCII.SPACE) ||
                        !ZProcessAddresses(pCursor, out var lTo) ||
                        !pCursor.SkipByte(cASCII.SPACE) ||
                        !ZProcessAddresses(pCursor, out var lCC) ||
                        !pCursor.SkipByte(cASCII.SPACE) ||
                        !ZProcessAddresses(pCursor, out var lBCC) ||
                        !pCursor.SkipByte(cASCII.SPACE) ||
                        !pCursor.GetNString(out IList<byte> lInReplyToBytes) ||
                        !pCursor.SkipByte(cASCII.SPACE) ||
                        !pCursor.GetNString(out IList<byte> lMessageIdBytes)
                       ) { rEnvelope = null; return false; }

                    if (!pCursor.SkipByte(cASCII.RPAREN)) { rEnvelope = null; return false; }

                    cTimestamp lSent;

                    if (lDateBytes == null || lDateBytes.Count == 0) lSent = null;
                    else
                    {
                        // rfc 5256 says quite a lot about how this date should be parsed: please note that I haven't done what it says at this stage (TODO?)
                        var lCursor = new cBytesCursor(lDateBytes);
                        if (lCursor.GetRFC822DateTime(out lSent) && !lCursor.Position.AtEnd) lSent = null;
                    }

                    cCulturedString lSubject;

                    if (lSubjectBytes == null || lSubjectBytes.Count == 0) lSubject = null;
                    else lSubject = new cCulturedString(lSubjectBytes, false);

                    cParsing.TryParseMsgIds(lInReplyToBytes, out var lInReplyTo);
                    cParsing.TryParseMsgId(lMessageIdBytes, out var lMessageId);

                    rEnvelope = new cEnvelope(lSent, lSubject, lFrom, lSender, lReplyTo, lTo, lCC, lBCC, lInReplyTo, lMessageId);
                    return true;
                }

                private static bool ZProcessAddresses(cBytesCursor pCursor, out cAddresses rAddresses)
                {
                    //  NOTE: this routine does not return the cursor to its original position if it fails

                    if (pCursor.SkipBytes(cBytesCursor.Nil)) { rAddresses = null; return true; }

                    List<cAddress> lAddresses = new List<cAddress>();

                    if (!pCursor.SkipByte(cASCII.LPAREN)) { rAddresses = null; return false; }

                    cCulturedString lGroupDisplayName = null;
                    List<cEmailAddress> lGroupAddresses = null;

                    while (true)
                    {
                        // extract an address
                        if (!pCursor.SkipByte(cASCII.LPAREN)) break;

                        if (!pCursor.GetNString(out IList<byte> lNameBytes) ||
                            !pCursor.SkipByte(cASCII.SPACE) ||
                            !pCursor.GetNString(out IList<byte> _) || // route information, not used
                            !pCursor.SkipByte(cASCII.SPACE) ||
                            !pCursor.GetNString(out IList<byte> lMailboxBytes) ||
                            !pCursor.SkipByte(cASCII.SPACE) ||
                            !pCursor.GetNString(out IList<byte> lHostBytes) ||
                            !pCursor.SkipByte(cASCII.RPAREN))
                        {
                            rAddresses = null;
                            return false;
                        }

                        if (lHostBytes == null)
                        {
                            // group start or finish

                            if (lMailboxBytes == null)
                            {
                                // group finish

                                if (lGroupAddresses == null) { rAddresses = null; return false; } // end of group without a start of group
                                lAddresses.Add(new cGroupAddress(lGroupDisplayName, lGroupAddresses));
                                lGroupDisplayName = null;
                                lGroupAddresses = null;
                            }
                            else
                            {
                                // group start

                                if (lGroupAddresses != null) lAddresses.Add(new cGroupAddress(lGroupDisplayName, lGroupAddresses)); // handle a start of group with no end of group
                                lGroupDisplayName = new cCulturedString(lMailboxBytes, true);
                                lGroupAddresses = new List<cEmailAddress>();
                            }
                        }
                        else
                        {
                            // not sure how mailbox names are meant to support i18n? rfc 6530 implies UTF8 but I can't find how that affects the value reported by IMAP in mailbox
                            //  at this stage if UTF8 were to appear in the mailbox then we would just accept it

                            string lMailbox = cMailTools.UTF8BytesToString(lMailboxBytes);
                            string lHost = cMailTools.UTF8BytesToString(lHostBytes);

                            cCulturedString lDisplayName;
                            if (lNameBytes == null) lDisplayName = null;
                            else lDisplayName = new cCulturedString(lNameBytes, true);

                            var lEmailAddress = new cEmailAddress(lMailbox, lHost, lDisplayName);

                            if (lGroupAddresses != null) lGroupAddresses.Add(lEmailAddress);
                            else lAddresses.Add(lEmailAddress);
                        }
                    }

                    // handle a start of group with no end of group
                    if (lGroupAddresses != null) lAddresses.Add(new cGroupAddress(lGroupDisplayName, lGroupAddresses));

                    if (!pCursor.SkipByte(cASCII.RPAREN)) { rAddresses = null; return false; }

                    rAddresses = new cAddresses(lAddresses);

                    return true;
                }

                private bool ZProcessBodyStructure(cBytesCursor pCursor, cSection pSection, bool pExtended, out cBodyPart rBodyPart)
                {
                    if (!pCursor.SkipByte(cASCII.LPAREN)) { rBodyPart = null; return false; }

                    string lSubType;
                    cBodyPart lBodyPart;

                    string lSubPartPrefix = pSection.GetSubPartPrefix();

                    // multi-part

                    if (ZProcessBodyStructure(pCursor, new cSection(lSubPartPrefix + "1"), pExtended, out lBodyPart))
                    {
                        List<cBodyPart> lParts = new List<cBodyPart>();

                        lParts.Add(lBodyPart);
                        int lPart = 2;

                        while (true)
                        {
                            if (!ZProcessBodyStructure(pCursor, new cSection(lSubPartPrefix + lPart++), pExtended, out lBodyPart)) break;
                            lParts.Add(lBodyPart);
                        }

                        if (!pCursor.SkipByte(cASCII.SPACE) || !pCursor.GetString(out lSubType)) { rBodyPart = null; return false; }

                        cMultiPartExtensionData lMultiPartExtensionData;

                        if (pExtended && pCursor.SkipByte(cASCII.SPACE))
                        {
                            if (!ZProcessBodyStructureParameters(pCursor, out var lExtendedParameters)) { rBodyPart = null; return false; }
                            if (!ZProcessBodyStructureExtensionData(pCursor, out var lDisposition, out var lLanguages, out var lLocation, out var lExtensionValues)) { rBodyPart = null; return false; }
                            lMultiPartExtensionData = new cMultiPartExtensionData(lExtendedParameters, lDisposition, lLanguages, lLocation, lExtensionValues);
                        }
                        else lMultiPartExtensionData = null;

                        if (!pCursor.SkipByte(cASCII.RPAREN)) { rBodyPart = null; return false; }

                        rBodyPart = new cMultiPartBody(lParts, mUTF8Enabled, lSubType, pSection, lMultiPartExtensionData);
                        return true;
                    }

                    // this code covers the situation where a message body (either the message itself or an embedded message) contains one and only one part
                    //  TODO: test this [and if it is wrong change the ondeserialised on cMessageBodyPart and cSection.CouldDescribeABodyPart and ondeserialised on cHeaderCacheItem]

                    cSection lSection;
                    if (pSection.TextPart == eSectionTextPart.text) lSection = new cSection(lSubPartPrefix + "1");
                    else lSection = pSection;

                    // single part

                    if (!pCursor.GetString(out string lType) ||
                        !pCursor.SkipByte(cASCII.SPACE) ||
                        !pCursor.GetString(out lSubType) ||
                        !pCursor.SkipByte(cASCII.SPACE) ||
                        !ZProcessBodyStructureParameters(pCursor, out var lParameters) ||
                        !pCursor.SkipByte(cASCII.SPACE) ||
                        !pCursor.GetNString(out string lContentId) ||
                        !pCursor.SkipByte(cASCII.SPACE) ||
                        !pCursor.GetNString(out IList<byte> lDescriptionBytes) ||
                        !pCursor.SkipByte(cASCII.SPACE) ||
                        !pCursor.GetString(out string lContentTransferEncoding) ||
                        !pCursor.SkipByte(cASCII.SPACE) ||
                        !pCursor.GetNumber(out _, out uint lSizeInBytes)) { rBodyPart = null; return false; }

                    cSinglePartExtensionData lExtensionData;

                    cCulturedString lDescription;
                    if (lDescriptionBytes == null) lDescription = null;
                    else lDescription = new cCulturedString(lDescriptionBytes, false);

                    if (lType.Equals(kMimeType.Text, StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (!pCursor.SkipByte(cASCII.SPACE) || !pCursor.GetNumber(out _, out var lSizeInLines)) { rBodyPart = null; return false; }

                        if (pExtended)
                        {
                            if (!ZProcessBodyStructureSinglePartExtensionData(pCursor, out lExtensionData)) { rBodyPart = null; return false; }
                        }
                        else lExtensionData = null;

                        if (!pCursor.SkipByte(cASCII.RPAREN)) { rBodyPart = null; return false; }

                        rBodyPart = new cTextBodyPart(lSubType, lSection, lParameters, lContentId, lDescription, lContentTransferEncoding, lSizeInBytes, lSizeInLines, lExtensionData);
                        return true;
                    }

                    if (lType.Equals(kMimeType.Message, StringComparison.InvariantCultureIgnoreCase) && lSubType.Equals(kMimeSubType.RFC822, StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (!pCursor.SkipByte(cASCII.SPACE) || 
                            !ZProcessEnvelope(pCursor, out var lEnvelope) ||
                            !pCursor.SkipByte(cASCII.SPACE) ||
                            !ZProcessBodyStructure(pCursor, new cSection(lSection.Part, eSectionTextPart.text), pExtended, out lBodyPart) ||
                            !pCursor.SkipByte(cASCII.SPACE) ||
                            !pCursor.GetNumber(out _, out var lSizeInLines)) { rBodyPart = null; return false; }

                        if (pExtended)
                        {
                            if (!ZProcessBodyStructureSinglePartExtensionData(pCursor, out lExtensionData)) { rBodyPart = null; return false; }
                        }
                        else lExtensionData = null;

                        if (!pCursor.SkipByte(cASCII.RPAREN)) { rBodyPart = null; return false; }

                        rBodyPart = new cMessageBodyPart(lSection, lParameters, lContentId, lDescription, lContentTransferEncoding, mUTF8Enabled, lSizeInBytes, lEnvelope, lBodyPart, lSizeInLines, lExtensionData);
                        return true;
                    }

                    if (pExtended)
                    {
                        if (!ZProcessBodyStructureSinglePartExtensionData(pCursor, out lExtensionData)) { rBodyPart = null; return false; }
                    }
                    else lExtensionData = null;

                    if (!pCursor.SkipByte(cASCII.RPAREN)) { rBodyPart = null; return false; }

                    rBodyPart = new cSinglePartBody(lType, lSubType, lSection, lParameters, lContentId, lDescription, lContentTransferEncoding, false, lSizeInBytes, lExtensionData);
                    return true;
                }

                private static bool ZProcessBodyStructureParameters(cBytesCursor pCursor, out cBodyStructureParameters rParameters)
                {
                    if (pCursor.SkipBytes(cBytesCursor.Nil)) { rParameters = null; return true; }

                    if (!pCursor.SkipByte(cASCII.LPAREN)) { rParameters = null; return false; }

                    var lParameters = new List<cBodyStructureParameter>();

                    while (true)
                    {
                        if (!pCursor.GetString(out IList<byte> lName) || !pCursor.SkipByte(cASCII.SPACE) || !pCursor.GetString(out IList<byte> lValue)) { rParameters = null; return false; }
                        lParameters.Add(new cBodyStructureParameter(lName, lValue));
                        if (!pCursor.SkipByte(cASCII.SPACE)) break;
                    }

                    if (!pCursor.SkipByte(cASCII.RPAREN)) { rParameters = null; return false; }

                    rParameters = new cBodyStructureParameters(lParameters);
                    return true;
                }

                private static bool ZProcessBodyStructureSinglePartExtensionData(cBytesCursor pCursor, out cSinglePartExtensionData rExtensionData)
                {
                    if (!pCursor.SkipByte(cASCII.SPACE)) { rExtensionData = null; return true; }
                    if (!pCursor.GetNString(out string lMD5)) { rExtensionData = null; return false; }
                    if (!ZProcessBodyStructureExtensionData(pCursor, out var lDisposition, out var lLanguages, out var lLocation, out var lExtensionValues)) { rExtensionData = null; return false; }
                    rExtensionData = new cSinglePartExtensionData(lMD5, lDisposition, lLanguages, lLocation, lExtensionValues);
                    return true;
                }

                private static bool ZProcessBodyStructureExtensionData(cBytesCursor pCursor, out cBodyPartDisposition rDisposition, out cStrings rLanguages, out string rLocation, out cBodyPartExtensionValues rExtensionValues)
                {
                    if (!pCursor.SkipByte(cASCII.SPACE)) { rDisposition = null; rLanguages = null; rLocation = null; rExtensionValues = null; return true; }
                    if (!ZProcessBodyStructureDisposition(pCursor, out rDisposition)) { rDisposition = null; rLanguages = null; rLocation = null; rExtensionValues = null; return false; }
                    if (!pCursor.SkipByte(cASCII.SPACE)) { rLanguages = null; rLocation = null; rExtensionValues = null; return true; }
                    if (!ZProcessBodyStructureLanguages(pCursor, out rLanguages)) { rDisposition = null; rLanguages = null; rLocation = null; rExtensionValues = null; return false; }
                    if (!pCursor.SkipByte(cASCII.SPACE)) { rLocation = null; rExtensionValues = null; return true; }
                    if (!pCursor.GetNString(out rLocation)) { rDisposition = null; rLanguages = null; rLocation = null; rExtensionValues = null; return false; }
                    if (!pCursor.SkipByte(cASCII.SPACE)) { rExtensionValues = null; return true; }
                    if (!ZProcessBodyStructureExtensionValues(pCursor, out rExtensionValues)) { rDisposition = null; rLanguages = null; rLocation = null; rExtensionValues = null; return false; }
                    return true;
                }
                
                private static bool ZProcessBodyStructureExtensionValues(cBytesCursor pCursor, out cBodyPartExtensionValues rExtensionValues)
                {
                    List<cBodyPartExtensionValue> lValues = new List<cBodyPartExtensionValue>();

                    while (true)
                    {
                        if (pCursor.GetNString(out string lString)) lValues.Add(new cBodyPartExtensionString(lString));
                        else if (pCursor.GetNumber(out _, out var lNumber)) lValues.Add(new cBodyPartExtensionNumber(lNumber));
                        else if (pCursor.SkipByte(cASCII.LPAREN) && ZProcessBodyStructureExtensionValues(pCursor, out var lVals) && pCursor.SkipByte(cASCII.RPAREN)) lValues.Add(lVals);
                        else { rExtensionValues = null; return false; }
                        if (!pCursor.SkipByte(cASCII.SPACE)) break;
                    }

                    rExtensionValues = new cBodyPartExtensionValues(lValues);
                    return true;
                }

                private static bool ZProcessBodyStructureDisposition(cBytesCursor pCursor, out cBodyPartDisposition rDisposition)
                {
                    if (pCursor.SkipBytes(cBytesCursor.Nil)) { rDisposition = null; return true; }

                    if (!pCursor.SkipByte(cASCII.LPAREN) ||
                        !pCursor.GetString(out string lType) ||
                        !pCursor.SkipByte(cASCII.SPACE) ||
                        !ZProcessBodyStructureParameters(pCursor, out var lParameters) ||
                        !pCursor.SkipByte(cASCII.RPAREN)) { rDisposition = null; return false; }

                    rDisposition = new cBodyPartDisposition(lType, lParameters);
                    return true;
                }

                private static bool ZProcessBodyStructureLanguages(cBytesCursor pCursor, out cStrings rLanguages)
                {
                    List<string> lLanguages = new List<string>();

                    if (pCursor.SkipByte(cASCII.LPAREN))
                    {
                        while (true)
                        {
                            if (!pCursor.GetString(out string lLanguage)) { rLanguages = null; return false; }
                            lLanguages.Add(lLanguage);
                            if (!pCursor.SkipByte(cASCII.SPACE)) break;
                        }

                        if (pCursor.SkipByte(cASCII.RPAREN))
                        {
                            rLanguages = new cStrings(lLanguages);
                            return true;
                        }
                    }
                    else if (pCursor.GetNString(out string lLanguage))
                    {
                        if (lLanguage == null) rLanguages = null;
                        else
                        {
                            lLanguages.Add(lLanguage);
                            rLanguages = new cStrings(lLanguages);
                        }

                        return true;
                    }

                    rLanguages = null;
                    return false;
                }

                private static bool ZProcessBody(cBytesCursor pCursor, bool pBinary, out cBody rBody)
                {
                    if (!ZProcessSection(pCursor, pBinary, out var lSection)) { rBody = null; return false; }

                    uint? lOrigin;

                    if (pCursor.SkipByte(cASCII.LESSTHAN))
                    {
                        if (!pCursor.GetNumber(out _, out var lNumber) || !pCursor.SkipByte(cASCII.GREATERTHAN)) { rBody = null; return false; }
                        lOrigin = lNumber;
                    }
                    else lOrigin = null;

                    if (!pCursor.SkipByte(cASCII.SPACE) || !pCursor.GetNString(out IList<byte> lBytes)) { rBody = null; return false; }

                    rBody = new cBody(pBinary, lSection, lOrigin, lBytes);
                    return true;
                }

                private static readonly cBytes kHeaderRBracket = new cBytes("HEADER]");
                private static readonly cBytes kHeaderFieldsSpace = new cBytes("HEADER.FIELDS ");
                private static readonly cBytes kHeaderFieldsNotSpace = new cBytes("HEADER.FIELDS.NOT ");
                private static readonly cBytes kTextRBracket = new cBytes("TEXT]");
                private static readonly cBytes kMimeRBracket = new cBytes("MIME]");

                private static bool ZProcessSection(cBytesCursor pCursor, bool pBinary, out cSection rSection)
                {
                    cByteList lPartBytes = new cByteList();
                    bool lDot = false;

                    while (true)
                    {
                        if (!pCursor.GetNZNumber(out var lPartSegment, out _)) break;

                        if (lDot) lPartBytes.Add(cASCII.DOT);
                        lPartBytes.AddRange(lPartSegment);

                        if (pCursor.SkipByte(cASCII.DOT)) lDot = true;
                        else
                        {
                            lDot = false;
                            break;
                        }
                    }

                    string lPart;
                    if (lPartBytes.Count == 0) lPart = null;
                    else lPart = cMailTools.ASCIIBytesToString(lPartBytes);

                    if (pBinary)
                    {
                        if (lDot || !pCursor.SkipByte(cASCII.RBRACKET))
                        {
                            rSection = null;
                            return false;
                        }

                        rSection = new cSection(lPart);
                        return true;
                    }

                    if (lPart == null || lDot)
                    {
                        if (pCursor.SkipBytes(kHeaderRBracket))
                        {
                            rSection = new cSection(lPart, eSectionTextPart.header);
                            return true;
                        }

                        if (pCursor.SkipBytes(kTextRBracket))
                        {
                            rSection = new cSection(lPart, eSectionTextPart.text);
                            return true;
                        }

                        if (pCursor.SkipBytes(kHeaderFieldsSpace))
                        {
                            if (!ZProcessFieldNames(pCursor, out var lNames) || !pCursor.SkipByte(cASCII.RBRACKET)) { rSection = null; return false; }
                            rSection = new cSection(lPart, lNames);
                            return true;
                        }


                        if (pCursor.SkipBytes(kHeaderFieldsNotSpace))
                        {
                            if (!ZProcessFieldNames(pCursor, out var lNames) || !pCursor.SkipByte(cASCII.RBRACKET)) { rSection = null; return false; }
                            rSection = new cSection(lPart, lNames, true);
                            return true;
                        }
                    }

                    if (!lDot)
                    {
                        if (pCursor.SkipByte(cASCII.RBRACKET))
                        {
                            rSection = new cSection(lPart);
                            return true;
                        }

                        rSection = null;
                        return false;
                    }

                    if (!pCursor.SkipBytes(kMimeRBracket))
                    {
                        rSection = null;
                        return false;
                    }

                    rSection = new cSection(lPart, eSectionTextPart.mime);
                    return true;
                }

                private static bool ZProcessFieldNames(cBytesCursor pCursor, out cHeaderFieldNames rNames)
                {
                    if (!pCursor.SkipByte(cASCII.LPAREN)) { rNames = null; return false; }

                    List<string> lNames = new List<string>();

                    while (true)
                    {
                        if (!pCursor.GetAString(out string lHeaderField)) { rNames = null; return false; }
                        lNames.Add(lHeaderField);
                        if (!pCursor.SkipByte(cASCII.SPACE)) break;
                    }

                    if (!pCursor.SkipByte(cASCII.RPAREN)) { rNames = null; return false; }

                    return cHeaderFieldNames.TryConstruct(lNames, out rNames);
                }

                private static bool ZProcessBinarySize(cBytesCursor pCursor, out string rSectionPart, out uint rBinarySize)
                {
                    if (!ZProcessSection(pCursor, true, out var lSection) ||
                        !pCursor.SkipByte(cASCII.SPACE) ||
                        !pCursor.GetNumber(out _, out rBinarySize)) { rSectionPart = null; rBinarySize = 0; return false; }

                    rSectionPart = lSection.Part;
                    return true;
                }

                [Conditional("DEBUG")]
                public static void _Tests(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cResponseDataFetch), nameof(_Tests));

                    cBytesCursor lCursor;

                    cResponseDataParserFetch lRDPF = new cResponseDataParserFetch(false);

                    cResponseData lRD;
                    cResponseDataFetch lData;

                    cEmailAddress lEmailAddress;
                    cTextBodyPart lTextPart;

                    lCursor = new cBytesCursor(
                            @"12 FETCH (FLAGS (\Seen) INTERNALDATE ""17-Jul-1996 02:44:25 -0700"" " +
                            @"RFC822.SIZE 4286 ENVELOPE (""Wed, 17 Jul 1996 02:23:25 -0700 (PDT)"" " +
                            @"""IMAP4rev1 WG mtg summary and minutes"" " +
                            @"((""Terry Gray"" NIL ""gray"" ""cac.washington.edu"")) " +
                            @"((""Terry Gray"" NIL ""gray"" ""cac.washington.edu"")) " +
                            @"((""Terry Gray"" NIL ""gray"" ""cac.washington.edu"")) " +
                            @"((NIL NIL ""imap"" ""cac.washington.edu"")) " +
                            @"((NIL NIL ""minutes"" ""CNRI.Reston.VA.US"")" +
                            @"(""John Klensin"" NIL ""KLENSIN"" ""MIT.EDU"")) NIL NIL " +
                            @"""<B27397-0100000@cac.washington.edu>"") " +
                            @"BODY (""TEXT"" ""PLAIN"" (""CHARSET"" ""US-ASCII"") NIL NIL ""7BIT"" 3028 92))");

                    if (!lRDPF.Process(lCursor, out lRD, lContext) || !lCursor.Position.AtEnd) throw new cTestsException($"{nameof(cResponseDataFetch)}.1.1");
                    lData = lRD as cResponseDataFetch;
                    if (lData == null) throw new cTestsException($"{nameof(cResponseDataFetch)}.1.1.1");

                    if (lData.ModSeqFlags.Flags.Count != 1 || !lData.ModSeqFlags.Flags.Contains(@"\SeEn")) throw new cTestsException($"{nameof(cResponseDataFetch)}.1.2");
                    if (lData.Received.UtcDateTime != new DateTime(1996, 7, 17, 9, 44, 25, DateTimeKind.Utc)) throw new cTestsException($"{nameof(cResponseDataFetch)}.1.3");
                    if (lData.Envelope.Sent.UtcDateTime != new DateTime(1996, 7, 17, 9, 23, 25, DateTimeKind.Utc)) throw new cTestsException($"{nameof(cResponseDataFetch)}.1.4");
                    if (lData.Envelope.Subject != "IMAP4rev1 WG mtg summary and minutes") throw new cTestsException($"{nameof(cResponseDataFetch)}.1.5");
                    if (lData.Envelope.From.Count != 1) throw new cTestsException($"{nameof(cResponseDataFetch)}.1.6.1");
                    lEmailAddress = lData.Envelope.From[0] as cEmailAddress;
                    if (lEmailAddress.DisplayName != "Terry Gray" || lEmailAddress.Address != "gray@cac.washington.edu") throw new cTestsException($"{nameof(cResponseDataFetch)}.1.6.2");
                    if (lData.Envelope.Sender.Count != 1) throw new cTestsException($"{nameof(cResponseDataFetch)}.1.7");
                    if (lData.Envelope.ReplyTo.Count != 1) throw new cTestsException($"{nameof(cResponseDataFetch)}.1.7");
                    if (lData.Envelope.To.Count != 1) throw new cTestsException($"{nameof(cResponseDataFetch)}.1.8.1");
                    lEmailAddress = lData.Envelope.To[0] as cEmailAddress;
                    if (lData.Envelope.To[0].DisplayName != "imap@cac.washington.edu" ||  lEmailAddress.DisplayName != null || lEmailAddress.Address != "imap@cac.washington.edu") throw new cTestsException($"{nameof(cResponseDataFetch)}.1.8.2");

                    if (lData.Envelope.CC.Count != 2) throw new cTestsException($"{nameof(cResponseDataFetch)}.1.9.1");
                    lEmailAddress = lData.Envelope.CC[0] as cEmailAddress;
                    if (lEmailAddress.DisplayName != null || lEmailAddress.Address != "minutes@CNRI.Reston.VA.US") throw new cTestsException($"{nameof(cResponseDataFetch)}.1.9.2");
                    lEmailAddress = lData.Envelope.CC[1] as cEmailAddress;
                    if (lEmailAddress.DisplayName != "John Klensin" || lEmailAddress.Address != "KLENSIN@MIT.EDU") throw new cTestsException($"{nameof(cResponseDataFetch)}.1.9.3");

                    if (lData.Envelope.BCC != null || lData.Envelope.InReplyTo != null) throw new cTestsException($"{nameof(cResponseDataFetch)}.1.10");

                    if (lData.Envelope.MessageId != "<B27397-0100000@cac.washington.edu>") throw new cTestsException($"{nameof(cResponseDataFetch)}.1.11");

                    lTextPart = lData.Body as cTextBodyPart;
                    if (lTextPart.SubTypeCode != eTextBodyPartSubTypeCode.plain || lTextPart.Parameters.Count != 1 || lTextPart.Charset != "US-ASCII" || lTextPart.SizeInBytes != 3028 || lTextPart.SizeInLines != 92) throw new cTestsException($"{nameof(cResponseDataFetch)}.1.12.1");
                    if (lTextPart.ContentId != null || lTextPart.Description != null || lTextPart.DecodingRequired != eDecodingRequired.none) throw new cTestsException($"{nameof(cResponseDataFetch)}.1.12.2");

                    if (lData.RFC822 != null || lData.RFC822Header != null || lData.RFC822Text != null || lData.Size != 4286 || lData.BodyStructure != null || lData.Bodies.Count != 0 || lData.UID != null || lData.BinarySizes.Count != 0) throw new cTestsException($"{nameof(cResponseDataFetch)}.1.13");

                    lCursor = new cBytesCursor(
                            @"12 FETCH (FLAGS (\Seen) INTERNALDATE ""17-Jul-1996 02:44:25 -0700"" " +
                            @"RFC822.SIZE 4286 ENVELOPE (""Wed, 17 Jul 1996 02:23:25 -0700 (PDT)"" " +
                            @"""IMAP4rev1 WG mtg summary and minutes"" " +
                            @"((""Terry Gray"" NIL ""gray"" ""cac.washington.edu"")) " +
                            @"((""Terry Gray"" NIL ""gray"" ""cac.washington.edu"")) " +
                            @"((""Terry Gray"" NIL ""gray"" ""cac.washington.edu"")) " +
                            @"((NIL NIL ""imap"" ""cac.washington.edu"")) " +
                            @"((NIL NIL ""minutes"" ""CNRI.Reston.VA.US"")" +
                            @"(""John Klensin"" NIL ""KLENSIN"" ""MIT.EDU"")) NIL ""<\""01KF8JCEOCBS0045PS\""@xxx.yyy.com>"" " +
                            @"""<B27397-0100000@cac.washington.edu>"") " +
                            @"BODY (""TEXT"" ""PLAIN"" (""CHARSET"" ""US-ASCII"") NIL NIL ""7BIT"" 3028 92))");

                    if (!lRDPF.Process(lCursor, out lRD, lContext) || !lCursor.Position.AtEnd) throw new cTestsException($"{nameof(cResponseDataFetch)}.1a.1");
                    lData = lRD as cResponseDataFetch;
                    if (lData == null) throw new cTestsException($"{nameof(cResponseDataFetch)}.1a.2");

                    if (lData.Envelope.InReplyTo == null || lData.Envelope.InReplyTo.Count != 1 || lData.Envelope.InReplyTo[0] != "<01KF8JCEOCBS0045PS@xxx.yyy.com>") throw new cTestsException($"{nameof(cResponseDataFetch)}.1a.3");


                    lCursor = new cBytesCursor(
                            @"12 FETCH (FLAGS (\Seen) INTERNALDATE ""17-Jul-1996 02:44:25 -0700"" " +
                            @"RFC822.SIZE 4286 ENVELOPE (""Wed, 17 Jul 1996 02:23:25 -0700 (PDT)"" " +
                            @"""IMAP4rev1 WG mtg summary and minutes"" " +
                            @"((NIL NIL ""gray"" ""xn--frd-l50a.com"")) " +
                            @"((""Terry Gray"" NIL ""gray"" ""cac.washington.edu"")) " +
                            @"((""Terry Gray"" NIL ""gray"" ""cac.washington.edu"")) " +
                            @"((NIL NIL ""imap"" ""cac.washington.edu"")) " +
                            @"((NIL NIL ""minutes"" ""CNRI.Reston.VA.US"")" +
                            @"(""John Klensin"" NIL ""KLENSIN"" ""MIT.EDU"")) NIL ""<\""01KF8JCEOCBS0045PS\""@xxx.yyy.com>"" " +
                            @"""<B27397-0100000@cac.washington.edu>"") " +
                            @"BODY (""TEXT"" ""PLAIN"" (""CHARSET"" ""US-ASCII"") NIL NIL ""7BIT"" 3028 92))");

                    if (!lRDPF.Process(lCursor, out lRD, lContext) || !lCursor.Position.AtEnd) throw new cTestsException($"{nameof(cResponseDataFetch)}.1b.1");
                    lData = lRD as cResponseDataFetch;
                    if (lData == null) throw new cTestsException($"{nameof(cResponseDataFetch)}.1b.2");

                    if (lData.Envelope.From[0].DisplayName != "gray@fr€d.com") throw new cTestsException($"{nameof(cResponseDataFetch)}.1b.3");
                    if (lData.Envelope.Sender[0].DisplayName != "Terry Gray") throw new cTestsException($"{nameof(cResponseDataFetch)}.1b.4");

                    var lAddress = lData.Envelope.From[0] as cEmailAddress;
                    if (lAddress.DisplayName != null || lAddress.Address != "gray@xn--frd-l50a.com" || lAddress.DisplayAddress != "gray@fr€d.com") throw new cTestsException($"{nameof(cResponseDataFetch)}.1b.5");

                    lAddress = lData.Envelope.Sender[0] as cEmailAddress;
                    if (lAddress.DisplayName != "Terry Gray" || lAddress.Address != "gray@cac.washington.edu" || lAddress.DisplayAddress != "gray@cac.washington.edu") throw new cTestsException($"{nameof(cResponseDataFetch)}.1b.6");


                    cBody lBody;

                    lCursor = MakeCursor(
                        "12 FETCH (BODY[HEADER] ",
                        "{Date: Wed, 17 Jul 1996 02:23:25 -0700 (PDT)\r\n" +
                            "From: Terry Gray <gray@cac.washington.edu>\r\n" +
                            "Subject: IMAP4rev1 WG mtg summary and minutes\r\n" +
                            "To: imap@cac.washington.edu\r\n" +
                            "cc: minutes@CNRI.Reston.VA.US, John Klensin <KLENSIN@MIT.EDU>\r\n" +
                            "Message-Id: <B27397-0100000@cac.washington.edu>\r\n" +
                            "MIME-Version: 1.0\r\n" +
                            "Content-Type: TEXT/PLAIN; CHARSET=US-ASCII\r\n" +
                            "\r\n",
                        ")");

                    if (!lRDPF.Process(lCursor, out lRD, lContext) || !lCursor.Position.AtEnd) throw new cTestsException($"{nameof(cResponseDataFetch)}.2.0");
                    lData = lRD as cResponseDataFetch;
                    if (lData == null) throw new cTestsException($"{nameof(cResponseDataFetch)}.2.0.1");

                    if (lData.Bodies.Count != 1) throw new cTestsException($"{nameof(cResponseDataFetch)}.2.1");
                    lBody = lData.Bodies[0];
                    if (lBody.Binary || lBody.Section.Part != null || lBody.Section.TextPart != eSectionTextPart.header || lBody.Section.Names != null || lBody.Origin != null || lBody.Bytes.Count != 342) throw new cTestsException($"{nameof(cResponseDataFetch)}.2.2");


                    lCursor = MakeCursor(
                        "12 FETCH (BODY[HEADER]<0> ",
                        "{Date: Wed, 17 Jul 1996 02:23:25 -0700 (PDT)\r\n" +
                            "From: Terry Gray <gray@cac.washington.edu>\r\n" +
                            "Subject: IMAP4rev1 WG mtg summary and minutes\r\n" +
                            "To: imap@cac.washington.edu\r\n" +
                            "cc: minutes@CNRI.Reston.VA.US, John Klensin <KLENSIN@MIT.EDU>\r\n" +
                            "Message-Id: <B27397-0100000@cac.washington.edu>\r\n" +
                            "MIME-Version: 1.0\r\n" +
                            "Content-Type: TEXT/PLAIN; CHARSET=US-ASCII\r\n" +
                            "\r\n",
                        ")");

                    if (!lRDPF.Process(lCursor, out lRD, lContext) || !lCursor.Position.AtEnd) throw new cTestsException($"{nameof(cResponseDataFetch)}.3.0");
                    lData = lRD as cResponseDataFetch;
                    if (lData == null) throw new cTestsException($"{nameof(cResponseDataFetch)}.3.0.1");

                    if (lData.Bodies.Count != 1) throw new cTestsException($"{nameof(cResponseDataFetch)}.3.1");
                    lBody = lData.Bodies[0];
                    if (lBody.Binary || lBody.Section.Part != null || lBody.Section.TextPart != eSectionTextPart.header || lBody.Section.Names != null || lBody.Origin != 0 || lBody.Bytes.Count != 342) throw new cTestsException($"{nameof(cResponseDataFetch)}.3.2");


                    //  "         1         2         3         4         5         6         7"
                    //  "1234567890123456789012345678901234567890123456789012345678901234567890"



                    // test groups
                    //  groups with no members
                    //  TODO


                    // test bodystructure

                    cBodyPart lPart;
                    cMultiPartBody lMultiPart;

                    lCursor = new cBytesCursor(
                            @"((""TEXT"" ""PLAIN"" (""CHARSET"" ""US-ASCII"") NIL NIL ""7BIT"" 1152 23)" +
                            @"(""TEXT"" ""PLAIN"" (""CHARSET"" ""US-ASCII"" ""NAME"" ""cc.diff"") ""<960723163407.20117h@cac.washington.edu>"" ""Compiler diff"" ""BASE64"" 4554 73) ""MIXED"")");

                    if (!lRDPF.ZProcessBodyStructure(lCursor, cSection.Text, false, out lPart) || !lCursor.Position.AtEnd) throw new cTestsException($"{nameof(cResponseDataFetch)}.4.1");

                    lMultiPart = lPart as cMultiPartBody;
                    if (lMultiPart.Parts.Count != 2 || lMultiPart.SubType != "MIXED") throw new cTestsException($"{nameof(cResponseDataFetch)}.4.2");

                    lTextPart = lMultiPart.Parts[0] as cTextBodyPart;
                    if (lTextPart.DecodingRequired != eDecodingRequired.none || lTextPart.SizeInBytes != 1152 || lTextPart.SizeInLines != 23 || lTextPart.Section != new cSection("1")) throw new cTestsException($"{nameof(cResponseDataFetch)}.4.3");

                    lTextPart = lMultiPart.Parts[1] as cTextBodyPart;
                    if (lTextPart.DecodingRequired != eDecodingRequired.base64 || lTextPart.SizeInBytes != 4554 || lTextPart.SizeInLines != 73 || lTextPart.Section != new cSection("2")) throw new cTestsException($"{nameof(cResponseDataFetch)}.4.3");
                    if (lTextPart.Parameters.First("name").StringValue != "cc.diff" || lTextPart.ContentId != "<960723163407.20117h@cac.washington.edu>" || lTextPart.Description != "Compiler diff") throw new cTestsException($"{nameof(cResponseDataFetch)}.4.4");



                    // part numbering
                    //  extension data for multipart: parameters, disposition, language, location
                    //  embedded message structures: envelope, body, size in lines
                    //  extension data for single part: md5, disposition, language, location
                    //  and extensions
                    // ALL TODO


                    // binary
                    //  TODO


                    // parameters

                    lCursor = new cBytesCursor(@"(""TEXT"" ""PLAIN"" (""CHARSET"" ""US-ASCII"" ""fred*"" ""us-ascii'en-us'This%20is%20%2A%2A%2Afun%2A%2A%2A"" ""angus"" ""us-ascii'en-us'This%20is%20%2A%2A%2Afun%2A%2A%2A"") NIL NIL ""7BIT"" 3028 92)");
                    if (!lRDPF.ZProcessBodyStructure(lCursor, cSection.Text, false, out lPart) || !lCursor.Position.AtEnd) throw new cTestsException($"{nameof(cResponseDataFetch)}.5.1");
                    lTextPart = lPart as cTextBodyPart;
                    if (lTextPart.Parameters.First("fred").StringValue != "This is ***fun***" || lTextPart.Parameters.First("FRED").LanguageTag != "en-us") throw new cTestsException($"{nameof(cResponseDataFetch)}.5.2");
                    if (lTextPart.Parameters.First("anGus").StringValue != "us-ascii'en-us'This%20is%20%2A%2A%2Afun%2A%2A%2A") throw new cTestsException($"{nameof(cResponseDataFetch)}.5.2");

                    // TODO : more tests: in particular, missing language tag, missing charset and invalid cases





                    lCursor = new cBytesCursor(@"(((""TEXT"" ""PLAIN"" (""CHARSET"" ""UTF-8"") NIL NIL ""7BIT"" 2 1 NIL NIL NIL)(""TEXT"" ""HTML"" (""CHARSET"" ""UTF-8"") NIL NIL ""7BIT"" 2 1 NIL NIL NIL) ""ALTERNATIVE"" (""BOUNDARY"" ""94eb2c14e866ddee50054fb3cf4b"") NIL NIL)(""IMAGE"" ""JPEG"" (""NAME"" ""IMG_20170517_194711.jpg"") NIL NIL ""BASE64"" 6619412 NIL (""ATTACHMENT"" (""FILENAME"" ""IMG_20170517_194711.jpg"")) NIL) ""MIXED"" (""BOUNDARY"" ""94eb2c14e866ddee56054fb3cf4d"") NIL NIL)");
                    if (!lRDPF.ZProcessBodyStructure(lCursor, cSection.Text, true, out lPart) || !lCursor.Position.AtEnd) throw new cTestsException($"{nameof(cResponseDataFetch)}.6.1");
                    if (!(lPart is cMultiPartBody lMultiPartBody)) throw new cTestsException($"{nameof(cResponseDataFetch)}.6.2");
                    if (lMultiPartBody.Parts[1].Disposition?.FileName != "IMG_20170517_194711.jpg") throw new cTestsException($"{nameof(cResponseDataFetch)}.6.3");





                    // section
                    LTestSection("HEADER]", false, null, eSectionTextPart.header, null);
                    LTestSection("TEXT]", false, null, eSectionTextPart.text, null);
                    LTestSection("HEADER.FIELDS (a xxy b)]", false, null, eSectionTextPart.headerfields, "A", "B", "XXY");
                    LTestSection(@"HEADER.FIELDS (a xxy ""b"")]", false, null, eSectionTextPart.headerfields, "A", "B", "XXY");
                    LTestSection("HEADER.FIELDS.NOT (a xxy b)]", false, null, eSectionTextPart.headerfieldsnot, "A", "B", "XXY");
                    LTestSection("1.2.3]", false, "1.2.3", eSectionTextPart.all, null);
                    LTestSection("1.2.3.HEADER]", false, "1.2.3", eSectionTextPart.header, null);
                    LTestSection("1.2.3.TEXT]", false, "1.2.3", eSectionTextPart.text, null);
                    LTestSection("1.2.3.HEADER.FIELDS (a xxy b)]", false, "1.2.3", eSectionTextPart.headerfields, "A", "B", "XXY");
                    LTestSection("1.2.3.HEADER.FIELDS.NOT (a xxy b)]", false, "1.2.3", eSectionTextPart.headerfieldsnot, "A", "B", "XXY");
                    LTestSection("1.2.3.MIME]", false, "1.2.3", eSectionTextPart.mime, null);
                    LTestSection("1.2.3]", true, "1.2.3", eSectionTextPart.all, null);

                    LTestSectionFail("HEADER.FIELDS]", false);
                    LTestSectionFail("HEADER.FIELDS.NOT]", false);
                    LTestSectionFail("MIME]", false);
                    LTestSectionFail("1.2.0]", false);

                    LTestSectionFail("HEADER]", true);
                    LTestSectionFail("TEXT]", true);
                    LTestSectionFail("HEADER.FIELDS (a xxy b)]", true);
                    LTestSectionFail(@"HEADER.FIELDS (a xxy ""b"")]", true);
                    LTestSectionFail("HEADER.FIELDS.NOT (a xxy b)]", true);
                    LTestSectionFail("1.2.0]", true);


                    LTestBaseSubject("fred (fwd)  \t   (fwd)", "fred", "1");
                    LTestBaseSubject("[fwd: fred (fwd)  \t   (fwd)]", "fred", "1");
                    LTestBaseSubject("re: [fwd: fred (fwd)  \t   (fwd)]", "fred", "1");
                    LTestBaseSubject("fw: [fwd: fred (fwd)  \t   (fwd)]", "fred", "1");
                    LTestBaseSubject("fwd: [fwd: fred (fwd)  \t   (fwd)]", "fred", "1");
                    LTestBaseSubject("[dunno] fwd: [fwd: fred (fwd)  \t   (fwd)]", "fred", "1");
                    LTestBaseSubject("[dunno]    fwd  [fred why is this here?]   :  \t   [fwd:    fred   (fwd)  \t   (fwd)]", "fred", "1");
                    LTestBaseSubject("[dunno]    fwd  [fred why is this here?]   :  \t   [fwd :    fred   (fwd)  \t   (fwd)]", "[fwd :    fred   (fwd)  \t   (fwd)]", "1");
                    LTestBaseSubject("[dunno]    fwd  [fred why is this here?]   :  \t   [fwd:    [fred]   (fwd)  \t   (fwd)]", "[fred]", "1");
                    LTestBaseSubject("[dunno] [ more ]   [of]     [fr€d]   fwd  [fred why is this here?]     :  \t   [fwd:    fred   (fwd)  \t   (fwd)]", "fred", "1");
                    LTestBaseSubject("[dunno] [ more ]   [of]     [fr€d]   fwd  [fred why is this here?] [x]    :  \t   [fwd:    fred   (fwd)  \t   (fwd)]", "fwd  [fred why is this here?] [x]    :  \t   [fwd:    fred   (fwd)  \t   (fwd)]", "1");

                    void LTestSection(string pText, bool pBinary, string pExpectedPart, eSectionTextPart pExpectedTextPart, params string[] pExpectedHeaderFields)
                    {
                        var lxCursor = new cBytesCursor(pText);
                        if (!ZProcessSection(lxCursor, pBinary, out var lSection) || !lxCursor.Position.AtEnd) throw new cTestsException($"{nameof(cResponseDataFetch)}.{nameof(LTestSection)}.{pText}.2");
                        if (lSection.Part != pExpectedPart || lSection.TextPart != pExpectedTextPart) throw new cTestsException($"{nameof(cResponseDataFetch)}.{nameof(LTestSection)}.{pText}.3");
                        if (pExpectedHeaderFields == null && lSection.Names == null) return;
                        if (pExpectedHeaderFields == null || lSection.Names == null) throw new cTestsException($"{nameof(cResponseDataFetch)}.{nameof(LTestSection)}.{pText}.4");

                        cHeaderFieldNames lNames = new cHeaderFieldNames(pExpectedHeaderFields);
                        if (lSection.Names != lNames) throw new cTestsException($"{nameof(cResponseDataFetch)}.{nameof(LTestSection)}.{pText}.6");
                    }

                    void LTestSectionFail(string pText, bool pBinary)
                    {
                        var lxCursor = new cBytesCursor(pText);
                        if (ZProcessSection(lxCursor, pBinary, out var lSection)) throw new cTestsException($"{nameof(cResponseDataFetch)}.{nameof(LTestSectionFail)}.2.{pText}.{pBinary}.2");
                    }

                    void LTestBaseSubject(string pSubject, string pBaseSubject, string pTest)
                    {
                        string lBaseSubject = cParsing.CalculateBaseSubject(pSubject);
                        if (lBaseSubject != pBaseSubject) throw new cTestsException($"{pSubject} -> {lBaseSubject} not {pBaseSubject}", lContext);
                    }

                    cBytesCursor MakeCursor(params string[] pLines)
                    {
                        List<cResponseLine> lLines = new List<cResponseLine>();

                        foreach (var lLine in pLines)
                        {
                            if (lLine.Length > 0 && lLine[0] == '{') lLines.Add(new cResponseLine(true, new cBytes(lLine.TrimStart('{'))));
                            else lLines.Add(new cResponseLine(false, new cBytes(lLine)));
                        }

                        return new cBytesCursor(new cResponse(lLines));
                    }
                }
            }
        }
    }
}