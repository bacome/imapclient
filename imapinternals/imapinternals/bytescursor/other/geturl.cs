using System;
using System.Collections.Generic;
using work.bacome.imapsupport;

namespace work.bacome.imapclient
{
    public partial class cBytesCursor
    {
        private static readonly cBytes kURLIMAPColonSlashSlash = new cBytes("imap://");
        private static readonly cBytes kURLSlashSlash = new cBytes("//");
        private static readonly cBytes kURLSemicolonAuthEquals = new cBytes(";AUTH=");
        private static readonly cBytes kURLSemicolonUIDValidityEquals = new cBytes(";UIDVALIDITY=");
        private static readonly cBytes kURLSemicolonUIDEquals = new cBytes(";UID=");
        private static readonly cBytes kURLSlashSemicolonSectionEquals = new cBytes("/;SECTION=");
        private static readonly cBytes kURLSlashSemicolonPartialEquals = new cBytes("/;PARTIAL=");
        private static readonly cBytes kURLSemicolonExpireEquals = new cBytes(";EXPIRE=");
        private static readonly cBytes kURLSemicolonURLAuthEquals = new cBytes(";URLAUTH=");

        public bool GetURL(out sURLParts rParts, out string rString, cTrace.cContext pParentContext)
        {

            var lContext = pParentContext.NewMethod(nameof(cBytesCursor), nameof(GetURL));

            var lBookmark = Position;

            if (!ZProcessURL(out rParts, lContext))
            {
                Position = lBookmark;
                rString = null;
                return false;
            }

            rString = GetFromAsString(lBookmark);
            return true;
        }

        private bool ZProcessURL(out sURLParts rParts, cTrace.cContext pParentContext)
        {
            // IMAP URL (rfc 5092, 5593)
            //  NOTE: this routine does not return the cursor to its original position if it fails

            var lContext = pParentContext.NewMethod(nameof(cBytesCursor), nameof(ZProcessURL));

            rParts = new sURLParts(true);

            var lStartBookmark = Position;

            sPosition lBookmark1;
            sPosition lBookmark2;

            bool lExpectServer;

            if (SkipBytes(kURLIMAPColonSlashSlash))
            {
                lContext.TraceVerbose("absolute URL");
                rParts.SetHasScheme();
                lExpectServer = true;
            }
            else if (SkipBytes(kURLSlashSlash))
            {
                lContext.TraceVerbose("network-path reference");
                lExpectServer = true;
            }
            else if (SkipByte(cASCII.SLASH))
            {
                lContext.TraceVerbose("absolute-path reference");
                lExpectServer = false;
            }
            else
            {
                lContext.TraceVerbose("relative-path references are not allowed by rfc5092 section 7.2");
                return false;
            }

            if (lExpectServer)
            {
                // userinfo

                bool lExpectAt = false;
                bool lMechanismAny = false;

                lBookmark1 = Position;
                if (GetToken(cCharset.AChar, cASCII.PERCENT, null, out string lUserId)) lExpectAt = true;

                lBookmark2 = Position;
                string lMechanismName;

                if (SkipBytes(kURLSemicolonAuthEquals))
                {
                    if (SkipByte(cASCII.ASTERISK))
                    {
                        lMechanismAny = true;
                        lMechanismName = null;
                        lExpectAt = true;
                    }
                    else if (GetToken(cCharset.AChar, cASCII.PERCENT, null, out lMechanismName)) lExpectAt = true;
                    else
                    {
                        Position = lBookmark2;
                        lContext.TraceWarning("likely malformed auth= section");
                    }
                }
                else lMechanismName = null;

                if (lExpectAt)
                {
                    if (SkipByte(cASCII.AT))
                    {
                        if (lUserId != null)
                        {
                            rParts.UserId = lUserId;
                            lContext.TraceVerbose("UserId: {0}", lUserId);
                        }

                        if (lMechanismName != null || lMechanismAny)
                        {
                            rParts.MechanismName = lMechanismName;
                            lContext.TraceVerbose("Mechanism: {0}", lMechanismName);
                        }
                    }
                    else
                    {
                        Position = lBookmark1;
                        lContext.TraceVerbose("no userinfo (1)");
                    }
                }
                else lContext.TraceVerbose("no userinfo (2)");

                // host
                //  (the syntax allows zero length in the reg name form, but not in the [] form)

                lBookmark1 = Position;

                cByteList lBytes;

                // note that the rules here for extracting the host name are substantially more liberal than the rfcs (3986, 6874)
                //
                if (SkipByte(cASCII.LBRACKET) && GetToken(cCharset.IPLiteral, cASCII.PERCENT, null, out lBytes) && SkipByte(cASCII.RBRACKET))
                {
                    rParts.Host = cTools.UTF8BytesToString(lBytes);
                    lContext.TraceVerbose("ipv6 (or greater) host: {0}", rParts.Host);
                }
                else
                {
                    Position = lBookmark1;

                    if (GetToken(cCharset.RegName, cASCII.PERCENT, null, out string lHost))
                    {
                        rParts.Host = lHost;
                        lContext.TraceVerbose("host: {0}", lHost);
                    }
                    else
                    {
                        lContext.TraceVerbose("blank host");
                        return false;
                    }
                }

                // port
                //  (optional) - the colon is allowed to be there even if the port is not
                //
                if (SkipByte(cASCII.COLON))
                {
                    if (GetToken(cCharset.Digit, null, null, out lBytes))
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
                                return false;
                            }

                            rParts.Port = lPort;
                            lContext.TraceVerbose("port: {0}", lPort);
                        }
                    }
                    else lContext.TraceVerbose("no port specified (but the ':' was there)");
                }

                if (!SkipByte(cASCII.SLASH))
                {
                    lContext.TraceVerbose("server only URL (1)");
                    return true;
                }
            }

            // mailbox

            lBookmark1 = Position; // may have to return to here if the mailbox reads in as a single '/' but it is the '/' from '/;UID='

            if (!GetToken(cCharset.BChar, cASCII.PERCENT, null, out cByteList lMailboxPathBytes))
            {
                // the mailbox needs to be present to consider anything further
                //
                if (!lExpectServer) lContext.TraceVerbose("URL was '/' (1)");
                else lContext.TraceVerbose("server only URL (2)");
                return true;
            }

            bool lLastByteWasASlash = (lMailboxPathBytes[lMailboxPathBytes.Count - 1] == cASCII.SLASH);

            if (lLastByteWasASlash)
            {
                if (lMailboxPathBytes.Count == 1) lContext.TraceVerbose("mailbox is just a slash - may not be a mailbox"); // this is the '/;UID=' case
                else lContext.TraceVerbose("mailbox ends with a slash - may be trimmed");
            }

            // uidvalidity

            lBookmark2 = Position;

            if (SkipBytes(kURLSemicolonUIDValidityEquals))
            {
                if (GetNZNumber(out _, out uint lUIDValidity))
                {
                    rParts.MailboxPath = ZProcessURLMailboxPath(lMailboxPathBytes, lContext);
                    rParts.UIDValidity = lUIDValidity;

                    if (lLastByteWasASlash)
                    {
                        lLastByteWasASlash = false;
                        lContext.TraceVerbose("mailbox ends with a slash!"); // allowed by the grammar, unlikely to be valid
                    }

                    lContext.TraceVerbose("mailbox: {0}, uidvalidity: {1}", rParts.MailboxPath, lUIDValidity);
                }
                else
                {
                    Position = lBookmark2;
                    lContext.TraceWarning("likely malformed uidvalidity= section");
                }
            }

            // search

            lBookmark2 = Position;

            if (SkipByte(cASCII.QUESTIONMARK))
            {
                if (GetToken(cCharset.BChar, cASCII.PERCENT, null, out string lSearch))
                {
                    rParts.MailboxPath = ZProcessURLMailboxPath(lMailboxPathBytes, lContext);
                    rParts.Search = lSearch;

                    if (lLastByteWasASlash) lContext.TraceVerbose("mailbox ends with a slash!");

                    lContext.TraceVerbose("mailbox: {0}, search: {1}", rParts.MailboxPath, lSearch);

                    return true;
                }

                Position = lBookmark2;
                lContext.TraceWarning("likely malformed search section");
            }

            // UID

            uint lUID = 0; // = 0 to shut the compiler up
            bool lGotUID = false;
            lBookmark2 = Position;

            if (!lLastByteWasASlash)
                if (!SkipByte(cASCII.SLASH))
                {
                    // the UID needs to be present to consider anything further, so that's it
                    rParts.MailboxPath = ZProcessURLMailboxPath(lMailboxPathBytes, lContext);
                    lContext.TraceVerbose("mailbox url (1): {0}", rParts.MailboxPath);
                    return true;
                }

            if (SkipBytes(kURLSemicolonUIDEquals))
            {
                if (GetNZNumber(out _, out lUID)) lGotUID = true;
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
                        Position = lBookmark1;
                        if (!lExpectServer) lContext.TraceVerbose("URL was '/' (2)");
                        else lContext.TraceVerbose("server only URL (3)");
                        return true;
                    }

                    byte[] lBytes = new byte[lMailboxPathBytes.Count - 1];
                    for (int i = 0; i < lMailboxPathBytes.Count - 1; i++) lBytes[i] = lMailboxPathBytes[i];
                    lMailboxPath = ZProcessURLMailboxPath(lBytes, lContext);

                    lContext.TraceVerbose("trimmed off trailing '/' of the mailbox");
                }
                else lMailboxPath = ZProcessURLMailboxPath(lMailboxPathBytes, lContext);

                rParts.MailboxPath = lMailboxPath;
                rParts.UID = lUID;

                lContext.TraceVerbose("mailbox: {0}, uid: {1}", lMailboxPath, lUID);
            }
            else
            {
                // the UID needs to be present to consider anything further, so that's it
                Position = lBookmark2;
                rParts.MailboxPath = ZProcessURLMailboxPath(lMailboxPathBytes, lContext);
                lContext.TraceVerbose("mailbox url (2): {0}", rParts.MailboxPath);
                if (lLastByteWasASlash) lContext.TraceVerbose("mailbox ends with a slash!");
                return true;
            }

            // section

            lBookmark1 = Position;

            if (SkipBytes(kURLSlashSemicolonSectionEquals))
            {
                if (GetToken(cCharset.BChar, cASCII.PERCENT, null, out string lSection))
                {
                    rParts.Section = lSection;
                    lContext.TraceVerbose("section: {0}", lSection);
                }
                else
                {
                    Position = lBookmark1;
                    lContext.TraceWarning("likely malformed section= section");
                }
            }

            // partial

            lBookmark1 = Position;

            if (SkipBytes(kURLSlashSemicolonPartialEquals))
            {
                if (GetNumber(out _, out uint lPartialOffset))
                {
                    rParts.PartialOffset = lPartialOffset;
                    lContext.TraceVerbose("fragment offset: {0}", lPartialOffset);

                    // partial length (according to the grammar it is optional, although what that means is not explained)

                    //lBookmark1 = pCursor.Position;

                    if (SkipByte(cASCII.DOT))
                    {
                        if (GetNZNumber(out _, out uint lPartialLength))
                        {
                            rParts.PartialLength = lPartialLength;
                            lContext.TraceVerbose("fragment length: {0}", lPartialLength);
                        }
                        else
                        {
                            lContext.TraceWarning("likely malformed partial= length section");
                            return false;

                            //pCursor.Position = lBookmark1;
                            //lContext.TraceVerbose("likely malformed partial= length section");
                        }
                    }
                    else
                    {
                        lContext.TraceVerbose("likely unusable partial= section - no length");
                        return false;
                    }
                }
                else
                {
                    Position = lBookmark1;
                    lContext.TraceWarning("likely malformed partial= section");
                }
            }

            // expire - only valid if ;URLAUTH= follows

            lBookmark1 = Position;

            cTimestamp lExpire = null;

            if (SkipBytes(kURLSemicolonExpireEquals))
            {
                if (!GetTimeStamp(out lExpire))
                {
                    Position = lBookmark1;
                    lContext.TraceWarning("likely malformed expire= section");
                }
            }

            // urlauth

            if (!SkipBytes(kURLSemicolonURLAuthEquals))
            {
                Position = lBookmark1;
                if (lExpire != null) lContext.TraceWarning("likely malformed urlauth section (expire but no urlauth)");
                return true;
            }

            // access

            if (!GetToken(cCharset.AlphaNumeric, null, null, out string lApplication))
            {
                Position = lBookmark1;
                lContext.TraceWarning("likely malformed urlauth section (no application)");
                return true;
            }

            rParts.Application = lApplication;
            lContext.TraceVerbose("application: {0}", lApplication);

            // expire can now be set
            if (lExpire != null)
            {
                rParts.Expire = lExpire;
                lContext.TraceVerbose("expire: {0}", lExpire);
            }

            // urlauth user

            lBookmark1 = Position;

            if (SkipByte(cASCII.PLUS))
            {
                if (GetToken(cCharset.AChar, cASCII.PERCENT, null, out string lAccessUserId))
                {
                    rParts.AccessUserId = lAccessUserId;
                    lContext.TraceVerbose("accessuserid: {0}", lAccessUserId);
                }
                else
                {
                    lContext.TraceWarning("likely malformed urlauth section (+ but no userid)");
                    Position = lBookmark1;
                }
            }

            // now the token

            lBookmark1 = Position;

            if (SkipByte(cASCII.COLON) && GetToken(cCharset.UAuthMechanism, null, null, out cByteList lTokenMechanism) && SkipByte(cASCII.COLON) && GetToken(cCharset.Hexidecimal, null, null, out cByteList lToken, 32, 32))
            {
                rParts.TokenMechanism = cTools.UTF8BytesToString(lTokenMechanism);
                rParts.Token = cTools.UTF8BytesToString(lToken);
                lContext.TraceVerbose("Token mechanism: {0}, token: {1}", rParts.TokenMechanism, rParts.Token);
            }
            else Position = lBookmark1;

            // done

            return true;
        }

        private static string ZProcessURLMailboxPath(IList<byte> pMailboxPath, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cBytesCursor), nameof(ZProcessURLMailboxPath));

            // try a utf-7 decode, if it fails treat the bytes as utf-8
            //  there is a problem if the separator character is a not a valid utf-7 unencoded byte - the separator character isn't allowed to be utf-7 encoded
            //   however, without connecting to the server I can't tell what the separator character is
            //   in this (very unlikely) case the utf-7 decode will fail and we will output the utf-7 encoded string as the mailbox name

            if (cModifiedUTF7.TryDecode(pMailboxPath, out string lMailboxPath, out _)) return lMailboxPath;
            return cTools.UTF8BytesToString(pMailboxPath);
        }
    }
}