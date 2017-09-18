﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using work.bacome.trace;

namespace work.bacome.imapclient.support
{
    public partial class cBytesCursor
    {
        [Conditional("DEBUG")]
        public static void _Tests_RFC822(cTrace.cContext pParentContext)
        {
            cBytesCursor lCursor;
            string lString;
            DateTime lDate;
            cByteList lByteList;


            // tests for WSP

            TryConstruct("x \t y \t\r\n\tz", out lCursor);
            if (!lCursor.SkipByte(cASCII.x) || !lCursor.SkipRFC822WSP() || lCursor.SkipRFC822WSP() || !lCursor.SkipByte(cASCII.y) || !lCursor.SkipRFC822FWS() || lCursor.SkipRFC822FWS() || !lCursor.SkipByte(cASCII.z)) throw new cTestsException("skip wsp 1");
            TryConstruct("x \t y \t\r\n\tz", out lCursor);
            if (!lCursor.SkipByte(cASCII.x) || !lCursor.SkipRFC822FWS() || lCursor.SkipRFC822FWS() || !lCursor.SkipByte(cASCII.y) || !lCursor.SkipRFC822FWS() || !lCursor.SkipByte(cASCII.z)) throw new cTestsException("skip wsp 2");
            TryConstruct("x \t\r\ny \t\r\n\t\r\n z", out lCursor);
            if (!lCursor.SkipByte(cASCII.x) || !lCursor.SkipRFC822FWS() || lCursor.SkipRFC822FWS() || !lCursor.SkipBytes(new cBytes("\r\ny")) || !lCursor.SkipRFC822FWS() || !lCursor.SkipByte(cASCII.z)) throw new cTestsException("skip wsp 3");

            TryConstruct("Muhammed.(I am  the greatest) Ali\r\n @(the)Vegas.WBA", out lCursor);
            if (!lCursor.GetToken(cCharset.Atom, null, null, out lString) || lString != "Muhammed." || !lCursor.SkipRFC822CFWS() || !lCursor.GetToken(cCharset.Atom, null, null, out lString) || lString != "Ali" || !lCursor.SkipRFC822CFWS() || !lCursor.SkipByte(cASCII.AT) || !lCursor.SkipRFC822CFWS() || !lCursor.GetToken(cCharset.Atom, null, null, out lString) || lString != "Vegas.WBA") throw new cTestsException("skip cfws 1");
            TryConstruct("(I am \r\n the(xx\\)\\\\\\() gre \t() \tatest)", out lCursor);
            if (!lCursor.SkipRFC822CFWS() || !lCursor.Position.AtEnd) throw new cTestsException("skip cfws 2");

            // TODO: more tests for failure cases 



            // tests for IMF date (these examples from rfc 5322)

            TryConstruct("Fri, 21 Nov 1997 09:55:06 -0600  x  Tue, 1 Jul 2003 10:52:37 +0200    x    Thu, 13 Feb 1969 23:32:54 -0330    x  Thu,\r\n\t13\r\n\t  Feb\r\n\t    1969\r\n\t23:32\r\n\t\t\t-0330 (Newfoundland Time)   x   21 Nov 97 09:55:06 GMT    x     Fri, 21 Nov 1997 09(comment):   55  :  06 -0600    x", out lCursor);

            if (!lCursor.GetRFC822DateTime(out lDate) || lDate != new DateTime(1997, 11, 21, 15, 55, 06) || !lCursor.SkipByte(cASCII.x)) throw new cTestsException("imf date 1");
            if (!lCursor.GetRFC822DateTime(out lDate) || lDate != new DateTime(2003, 7, 1, 8, 52, 37) || !lCursor.SkipByte(cASCII.x)) throw new cTestsException("imf date 2");
            if (!lCursor.GetRFC822DateTime(out lDate) || lDate != new DateTime(1969, 2, 14, 3, 02, 54) || !lCursor.SkipByte(cASCII.x)) throw new cTestsException("imf date 3");
            if (!lCursor.GetRFC822DateTime(out lDate) || lDate != new DateTime(1969, 2, 14, 3, 02, 00) || !lCursor.SkipByte(cASCII.x)) throw new cTestsException("imf date 4");
            if (!lCursor.GetRFC822DateTime(out lDate) || lDate != new DateTime(1997, 11, 21, 9, 55, 06) || !lCursor.SkipByte(cASCII.x)) throw new cTestsException("imf date 5");
            if (!lCursor.GetRFC822DateTime(out lDate) || lDate != new DateTime(1997, 11, 21, 15, 55, 06) || !lCursor.SkipByte(cASCII.x)) throw new cTestsException("imf date 6");

            // TODO: more tests for failure cases and alphanumeric zones
            //   Wed, 17 Jul 1996 02:23:25 -0700 (PDT)



            // header values
            TryConstruct("   \t  \r\nHeader    \t:      \r\n\t       \t\r\n\r\n", out lCursor);

            if (lCursor.GetRFC822FieldName(out lString) || !lCursor.GetRFC822FieldValue(out lByteList) || cTools.UTF8BytesToString(lByteList) != "   \t  ") throw new cTestsException("header 1.1");
            if (!lCursor.GetRFC822FieldName(out lString) || lString != "Header" || !lCursor.SkipRFC822WSP() || !lCursor.SkipByte(cASCII.COLON) || !lCursor.GetRFC822FieldValue(out lByteList) || cTools.UTF8BytesToString(lByteList) != "      \t       \t") throw new cTestsException("header 1.2");

            TryConstruct("Header  \t  :      \r\n        \t\r\nFred", out lCursor);
            if (!lCursor.GetRFC822FieldName(out lString) || lString != "Header" || !lCursor.SkipRFC822WSP() || !lCursor.SkipByte(cASCII.COLON) || !lCursor.GetRFC822FieldValue(out lByteList) || cTools.UTF8BytesToString(lByteList) != "              \t") throw new cTestsException("header 2.1");

            TryConstruct("Header:\r\n  this  is  \r\n   the\tvalue     \t\r\n", out lCursor);
            if (!lCursor.GetRFC822FieldName(out lString) || lString != "Header" || lCursor.SkipRFC822WSP() || !lCursor.SkipByte(cASCII.COLON) || !lCursor.GetRFC822FieldValue(out lByteList) || cTools.UTF8BytesToString(lByteList) != "  this  is     the\tvalue     \t") throw new cTestsException("header 3.1");

            TryConstruct("Header:\r\n   should   \r\n    fail    \t\r\n more stuff", out lCursor);
            if (!lCursor.GetRFC822FieldName(out lString) || lString != "Header" || lCursor.SkipRFC822WSP() || !lCursor.SkipByte(cASCII.COLON) || lCursor.GetRFC822FieldValue(out lByteList)) throw new cTestsException("header 4.1");


            // atoms
            TryConstruct("   \t  \r\n Header    \tAtom(comment)      \r\nAt?Om\tAt!om:{Atom}       \t\r\n\r\n", out lCursor);

            if (!lCursor.GetRFC822Atom(out lString) || lString != "Header" || !lCursor.GetRFC822Atom(out lString) || lString != "Atom") throw new cTestsException("atom 1.1");
            if (!lCursor.SkipByte(cASCII.CR) || !lCursor.SkipByte(cASCII.LF)) throw new cTestsException("atom 1.2");
            if (!lCursor.GetRFC822Atom(out lString) || lString != "At?Om") throw new cTestsException("atom 1.3");
            if (!lCursor.GetRFC822Atom(out lString) || lString != "At!om") throw new cTestsException("atom 1.4");
            if (lCursor.GetRFC822Atom(out lString)) throw new cTestsException("atom 1.5.1");
            if (!lCursor.SkipByte(cASCII.COLON)) throw new cTestsException("atom 1.5.2");
            if ( !lCursor.GetRFC822Atom(out lString) || lString != "{Atom}") throw new cTestsException("atom 1.6");
            if (!lCursor.SkipByte(cASCII.CR) || !lCursor.SkipByte(cASCII.LF) || !lCursor.SkipByte(cASCII.CR) || !lCursor.SkipByte(cASCII.LF) || !lCursor.Position.AtEnd) throw new cTestsException("atom 1.7");

            // quoted strings
            TryConstruct("   \t  \r\n \"Header\r\n with FWS\"    \t\"Atom(not a \\\"comment\\\")\"      \r\n\"At?Om\"\t\"At!om:{Atom}\"    \"\r\n\tFWS beginning and end\r\n\t\"   \t\r\n\r\n", out lCursor);

            if (!lCursor.GetRFC822QuotedString(out lString) || lString != "Header with FWS" || !lCursor.GetRFC822QuotedString(out lString) || lString != "Atom(not a \"comment\")") throw new cTestsException("quoted string 1.1");
            if (!lCursor.SkipByte(cASCII.CR) || !lCursor.SkipByte(cASCII.LF)) throw new cTestsException("quoted string 1.2");
            if (!lCursor.GetRFC822QuotedString(out lString) || lString != "At?Om") throw new cTestsException("quoted string 1.3");
            if (!lCursor.GetRFC822QuotedString(out lString) || lString != "At!om:{Atom}") throw new cTestsException("quoted string 1.4");
            if (!lCursor.GetRFC822QuotedString(out lString) || lString != "\tFWS beginning and end\t") throw new cTestsException("quoted string 1.5");
            if (lCursor.GetRFC822QuotedString(out lString)) throw new cTestsException("quoted string 1.6.1");
            if (!lCursor.SkipByte(cASCII.CR) || !lCursor.SkipByte(cASCII.LF) || !lCursor.SkipByte(cASCII.CR) || !lCursor.SkipByte(cASCII.LF) || !lCursor.Position.AtEnd) throw new cTestsException("quoted string 1.6.2");

            // mix
            TryConstruct("   \t  \r\n Header    \tA", out lCursor);
            if (lCursor.GetRFC822QuotedString(out lString) || lCursor.Position.Byte != 0 || lCursor.GetRFC822Atom(out lString) || lString != "Header" || !lCursor.SkipByte(cASCII.A)) throw new cTestsException("mix 1.1");
            TryConstruct("   \t  \r\n \"Header\"    \tA", out lCursor);
            if (lCursor.GetRFC822Atom(out lString) || lCursor.Position.Byte != 0 || lCursor.GetRFC822QuotedString(out lString) || lString != "Header" || !lCursor.SkipByte(cASCII.A)) throw new cTestsException("mix 1.2");

            // domain literal
            TryConstruct("   \t (there is a domain\r\n literal coming up(and\tit'll\r\n\tbe a good one))  \r\n [Header]      \r\n ( now with with FWS ) [  \t  \r\n\t the.name.com  \r\n     ]    (now with embedded FWS)  [  \t  \r\n\t the \t   name   \r\n   com  \r\n     ]   (with quotes and utf8)    [     \\[   fr€d     ]     (something invalid)     [    [   ]   \r\n", out lCursor);

            if (!lCursor.GetDomainLiteral(out lString) || lString != "[Header]") throw new cTestsException("domain literal 1.1");
            if (!lCursor.GetDomainLiteral(out lString) || lString != "[the.name.com]") throw new cTestsException("domain literal 1.2");
            if (!lCursor.GetDomainLiteral(out lString) || lString != "[the name com]") throw new cTestsException("domain literal 1.3");
            if (!lCursor.GetDomainLiteral(out lString) || lString != "[[ fr€d]") throw new cTestsException("domain literal 1.4");
            if (lCursor.GetDomainLiteral(out lString)) throw new cTestsException("domain literal 1.5");
            if (!lCursor.GetRFC822FieldValue(out lByteList) || cTools.UTF8BytesToString(lByteList) != "[    [   ]   ") throw new cTestsException("domain literal 1.6");

            // domain
            TryConstruct("    \t   (first a dot-atom form)   fred.angus.bart   (now a obs form)    frxd  \t   .       angxs     .   \t  bxrt      (now a\r\n literal)     [    192.168.1.1     ]       \r\nNextHeader", out lCursor);

            if (!lCursor.GetRFC822Domain(out lString) || lString != "fred.angus.bart") throw new cTestsException("domain 1.1");
            if (!lCursor.GetRFC822Domain(out lString) || lString != "frxd.angxs.bxrt") throw new cTestsException("domain 1.2");
            if (!lCursor.GetRFC822Domain(out lString) || lString != "[192.168.1.1]") throw new cTestsException("domain 1.3");
            if (!lCursor.GetRFC822FieldValue(out lByteList) || lByteList.Count != 0) throw new cTestsException("domain 1.4");
            if (lCursor.GetRestAsString() != "NextHeader") throw new cTestsException("domain 1.5");

            // local part
            TryConstruct("    \t   (first a dot-atom form)   fred.angus.bart   (now a obs form)    frxd  \t   .       angxs     .   \t  bxrt      (now a\r\n quoted string)     \"th€ local part as a string\"       (then a second obsolete form)     \"fr€d\"  \t   .       angxs     .   \t  \"bzrt\"        \r\n ", out lCursor);

            if (!lCursor.GetRFC822LocalPart(out lString) || lString != "fred.angus.bart") throw new cTestsException("local part 1.1");
            if (!lCursor.GetRFC822LocalPart(out lString) || lString != "frxd.angxs.bxrt") throw new cTestsException("local part 1.2");
            if (!lCursor.GetRFC822LocalPart(out lString) || lString != "th€ local part as a string") throw new cTestsException("local part 1.3");
            if (!lCursor.GetRFC822LocalPart(out lString) || lString != "fr€d.angxs.bzrt") throw new cTestsException("local part 1.4");
            if (!lCursor.Position.AtEnd) throw new cTestsException("local part 1.5");

            TryConstruct("    \t   (edge case)   fred.angus.bart    .    []         \r\nNextHeader", out lCursor);
            if (!lCursor.GetRFC822LocalPart(out lString) || lString != "fred.angus.bart") throw new cTestsException("local part 2.1");
            if (!lCursor.GetRFC822FieldValue(out lByteList) || cTools.UTF8BytesToString(lByteList) != ".    []         ") throw new cTestsException("local part 2.2");

            // message id
            TryConstruct("     \r\n\t   (one)  <1234@local.machine.example>      <5678.21-Nov-1997@example.com>    <testabcd.1234@silly.example>     <1234   @   local(blah)  .machine .example>    ", out lCursor);

            if (!lCursor.GetRFC822MsgId(out lString) || lString != "1234@local.machine.example") throw new cTestsException("msgid 1.1");
            if (!lCursor.GetRFC822MsgId(out lString) || lString != "5678.21-Nov-1997@example.com") throw new cTestsException("msgid 1.2");
            if (!lCursor.GetRFC822MsgId(out lString) || lString != "testabcd.1234@silly.example") throw new cTestsException("msgid 1.3");
            if (!lCursor.GetRFC822MsgId(out lString) || lString != "1234@local.machine.example") throw new cTestsException("msgid 1.4");
        }
    }
}