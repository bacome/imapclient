using System;
using System.Collections.Generic;
using work.bacome.imapclient;
using work.bacome.imapsupport;

namespace work.bacome.imapinternals
{
    public partial class cBytesCursor
    {
        private static readonly cBytes kIMAPColonSlashSlash = new cBytes("imap://");
        private static readonly cBytes kSemicolonAuthEquals = new cBytes(";AUTH=");
        private static readonly cBytes kSemicolonUIDValidityEquals = new cBytes(";UIDVALIDITY=");
        private static readonly cBytes kSemicolonUIDEquals = new cBytes(";UID=");
        private static readonly cBytes kSlashSemicolonSectionEquals = new cBytes("/;SECTION=");
        private static readonly cBytes kSlashSemicolonPartialEquals = new cBytes("/;PARTIAL=");
        private static readonly cBytes kSemicolonExpireEquals = new cBytes(";EXPIRE=");
        private static readonly cBytes kSemicolonURLAuthEquals = new cBytes(";URLAUTH=");

        public bool ProcessURLParts(out cURLParts rParts)
        {
            // IMAP URL (rfc 5092, 5593)
            //  NOTE: this routine does not return the cursor to its original position if it fails

            rParts = new cURLParts();

            var lStartBookmark = Position;

            sPosition lBookmark1;
            sPosition lBookmark2;

            bool lExpectServer;

            if (SkipBytes(kIMAPColonSlashSlash))
            {
                rParts.SetHasScheme();
                lExpectServer = true;
            }
            else if (SkipBytes(kSlashSlash)) lExpectServer = true;
            else if (SkipByte(cASCII.SLASH)) lExpectServer = false;
            else return false;

            if (lExpectServer)
            {
                // userinfo

                bool lExpectAt = false;
                bool lMechanismAny = false;

                lBookmark1 = Position;
                if (GetToken(cCharset.AChar, cASCII.PERCENT, null, out string lUserId)) lExpectAt = true;

                lBookmark2 = Position;
                string lMechanismName;

                if (SkipBytes(kSemicolonAuthEquals))
                {
                    if (SkipByte(cASCII.ASTERISK))
                    {
                        lMechanismAny = true;
                        lMechanismName = null;
                        lExpectAt = true;
                    }
                    else if (GetToken(cCharset.AChar, cASCII.PERCENT, null, out lMechanismName)) lExpectAt = true;
                    else Position = lBookmark2;
                }
                else lMechanismName = null;

                if (lExpectAt)
                {
                    if (SkipByte(cASCII.AT))
                    {
                        if (lUserId != null) rParts.UserId = lUserId;
                        if (lMechanismName != null || lMechanismAny) rParts.MechanismName = lMechanismName;
                    }
                    else Position = lBookmark1;
                }

                // host
                //  (the syntax allows zero length in the reg name form, but not in the [] form)

                lBookmark1 = Position;

                cByteList lBytes;

                // note that the rules here for extracting the host name are substantially more liberal than the rfcs (3986, 6874)
                //
                if (SkipByte(cASCII.LBRACKET) && GetToken(cCharset.IPLiteral, cASCII.PERCENT, null, out lBytes) && SkipByte(cASCII.RBRACKET)) rParts.Host = "[" + cTools.UTF8BytesToString(lBytes) + "]";
                else
                {
                    Position = lBookmark1;

                    if (GetToken(cCharset.RegName, cASCII.PERCENT, null, out string lHost)) rParts.Host = lHost;
                    else return false;
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
                            }

                            if (lPort < 1 || lPort > 65535) return false;

                            rParts.Port = lPort;
                        }
                    }
                }

                if (!SkipByte(cASCII.SLASH)) return true;
            }

            // mailbox

            lBookmark1 = Position; // may have to return to here if the mailbox reads in as a single '/' but it is the '/' from '/;UID='

            if (!GetToken(cCharset.BChar, cASCII.PERCENT, null, out cByteList lMailboxPathBytes)) return true;

            bool lLastByteWasASlash = (lMailboxPathBytes[lMailboxPathBytes.Count - 1] == cASCII.SLASH);

            // uidvalidity

            lBookmark2 = Position;

            if (SkipBytes(kSemicolonUIDValidityEquals))
            {
                if (GetNZNumber(out _, out uint lUIDValidity))
                {
                    rParts.MailboxPath = ZProcessURLMailboxPath(lMailboxPathBytes);
                    rParts.UIDValidity = lUIDValidity;

                    if (lLastByteWasASlash) lLastByteWasASlash = false;
                }
                else Position = lBookmark2;
            }

            // search

            lBookmark2 = Position;

            if (SkipByte(cASCII.QUESTIONMARK))
            {
                if (GetToken(cCharset.BChar, cASCII.PERCENT, null, out string lSearch))
                {
                    rParts.MailboxPath = ZProcessURLMailboxPath(lMailboxPathBytes);
                    rParts.Search = lSearch;
                    return true;
                }

                Position = lBookmark2;
            }

            // UID

            uint lUID = 0; // = 0 to shut the compiler up
            bool lGotUID = false;
            lBookmark2 = Position;

            if (!lLastByteWasASlash)
                if (!SkipByte(cASCII.SLASH))
                {
                    // the UID needs to be present to consider anything further, so that's it
                    rParts.MailboxPath = ZProcessURLMailboxPath(lMailboxPathBytes);
                    return true;
                }

            if (SkipBytes(kSemicolonUIDEquals))
            {
                if (GetNZNumber(out _, out lUID)) lGotUID = true;
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
                        return true;
                    }

                    byte[] lBytes = new byte[lMailboxPathBytes.Count - 1];
                    for (int i = 0; i < lMailboxPathBytes.Count - 1; i++) lBytes[i] = lMailboxPathBytes[i];
                    lMailboxPath = ZProcessURLMailboxPath(lBytes);
                }
                else lMailboxPath = ZProcessURLMailboxPath(lMailboxPathBytes);

                rParts.MailboxPath = lMailboxPath;
                rParts.UID = lUID;
            }
            else
            {
                // the UID needs to be present to consider anything further, so that's it
                Position = lBookmark2;
                rParts.MailboxPath = ZProcessURLMailboxPath(lMailboxPathBytes);
                return true;
            }

            // section

            lBookmark1 = Position;

            if (SkipBytes(kSlashSemicolonSectionEquals))
            {
                if (GetToken(cCharset.BChar, cASCII.PERCENT, null, out string lSection)) rParts.Section = lSection;
                else Position = lBookmark1;
            }

            // partial

            lBookmark1 = Position;

            if (SkipBytes(kSlashSemicolonPartialEquals))
            {
                if (GetNumber(out _, out uint lPartialOffset))
                {
                    rParts.PartialOffset = lPartialOffset;

                    // partial length (according to the grammar it is optional, although what that means is not explained)

                    //lBookmark1 = pCursor.Position;

                    if (SkipByte(cASCII.DOT))
                    {
                        if (GetNZNumber(out _, out uint lPartialLength)) rParts.PartialLength = lPartialLength;
                        else return false;
                    }
                    else return false;
                }
                else Position = lBookmark1;
            }

            // expire - only valid if ;URLAUTH= follows

            lBookmark1 = Position;

            cTimestamp lExpire = null;

            if (SkipBytes(kSemicolonExpireEquals))
            {
                if (!GetTimeStamp(out lExpire)) Position = lBookmark1;
            }

            // urlauth

            if (!SkipBytes(kSemicolonURLAuthEquals))
            {
                Position = lBookmark1;
                return true;
            }

            // access

            if (!GetToken(cCharset.AlphaNumeric, null, null, out string lApplication))
            {
                Position = lBookmark1;
                return true;
            }

            rParts.Application = lApplication;

            // expire can now be set
            if (lExpire != null) rParts.Expire = lExpire;

            // urlauth user

            lBookmark1 = Position;

            if (SkipByte(cASCII.PLUS))
            {
                if (GetToken(cCharset.AChar, cASCII.PERCENT, null, out string lAccessUserId)) rParts.AccessUserId = lAccessUserId;
                else Position = lBookmark1;
            }

            // now the token

            lBookmark1 = Position;

            if (SkipByte(cASCII.COLON) && GetToken(cCharset.UAuthMechanism, null, null, out cByteList lTokenMechanism) && SkipByte(cASCII.COLON) && GetToken(cCharset.Hexidecimal, null, null, out cByteList lToken, 32, 32))
            {
                rParts.TokenMechanism = cTools.UTF8BytesToString(lTokenMechanism);
                rParts.Token = cTools.UTF8BytesToString(lToken);
            }
            else Position = lBookmark1;

            // done

            return true;
        }

        private static string ZProcessURLMailboxPath(IList<byte> pMailboxPath)
        {
            // try a utf-7 decode, if it fails treat the bytes as utf-8
            //  there is a problem if the separator character is a not a valid utf-7 unencoded byte - the separator character isn't allowed to be utf-7 encoded
            //   however, without connecting to the server I can't tell what the separator character is
            //   in this (very unlikely) case the utf-7 decode will fail and we will output the utf-7 encoded string as the mailbox name

            if (cModifiedUTF7.TryDecode(pMailboxPath, out string lMailboxPath, out _)) return lMailboxPath;
            return cTools.UTF8BytesToString(pMailboxPath);
        }
    }
}