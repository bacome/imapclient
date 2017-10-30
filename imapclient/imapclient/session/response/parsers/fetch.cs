using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using work.bacome.imapclient.support;
using work.bacome.trace;

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

                public cResponseDataParserFetch() { }

                public bool Process(cBytesCursor pCursor, out cResponseData rResponseData, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cResponseDataParserFetch), nameof(Process));

                    if (!pCursor.GetNZNumber(out _, out var lMSN) || !pCursor.SkipByte(cASCII.SPACE) || !pCursor.SkipBytes(kFetchSpace)) { rResponseData = null; return false; }

                    if (!pCursor.SkipByte(cASCII.LPAREN))
                    {
                        lContext.TraceWarning("likely malformed fetch response");
                        rResponseData = null;
                        return true;
                    }

                    fCacheAttributes lAttributes = 0;
                    cFetchableFlags lFlags = null;
                    cEnvelope lEnvelope = null;
                    DateTime? lReceived = null;
                    IList<byte> lRFC822 = null;
                    IList<byte> lRFC822Header = null;
                    IList<byte> lRFC822Text = null;
                    uint? lSize = null;
                    cBodyPart lBody = null;
                    cBodyPart lBodyStructure = null;
                    List<cBody> lBodies = new List<cBody>();
                    uint? lUID = null;
                    cHeaderFields lHeaderFields = null;
                    Dictionary<string, uint> lBinarySizes = new Dictionary<string, uint>();
                    ulong? lModSeq = null;

                    while (true)
                    {
                        fCacheAttributes lAttribute;
                        bool lOK;

                        if (pCursor.SkipBytes(kFlagsSpace))
                        {
                            lAttribute = fCacheAttributes.flags;
                            lOK = pCursor.GetFlags(out var lRawFlags) && cFetchableFlags.TryConstruct(lRawFlags, out lFlags);
                        }
                        else if (pCursor.SkipBytes(kEnvelopeSpace))
                        {
                            lAttribute = fCacheAttributes.envelope;
                            lOK = ZProcessEnvelope(pCursor, out lEnvelope);
                        }
                        else if (pCursor.SkipBytes(kInternalDateSpace))
                        {
                            lAttribute = fCacheAttributes.received;
                            lOK = pCursor.GetDateTime(out var lDateTime);
                            if (lOK) lReceived = lDateTime;
                        }
                        else if (pCursor.SkipBytes(kRFC822Space))
                        {
                            lAttribute = 0;
                            lOK = pCursor.GetNString(out lRFC822);
                            if (lOK && cHeaderFields.TryConstruct(lRFC822, out var lTemp)) lHeaderFields += lTemp;
                        }
                        else if (pCursor.SkipBytes(kRFC822HeaderSpace))
                        {
                            lAttribute = 0;
                            lOK = pCursor.GetNString(out lRFC822Header); 
                            if (lOK && cHeaderFields.TryConstruct(lRFC822Header, out var lTemp)) lHeaderFields += lTemp;
                        }
                        else if (pCursor.SkipBytes(kRFC822TextSpace))
                        {
                            lAttribute = 0;
                            lOK = pCursor.GetNString(out lRFC822Text);
                        }
                        else if (pCursor.SkipBytes(kRFC822SizeSpace))
                        {
                            lAttribute = fCacheAttributes.size;
                            lOK = pCursor.GetNumber(out _, out var lNumber);
                            if (lOK) lSize = lNumber;
                        }
                        else if (pCursor.SkipBytes(kBodySpace))
                        {
                            lAttribute = fCacheAttributes.body;
                            lOK = ZProcessBodyStructure(pCursor, cSection.Text, false, out lBody);
                        }
                        else if (pCursor.SkipBytes(kBodyStructureSpace))
                        {
                            lAttribute = fCacheAttributes.bodystructure;
                            lOK = ZProcessBodyStructure(pCursor, cSection.Text, true, out lBodyStructure);
                        }
                        else if (pCursor.SkipBytes(kBodyLBracket))
                        {
                            lAttribute = 0;
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
                            lAttribute = fCacheAttributes.uid;
                            lOK = pCursor.GetNZNumber(out _, out var lNumber);
                            if (lOK) lUID = lNumber;
                        }
                        else if (pCursor.SkipBytes(kBinaryLBracket))
                        {
                            lAttribute = 0;
                            lOK = ZProcessBody(pCursor, true, out var lABody);
                            if (lOK) lBodies.Add(lABody);
                        }
                        else if (pCursor.SkipBytes(kBinarySizeLBracket))
                        {
                            lAttribute = 0;
                            lOK = ZProcessBinarySize(pCursor, out var lPart, out var lBytes);
                            if (lOK) lBinarySizes[lPart] = lBytes;
                        }
                        else if (pCursor.SkipBytes(kModSeqSpaceLParen))
                        {
                            lAttribute = fCacheAttributes.modseq;
                            lOK = pCursor.GetNumber(out var lTemp) && pCursor.SkipByte(cASCII.RPAREN);
                            if (lOK) lModSeq = lTemp;
                        }
                        else break;

                        lAttributes |= lAttribute;

                        if (!lOK)
                        {
                            lContext.TraceWarning("likely malformed fetch response");
                            rResponseData = null;
                            return true;
                        }

                        if (!pCursor.SkipByte(cASCII.SPACE)) break;
                    }

                    if (!pCursor.SkipByte(cASCII.RPAREN) || !pCursor.Position.AtEnd)
                    { 
                        lContext.TraceWarning("likely malformed fetch response");
                        rResponseData = null;
                        return true;
                    }

                    rResponseData = new cResponseDataFetch(lMSN, lAttributes, lFlags, lEnvelope, lReceived, lRFC822, lRFC822Header, lRFC822Text, lSize, lBody, lBodyStructure, lBodies, lUID, lHeaderFields, lBinarySizes, lModSeq);
                    return true;
                }

                private static bool ZProcessEnvelope(cBytesCursor pCursor, out cEnvelope rEnvelope)
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

                    DateTime? lSent;

                    if (lDateBytes == null || lDateBytes.Count == 0) lSent = null;
                    {
                        // rfc 5256 says quite a lot about how this date should be parsed: please note that I haven't done what it says at this stage (TODO?)
                        var lCursor = new cBytesCursor(lDateBytes);
                        if (lCursor.GetRFC822DateTime(out var lTempDate) && lCursor.Position.AtEnd) lSent = lTempDate;
                        else lSent = null;
                    }

                    cCulturedString lSubject;
                    string lBaseSubject;

                    if (lSubjectBytes == null || lSubjectBytes.Count == 0)
                    {
                        lSubject = null;
                        lBaseSubject = null;
                    }
                    else
                    {
                        lSubject = new cCulturedString(lSubjectBytes);
                        lBaseSubject = cBaseSubject.Calculate(lSubject);
                    }

                    cHeaderFieldMsgIds.TryConstruct(kHeaderFieldName.InReplyTo, lInReplyToBytes, out var lInReplyTo);
                    cHeaderFieldMsgId.TryConstruct(kHeaderFieldName.MessageId, lMessageIdBytes, out var lMessageId);

                    rEnvelope = new cEnvelope(lSent, lSubject, lBaseSubject, lFrom, lSender, lReplyTo, lTo, lCC, lBCC, lInReplyTo, lMessageId);
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

                    bool lFirst = true;

                    string lSortString = null; // rfc5256
                    string lDisplaySortString = null; // rfc5957

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

                                if (lGroupAddresses != null) { rAddresses = null; return false; } // start of group while in a group
                                lGroupDisplayName = new cCulturedString(lMailboxBytes);
                                lGroupAddresses = new List<cEmailAddress>();

                                if (lFirst)
                                {
                                    lSortString = lGroupDisplayName;

                                    if (lNameBytes != null)
                                    {
                                        lDisplaySortString = new cCulturedString(lNameBytes);
                                        if (lDisplaySortString.Length != 0) lFirst = false;
                                    }

                                    if (lFirst)
                                    {
                                        lDisplaySortString = lGroupDisplayName;
                                        lFirst = false;
                                    }
                                }
                            }
                        }
                        else
                        {
                            // not sure how mailbox names are meant to support i18n? rfc 6530 implies UTF8 but I can't find how that affects the value reported by IMAP in mailbox
                            //  at this stage if UTF8 were to appear in the mailbox then we would just accept it

                            string lMailbox = cTools.UTF8BytesToString(lMailboxBytes);
                            string lAddress = lMailbox + "@" + cTools.ASCIIBytesToString(lHostBytes);
                            string lDisplayAddress = lMailbox + "@" + cTools.PunycodeBytesToString(lHostBytes);

                            cCulturedString lDisplayName;
                            if (lNameBytes == null) lDisplayName = new cCulturedString("<" + lDisplayAddress + ">");
                            else lDisplayName = new cCulturedString(lNameBytes);

                            var lEmailAddress = new cEmailAddress(lDisplayName, lAddress, lDisplayAddress);

                            if (lGroupAddresses != null) lGroupAddresses.Add(lEmailAddress);
                            else lAddresses.Add(lEmailAddress);

                            if (lFirst)
                            {
                                lSortString = lMailbox;

                                if (lDisplayName != null)
                                {
                                    lDisplaySortString = lDisplayName;
                                    if (lDisplaySortString.Length != 0) lFirst = false;
                                }

                                if (lFirst)
                                {
                                    lDisplaySortString = lAddress;
                                    lFirst = false;
                                }
                            }
                        }
                    }

                    if (lGroupAddresses != null) { rAddresses = null; return false; } // missed the end of group

                    if (!pCursor.SkipByte(cASCII.RPAREN)) { rAddresses = null; return false; }

                    rAddresses = new cAddresses(lSortString, lDisplaySortString, lAddresses);

                    return true;
                }

                private static bool ZProcessBodyStructure(cBytesCursor pCursor, cSection pSection, bool pExtended, out cBodyPart rBodyPart)
                {
                    if (!pCursor.SkipByte(cASCII.LPAREN)) { rBodyPart = null; return false; }

                    string lSubType;
                    cBodyPart lBodyPart;

                    string lSubPartPrefix;
                    if (pSection.Part == null) lSubPartPrefix = "";
                    else lSubPartPrefix = pSection.Part + ".";

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

                        rBodyPart = new cMultiPartBody(lParts, lSubType, pSection, lMultiPartExtensionData);
                        return true;
                    }

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
                    else lDescription = new cCulturedString(lDescriptionBytes);

                    if (lType.Equals(kMimeType.Text, StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (!pCursor.SkipByte(cASCII.SPACE) || !pCursor.GetNumber(out _, out var lSizeInLines)) { rBodyPart = null; return false; }

                        if (pExtended)
                        {
                            if (!ZProcessBodyStructureSinglePartExtensionData(pCursor, out lExtensionData)) { rBodyPart = null; return false; }
                        }
                        else lExtensionData = null;

                        if (!pCursor.SkipByte(cASCII.RPAREN)) { rBodyPart = null; return false; }

                        rBodyPart = new cTextBodyPart(lSubType, pSection, lParameters, lContentId, lDescription, lContentTransferEncoding, lSizeInBytes, lSizeInLines, lExtensionData);
                        return true;
                    }

                    if (lType.Equals(kMimeType.Message, StringComparison.InvariantCultureIgnoreCase) && lSubType.Equals(kMimeSubType.RFC822, StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (!pCursor.SkipByte(cASCII.SPACE) || 
                            !ZProcessEnvelope(pCursor, out var lEnvelope) ||
                            !pCursor.SkipByte(cASCII.SPACE) ||
                            !ZProcessBodyStructure(pCursor, new cSection(pSection.Part, eSectionTextPart.text), pExtended, out lBodyPart) ||
                            !pCursor.SkipByte(cASCII.SPACE) ||
                            !pCursor.GetNumber(out _, out var lSizeInLines)) { rBodyPart = null; return false; }

                        cBodyPart lBody;
                        cBodyPart lBodyStructure;

                        if (pExtended)
                        {
                            lBody = null;
                            lBodyStructure = lBodyPart;
                            if (!ZProcessBodyStructureSinglePartExtensionData(pCursor, out lExtensionData)) { rBodyPart = null; return false; }
                        }
                        else
                        {
                            lBody = lBodyPart;
                            lBodyStructure = null;
                            lExtensionData = null;
                        }

                        if (!pCursor.SkipByte(cASCII.RPAREN)) { rBodyPart = null; return false; }

                        rBodyPart = new cMessageBodyPart(pSection, lParameters, lContentId, lDescription, lContentTransferEncoding, lSizeInBytes, lEnvelope, lBody, lBodyStructure, lSizeInLines, lExtensionData);
                        return true;
                    }

                    if (pExtended)
                    {
                        if (!ZProcessBodyStructureSinglePartExtensionData(pCursor, out lExtensionData)) { rBodyPart = null; return false; }
                    }
                    else lExtensionData = null;

                    if (!pCursor.SkipByte(cASCII.RPAREN)) { rBodyPart = null; return false; }

                    rBodyPart = new cSinglePartBody(lType, lSubType, pSection, lParameters, lContentId, lDescription, lContentTransferEncoding, lSizeInBytes, lExtensionData);
                    return true;
                }

                private static bool ZProcessBodyStructureParameters(cBytesCursor pCursor, out cBodyStructureParameters rParameters)
                {
                    if (pCursor.SkipBytes(cBytesCursor.Nil)) { rParameters = null; return true; }

                    if (!pCursor.SkipByte(cASCII.LPAREN)) { rParameters = null; return false; }

                    cBodyStructureParametersBuilder lBuilder = new cBodyStructureParametersBuilder();

                    while (true)
                    {
                        if (!pCursor.GetString(out IList<byte> lName) || !pCursor.SkipByte(cASCII.SPACE) || !pCursor.GetString(out IList<byte> lValue)) { rParameters = null; return false; }
                        lBuilder.Add(lName, lValue);
                        if (!pCursor.SkipByte(cASCII.SPACE)) break;
                    }

                    if (!pCursor.SkipByte(cASCII.RPAREN)) { rParameters = null; return false; }

                    rParameters = lBuilder.ToBodyStructureParameters();
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
                    else lPart = cTools.ASCIIBytesToString(lPartBytes);

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

                private static bool ZProcessBinarySize(cBytesCursor pCursor, out string rPart, out uint rSize)
                {
                    if (!ZProcessSection(pCursor, true, out var lSection) ||
                        !pCursor.SkipByte(cASCII.SPACE) ||
                        !pCursor.GetNumber(out _, out rSize)) { rPart = null; rSize = 0; return false; }

                    rPart = lSection.Part;
                    return true;
                }

                private class cBodyStructureParametersBuilder
                {
                    private readonly List<cBodyStructureParameter> mParameters = new List<cBodyStructureParameter>();

                    public cBodyStructureParametersBuilder() { }

                    public void Add(IList<byte> pName, IList<byte> pValue)
                    {
                        cBytes lRawValue = new cBytes(pValue);

                        if (pName.Count == 0 || pName[pName.Count - 1] != cASCII.ASTERISK)
                        {
                            // just plain name=value
                            mParameters.Add(new cBodyStructureParameter(pName, pValue));
                            return;
                        }
                        
                        // should be rfc 2231 syntax ... we will attempt to decode it if the IMAP server has decoded the parameter value continuations as per rfc 2231.6

                        // check if there is another '*' in the name
                        for (int i = 0; i < pName.Count - 1; i++)
                            if (pName[i] == cASCII.ASTERISK)
                            {
                                // not decoded rfc 2231, store name=value
                                mParameters.Add(new cBodyStructureParameter(pName, pValue));
                                return;
                            }

                        // try to parse the value

                        cBytesCursor lCursor = new cBytesCursor(pValue);

                        // note that the code to get the charset is not strictly correct, as only registered charset names should be extracted
                        //  also note that "'" is a valid character in a charset name but it is also the rfc 2231 delimiter, so if there were a charset with a "'" in the name this code would garble it
                        //   (that is why charsetnamedash is used rather than charsetname)

                        lCursor.GetToken(cCharset.CharsetNameDash, null, null, out var lCharsetName); // charset is optional

                        if (lCursor.SkipByte(cASCII.QUOTE))
                        {
                            lCursor.GetLanguageTag(out var lLanguageTag); // language tag is optional

                            if (lCursor.SkipByte(cASCII.QUOTE))
                            {
                                lCursor.GetToken(cCharset.All, cASCII.PERCENT, null, out cByteList lValueBytes); // the value itself is optional (!)

                                if (lCursor.Position.AtEnd && cTools.TryCharsetBytesToString(lCharsetName, lValueBytes, out var lValue))
                                {
                                    // successful decode of rfc 2231 syntax

                                    // trim off the asterisk at the end of the name
                                    byte[] lName = new byte[pName.Count - 1];
                                    for (int i = 0; i < pName.Count - 1; i++) lName[i] = pName[i];

                                    // store the decoded name and the decoded value
                                    mParameters.Add(new cBodyStructureParameter(lName, pValue, lValue, lLanguageTag));
                                    return;
                                }
                            }
                        }

                        // failed to decode rfc 2231 syntax: store name=value
                        mParameters.Add(new cBodyStructureParameter(pName, pValue));
                    }

                    public cBodyStructureParameters ToBodyStructureParameters() => new cBodyStructureParameters(mParameters);
                }

                private class cBaseSubject
                {
                    // from rfc5256 section 2.1

                    private string mSubject;
                    private int mFront;
                    private int mRear;

                    private cBaseSubject(string pChars)
                    {
                        mSubject = pChars;
                        mFront = 0;
                        mRear = mSubject.Length - 1;
                    }

                    private bool ZSkipCharAtFront(char pChar)
                    {
                        if (mFront > mRear) return false;
                        if (ZCompare(mSubject[mFront], pChar)) { mFront++; return true; }
                        return false;
                    }

                    private bool ZSkipCharsAtFront(string pChars)
                    {
                        if (mFront > mRear) return false;

                        var lBookmark = mFront;

                        int lChar = 0;

                        while (true)
                        {
                            if (!ZCompare(mSubject[mFront], pChars[lChar]))
                            {
                                mFront = lBookmark;
                                return false;
                            }

                            mFront++;
                            lChar++;

                            if (lChar == pChars.Length) return true;

                            if (mFront > mRear)
                            {
                                mFront = lBookmark;
                                return false;
                            }
                        }
                    }

                    private bool ZGetCharAtFront(out char rChar)
                    {
                        if (mFront > mRear) { rChar = '\0'; return false; }
                        rChar = mSubject[mFront++];
                        return true;
                    }

                    private bool ZSkipCharAtRear(char pChar)
                    {
                        if (mFront > mRear) return false;
                        if (ZCompare(mSubject[mRear], pChar)) { mRear--; return true; }
                        return false;
                    }

                    private bool ZSkipCharsAtRear(string pChars)
                    {
                        if (mFront > mRear) return false;

                        var lBookmark = mRear;

                        int lChar = pChars.Length - 1;

                        while (true)
                        {
                            if (!ZCompare(mSubject[mRear], pChars[lChar]))
                            {
                                mRear = lBookmark;
                                return false;
                            }

                            mRear--;
                            lChar--;

                            if (lChar == -1) return true;

                            if (mFront > mRear)
                            {
                                mRear = lBookmark;
                                return false;
                            }
                        }
                    }

                    private bool ZSkipWSP()
                    {
                        bool lResult = false;

                        while (true)
                        {
                            if (!ZSkipCharAtFront(' ') && !ZSkipCharAtFront('\t')) return lResult;
                            lResult = true;
                        }
                    }

                    private bool ZSkipBlobReFwd()
                    {
                        int lBookmark = mFront;

                        while (true)
                        {
                            if (!ZSkipBlob()) break;
                        }

                        if (!ZSkipCharsAtFront("re") &&
                            !ZSkipCharsAtFront("fwd") &&
                            !ZSkipCharsAtFront("fw"))
                        {
                            mFront = lBookmark;
                            return false;
                        }

                        ZSkipWSP();
                        ZSkipBlob();

                        if (!ZSkipCharAtFront(':'))
                        {
                            mFront = lBookmark;
                            return false;
                        }

                        return true;
                    }

                    private bool ZSkipBlob()
                    {
                        int lBookmark = mFront;

                        if (!ZSkipCharAtFront('[')) return false;

                        while (true)
                        {
                            if (!ZGetCharAtFront(out var lChar) || lChar == '[') { mFront = lBookmark; return false; }
                            if (lChar == ']') break;
                        }

                        ZSkipWSP();

                        return true;
                    }

                    private bool ZCompare(char pChar1, char pChar2)
                    {
                        char lChar1;
                        if (pChar1 < 'a') lChar1 = pChar1;
                        else if (pChar1 > 'z') lChar1 = pChar1;
                        else lChar1 =(char)(pChar1 - 'a' + 'A');

                        char lChar2;
                        if (pChar2 < 'a') lChar2 = pChar2;
                        else if (pChar2 > 'z') lChar2 = pChar2;
                        else lChar2 = (char)(pChar2 - 'a' + 'A');

                        return lChar1 == lChar2;
                    }

                    public static string Calculate(string pSubject)
                    {
                        cBaseSubject lBaseSubject = new cBaseSubject(pSubject);
                        int lBookmark;

                        while (true)
                        {
                            // 2.1.2
                            while (true)
                            {
                                if (!lBaseSubject.ZSkipCharsAtRear("(fwd)") &&
                                    !lBaseSubject.ZSkipCharAtRear(' ') &&
                                    !lBaseSubject.ZSkipCharAtRear('\t')) break;
                            }

                            while (true) // 2.1.5
                            {
                                // 2.1.3
                                if (lBaseSubject.ZSkipWSP()) continue;
                                if (lBaseSubject.ZSkipBlobReFwd()) continue;

                                // 2.1.4

                                lBookmark = lBaseSubject.mFront;

                                if (lBaseSubject.ZSkipBlob())
                                {
                                    if (lBaseSubject.mFront > lBaseSubject.mRear)
                                    {
                                        lBaseSubject.mFront = lBookmark;
                                        break;
                                    }

                                    continue;
                                }

                                break;
                            }

                            // 2.1.6

                            lBookmark = lBaseSubject.mFront;

                            if (!lBaseSubject.ZSkipCharsAtFront("[fwd:")) break;

                            if (!lBaseSubject.ZSkipCharAtRear(']'))
                            {
                                lBaseSubject.mFront = lBookmark;
                                break;
                            }
                        }

                        if (lBaseSubject.mFront > lBaseSubject.mRear) return null;

                        return lBaseSubject.mSubject.Substring(lBaseSubject.mFront, lBaseSubject.mRear - lBaseSubject.mFront + 1);
                    }
                }

                [Conditional("DEBUG")]
                public static void _Tests(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cResponseDataFetch), nameof(_Tests));

                    cBytesCursor lCursor;

                    cResponseDataParserFetch lRDPF = new cResponseDataParserFetch();

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

                    if (lData.Flags.Count != 1 || !lData.Flags.Contains(@"\SeEn")) throw new cTestsException($"{nameof(cResponseDataFetch)}.1.2");
                    if (lData.Received != new DateTime(1996, 7, 17, 9, 44, 25, DateTimeKind.Utc)) throw new cTestsException($"{nameof(cResponseDataFetch)}.1.3");
                    if (lData.Envelope.Sent != new DateTime(1996, 7, 17, 9, 23, 25, DateTimeKind.Utc)) throw new cTestsException($"{nameof(cResponseDataFetch)}.1.4");
                    if (lData.Envelope.Subject != "IMAP4rev1 WG mtg summary and minutes") throw new cTestsException($"{nameof(cResponseDataFetch)}.1.5");
                    if (lData.Envelope.From.Count != 1) throw new cTestsException($"{nameof(cResponseDataFetch)}.1.6.1");
                    lEmailAddress = lData.Envelope.From[0] as cEmailAddress;
                    if (lEmailAddress.DisplayName != "Terry Gray" || lEmailAddress.Address != "gray@cac.washington.edu") throw new cTestsException($"{nameof(cResponseDataFetch)}.1.6.2");
                    if (lData.Envelope.Sender.Count != 1) throw new cTestsException($"{nameof(cResponseDataFetch)}.1.7");
                    if (lData.Envelope.ReplyTo.Count != 1) throw new cTestsException($"{nameof(cResponseDataFetch)}.1.7");
                    if (lData.Envelope.To.Count != 1) throw new cTestsException($"{nameof(cResponseDataFetch)}.1.8.1");
                    lEmailAddress = lData.Envelope.To[0] as cEmailAddress;
                    if (lEmailAddress.DisplayName != "<imap@cac.washington.edu>" || lEmailAddress.Address != "imap@cac.washington.edu") throw new cTestsException($"{nameof(cResponseDataFetch)}.1.8.2");

                    if (lData.Envelope.CC.Count != 2) throw new cTestsException($"{nameof(cResponseDataFetch)}.1.9.1");
                    lEmailAddress = lData.Envelope.CC[0] as cEmailAddress;
                    if (lEmailAddress.DisplayName != "<minutes@CNRI.Reston.VA.US>" || lEmailAddress.Address != "minutes@CNRI.Reston.VA.US") throw new cTestsException($"{nameof(cResponseDataFetch)}.1.9.2");
                    lEmailAddress = lData.Envelope.CC[1] as cEmailAddress;
                    if (lEmailAddress.DisplayName != "John Klensin" || lEmailAddress.Address != "KLENSIN@MIT.EDU") throw new cTestsException($"{nameof(cResponseDataFetch)}.1.9.3");

                    if (lData.Envelope.BCC != null || lData.Envelope.InReplyTo != null) throw new cTestsException($"{nameof(cResponseDataFetch)}.1.10");

                    if (lData.Envelope.MessageId.MsgId != "B27397-0100000@cac.washington.edu") throw new cTestsException($"{nameof(cResponseDataFetch)}.1.11");

                    lTextPart = lData.Body as cTextBodyPart;
                    if (lTextPart.SubType != "PLAIN" || lTextPart.Parameters.Count != 1 || lTextPart.Charset != "US-ASCII" || lTextPart.SizeInBytes != 3028 || lTextPart.SizeInLines != 92) throw new cTestsException($"{nameof(cResponseDataFetch)}.1.12.1");
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

                    if (lData.Envelope.InReplyTo == null || lData.Envelope.InReplyTo.MsgIds.Count != 1 || lData.Envelope.InReplyTo.MsgIds[0] != "01KF8JCEOCBS0045PS@xxx.yyy.com") throw new cTestsException($"{nameof(cResponseDataFetch)}.1a.3");





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

                    if (!ZProcessBodyStructure(lCursor, cSection.Text, false, out lPart) || !lCursor.Position.AtEnd) throw new cTestsException($"{nameof(cResponseDataFetch)}.4.1");

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
                    if (!ZProcessBodyStructure(lCursor, cSection.Text, false, out lPart) || !lCursor.Position.AtEnd) throw new cTestsException($"{nameof(cResponseDataFetch)}.5.1");
                    lTextPart = lPart as cTextBodyPart;
                    if (lTextPart.Parameters.First("fred").StringValue != "This is ***fun***" || lTextPart.Parameters.First("FRED").LanguageTag != "en-us") throw new cTestsException($"{nameof(cResponseDataFetch)}.5.2");
                    if (lTextPart.Parameters.First("anGus").StringValue != "us-ascii'en-us'This%20is%20%2A%2A%2Afun%2A%2A%2A") throw new cTestsException($"{nameof(cResponseDataFetch)}.5.2");

                    // TODO : more tests: in particular, missing language tag, missing charset and invalid cases





                    lCursor = new cBytesCursor(@"(((""TEXT"" ""PLAIN"" (""CHARSET"" ""UTF-8"") NIL NIL ""7BIT"" 2 1 NIL NIL NIL)(""TEXT"" ""HTML"" (""CHARSET"" ""UTF-8"") NIL NIL ""7BIT"" 2 1 NIL NIL NIL) ""ALTERNATIVE"" (""BOUNDARY"" ""94eb2c14e866ddee50054fb3cf4b"") NIL NIL)(""IMAGE"" ""JPEG"" (""NAME"" ""IMG_20170517_194711.jpg"") NIL NIL ""BASE64"" 6619412 NIL (""ATTACHMENT"" (""FILENAME"" ""IMG_20170517_194711.jpg"")) NIL) ""MIXED"" (""BOUNDARY"" ""94eb2c14e866ddee56054fb3cf4d"") NIL NIL)");
                    if (!ZProcessBodyStructure(lCursor, cSection.Text, true, out lPart) || !lCursor.Position.AtEnd) throw new cTestsException($"{nameof(cResponseDataFetch)}.6.1");
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
                        string lBaseSubject = cBaseSubject.Calculate(pSubject);
                        if (lBaseSubject != pBaseSubject) throw new cTestsException($"{pSubject} -> {lBaseSubject} not {pBaseSubject}", lContext);
                    }

                    cBytesCursor MakeCursor(params string[] pLines)
                    {
                        List<cBytesLine> lLines = new List<cBytesLine>();

                        foreach (var lLine in pLines)
                        {
                            if (lLine.Length > 0 && lLine[0] == '{') lLines.Add(new cBytesLine(true, new cBytes(lLine.TrimStart('{'))));
                            else lLines.Add(new cBytesLine(false, new cBytes(lLine)));
                        }

                        return new cBytesCursor(new cBytesLines(lLines));
                    }
                }
            }
        }
    }
}