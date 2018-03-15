using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    internal static class cBase64
    {
        private static readonly ReadOnlyCollection<byte> kEncode = new cBytes("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/");

        // these are ints because the shift operator works on ints
        private static readonly ReadOnlyCollection<int> kDecode = Array.AsReadOnly(new int[]
            {
                -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, // 0-9
                -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, // 10-19
                -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, // 20-29
                -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, // 30-39
                -1, -1, -1, // 40, 41, 42
                62, // 43 = PLUS
                -1, -1, -1, // 44, 45, 46
                63, // 47 = SLASH
                52, 53, 54, 55, 56, 57, 58, 59, 60, 61, // 48 - 57 = zero - nine
                -1, -1, -1, -1, -1, -1, -1, // 58 - 64
                00, 01, 02, 03, 04, 05, 06, 07, 08, 09, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, // A-Z
                -1, -1, -1, -1, -1, -1, // 91, 92, 93, 94, 95, 96
                26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, // a - z
                -1, -1, -1, -1, -1, -1, -1, // 123 - 129
                -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, // 130 - 139
                -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, // 140 - 149 
                -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, // 150 - 159
                -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, // 160 - 169
                -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, // 170 - 179
                -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, // 180 - 189
                -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, // 190 - 199
                -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, // 200 - 209
                -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, // 210 - 219
                -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, // 220 - 229
                -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, // 230 - 239
                -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, // 240 - 249
                -1, -1, -1, -1, -1 // 250 - 255
            }); // the byte 0-255 to be decoded is the index and the value at that position in the array is the byte that is encoded or -1 if this byte encodes nothing

        public static cByteList Encode(IList<byte> pBytes)
        {
            if (pBytes.Count == 0) return new cByteList();
            var lBytes = new cByteList((pBytes.Count + 2) / 3 * 4);
            var lBuffer = new cToBase64Buffer(lBytes);
            foreach (byte lByte in pBytes) lBuffer.Add(lByte);
            lBuffer.Flush();
            return lBytes;
        }

        public static bool IsInAlphabet(byte lByte) => ZTryDecodeBase64Byte(lByte, out _) || lByte == cASCII.EQUALS;

        public static bool TryDecode(IList<byte> pBytes, out cByteList rBytes, out string rError)
        {
            if (pBytes.Count == 0)
            {
                rBytes = new cByteList();
                rError = null;
                return true;
            }

            if (pBytes.Count % 4 != 0)
            {
                rBytes = null;
                rError = "base64 bytes must be in multiples of 4";
                return false;
            }

            int lCount;

            if (pBytes[pBytes.Count - 2] == cASCII.EQUALS)
            {
                if (pBytes[pBytes.Count - 1] != cASCII.EQUALS)
                {
                    rBytes = null;
                    rError = "invalid termination";
                    return false;
                }

                lCount = pBytes.Count - 2;
            }
            else if (pBytes[pBytes.Count - 1] == cASCII.EQUALS) lCount = pBytes.Count - 1;
            else lCount = pBytes.Count;

            rBytes = new cByteList((lCount + 2) / 4 * 3);

            var lBuffer = new cFromBase64Buffer(rBytes);

            for (int i = 0; i < lCount; i++) if (!lBuffer.TryAdd(pBytes[i], out rError)) { rBytes = null; return false; }

            if (!lBuffer.TryFlush(out rError)) { rBytes = null; return false; }

            return true;
        }

        private static bool ZTryDecodeBase64Byte(byte pByte, out int rDecodedByte)
        {
            rDecodedByte = kDecode[pByte];
            if (rDecodedByte == -1) return false;
            return true;
        }

        private class cToBase64Buffer
        {
            private cByteList mBytes;

            private int[] mInputBytes = new int[3];
            private int mInputByteCount = 0;

            public cToBase64Buffer(cByteList pBytes) { mBytes = pBytes; }

            public void Add(byte pByte)
            {
                mInputBytes[mInputByteCount++] = pByte;

                if (mInputByteCount == 3)
                {
                    mBytes.Add(kEncode[mInputBytes[0] >> 2]);
                    mBytes.Add(kEncode[((mInputBytes[0] & 3) << 4) | (mInputBytes[1] >> 4)]);
                    mBytes.Add(kEncode[((mInputBytes[1] & 15) << 2) | (mInputBytes[2] >> 6)]);
                    mBytes.Add(kEncode[mInputBytes[2] & 63]);
                    mInputByteCount = 0;
                }
            }

            public void Flush()
            {
                if (mInputByteCount == 0) return;

                mInputBytes[mInputByteCount] = 0;

                mBytes.Add(kEncode[mInputBytes[0] >> 2]);
                mBytes.Add(kEncode[((mInputBytes[0] & 3) << 4) | (mInputBytes[1] >> 4)]);

                if (mInputByteCount == 1)
                {
                    mBytes.Add(cASCII.EQUALS);
                    mBytes.Add(cASCII.EQUALS);
                    return;
                }

                mBytes.Add(kEncode[((mInputBytes[1] & 15) << 2) | (mInputBytes[2] >> 6)]);
                mBytes.Add(cASCII.EQUALS);
            }
        }

        private class cFromBase64Buffer
        {
            private cByteList mBytes;

            private byte[] mInputBytes = new byte[4];
            private int mInputByteCount = 0;

            public cFromBase64Buffer(cByteList pBytes) { mBytes = pBytes; }

            public bool TryAdd(byte pByte, out string rError)
            {
                mInputBytes[mInputByteCount++] = pByte;

                if (mInputByteCount == 4)
                {
                    if (!ZTryDecodeBase64Byte(mInputBytes[0], out int l61) ||
                        !ZTryDecodeBase64Byte(mInputBytes[1], out int l62) ||
                        !ZTryDecodeBase64Byte(mInputBytes[2], out int l63) ||
                        !ZTryDecodeBase64Byte(mInputBytes[3], out int l64)
                        )
                    {
                        rError = "invalid base64 byte";
                        return false;
                    }

                    mBytes.Add((byte)((l61 << 2) | (l62 >> 4)));
                    mBytes.Add((byte)(((l62 & 15) << 4) | (l63 >> 2)));
                    mBytes.Add((byte)(((l63 & 3) << 6) | l64));

                    mInputByteCount = 0;
                }

                rError = null;
                return true;
            }

            public bool TryFlush(out string rError)
            {
                if (mInputByteCount == 0) { rError = null; return true; }

                if (!ZTryDecodeBase64Byte(mInputBytes[0], out int l61) ||
                    !ZTryDecodeBase64Byte(mInputBytes[1], out int l62)
                    )
                {
                    rError = "invalid base64 byte";
                    return false;
                }

                mBytes.Add((byte)((l61 << 2) | (l62 >> 4)));

                if (mInputByteCount == 2)
                {
                    rError = null;
                    return true;
                }

                if (!ZTryDecodeBase64Byte(mInputBytes[2], out int l63))
                {
                    rError = "invalid base64 byte";
                    return false;
                }

                mBytes.Add((byte)(((l62 & 15) << 4) | (l63 >> 2)));

                rError = null;
                return true;
            }
        }

        [Conditional("DEBUG")]
        internal static void _Tests(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cBase64), nameof(_Tests));

            LCheck(
                "Man is distinguished, not only by his reason, but by this singular passion from other animals, which is a lust of the mind, that by a perseverance of delight in the continued and indefatigable generation of knowledge, exceeds the short vehemence of any carnal pleasure.",
                "TWFuIGlzIGRpc3Rpbmd1aXNoZWQsIG5vdCBvbmx5IGJ5IGhpcyByZWFzb24sIGJ1dCBieSB0aGlzIHNpbmd1bGFyIHBhc3Npb24gZnJvbSBvdGhlciBhbmltYWxzLCB3aGljaCBpcyBhIGx1c3Qgb2YgdGhlIG1pbmQsIHRoYXQgYnkgYSBwZXJzZXZlcmFuY2Ugb2YgZGVsaWdodCBpbiB0aGUgY29udGludWVkIGFuZCBpbmRlZmF0aWdhYmxlIGdlbmVyYXRpb24gb2Yga25vd2xlZGdlLCBleGNlZWRzIHRoZSBzaG9ydCB2ZWhlbWVuY2Ugb2YgYW55IGNhcm5hbCBwbGVhc3VyZS4=",
                lContext);

            LCheck("pleasure.", "cGxlYXN1cmUu", lContext);
            LCheck("leasure.", "bGVhc3VyZS4=", lContext);
            LCheck("easure.", "ZWFzdXJlLg==", lContext);
            LCheck("asure.", "YXN1cmUu", lContext);
            LCheck("sure.", "c3VyZS4=", lContext);

            lContext.TraceVerbose(new cBytes(Encode(new cBytes("\0fred\0angus"))).ToString());

            void LCheck(string pFrom, string pExpected, cTrace.cContext pContext)
            {
                cBytes lFrom = new cBytes(pFrom);
                cBytes lExpected = new cBytes(pExpected);
                cBytes lTo = new cBytes(Encode(lFrom));

                if (!TryDecode(lTo, out var lReturn, out _)) throw new cTestsException();

                var lResult = $"'{lFrom}'\t'{lTo}'\t'{lReturn}'";
                pContext.TraceVerbose(lResult);

                if (!cASCII.Compare(lFrom, lReturn, true)) throw new cTestsException($"base64 round trip failure {lResult}", pContext);
                if (!cASCII.Compare(lTo, lExpected, true)) throw new cTestsException($"base64 unexpected intermediate {lTo} vs {lExpected}", pContext);
            }
        }
    }
}