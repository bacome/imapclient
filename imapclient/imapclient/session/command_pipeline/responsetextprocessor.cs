using System;
using System.Collections.Generic;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private partial class cCommandPipeline
            {
                private class cResponseTextProcessor
                {
                    // rfc 3501
                    private static readonly cBytes kAlert = new cBytes("ALERT");
                    private static readonly cBytes kParse = new cBytes("PARSE");
                    private static readonly cBytes kTryCreate = new cBytes("TRYCREATE");

                    // rfc 5530
                    private static readonly cBytes kUnavailable = new cBytes("UNAVAILABLE");
                    private static readonly cBytes kAuthenticationFailed = new cBytes("AUTHENTICATIONFAILED");
                    private static readonly cBytes kAuthorizationFailed = new cBytes("AUTHORIZATIONFAILED");
                    private static readonly cBytes kExpired = new cBytes("EXPIRED");
                    private static readonly cBytes kPrivacyRequired = new cBytes("PRIVACYREQUIRED");
                    private static readonly cBytes kContactAdmin = new cBytes("CONTACTADMIN");
                    private static readonly cBytes kNoPerm = new cBytes("NOPERM");
                    private static readonly cBytes kInUse = new cBytes("INUSE");
                    private static readonly cBytes kExpungeIssued = new cBytes("EXPUNGEISSUED");
                    private static readonly cBytes kCorruption = new cBytes("CORRUPTION");
                    private static readonly cBytes kServerBug = new cBytes("SERVERBUG");
                    private static readonly cBytes kClientBug = new cBytes("CLIENTBUG");
                    private static readonly cBytes kCannot = new cBytes("CANNOT");
                    private static readonly cBytes kLimit = new cBytes("LIMIT");
                    private static readonly cBytes kOverQuota = new cBytes("OVERQUOTA");
                    private static readonly cBytes kAlreadyExists = new cBytes("ALREADYEXISTS");
                    private static readonly cBytes kNonExistent = new cBytes("NONEXISTENT");

                    // rfc 3501
                    private static readonly cBytes kBadCharset = new cBytes("BADCHARSET");

                    // rfc 2193
                    private static readonly cBytes kReferral = new cBytes("REFERRAL");

                    // rfc 6154
                    private static readonly cBytes kUseAttr = new cBytes("USEATTR");

                    // rfc 3516
                    private static readonly cBytes kUnknownCTE = new cBytes("UNKNOWN-CTE");

                    private readonly cCallbackSynchroniser mSynchroniser;
                    private readonly List<iResponseTextCodeParser> mResponseTextCodeParsers = new List<iResponseTextCodeParser>();
                    private cMailboxCache mMailboxCache = null;

                    public cResponseTextProcessor(cCallbackSynchroniser pSynchroniser)
                    {
                        mSynchroniser = pSynchroniser ?? throw new ArgumentNullException(nameof(pSynchroniser));
                    }

                    public void Install(iResponseTextCodeParser pResponseTextCodeParser) => mResponseTextCodeParsers.Add(pResponseTextCodeParser);

                    public void Enable(cMailboxCache pMailboxCache, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cResponseTextProcessor), nameof(Enable));
                        if (mMailboxCache != null) throw new InvalidOperationException(kInvalidOperationExceptionMessage.AlreadyEnabled);
                        mMailboxCache = pMailboxCache ?? throw new ArgumentNullException(nameof(pMailboxCache));
                    }

                    public cResponseText Process(eResponseTextContext pTextContext, cBytesCursor pCursor, iTextCodeProcessor pTextCodeProcessor, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cResponseTextProcessor), nameof(Process), pTextContext);

                        cResponseText lResponseText;

                        var lBookmarkBeforeLBRACET = pCursor.Position;

                        if (pCursor.SkipByte(cASCII.LBRACKET))
                        {
                            if (ZTryGetCodeAndArguments(pCursor, out var lCodeBytes, out var lArgumentsBytes))
                            {
                                string lText = pCursor.GetRestAsString();

                                if (lArgumentsBytes == null)
                                {
                                    eResponseTextCode lCode;

                                    if (lCodeBytes.Equals(kAlert)) lCode = eResponseTextCode.alert;
                                    else if (lCodeBytes.Equals(kParse)) lCode = eResponseTextCode.parse;
                                    else if (lCodeBytes.Equals(kTryCreate)) lCode = eResponseTextCode.trycreate;
                                    else if (lCodeBytes.Equals(kUnavailable)) lCode = eResponseTextCode.unavailable;
                                    else if (lCodeBytes.Equals(kAuthenticationFailed)) lCode = eResponseTextCode.authenticationfailed;
                                    else if (lCodeBytes.Equals(kAuthorizationFailed)) lCode = eResponseTextCode.authorizationfailed;
                                    else if (lCodeBytes.Equals(kExpired)) lCode = eResponseTextCode.expired;
                                    else if (lCodeBytes.Equals(kPrivacyRequired)) lCode = eResponseTextCode.privacyrequired;
                                    else if (lCodeBytes.Equals(kContactAdmin)) lCode = eResponseTextCode.contactadmin;
                                    else if (lCodeBytes.Equals(kNoPerm)) lCode = eResponseTextCode.noperm;
                                    else if (lCodeBytes.Equals(kInUse)) lCode = eResponseTextCode.inuse;
                                    else if (lCodeBytes.Equals(kExpungeIssued)) lCode = eResponseTextCode.expungeissued;
                                    else if (lCodeBytes.Equals(kCorruption)) lCode = eResponseTextCode.corruption;
                                    else if (lCodeBytes.Equals(kServerBug)) lCode = eResponseTextCode.serverbug;
                                    else if (lCodeBytes.Equals(kClientBug)) lCode = eResponseTextCode.clientbug;
                                    else if (lCodeBytes.Equals(kCannot)) lCode = eResponseTextCode.cannot;
                                    else if (lCodeBytes.Equals(kLimit)) lCode = eResponseTextCode.limit;
                                    else if (lCodeBytes.Equals(kOverQuota)) lCode = eResponseTextCode.overquota;
                                    else if (lCodeBytes.Equals(kAlreadyExists)) lCode = eResponseTextCode.alreadyexists;
                                    else if (lCodeBytes.Equals(kNonExistent)) lCode = eResponseTextCode.nonexistent;
                                    else if (lCodeBytes.Equals(kBadCharset)) lCode = eResponseTextCode.badcharset;
                                    else if (lCodeBytes.Equals(kUseAttr)) lCode = eResponseTextCode.useattr;
                                    else if (lCodeBytes.Equals(kUnknownCTE)) lCode = eResponseTextCode.unknowncte;
                                    else
                                    {
                                        lCode = eResponseTextCode.other;
                                        ZProcess(pTextContext, lCodeBytes, null, pTextCodeProcessor, lContext);
                                    }

                                    lResponseText = new cResponseText(lCodeBytes, lCode, lText);
                                }
                                else
                                {
                                    eResponseTextCode lCode;
                                    cStrings lArguments;

                                    if (lCodeBytes.Equals(kBadCharset))
                                    {
                                        lCode = eResponseTextCode.badcharset;
                                        lArguments = ZProcessCharsets(lArgumentsBytes);
                                    }
                                    else if (lCodeBytes.Equals(kReferral))
                                    {
                                        lCode = eResponseTextCode.referral;
                                        lArguments = ZProcessReferrals(lArgumentsBytes, lContext);
                                    }
                                    else
                                    {
                                        lCode = eResponseTextCode.other;
                                        lArguments = null;
                                        ZProcess(pTextContext, lCodeBytes, lArgumentsBytes, pTextCodeProcessor, lContext);
                                    }

                                    lResponseText = new cResponseText(lCodeBytes, lArgumentsBytes, lCode, lArguments, lText);
                                }
                            }
                            else
                            {
                                lContext.TraceWarning("likely badly formed response text code");
                                pCursor.Position = lBookmarkBeforeLBRACET;
                                lResponseText = new cResponseText(pCursor.GetRestAsString());
                            }
                        }
                        else lResponseText = new cResponseText(pCursor.GetRestAsString());

                        lContext.TraceVerbose("response text received: {0}", lResponseText);
                        mSynchroniser.InvokeResponseText(pTextContext, lResponseText, lContext);
                        return lResponseText;
                    }

                    private bool ZTryGetCodeAndArguments(cBytesCursor pCursor, out cByteList rCode, out cByteList rArguments)
                    {
                        if (!pCursor.GetToken(cCharset.Atom, null, null, out rCode)) { rArguments = null; return false; }
                        if (pCursor.SkipBytes(cBytesCursor.RBracketSpace)) { rArguments = null; return true; }
                        if (!pCursor.SkipByte(cASCII.SPACE)) { rArguments = null; return false; }
                        if (!pCursor.GetToken(cCharset.TextNotRBRACKET, null, null, out rArguments)) return false;
                        if (!pCursor.SkipBytes(cBytesCursor.RBracketSpace)) return false;
                        return true;
                    }

                    private cStrings ZProcessCharsets(cByteList pArguments)
                    {
                        cBytesCursor lCursor = new cBytesCursor(pArguments);

                        if (!lCursor.SkipByte(cASCII.LPAREN)) return null;

                        List<string> lCharsets = new List<string>();

                        while (true)
                        {
                            if (!lCursor.GetAString(out string lCharset)) break;
                            lCharsets.Add(lCharset);
                            if (!lCursor.SkipByte(cASCII.SPACE)) break;
                        }

                        if (!lCursor.SkipByte(cASCII.RPAREN)) return null;
                        if (!lCursor.Position.AtEnd) return null;

                        return new cStrings(lCharsets);
                    }

                    private cStrings ZProcessReferrals(cByteList pArguments, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cResponseTextProcessor), nameof(ZProcessReferrals), pArguments);

                        cBytesCursor lCursor = new cBytesCursor(pArguments);

                        List<string> lURIs = new List<string>();

                        while (true)
                        {
                            if (!lCursor.GetURI(out _, out var lURI, lContext)) break;
                            lURIs.Add(lURI);
                            if (!lCursor.SkipByte(cASCII.SPACE)) break;
                        }

                        if (!lCursor.Position.AtEnd) return null;

                        return new cStrings(lURIs);
                    }

                    private void ZProcess(eResponseTextContext pTextContext, cByteList pCode, cByteList pArguments, iTextCodeProcessor pTextCodeProcessor, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cResponseTextProcessor), nameof(ZProcess), pTextContext, pCode, pArguments);

                        foreach (var lParser in mResponseTextCodeParsers)
                        {
                            if (lParser.Process(pCode, pArguments, out var lData, lContext))
                            {
                                if (mMailboxCache != null) mMailboxCache.ProcessTextCode(pTextContext, lData, lContext);
                                if (pTextCodeProcessor != null) pTextCodeProcessor.ProcessTextCode(pTextContext, lData, lContext);
                                return;
                            }
                        }

                        if (pTextCodeProcessor != null) pTextCodeProcessor.ProcessTextCode(pTextContext, pCode, pArguments, lContext);
                    }
                }
            }
        }
    }
}