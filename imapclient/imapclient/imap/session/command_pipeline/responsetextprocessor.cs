using System;
using System.Collections.Generic;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

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

                    // rfc 4469
                    private static readonly cBytes kBadURL = new cBytes("BADURL");
                    private static readonly cBytes kTooBig = new cBytes("TOOBIG");


                    private readonly cIMAPCallbackSynchroniser mSynchroniser;
                    private readonly List<iResponseTextCodeParser> mResponseTextCodeParsers = new List<iResponseTextCodeParser>();
                    private cMailboxCache mMailboxCache = null;

                    public cResponseTextProcessor(cIMAPCallbackSynchroniser pSynchroniser)
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

                    public cIMAPResponseText Process(eIMAPResponseTextContext pTextContext, cBytesCursor pCursor, iTextCodeProcessor pTextCodeProcessor, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cResponseTextProcessor), nameof(Process), pTextContext);

                        cIMAPResponseText lResponseText;

                        var lBookmarkBeforeLBRACET = pCursor.Position;

                        if (pCursor.SkipByte(cASCII.LBRACKET))
                        {
                            if (ZTryGetCodeAndArguments(pCursor, out var lCodeBytes, out var lArgumentsBytes))
                            {
                                string lText = pCursor.GetRestAsString();

                                if (lArgumentsBytes == null)
                                {
                                    eIMAPResponseTextCode lCode;
                                    bool lCodeIsAlwaysAnError;

                                    if (lCodeBytes.Equals(kAlert)) { lCode = eIMAPResponseTextCode.alert; lCodeIsAlwaysAnError = false; }
                                    else if (lCodeBytes.Equals(kParse)) { lCode = eIMAPResponseTextCode.parse; lCodeIsAlwaysAnError = false; }
                                    else if (lCodeBytes.Equals(kTryCreate)) { lCode = eIMAPResponseTextCode.trycreate; lCodeIsAlwaysAnError = true; }
                                    else if (lCodeBytes.Equals(kUnavailable)) { lCode = eIMAPResponseTextCode.unavailable; lCodeIsAlwaysAnError = true; }
                                    else if (lCodeBytes.Equals(kAuthenticationFailed)) { lCode = eIMAPResponseTextCode.authenticationfailed; lCodeIsAlwaysAnError = true; }
                                    else if (lCodeBytes.Equals(kAuthorizationFailed)) { lCode = eIMAPResponseTextCode.authorizationfailed; lCodeIsAlwaysAnError = true; }
                                    else if (lCodeBytes.Equals(kExpired)) { lCode = eIMAPResponseTextCode.expired; lCodeIsAlwaysAnError = true; }
                                    else if (lCodeBytes.Equals(kPrivacyRequired)) { lCode = eIMAPResponseTextCode.privacyrequired; lCodeIsAlwaysAnError = true; }
                                    else if (lCodeBytes.Equals(kContactAdmin)) { lCode = eIMAPResponseTextCode.contactadmin; lCodeIsAlwaysAnError = false; }
                                    else if (lCodeBytes.Equals(kNoPerm)) { lCode = eIMAPResponseTextCode.noperm; lCodeIsAlwaysAnError = true; }
                                    else if (lCodeBytes.Equals(kInUse)) { lCode = eIMAPResponseTextCode.inuse; lCodeIsAlwaysAnError = true; }
                                    else if (lCodeBytes.Equals(kExpungeIssued)) { lCode = eIMAPResponseTextCode.expungeissued; lCodeIsAlwaysAnError = false; }
                                    else if (lCodeBytes.Equals(kCorruption)) { lCode = eIMAPResponseTextCode.corruption; lCodeIsAlwaysAnError = false; }
                                    else if (lCodeBytes.Equals(kServerBug)) { lCode = eIMAPResponseTextCode.serverbug; lCodeIsAlwaysAnError = false; }
                                    else if (lCodeBytes.Equals(kClientBug)) { lCode = eIMAPResponseTextCode.clientbug; lCodeIsAlwaysAnError = false; }
                                    else if (lCodeBytes.Equals(kCannot)) { lCode = eIMAPResponseTextCode.cannot; lCodeIsAlwaysAnError = true; }
                                    else if (lCodeBytes.Equals(kLimit)) { lCode = eIMAPResponseTextCode.limit; lCodeIsAlwaysAnError = true; }
                                    else if (lCodeBytes.Equals(kOverQuota)) { lCode = eIMAPResponseTextCode.overquota; lCodeIsAlwaysAnError = false; }
                                    else if (lCodeBytes.Equals(kAlreadyExists)) { lCode = eIMAPResponseTextCode.alreadyexists; lCodeIsAlwaysAnError = true; }
                                    else if (lCodeBytes.Equals(kNonExistent)) { lCode = eIMAPResponseTextCode.nonexistent; lCodeIsAlwaysAnError = true; }
                                    else if (lCodeBytes.Equals(kBadCharset)) { lCode = eIMAPResponseTextCode.badcharset; lCodeIsAlwaysAnError = true; }
                                    else if (lCodeBytes.Equals(kUseAttr)) { lCode = eIMAPResponseTextCode.useattr; lCodeIsAlwaysAnError = true; }
                                    else if (lCodeBytes.Equals(kUnknownCTE)) { lCode = eIMAPResponseTextCode.unknowncte; lCodeIsAlwaysAnError = true; }
                                    else if (lCodeBytes.Equals(kTooBig)) { lCode = eIMAPResponseTextCode.toobig; lCodeIsAlwaysAnError = true; }
                                    else
                                    {
                                        lCode = eIMAPResponseTextCode.other;
                                        lCodeIsAlwaysAnError = false;
                                        ZProcess(pTextContext, lCodeBytes, null, pTextCodeProcessor, lContext);
                                    }

                                    lResponseText = new cIMAPResponseText(lCodeBytes, lCode, lCodeIsAlwaysAnError, lText);
                                }
                                else
                                {
                                    eIMAPResponseTextCode lCode;
                                    bool lCodeIsAlwaysAnError;
                                    cStrings lArguments;

                                    if (lCodeBytes.Equals(kBadCharset))
                                    {
                                        lCode = eIMAPResponseTextCode.badcharset;
                                        lCodeIsAlwaysAnError = true;
                                        lArguments = ZProcessCharsets(lArgumentsBytes);
                                    }
                                    else if (lCodeBytes.Equals(kReferral))
                                    {
                                        lCode = eIMAPResponseTextCode.referral;
                                        lCodeIsAlwaysAnError = false;
                                        lArguments = ZProcessReferrals(lArgumentsBytes, lContext);
                                    }
                                    else if (lCodeBytes.Equals(kBadURL))
                                    {
                                        lCode = eIMAPResponseTextCode.badurl;
                                        lCodeIsAlwaysAnError = true;
                                        lArguments = new cStrings(new string[] { cTools.UTF8BytesToString(lArgumentsBytes) });
                                    }
                                    else
                                    {
                                        lCode = eIMAPResponseTextCode.other;
                                        lCodeIsAlwaysAnError = false;
                                        ZProcess(pTextContext, lCodeBytes, lArgumentsBytes, pTextCodeProcessor, lContext);
                                        lArguments = null;
                                    }

                                    lResponseText = new cIMAPResponseText(lCodeBytes, lArgumentsBytes, lCode, lCodeIsAlwaysAnError, lArguments, lText);
                                }
                            }
                            else
                            {
                                lContext.TraceWarning("likely badly formed response text code");
                                pCursor.Position = lBookmarkBeforeLBRACET;
                                lResponseText = new cIMAPResponseText(pCursor.GetRestAsString());
                            }
                        }
                        else lResponseText = new cIMAPResponseText(pCursor.GetRestAsString());

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

                    private void ZProcess(eIMAPResponseTextContext pTextContext, cByteList pCode, cByteList pArguments, iTextCodeProcessor pTextCodeProcessor, cTrace.cContext pParentContext)
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