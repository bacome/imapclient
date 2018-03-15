using System;
using System.Collections.Generic;

namespace work.bacome.mailclient
{
    internal static class cASCII
    {
        // null
        public const byte NUL = 0;

        // control characters
        public const byte TAB = 9;
        public const byte LF = 10;
        public const byte CR = 13;

        // printable characters
        public const byte SPACE = 32;
        public const byte EXCLAMATION = 33;
        public const byte DQUOTE = 34;
        public const byte HASH = 35;
        public const byte PERCENT = 37;
        public const byte AMPERSAND = 38;
        public const byte QUOTE = 39;
        public const byte LPAREN = 40;
        public const byte RPAREN = 41;
        public const byte ASTERISK = 42;
        public const byte PLUS = 43;
        public const byte COMMA = 44;
        public const byte HYPEN = 45;
        public const byte DOT = 46;
        public const byte SLASH = 47;

        // 0 - 9
        public const byte ZERO = 48;
        public const byte ONE = 49;
        public const byte TWO = 50;
        public const byte THREE = 51;
        public const byte FOUR = 52;
        public const byte FIVE = 53;
        public const byte SIX = 54;
        public const byte SEVEN = 55;
        public const byte EIGHT = 56;
        public const byte NINE = 57;

        // specials

        public const byte COLON = 58;
        public const byte SEMICOLON = 59;
        public const byte LESSTHAN = 60;
        public const byte EQUALS = 61;
        public const byte GREATERTHAN = 62;
        public const byte QUESTIONMARK = 63;
        public const byte AT = 64;

        // A-Z
        public const byte A = 65;
        public const byte B = 66;
        public const byte C = 67;
        public const byte D = 68;
        public const byte E = 69;
        public const byte F = 70;
        public const byte G = 71;
        public const byte H = 72;
        public const byte I = 73;
        public const byte J = 74;
        public const byte K = 75;
        public const byte L = 76;
        public const byte M = 77;
        public const byte N = 78;
        public const byte O = 79;
        public const byte P = 80;
        public const byte Q = 81;
        public const byte R = 82;
        public const byte S = 83;
        public const byte T = 84;
        public const byte U = 85;
        public const byte V = 86;
        public const byte W = 87;
        public const byte X = 88;
        public const byte Y = 89;
        public const byte Z = 90;

        // specials

        public const byte LBRACKET = 91;
        public const byte BACKSL = 92;
        public const byte RBRACKET = 93;
        public const byte UNDERSCORE = 95;
        public const byte GRAVE = 96;

        // a-z
        public const byte a = 97;
        public const byte b = 98;
        public const byte c = 99;
        public const byte d = 100;
        public const byte e = 101;
        public const byte f = 102;
        public const byte g = 103;
        public const byte h = 104;
        public const byte i = 105;
        public const byte j = 106;
        public const byte k = 107;
        public const byte l = 108;
        public const byte m = 109;
        public const byte n = 110;
        public const byte o = 111;
        public const byte p = 112;
        public const byte q = 113;
        public const byte r = 114;
        public const byte s = 115;
        public const byte t = 116;
        public const byte u = 117;
        public const byte v = 118;
        public const byte w = 119;
        public const byte x = 120;
        public const byte y = 121;
        public const byte z = 122;

        // specials

        public const byte LBRACE = 123;
        public const byte RBRACE = 125;
        public const byte TILDA = 126;
        public const byte DEL = 127;

        // helper functions

        public static bool Compare(byte pByte1, byte pByte2, bool pCaseSensitive)
        {
            if (pCaseSensitive) return pByte1 == pByte2;

            byte lByte1;
            if (pByte1 < a) lByte1 = pByte1;
            else if (pByte1 > z) lByte1 = pByte1;
            else lByte1 = (byte)(pByte1 - a + A);

            byte lByte2;
            if (pByte2 < a) lByte2 = pByte2;
            else if (pByte2 > z) lByte2 = pByte2;
            else lByte2 = (byte)(pByte2 - a + A);

            return lByte1 == lByte2;
        }

        public static bool Compare(IList<byte> pBytes1, IList<byte> pBytes2, bool pCaseSensitive)
        {
            if (pBytes1.Count != pBytes2.Count) return false;
            for (int i = 0; i < pBytes1.Count; i++) if (!Compare(pBytes1[i], pBytes2[i], pCaseSensitive)) return false;
            return true;
        }
    }
}