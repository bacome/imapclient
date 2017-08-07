using System;
using System.Collections.Generic;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cResponseTextProcessor
            {
                // rfc 3501
                private static readonly cBytes kAlertRBracketSpace = new cBytes("ALERT] ");
                private static readonly cBytes kParseRBracketSpace = new cBytes("PARSE] ");
                private static readonly cBytes kTryCreateRBracketSpace = new cBytes("TRYCREATE] ");

                // rfc 5530
                private static readonly cBytes kUnavailableRBracketSpace = new cBytes("UNAVAILABLE] ");
                private static readonly cBytes kAuthenticationFailedRBracketSpace = new cBytes("AUTHENTICATIONFAILED] ");
                private static readonly cBytes kAuthorizationFailedRBracketSpace = new cBytes("AUTHORIZATIONFAILED] ");
                private static readonly cBytes kExpiredRBracketSpace = new cBytes("EXPIRED] ");
                private static readonly cBytes kPrivacyRequiredRBracketSpace = new cBytes("PRIVACYREQUIRED] ");
                private static readonly cBytes kContactAdminRBracketSpace = new cBytes("CONTACTADMIN] ");
                private static readonly cBytes kNoPermRBracketSpace = new cBytes("NOPERM] ");
                private static readonly cBytes kInUseRBracketSpace = new cBytes("INUSE] ");
                private static readonly cBytes kExpungeIssuedRBracketSpace = new cBytes("EXPUNGEISSUED] ");
                private static readonly cBytes kCorruptionRBracketSpace = new cBytes("CORRUPTION] ");
                private static readonly cBytes kServerBugRBracketSpace = new cBytes("SERVERBUG] ");
                private static readonly cBytes kClientBugRBracketSpace = new cBytes("CLIENTBUG] ");
                private static readonly cBytes kCannotRBracketSpace = new cBytes("CANNOT] ");
                private static readonly cBytes kLimitRBracketSpace = new cBytes("LIMIT] ");
                private static readonly cBytes kOverQuotaRBracketSpace = new cBytes("OVERQUOTA] ");
                private static readonly cBytes kAlreadyExistsRBracketSpace = new cBytes("ALREADYEXISTS] ");
                private static readonly cBytes kNonExistentRBracketSpace = new cBytes("NONEXISTENT] ");

                // rfc 3501
                private static readonly cBytes kBadCharset = new cBytes("BADCHARSET");

                // rfc 2193
                private static readonly cBytes kReferralSpace = new cBytes("REFERRAL ");

                // rfc 6154
                private static readonly cBytes kUseAttrRBracketSpace = new cBytes("USEATTR] ");

                // rfc 3516
                private static readonly cBytes kUnknownCTERBracketSpace = new cBytes("UNKNOWN-CTE] ");

                private readonly cEventSynchroniser mEventSynchroniser;
                private readonly List<iResponseTextCodeParser> mResponseTextCodeParsers = new List<iResponseTextCodeParser>();
                private cMailboxCache mMailboxCache = null;

                public cResponseTextProcessor(cEventSynchroniser pEventSynchroniser)
                {
                    mEventSynchroniser = pEventSynchroniser ?? throw new ArgumentNullException(nameof(pEventSynchroniser));
                }

                public void Enable(cMailboxCache pMailboxCache, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cResponseTextProcessor), nameof(Enable));
                    if (mMailboxCache != null) throw new InvalidOperationException();
                    mMailboxCache = pMailboxCache ?? throw new ArgumentNullException(nameof(pMailboxCache));
                }

                public cResponseText Process(cBytesCursor pCursor, eResponseTextType pTextType, iTextCodeProcessor pTextCodeProcessor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cResponseTextProcessor), nameof(Process), pTextType);

                    cResponseText lResponseText;

                    var lBookmarkBeforeLBRACET = pCursor.Position;

                    if (!pCursor.SkipByte(cASCII.LBRACKET))
                    {
                        lResponseText = new cResponseText(pCursor.GetRestAsString());
                        lContext.TraceVerbose("response text received: {0}", lResponseText);
                        mEventSynchroniser.FireResponseText(pTextType, lResponseText, lContext);
                        return lResponseText;
                    }

                    var lBookmarkAfterLBRACET = pCursor.Position;

                    if (ZGetResponseText(pCursor, pTextType, kAlertRBracketSpace, eResponseTextCode.alert, out lResponseText, lContext)) return lResponseText;
                    if (ZGetResponseText(pCursor, pTextType, kParseRBracketSpace, eResponseTextCode.parse, out lResponseText, lContext)) return lResponseText;
                    if (ZGetResponseText(pCursor, pTextType, kTryCreateRBracketSpace, eResponseTextCode.trycreate, out lResponseText, lContext)) return lResponseText;
                    if (ZGetResponseText(pCursor, pTextType, kUnavailableRBracketSpace, eResponseTextCode.unavailable, out lResponseText, lContext)) return lResponseText;
                    if (ZGetResponseText(pCursor, pTextType, kAuthenticationFailedRBracketSpace, eResponseTextCode.authenticationfailed, out lResponseText, lContext)) return lResponseText;
                    if (ZGetResponseText(pCursor, pTextType, kAuthorizationFailedRBracketSpace, eResponseTextCode.authorizationfailed, out lResponseText, lContext)) return lResponseText;
                    if (ZGetResponseText(pCursor, pTextType, kExpiredRBracketSpace, eResponseTextCode.expired, out lResponseText, lContext)) return lResponseText;
                    if (ZGetResponseText(pCursor, pTextType, kPrivacyRequiredRBracketSpace, eResponseTextCode.privacyrequired, out lResponseText, lContext)) return lResponseText;
                    if (ZGetResponseText(pCursor, pTextType, kContactAdminRBracketSpace, eResponseTextCode.contactadmin, out lResponseText, lContext)) return lResponseText;
                    if (ZGetResponseText(pCursor, pTextType, kNoPermRBracketSpace, eResponseTextCode.noperm, out lResponseText, lContext)) return lResponseText;
                    if (ZGetResponseText(pCursor, pTextType, kInUseRBracketSpace, eResponseTextCode.inuse, out lResponseText, lContext)) return lResponseText;
                    if (ZGetResponseText(pCursor, pTextType, kExpungeIssuedRBracketSpace, eResponseTextCode.expungeissued, out lResponseText, lContext)) return lResponseText;
                    if (ZGetResponseText(pCursor, pTextType, kCorruptionRBracketSpace, eResponseTextCode.corruption, out lResponseText, lContext)) return lResponseText;
                    if (ZGetResponseText(pCursor, pTextType, kServerBugRBracketSpace, eResponseTextCode.serverbug, out lResponseText, lContext)) return lResponseText;
                    if (ZGetResponseText(pCursor, pTextType, kClientBugRBracketSpace, eResponseTextCode.clientbug, out lResponseText, lContext)) return lResponseText;
                    if (ZGetResponseText(pCursor, pTextType, kCannotRBracketSpace, eResponseTextCode.cannot, out lResponseText, lContext)) return lResponseText;
                    if (ZGetResponseText(pCursor, pTextType, kLimitRBracketSpace, eResponseTextCode.limit, out lResponseText, lContext)) return lResponseText;
                    if (ZGetResponseText(pCursor, pTextType, kOverQuotaRBracketSpace, eResponseTextCode.overquota, out lResponseText, lContext)) return lResponseText;
                    if (ZGetResponseText(pCursor, pTextType, kAlreadyExistsRBracketSpace, eResponseTextCode.alreadyexists, out lResponseText, lContext)) return lResponseText;
                    if (ZGetResponseText(pCursor, pTextType, kNonExistentRBracketSpace, eResponseTextCode.nonexistent, out lResponseText, lContext)) return lResponseText;
                    if (ZGetResponseText(pCursor, pTextType, kUseAttrRBracketSpace, eResponseTextCode.useattr, out lResponseText, lContext)) return lResponseText;
                    if (ZGetResponseText(pCursor, pTextType, kUnknownCTERBracketSpace, eResponseTextCode.unknowncte, out lResponseText, lContext)) return lResponseText;

                    if (pCursor.SkipBytes(kBadCharset))
                    {
                        if (pCursor.SkipBytes(cBytesCursor.RBracketSpace))
                        {
                            lResponseText = new cResponseText(eResponseTextCode.badcharset, pCursor.GetRestAsString());
                            lContext.TraceWarning("response text received: {0}", lResponseText);
                            mEventSynchroniser.FireResponseText(pTextType, lResponseText, lContext);
                            return lResponseText;
                        }

                        if (pCursor.SkipBytes(cBytesCursor.SpaceLParen))
                        {
                            List<string> lCharsets = new List<string>();

                            while (true)
                            {
                                if (!pCursor.GetAString(out string lCharset)) break;
                                lCharsets.Add(lCharset);

                                if (pCursor.SkipByte(cASCII.SPACE)) continue;

                                if (pCursor.SkipBytes(cBytesCursor.RParenRBracketSpace))
                                {
                                    lResponseText = new cResponseText(eResponseTextCode.badcharset, new cStrings(lCharsets), pCursor.GetRestAsString());
                                    lContext.TraceWarning("response text received: {0}", lResponseText);
                                    mEventSynchroniser.FireResponseText(pTextType, lResponseText, lContext);
                                    return lResponseText;
                                }

                                break;
                            }
                        }

                        lContext.TraceWarning("likely badly formed badcharset: {0}", pCursor);
                        pCursor.Position = lBookmarkAfterLBRACET;
                    }
                    else if (pCursor.SkipBytes(kReferralSpace))
                    {
                        List<string> lURIs = new List<string>();

                        while (true)
                        {
                            if (!pCursor.GetURI(out _, out var lURI, lContext)) break;
                            lURIs.Add(lURI);

                            if (pCursor.SkipByte(cASCII.SPACE)) continue;

                            if (pCursor.SkipBytes(cBytesCursor.RBracketSpace))
                            {
                                lResponseText = new cResponseText(eResponseTextCode.referral, new cStrings(lURIs), pCursor.GetRestAsString());
                                lContext.TraceWarning("response text received: {0}", lResponseText);
                                mEventSynchroniser.FireResponseText(pTextType, lResponseText, lContext);
                                return lResponseText;
                            }

                            break;
                        }

                        lContext.TraceWarning("likely badly formed referral: {0}", pCursor);
                        pCursor.Position = lBookmarkAfterLBRACET;
                    }
                    else
                    {
                        foreach (var lParser in mResponseTextCodeParsers)
                        {
                            if (lParser.Process(pCursor, out var lResponseData, lContext))
                            {
                                if (mMailboxCache != null) mMailboxCache.ProcessTextCode(lResponseData, lContext);
                                if (pTextCodeProcessor != null) pTextCodeProcessor.ProcessTextCode(lResponseData, lContext);

                                lResponseText = new cResponseText(pCursor.GetRestAsString());
                                lContext.TraceVerbose("response text received: {0}", lResponseText);
                                mEventSynchroniser.FireResponseText(pTextType, lResponseText, lContext);
                                return lResponseText;
                            }
                        }

                        if (pTextCodeProcessor != null)
                        {
                            if (pTextCodeProcessor.ProcessTextCode(pCursor, lContext))
                            {
                                lResponseText = new cResponseText(pCursor.GetRestAsString());
                                lContext.TraceVerbose("response text received: {0}", lResponseText);
                                mEventSynchroniser.FireResponseText(pTextType, lResponseText, lContext);
                                return lResponseText;
                            }

                            pCursor.Position = lBookmarkAfterLBRACET;
                        }
                    }

                    if (pCursor.GetToken(cCharset.Atom, null, null, out string lUnknownCodeAtom))
                    {
                        if (pCursor.SkipBytes(cBytesCursor.RBracketSpace))
                        {
                            lResponseText = new cResponseText(lUnknownCodeAtom, null, pCursor.GetRestAsString());
                            lContext.TraceVerbose("response text received: {0}", lResponseText);
                            mEventSynchroniser.FireResponseText(pTextType, lResponseText, lContext);
                            return lResponseText;
                        }

                        if (pCursor.SkipByte(cASCII.SPACE) && pCursor.GetToken(cCharset.TextNotRBRACKET, null, null, out string lUnknownCodeText) && pCursor.SkipBytes(cBytesCursor.RBracketSpace))
                        {
                            lResponseText = new cResponseText(lUnknownCodeAtom, lUnknownCodeText, pCursor.GetRestAsString());
                            lContext.TraceVerbose("response text received: {0}", lResponseText);
                            mEventSynchroniser.FireResponseText(pTextType, lResponseText, lContext);
                            return lResponseText;
                        }
                    }

                    lContext.TraceWarning("likely badly formed response text code: {0}", pCursor);
                    pCursor.Position = lBookmarkBeforeLBRACET;

                    lResponseText = new cResponseText(pCursor.GetRestAsString());
                    lContext.TraceVerbose("response text received: {0}", lResponseText);
                    mEventSynchroniser.FireResponseText(pTextType, lResponseText, lContext);
                    return lResponseText;
                }

                private bool ZGetResponseText(cBytesCursor pCursor, eResponseTextType pTextType, cBytes pResponseTextCodeBracketSpace, eResponseTextCode pResponseTextCode, out cResponseText rResponseText, cTrace.cContext pParentContext)
                {
                    // SUPERVERBOSE
                    var lContext = pParentContext.NewMethod(true, nameof(cResponseTextProcessor), nameof(ZGetResponseText), pResponseTextCodeBracketSpace, pResponseTextCode);
                    if (!pCursor.SkipBytes(pResponseTextCodeBracketSpace)) { rResponseText = null; return false; }
                    rResponseText = new cResponseText(pResponseTextCode, pCursor.GetRestAsString());
                    lContext.TraceWarning("response text received: {0}", rResponseText);
                    mEventSynchroniser.FireResponseText(pTextType, rResponseText, lContext);
                    return true;
                }
            }
        }
    }
}