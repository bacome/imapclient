using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using work.bacome.imapsupport;

namespace work.bacome.imapinternals
{
    public static class cModifiedUTF7
    {
        private static readonly ReadOnlyCollection<byte> kEncode = new cBytes("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+,");

        // these are ints because the shift operator works on ints
        private static readonly ReadOnlyCollection<int> kDecode = Array.AsReadOnly(new int[]
            {
                -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, // 0-9
                -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, // 10-19
                -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, // 20-29
                -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, // 30-39
                -1, -1, -1, // 40, 41, 42
                62, 63, // 43, 44 = PLUS, COMMA
                -1, -1, -1, // 45, 46, 47, 
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

        public static cByteList Encode(string pString)
        {
            if (pString.Length == 0) return new cByteList();

            var lBytes = new cByteList();
            var lBuffer = new cToBase64Buffer(lBytes);
            bool lInBase64 = false;

            foreach (char lChar in pString)
            {
                if (lChar < ' ' || lChar > '~')
                {
                    if (!lInBase64)
                    {
                        lBytes.Add(cASCII.AMPERSAND);
                        lInBase64 = true;
                    }

                    byte[] lCharBytes = BitConverter.GetBytes(lChar);

                    if (BitConverter.IsLittleEndian)
                    {
                        lBuffer.Add(lCharBytes[1]);
                        lBuffer.Add(lCharBytes[0]);
                    }
                    else
                    {
                        lBuffer.Add(lCharBytes[0]);
                        lBuffer.Add(lCharBytes[1]);
                    }
                }
                else
                {
                    if (lInBase64)
                    {
                        lBuffer.Flush();
                        lBytes.Add(cASCII.HYPEN);
                        lInBase64 = false;
                    }

                    lBytes.Add((byte)lChar);
                    if (lChar == '&') lBytes.Add(cASCII.HYPEN);
                }
            }

            if (lInBase64)
            {
                lBuffer.Flush();
                lBytes.Add(cASCII.HYPEN);
                lInBase64 = false;
            }

            return lBytes;
        }

        public static bool TryDecode(IList<byte> pBytes, out string rString, out string rError)
        {
            if (pBytes.Count == 0)
            {
                rString = string.Empty;
                rError = null;
                return true;
            }

            var lBuilder = new StringBuilder();
            var lBuffer = new cFromBase64Buffer(lBuilder);

            int lByteNumber = 1;
            byte lByte = pBytes[0];

            while (true)
            {
                if (lByte == cASCII.AMPERSAND)
                {
                    if (lByteNumber == pBytes.Count)
                    {
                        rString = null;
                        rError = "ends with an ampersand";
                        return false;
                    }

                    lByte = pBytes[lByteNumber++];

                    if (lByte == cASCII.HYPEN) lBuilder.Append('&');
                    else
                    {
                        // base64 loop

                        while (true)
                        {
                            if (lByteNumber + 2 > pBytes.Count)
                            {
                                rString = null;
                                rError = "invalid base64 character sequence";
                                return false;
                            }

                            if (!ZTryDecodeBase64Byte(lByte, out int l61) ||
                                !ZTryDecodeBase64Byte(pBytes[lByteNumber++], out int l62)
                                )
                            {
                                rString = null;
                                rError = "invalid base64 character";
                                return false;
                            }

                            lBuffer.Add((l61 << 2) | (l62 >> 4));

                            lByte = pBytes[lByteNumber++];

                            if (lByte == cASCII.HYPEN)
                            {
                                if (lBuffer.ContainsAByte)
                                {
                                    rString = null;
                                    rError = "odd number of encoded bytes";
                                    return false;
                                }

                                break;
                            }

                            if (lByteNumber + 1 > pBytes.Count)
                            {
                                rString = null;
                                rError = "invalid base64 character sequence";
                                return false;
                            }

                            if (!ZTryDecodeBase64Byte(lByte, out int l63))
                            {
                                rString = null;
                                rError = "invalid base64 character";
                                return false;
                            }

                            lBuffer.Add(((l62 & 15) << 4) | (l63 >> 2));

                            lByte = pBytes[lByteNumber++];

                            if (lByte == cASCII.HYPEN)
                            {
                                if (lBuffer.ContainsAByte)
                                {
                                    rString = null;
                                    rError = "odd number of encoded bytes";
                                    return false;
                                }

                                break;
                            }

                            if (lByteNumber + 1 > pBytes.Count)
                            {
                                rString = null;
                                rError = "invalid base64 character sequence";
                                return false;
                            }

                            if (!ZTryDecodeBase64Byte(lByte, out int l64))
                            {
                                rString = null;
                                rError = "invalid base64 character";
                                return false;
                            }

                            lBuffer.Add(((l63 & 3) << 6) | l64);

                            lByte = pBytes[lByteNumber++];

                            if (lByte == cASCII.HYPEN)
                            {
                                if (lBuffer.ContainsAByte)
                                {
                                    rString = null;
                                    rError = "odd number of encoded bytes";
                                    return false;
                                }

                                break;
                            }
                        }
                    }
                }
                else
                {
                    if (lByte < cASCII.SPACE || lByte > cASCII.TILDA)
                    {
                        rString = null;
                        rError = "invalid unencoded byte";
                        return false;
                    }

                    lBuilder.Append((char)lByte);
                }

                if (lByteNumber == pBytes.Count)
                {
                    rString = lBuilder.ToString();
                    rError = null;
                    return true;
                }

                lByte = pBytes[lByteNumber++];
            }
        }

        private static bool ZTryDecodeBase64Byte(byte pByte, out int rByte)
        {
            rByte = kDecode[pByte];
            if (rByte == -1) return false;
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
                if (mInputByteCount == 3) Flush();
                mInputBytes[mInputByteCount++] = pByte;
            }

            public void Flush()
            {
                if (mInputByteCount < 3) mInputBytes[mInputByteCount] = 0;

                mBytes.Add(kEncode[mInputBytes[0] >> 2]);
                mBytes.Add(kEncode[((mInputBytes[0] & 3) << 4) | (mInputBytes[1] >> 4)]);

                if (mInputByteCount != 1)
                {
                    mBytes.Add(kEncode[((mInputBytes[1] & 15) << 2) | (mInputBytes[2] >> 6)]);
                        
                    if (mInputByteCount == 3)
                    {
                        mBytes.Add(kEncode[mInputBytes[2] & 63]);
                    }
                }

                mInputByteCount = 0;
                return;
            }
        }

        private class cFromBase64Buffer
        {
            private StringBuilder mBuilder;
            private byte mByte;
            private byte[] mChar = new byte[2];

            public cFromBase64Buffer(StringBuilder pBuilder)
            {
                mBuilder = pBuilder;
            }

            public void Add(int pByte)
            {
                if (ContainsAByte)
                {
                    if (BitConverter.IsLittleEndian)
                    {
                        mChar[0] = (byte)pByte;
                        mChar[1] = mByte;
                    }
                    else
                    {
                        mChar[0] = mByte;
                        mChar[1] = (byte)pByte;
                    }

                    char lChar = BitConverter.ToChar(mChar, 0);
                    mBuilder.Append(lChar);
                    ContainsAByte = false;
                }
                else
                {
                    mByte = (byte)pByte;
                    ContainsAByte = true;
                }
            }

            public bool ContainsAByte { get; private set; } = false;
        }
    }
}