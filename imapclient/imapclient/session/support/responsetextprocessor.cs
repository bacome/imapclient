﻿using System;
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

                private readonly Action<eResponseTextType, cResponseText, cTrace.cContext> mResponseText;

                public cResponseTextProcessor(Action<eResponseTextType, cResponseText, cTrace.cContext> pResponseText)
                {
                    mResponseText = pResponseText ?? throw new ArgumentNullException(nameof(pResponseText));
                }

                public bool MailboxReferrals { get; set; } = false;

                public cSelectedMailbox SelectedMailbox { get; set; } = null;

                public cResponseText Process(cBytesCursor pCursor, eResponseTextType pResponseTextType, iTextCodeProcessor pTextCodeProcessor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cResponseTextProcessor), nameof(Process), pResponseTextType);

                    cResponseText lResponseText;

                    var lBookmarkBeforeLBRACET = pCursor.Position;

                    if (!pCursor.SkipByte(cASCII.LBRACKET))
                    {
                        lResponseText = new cResponseText(pCursor.GetRestAsString());
                        lContext.TraceVerbose("response text received: {0}", lResponseText);
                        mResponseText(pResponseTextType, lResponseText, lContext);
                        return lResponseText;
                    }

                    var lBookmarkAfterLBRACET = pCursor.Position;

                    if (ZGetResponseText(pCursor, pResponseTextType, kAlertRBracketSpace, eResponseTextCode.alert, out lResponseText, lContext)) return lResponseText;
                    if (ZGetResponseText(pCursor, pResponseTextType, kParseRBracketSpace, eResponseTextCode.parse, out lResponseText, lContext)) return lResponseText;
                    if (ZGetResponseText(pCursor, pResponseTextType, kTryCreateRBracketSpace, eResponseTextCode.trycreate, out lResponseText, lContext)) return lResponseText;
                    if (ZGetResponseText(pCursor, pResponseTextType, kUnavailableRBracketSpace, eResponseTextCode.unavailable, out lResponseText, lContext)) return lResponseText;
                    if (ZGetResponseText(pCursor, pResponseTextType, kAuthenticationFailedRBracketSpace, eResponseTextCode.authenticationfailed, out lResponseText, lContext)) return lResponseText;
                    if (ZGetResponseText(pCursor, pResponseTextType, kAuthorizationFailedRBracketSpace, eResponseTextCode.authorizationfailed, out lResponseText, lContext)) return lResponseText;
                    if (ZGetResponseText(pCursor, pResponseTextType, kExpiredRBracketSpace, eResponseTextCode.expired, out lResponseText, lContext)) return lResponseText;
                    if (ZGetResponseText(pCursor, pResponseTextType, kPrivacyRequiredRBracketSpace, eResponseTextCode.privacyrequired, out lResponseText, lContext)) return lResponseText;
                    if (ZGetResponseText(pCursor, pResponseTextType, kContactAdminRBracketSpace, eResponseTextCode.contactadmin, out lResponseText, lContext)) return lResponseText;
                    if (ZGetResponseText(pCursor, pResponseTextType, kNoPermRBracketSpace, eResponseTextCode.noperm, out lResponseText, lContext)) return lResponseText;
                    if (ZGetResponseText(pCursor, pResponseTextType, kInUseRBracketSpace, eResponseTextCode.inuse, out lResponseText, lContext)) return lResponseText;
                    if (ZGetResponseText(pCursor, pResponseTextType, kExpungeIssuedRBracketSpace, eResponseTextCode.expungeissued, out lResponseText, lContext)) return lResponseText;
                    if (ZGetResponseText(pCursor, pResponseTextType, kCorruptionRBracketSpace, eResponseTextCode.corruption, out lResponseText, lContext)) return lResponseText;
                    if (ZGetResponseText(pCursor, pResponseTextType, kServerBugRBracketSpace, eResponseTextCode.serverbug, out lResponseText, lContext)) return lResponseText;
                    if (ZGetResponseText(pCursor, pResponseTextType, kClientBugRBracketSpace, eResponseTextCode.clientbug, out lResponseText, lContext)) return lResponseText;
                    if (ZGetResponseText(pCursor, pResponseTextType, kCannotRBracketSpace, eResponseTextCode.cannot, out lResponseText, lContext)) return lResponseText;
                    if (ZGetResponseText(pCursor, pResponseTextType, kLimitRBracketSpace, eResponseTextCode.limit, out lResponseText, lContext)) return lResponseText;
                    if (ZGetResponseText(pCursor, pResponseTextType, kOverQuotaRBracketSpace, eResponseTextCode.overquota, out lResponseText, lContext)) return lResponseText;
                    if (ZGetResponseText(pCursor, pResponseTextType, kAlreadyExistsRBracketSpace, eResponseTextCode.alreadyexists, out lResponseText, lContext)) return lResponseText;
                    if (ZGetResponseText(pCursor, pResponseTextType, kNonExistentRBracketSpace, eResponseTextCode.nonexistent, out lResponseText, lContext)) return lResponseText;
                    if (ZGetResponseText(pCursor, pResponseTextType, kUseAttrRBracketSpace, eResponseTextCode.useattr, out lResponseText, lContext)) return lResponseText;
                    if (ZGetResponseText(pCursor, pResponseTextType, kUnknownCTERBracketSpace, eResponseTextCode.unknowncte, out lResponseText, lContext)) return lResponseText;

                    if (pCursor.SkipBytes(kBadCharset))
                    {
                        if (pCursor.SkipBytes(cBytesCursor.RBracketSpace))
                        {
                            lResponseText = new cResponseText(eResponseTextCode.badcharset, pCursor.GetRestAsString());
                            lContext.TraceWarning("response text received: {0}", lResponseText);
                            mResponseText(pResponseTextType, lResponseText, lContext);
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
                                    mResponseText(pResponseTextType, lResponseText, lContext);
                                    return lResponseText;
                                }

                                break;
                            }
                        }

                        lContext.TraceWarning("likely badly formed badcharset: {0}", pCursor);
                        pCursor.Position = lBookmarkAfterLBRACET;
                    }

                    if (MailboxReferrals && pCursor.SkipBytes(kReferralSpace))
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
                                mResponseText(pResponseTextType, lResponseText, lContext);
                                return lResponseText;
                            }

                            break;
                        }

                        lContext.TraceWarning("likely badly formed referral: {0}", pCursor);
                        pCursor.Position = lBookmarkAfterLBRACET;
                    }

                    if (SelectedMailbox != null)
                    {
                        if (SelectedMailbox.ProcessTextCode(pCursor, lContext))
                        {
                            lResponseText = new cResponseText(pCursor.GetRestAsString());
                            lContext.TraceVerbose("response text received: {0}", lResponseText);
                            mResponseText(pResponseTextType, lResponseText, lContext);
                            return lResponseText;
                        }

                        pCursor.Position = lBookmarkAfterLBRACET;
                    }

                    if (pTextCodeProcessor != null)
                    {
                        if (pTextCodeProcessor.ProcessTextCode(pCursor, lContext))
                        {
                            lResponseText = new cResponseText(pCursor.GetRestAsString());
                            lContext.TraceVerbose("response text received: {0}", lResponseText);
                            mResponseText(pResponseTextType, lResponseText, lContext);
                            return lResponseText;
                        }

                        pCursor.Position = lBookmarkAfterLBRACET;
                    }

                    if (pCursor.GetToken(cCharset.Atom, null, null, out string lUnknownCodeAtom))
                    {
                        if (pCursor.SkipBytes(cBytesCursor.RBracketSpace))
                        {
                            lResponseText = new cResponseText(lUnknownCodeAtom, null, pCursor.GetRestAsString());
                            lContext.TraceVerbose("response text received: {0}", lResponseText);
                            mResponseText(pResponseTextType, lResponseText, lContext);
                            return lResponseText;
                        }

                        if (pCursor.SkipByte(cASCII.SPACE) && pCursor.GetToken(cCharset.TextNotRBRACKET, null, null, out string lUnknownCodeText) && pCursor.SkipBytes(cBytesCursor.RBracketSpace))
                        {
                            lResponseText = new cResponseText(lUnknownCodeAtom, lUnknownCodeText, pCursor.GetRestAsString());
                            lContext.TraceVerbose("response text received: {0}", lResponseText);
                            mResponseText(pResponseTextType, lResponseText, lContext);
                            return lResponseText;
                        }
                    }

                    lContext.TraceWarning("likely badly formed response text code: {0}", pCursor);
                    pCursor.Position = lBookmarkBeforeLBRACET;

                    lResponseText = new cResponseText(pCursor.GetRestAsString());
                    lContext.TraceVerbose("response text received: {0}", lResponseText);
                    mResponseText(pResponseTextType, lResponseText, lContext);
                    return lResponseText;
                }

                private bool ZGetResponseText(cBytesCursor pCursor, eResponseTextType pResponseTextType, cBytes pResponseTextCodeBracketSpace, eResponseTextCode pResponseTextCode, out cResponseText rResponseText, cTrace.cContext pParentContext)
                {
                    // SUPERVERBOSE
                    var lContext = pParentContext.NewMethod(true, nameof(cResponseTextProcessor), nameof(ZGetResponseText), pResponseTextCodeBracketSpace, pResponseTextCode);
                    if (!pCursor.SkipBytes(pResponseTextCodeBracketSpace)) { rResponseText = null; return false; }
                    rResponseText = new cResponseText(pResponseTextCode, pCursor.GetRestAsString());
                    lContext.TraceWarning("response text received: {0}", rResponseText);
                    mResponseText(pResponseTextType, rResponseText, lContext);
                    return true;
                }
            }
        }
    }
}