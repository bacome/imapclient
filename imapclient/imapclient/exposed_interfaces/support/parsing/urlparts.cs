using System;
using System.Collections.Generic;
using System.Diagnostics;
using work.bacome.trace;

namespace work.bacome.imapclient.support
{
    public class cURLParts
    {
        // IMAP URL (rfc 5092, 5593)
        //  Note: TODO  punycode displayhost
            
        private static readonly cBytes kIMAPColonSlashSlash = new cBytes("imap://");
        private static readonly cBytes kSlashSlash = new cBytes("//");
        private static readonly cBytes kSemicolonAuthEquals = new cBytes(";AUTH=");
        private static readonly cBytes kSemicolonUIDValidityEquals = new cBytes(";UIDVALIDITY=");
        private static readonly cBytes kSemicolonUIDEquals = new cBytes(";UID=");
        private static readonly cBytes kSlashSemicolonSectionEquals = new cBytes("/;SECTION=");
        private static readonly cBytes kSlashSemicolonPartialEquals = new cBytes("/;PARTIAL=");
        private static readonly cBytes kSemicolonExpireEquals = new cBytes(";EXPIRE=");
        private static readonly cBytes kSemicolonURLAuthEquals = new cBytes(";URLAUTH=");

        [Flags]
        private enum fParts
        {
            scheme = 1 << 0,
            userid = 1 << 1,
            mechanismname = 1 << 2,
            host = 1 << 3,
            port = 1 << 4,
            mailboxname = 1 << 5,
            uidvalidity = 1 << 6,
            search = 1 << 7,
            uid = 1 << 8,
            section = 1 << 9,
            partial = 1 << 10,
            partiallength = 1 << 11,
            expire = 1 << 12,
            urlauth = 1 << 13,
            accessuserid = 1 << 14,
            token = 1 << 15
        }

        private fParts mParts = 0;

        private string _UserId = null;
        private string _MechanismName = null;
        private string _Host = null;
        private int _Port = 143;
        private string _MailboxPath = null;
        private uint? _UIDValidity = null;
        private string _Search = null;
        private uint? _UID = null;
        private string _Section = null;
        private uint? _PartialOffset = null;
        private uint? _PartialLength = null;
        private DateTime? _Expire = null;
        private string _Application = null;
        private string _AccessUserId = null;
        private string _Token = null;

        private cURLParts() { }

        public string UserId
        {
            get => _UserId;

            private set
            {
                _UserId = value;
                mParts |= fParts.userid;
            }
        }

        public string MechanismName
        {
            get => _MechanismName;

            private set
            {
                _MechanismName = value;
                mParts |= fParts.mechanismname;
            }
        }

        public string Host
        {
            get => _Host;

            private set
            {
                _Host = value;
                mParts |= fParts.host;
            }
        }

        public int Port
        {
            get => _Port;

            private set
            {
                _Port = value;
                mParts |= fParts.port;
            }
        }

        public string MailboxPath
        {
            get => _MailboxPath;

            private set
            {
                _MailboxPath = value;
                mParts |= fParts.mailboxname;
            }
        }

        public uint? UIDValidity
        {
            get => _UIDValidity;

            private set
            {
                _UIDValidity = value;
                mParts |= fParts.uidvalidity;
            }
        }

        public string Search
        {
            get => _Search;

            private set
            {
                _Search = value;
                mParts |= fParts.search;
            }
        }

        public uint? UID
        {
            get => _UID;

            private set
            {
                _UID = value;
                mParts |= fParts.uid;
            }
        }

        public string Section
        {
            get => _Section;

            private set
            {
                _Section = value;
                mParts |= fParts.section;
            }
        }

        public uint? PartialOffset
        {
            get => _PartialOffset;

            private set
            {
                _PartialOffset = value;
                mParts |= fParts.partial;
            }
        }

        public uint? PartialLength
        {
            get => _PartialLength;

            private set
            {
                _PartialLength = value;
                mParts |= fParts.partiallength;
            }
        }

        public DateTime? Expire
        {
            get => _Expire;

            private set
            {
                _Expire = value;
                mParts |= fParts.expire;
            }
        }

        public string Application
        {
            get => _Application;

            private set
            {
                _Application = value;
                mParts |= fParts.urlauth;
            }
        }

        public string AccessUserId
        {
            get => _AccessUserId;

            private set
            {
                _AccessUserId = value;
                mParts |= fParts.accessuserid;
            }
        }

        public string TokenMechanism { get; private set; } = null;

        public string Token
        {
            get => _Token;

            private set
            {
                _Token = value;
                mParts |= fParts.token;
            }
        }

        public bool MustUseAnonymous => ((mParts & fParts.host) != 0) && (mParts & (fParts.userid | fParts.mechanismname)) == 0;

        public bool IsHomeServerReferral =>
            ZHasParts(
                fParts.scheme | fParts.host,
                fParts.userid | fParts.mechanismname | fParts.port
                ) &&
            !MustUseAnonymous;

        public bool IsMailboxReferral =>
            ZHasParts(
                fParts.scheme | fParts.host | fParts.mailboxname,
                fParts.userid | fParts.mechanismname | fParts.port | fParts.uidvalidity
                );

        public bool IsMailboxSearch =>
            ZHasParts(
                fParts.scheme | fParts.host | fParts.mailboxname | fParts.search,
                fParts.userid | fParts.mechanismname | fParts.port | fParts.uidvalidity
                );

        public bool IsMessageReference =>
            ZHasParts(
                fParts.scheme | fParts.host | fParts.mailboxname | fParts.uid,
                fParts.userid | fParts.mechanismname | fParts.port | fParts.uidvalidity | fParts.section | fParts.partial | fParts.partiallength
                );

        public bool IsPartial => !((mParts & (fParts.section | fParts.partial)) != 0);

        public bool IsAuthorisable =>
            ZHasParts(
                fParts.scheme | fParts.userid | fParts.host | fParts.mailboxname | fParts.uid | fParts.urlauth,
                fParts.mechanismname | fParts.port | fParts.uidvalidity | fParts.section | fParts.partial | fParts.partiallength | fParts.expire | fParts.accessuserid
                );

        public bool IsAuthorised =>
            ZHasParts(
                fParts.scheme | fParts.userid | fParts.host | fParts.mailboxname | fParts.uid | fParts.urlauth | fParts.token,
                fParts.mechanismname | fParts.port | fParts.uidvalidity | fParts.section | fParts.partial | fParts.partiallength | fParts.expire | fParts.accessuserid
                );

        private bool ZHasParts(fParts pMustHave, fParts pMayHave = 0)
        {
            if ((mParts & pMustHave) != pMustHave) return false;
            if ((mParts & ~(pMustHave | pMayHave)) != 0) return false;
            return true;
        }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cURLParts));

            lBuilder.Append(mParts);
            if (UserId != null) lBuilder.Append(nameof(UserId), UserId);
            if (MechanismName != null) lBuilder.Append(nameof(MechanismName), MechanismName);
            if (Host != null) lBuilder.Append(nameof(Host), Host);
            lBuilder.Append(nameof(Port), Port);
            if (MailboxPath != null) lBuilder.Append(nameof(MailboxPath), MailboxPath);
            if (UIDValidity != null) lBuilder.Append(nameof(UIDValidity), UIDValidity);
            if (Search != null) lBuilder.Append(nameof(Search), Search);
            if (UID != null) lBuilder.Append(nameof(UID), UID);
            if (Section != null) lBuilder.Append(nameof(Section), Section);
            if (PartialOffset != null) lBuilder.Append(nameof(PartialOffset), PartialOffset);
            if (PartialLength != null) lBuilder.Append(nameof(PartialLength), PartialLength);
            if (Expire != null) lBuilder.Append(nameof(Expire), Expire);
            if (Application != null) lBuilder.Append(nameof(Application), Application);
            if (AccessUserId != null) lBuilder.Append(nameof(AccessUserId), AccessUserId);
            if (TokenMechanism != null) lBuilder.Append(nameof(TokenMechanism), TokenMechanism);
            if (Token != null) lBuilder.Append(nameof(Token), Token);

            return lBuilder.ToString();
        }

        public static bool Process(cBytesCursor pCursor, out cURLParts rParts, cTrace.cContext pParentContext)
        {
            //  NOTE: this routine does not return the cursor to its original position if it fails

            var lContext = pParentContext.NewMethod(nameof(cURLParts), nameof(Process));

            cURLParts lParts = new cURLParts();

            var lStartBookmark = pCursor.Position;

            cBytesCursor.sPosition lBookmark1;
            cBytesCursor.sPosition lBookmark2;

            bool lExpectServer;

            if (pCursor.SkipBytes(kIMAPColonSlashSlash))
            {
                lContext.TraceVerbose("absolute URL");
                lParts.mParts |= fParts.scheme;
                lExpectServer = true;
            }
            else if (pCursor.SkipBytes(kSlashSlash))
            {
                lContext.TraceVerbose("network-path reference");
                lExpectServer = true;
            }
            else if (pCursor.SkipByte(cASCII.SLASH))
            {
                lContext.TraceVerbose("absolute-path reference");
                lExpectServer = false;
            }
            else
            {
                lContext.TraceVerbose("relative-path references are not allowed by rfc5092 section 7.2");
                rParts = null;
                return false;
            }

            if (lExpectServer)
            {
                // userinfo

                bool lExpectAt = false;
                bool lMechanismAny = false;

                lBookmark1 = pCursor.Position;
                if (pCursor.GetToken(cCharset.AChar, cASCII.PERCENT, null, out string lUserId)) lExpectAt = true;

                lBookmark2 = pCursor.Position;
                string lMechanismName;

                if (pCursor.SkipBytes(kSemicolonAuthEquals))
                {
                    if (pCursor.SkipByte(cASCII.ASTERISK))
                    {
                        lMechanismAny = true;
                        lMechanismName = null;
                        lExpectAt = true;
                    }
                    else if (pCursor.GetToken(cCharset.AChar, cASCII.PERCENT, null, out lMechanismName)) lExpectAt = true;
                    else
                    {
                        pCursor.Position = lBookmark2;
                        lContext.TraceWarning("likely malformed auth= section");
                    }
                }
                else lMechanismName = null;

                if (lExpectAt)
                {
                    if (pCursor.SkipByte(cASCII.AT))
                    {
                        if (lUserId != null)
                        {
                            lParts.UserId = lUserId;
                            lContext.TraceVerbose("UserId: {0}", lUserId);
                        }

                        if (lMechanismName != null || lMechanismAny)
                        {
                            lParts.MechanismName = lMechanismName;
                            lContext.TraceVerbose("Mechanism: {0}", lMechanismName);
                        }
                    }
                    else
                    {
                        pCursor.Position = lBookmark1;
                        lContext.TraceVerbose("no userinfo (1)");
                    }
                }
                else lContext.TraceVerbose("no userinfo (2)");

                // host
                //  (the syntax allows zero length in the reg name form, but not in the [] form)

                lBookmark1 = pCursor.Position;

                cByteList lBytes;

                // note that the rules here for extracting the host name are substantially more liberal than the rfcs (3986, 6874)
                //
                if (pCursor.SkipByte(cASCII.LBRACKET) && pCursor.GetToken(cCharset.IPLiteral, cASCII.PERCENT, null, out lBytes) && pCursor.SkipByte(cASCII.RBRACKET))
                {
                    lParts.Host = cTools.UTF8BytesToString(lBytes);
                    lContext.TraceVerbose("ipv6 (or greater) host: {0}", lParts.Host);
                }
                else
                {
                    pCursor.Position = lBookmark1;

                    if (pCursor.GetToken(cCharset.RegName, cASCII.PERCENT, null, out string lHost))
                    {
                        lParts.Host = lHost;
                        lContext.TraceVerbose("host: {0}", lHost);
                    }
                    else
                    {
                        lContext.TraceVerbose("blank host");
                        rParts = null;
                        return false;
                    }
                }

                // port
                //  (optional) - the colon is allowed to be there even if the port is not
                //
                if (pCursor.SkipByte(cASCII.COLON))
                {
                    if (pCursor.GetToken(cCharset.Digit, null, null, out lBytes))
                    {
                        int lPort = 0;

                        checked
                        {
                            try { foreach (byte lByte in lBytes) lPort = lPort * 10 + lByte - cASCII.ZERO; }
                            catch
                            {
                                lPort = -1;
                                lContext.TraceVerbose("port number outside the normal range specified (overflow)");
                            }

                            if (lPort < 1 || lPort > 65535)
                            {
                                lContext.TraceVerbose("port number outside the normal range specified: {0}", lPort);
                                rParts = null;
                                return false;
                            }

                            lParts.Port = lPort;
                            lContext.TraceVerbose("port: {0}", lPort);
                        }
                    }
                    else lContext.TraceVerbose("no port specified (but the ':' was there)");
                }

                if (!pCursor.SkipByte(cASCII.SLASH))
                {
                    lContext.TraceVerbose("server only URL (1)");
                    rParts = lParts;
                    return true;
                }
            }

            // mailbox

            lBookmark1 = pCursor.Position; // may have to return to here if the mailbox reads in as a single '/' but it is the '/' from '/;UID='

            if (!pCursor.GetToken(cCharset.BChar, cASCII.PERCENT, null, out cByteList lMailboxPathBytes))
            {
                // the mailbox needs to be present to consider anything further
                //
                if (!lExpectServer) lContext.TraceVerbose("URL was '/' (1)");
                else lContext.TraceVerbose("server only URL (2)");
                rParts = lParts;
                return true;
            }

            bool lLastByteWasASlash = (lMailboxPathBytes[lMailboxPathBytes.Count - 1] == cASCII.SLASH);

            if (lLastByteWasASlash)
            {
                if (lMailboxPathBytes.Count == 1) lContext.TraceVerbose("mailbox is just a slash - may not be a mailbox"); // this is the '/;UID=' case
                else lContext.TraceVerbose("mailbox ends with a slash - may be trimmed");
            }

            // uidvalidity

            lBookmark2 = pCursor.Position;

            if (pCursor.SkipBytes(kSemicolonUIDValidityEquals))
            {
                if (pCursor.GetNZNumber(out _, out uint lUIDValidity))
                {
                    lParts.MailboxPath = ZMailboxPath(lMailboxPathBytes, lContext);
                    lParts.UIDValidity = lUIDValidity;

                    if (lLastByteWasASlash)
                    {
                        lLastByteWasASlash = false;
                        lContext.TraceVerbose("mailbox ends with a slash!"); // allowed by the grammar, unlikely to be valid
                    }

                    lContext.TraceVerbose("mailbox: {0}, uidvalidity: {1}", lParts.MailboxPath, lUIDValidity);
                }
                else
                {
                    pCursor.Position = lBookmark2;
                    lContext.TraceWarning("likely malformed uidvalidity= section");
                }
            }

            // search

            lBookmark2 = pCursor.Position;

            if (pCursor.SkipByte(cASCII.QUESTIONMARK))
            {
                if (pCursor.GetToken(cCharset.BChar, cASCII.PERCENT, null, out string lSearch))
                {
                    lParts.MailboxPath = ZMailboxPath(lMailboxPathBytes, lContext);
                    lParts.Search = lSearch;

                    if (lLastByteWasASlash) lContext.TraceVerbose("mailbox ends with a slash!");

                    lContext.TraceVerbose("mailbox: {0}, search: {1}", lParts.MailboxPath, lSearch);

                    rParts = lParts;
                    return true;
                }

                pCursor.Position = lBookmark2;
                lContext.TraceWarning("likely malformed search section");
            }

            // UID

            uint lUID = 0; // = 0 to shut the compiler up
            bool lGotUID = false;
            lBookmark2 = pCursor.Position;

            if (!lLastByteWasASlash)
                if (!pCursor.SkipByte(cASCII.SLASH))
                {
                    // the UID needs to be present to consider anything further, so that's it
                    lParts.MailboxPath = ZMailboxPath(lMailboxPathBytes, lContext);
                    lContext.TraceVerbose("mailbox url (1): {0}", lParts.MailboxPath);
                    rParts = lParts;
                    return true;
                }

            if (pCursor.SkipBytes(kSemicolonUIDEquals))
            {
                if (pCursor.GetNZNumber(out _, out lUID)) lGotUID = true;
                else lContext.TraceWarning("likely malformed uid= section");
            }

            if (lGotUID)
            {
                string lMailboxPath;

                if (lLastByteWasASlash)
                {
                    if (lMailboxPathBytes.Count == 1)
                    {
                        // there was no mailbox so we shouldn't be here
                        pCursor.Position = lBookmark1;
                        if (!lExpectServer) lContext.TraceVerbose("URL was '/' (2)");
                        else lContext.TraceVerbose("server only URL (3)");
                        rParts = lParts;
                        return true;
                    }

                    byte[] lBytes = new byte[lMailboxPathBytes.Count - 1];
                    for (int i = 0; i < lMailboxPathBytes.Count - 1; i++) lBytes[i] = lMailboxPathBytes[i];
                    lMailboxPath = ZMailboxPath(lBytes, lContext);

                    lContext.TraceVerbose("trimmed off trailing '/' of the mailbox");
                }
                else lMailboxPath = ZMailboxPath(lMailboxPathBytes, lContext);

                lParts.MailboxPath = lMailboxPath;
                lParts.UID = lUID;

                lContext.TraceVerbose("mailbox: {0}, uid: {1}", lMailboxPath, lUID);
            }
            else
            {
                // the UID needs to be present to consider anything further, so that's it
                pCursor.Position = lBookmark2;
                lParts.MailboxPath = ZMailboxPath(lMailboxPathBytes, lContext);
                lContext.TraceVerbose("mailbox url (2): {0}", lParts.MailboxPath);
                if (lLastByteWasASlash) lContext.TraceVerbose("mailbox ends with a slash!");
                rParts = lParts;
                return true;
            }

            // section

            lBookmark1 = pCursor.Position;

            if (pCursor.SkipBytes(kSlashSemicolonSectionEquals))
            {
                if (pCursor.GetToken(cCharset.BChar, cASCII.PERCENT, null, out string lSection))
                {
                    lParts.Section = lSection;
                    lContext.TraceVerbose("section: {0}", lSection);
                }
                else
                {
                    pCursor.Position = lBookmark1;
                    lContext.TraceWarning("likely malformed section= section");
                }
            }

            // partial

            lBookmark1 = pCursor.Position;

            if (pCursor.SkipBytes(kSlashSemicolonPartialEquals))
            {
                if (pCursor.GetNumber(out _, out uint lPartialOffset))
                {
                    lParts.PartialOffset = lPartialOffset;
                    lContext.TraceVerbose("fragment offset: {0}", lPartialOffset);

                    // partial length (according to the grammar it is optional, although what that means is not explained)

                    //lBookmark1 = pCursor.Position;

                    if (pCursor.SkipByte(cASCII.DOT))
                    {
                        if (pCursor.GetNZNumber(out _, out uint lPartialLength))
                        {
                            lParts.PartialLength = lPartialLength;
                            lContext.TraceVerbose("fragment length: {0}", lPartialLength);
                        }
                        else
                        {
                            lContext.TraceWarning("likely malformed partial= length section");
                            rParts = null;
                            return false;

                            //pCursor.Position = lBookmark1;
                            //lContext.TraceVerbose("likely malformed partial= length section");
                        }
                    }
                    else
                    {
                        lContext.TraceVerbose("likely unusable partial= section - no length");
                        rParts = null;
                        return false;
                    }
                }
                else
                {
                    pCursor.Position = lBookmark1;
                    lContext.TraceWarning("likely malformed partial= section");
                }
            }

            // expire - only valid if ;URLAUTH= follows

            lBookmark1 = pCursor.Position;
            DateTime? lExpire = null;

            if (pCursor.SkipBytes(kSemicolonExpireEquals))
            {
                if (pCursor.GetTimeStamp(out var lTimeStamp)) lExpire = lTimeStamp;
                else
                {
                    pCursor.Position = lBookmark1;
                    lContext.TraceWarning("likely malformed expire= section");
                }
            }

            // urlauth

            if (!pCursor.SkipBytes(kSemicolonURLAuthEquals))
            {
                pCursor.Position = lBookmark1;
                if (lExpire != null) lContext.TraceWarning("likely malformed urlauth section (expire but no urlauth)");
                rParts = lParts;
                return true;
            }

            // access

            if (!pCursor.GetToken(cCharset.AlphaNumeric, null, null, out string lApplication))
            {
                pCursor.Position = lBookmark1;
                lContext.TraceWarning("likely malformed urlauth section (no application)");
                rParts = lParts;
                return true;
            }

            lParts.Application = lApplication;
            lContext.TraceVerbose("application: {0}", lApplication);

            // expire can now be set
            if (lExpire != null)
            {
                lParts.Expire = lExpire;
                lContext.TraceVerbose("expire: {0}", lExpire);
            }

            // urlauth user

            lBookmark1 = pCursor.Position;

            if (pCursor.SkipByte(cASCII.PLUS))
            {
                if (pCursor.GetToken(cCharset.AChar, cASCII.PERCENT, null, out string lAccessUserId))
                {
                    lParts.AccessUserId = lAccessUserId;
                    lContext.TraceVerbose("accessuserid: {0}", lAccessUserId);
                }
                else
                {
                    lContext.TraceWarning("likely malformed urlauth section (+ but no userid)");
                    pCursor.Position = lBookmark1;
                }
            }

            // now the token

            lBookmark1 = pCursor.Position;

            if (pCursor.SkipByte(cASCII.COLON) && pCursor.GetToken(cCharset.UAuthMechanism, null, null, out cByteList lTokenMechanism) && pCursor.SkipByte(cASCII.COLON) && pCursor.GetToken(cCharset.Hexidecimal, null, null, out cByteList lToken, 32, 32))
            {
                lParts.TokenMechanism = cTools.UTF8BytesToString(lTokenMechanism);
                lParts.Token = cTools.UTF8BytesToString(lToken);
                lContext.TraceVerbose("Token mechanism: {0}, token: {1}", lParts.TokenMechanism, lParts.Token);
            }
            else pCursor.Position = lBookmark1;

            // done

            rParts = lParts;
            return true;
        }

        private static string ZMailboxPath(IList<byte> pMailboxPath, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cURLParts), nameof(ZMailboxPath));

            // try a utf-7 decode, if it fails treat the bytes as utf-8
            //  there is a problem if the separator character is a not a valid utf-7 unencoded byte - the separator character isn't allowed to be utf-7 encoded
            //   however, without connecting to the server I can't tell what the separator character is
            //   in this (very unlikely) case the utf-7 decode will fail and we will output the utf-7 encoded string as the mailbox name

            if (cModifiedUTF7.TryDecode(pMailboxPath, out string lMailboxPath, out _)) return lMailboxPath;
            return cTools.UTF8BytesToString(pMailboxPath);
        }

        [Conditional("DEBUG")]
        public static void _Tests(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cURLParts), nameof(_Tests));

            cURLParts lParts;
                    
            // from rfc 2221

            if (!LTryParse("IMAP://MIKE@SERVER2/", out lParts) || !lParts.IsHomeServerReferral) throw new cTestsException("2221.1");
            if (!LTryParse("IMAP://user;AUTH=GSSAPI@SERVER2/", out lParts) || !lParts.IsHomeServerReferral) throw new cTestsException("2221.2");
            if (!LTryParse("IMAP://user;AUTH=*@SERVER2/", out lParts) || !lParts.IsHomeServerReferral) throw new cTestsException("2221.3");

            // from rfc 2193

            if (!LTryParse("IMAP://user;AUTH=*@SERVER2/SHARED/FOO", out lParts) || !lParts.IsMailboxReferral || lParts.MailboxPath != "SHARED/FOO") throw new cTestsException("2193.1");
            if (LTryParse("IMAP://user;AUTH=*@SERVER2/REMOTE IMAP://user;AUTH=*@SERVER3/REMOTE", out lParts)) throw new cTestsException("2193.2");

            // from rfc 5092

            if (!LTryParse("imap://minbari.example.org/gray-council;UIDVALIDITY=385759045/;UID=20/;PARTIAL=0.1024", out lParts)) throw new cTestsException("5092.1.1");

            if (lParts.IsHomeServerReferral) throw new cTestsException("5092.1.2");
            if (lParts.IsMailboxReferral) throw new cTestsException("5092.1.3");

            if (!lParts.ZHasParts(fParts.scheme | fParts.host | fParts.mailboxname | fParts.uidvalidity | fParts.uid | fParts.partial | fParts.partiallength) ||
                !lParts.MustUseAnonymous ||
                lParts.Host != "minbari.example.org" ||
                lParts.MailboxPath != "gray-council" ||
                lParts.UIDValidity.Value != 385759045 ||
                lParts.UID != 20 ||
                lParts.PartialOffset != 0 ||
                lParts.PartialLength != 1024
                )
                throw new cTestsException("5092.1.4");

            if (!LTryParse("imap://psicorp.example.org/~peter/%E6%97%A5%E6%9C%AC%E8%AA%9E/%E5%8F%B0%E5%8C%97", out lParts)) throw new cTestsException("5092.2");

            if (lParts.IsHomeServerReferral) throw new cTestsException("5092.2.1");
            if (!lParts.IsMailboxReferral) throw new cTestsException("5092.2.2");

            if (!lParts.ZHasParts(fParts.scheme | fParts.host | fParts.mailboxname) ||
                !lParts.MustUseAnonymous ||
                lParts.Host != "psicorp.example.org" ||
                lParts.MailboxPath != "~peter/日本語/台北"
                )
                throw new cTestsException("5092.2.3");


            if (!LTryParse("imap://;AUTH=GSSAPI@minbari.example.org/gray-council/;uid=20/;section=1.2", out lParts)) throw new cTestsException("5092.3");

            if (lParts.IsHomeServerReferral) throw new cTestsException("5092.3.1");
            if (lParts.IsMailboxReferral) throw new cTestsException("5092.3.2");

            if (!lParts.ZHasParts(fParts.scheme | fParts.mechanismname | fParts.host | fParts.mailboxname | fParts.uid | fParts.section) ||
                lParts.MustUseAnonymous ||
                lParts.MechanismName != "GSSAPI" ||
                lParts.Host != "minbari.example.org" ||
                lParts.MailboxPath != "gray-council" ||
                lParts.UID != 20 ||
                lParts.Section != "1.2"
                )
                throw new cTestsException("5092.3.3");

            if (!LTryParse("imap://;AUTH=*@minbari.example.org/gray%20council?SUBJECT%20shadows", out lParts)) throw new cTestsException("5092.5");

            if (lParts.IsHomeServerReferral) throw new cTestsException("5092.5.1");
            if (lParts.IsMailboxReferral) throw new cTestsException("5092.5.2");

            if (!lParts.ZHasParts(fParts.scheme | fParts.mechanismname | fParts.host | fParts.mailboxname | fParts.search) ||
                lParts.MustUseAnonymous ||
                lParts.MechanismName != null ||
                lParts.Host != "minbari.example.org" ||
                lParts.MailboxPath != "gray council" ||
                lParts.Search != "SUBJECT shadows"
                )
                throw new cTestsException("5092.5.3");

            if (!LTryParse("imap://john;AUTH=*@minbari.example.org/babylon5/personel?charset%20UTF-8%20SUBJECT%20%7B14+%7D%0D%0A%D0%98%D0%B2%D0%B0%D0%BD%D0%BE%D0%B2%D0%B0", out lParts)) throw new cTestsException("5092.6");

            if (lParts.IsHomeServerReferral) throw new cTestsException("5092.6.1");
            if (lParts.IsMailboxReferral) throw new cTestsException("5092.6.2");

            if (!lParts.ZHasParts(fParts.scheme | fParts.userid | fParts.mechanismname | fParts.host | fParts.mailboxname | fParts.search) ||
                lParts.MustUseAnonymous ||
                lParts.UserId != "john" ||
                lParts.MechanismName != null ||
                lParts.Host != "minbari.example.org" ||
                lParts.MailboxPath != "babylon5/personel" ||
                lParts.Search != "charset UTF-8 SUBJECT {14+}\r\nИванова"
                )
                throw new cTestsException("5092.6.3");

            // URLAUTH - rfc 4467

            if (!LTryParse("imap://joe@example.com/INBOX/;uid=20/;section=1.2", out lParts)) throw new cTestsException("4467.1");
            if (lParts.IsHomeServerReferral || lParts.IsMailboxReferral || lParts.IsAuthorisable || lParts.IsAuthorised) throw new cTestsException("4467.1.1");

            if (!LTryParse("imap://example.com/Shared/;uid=20/;section=1.2;urlauth=submit+fred", out lParts)) throw new cTestsException("4467.2");
            if (lParts.IsHomeServerReferral || lParts.IsMailboxReferral || lParts.IsAuthorisable || lParts.IsAuthorised) throw new cTestsException("4467.2.1");

            if (!LTryParse("imap://joe@example.com/INBOX/;uid=20/;section=1.2;urlauth=submit+fred", out lParts)) throw new cTestsException("4467.3");
            if (lParts.IsHomeServerReferral || lParts.IsMailboxReferral || !lParts.IsAuthorisable || lParts.IsAuthorised) throw new cTestsException("4467.3.1");

            if (!LTryParse("imap://joe@example.com/INBOX/;uid=20/;section=1.2;urlauth=submit+fred:internal:91354a473744909de610943775f92038", out lParts)) throw new cTestsException("4467.4");
            if (lParts.IsHomeServerReferral || lParts.IsMailboxReferral || lParts.IsAuthorisable || !lParts.IsAuthorised) throw new cTestsException("4467.4.1");

            // expiry
            //  TODO

            // network-path
            //  TODO

            // absolute-path
            //  TODO

            // edge cases for the URL
            //  TODO

            bool LTryParse(string pURL, out cURLParts rParts)
            {
                if (!cBytesCursor.TryConstruct(pURL, out var lCursor)) { rParts = null; return false; }
                if (!Process(lCursor, out rParts, lContext)) return false;
                if (!lCursor.Position.AtEnd) return false;
                return true;
            }
        }
    }
}