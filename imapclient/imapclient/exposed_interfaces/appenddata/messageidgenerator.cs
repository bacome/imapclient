using System;
using System.Collections.Generic;

namespace work.bacome.imapclient
{
    public static class cMessageIdGenerator
    {
        private static readonly char[] kChars =
            new char[]
            {
                '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
                'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J',
                'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T',
                'U', 'V', 'W', 'X', 'Y', 'Z'
            };

        private static readonly Random mRandom = new Random();

        public static string IdLeft()
        {
            List<char> lChars = new List<char>(27);

            lChars.AddRange(ZBase36Reverse((ulong)DateTime.Now.Ticks));

            lChars.Add('.');

            byte[] lRandomBytes = new byte[8];
            mRandom.NextBytes(lRandomBytes);

            lChars.AddRange(ZBase36Reverse(BitConverter.ToUInt64(lRandomBytes, 0)));

            return new string(lChars.ToArray());
        }

        public static string IdRight() => Environment.MachineName;

        public static string MsgId() => "<" + IdLeft() + "@" + IdRight() + ">";

        private static List<char> ZBase36Reverse(ulong pNumber)
        {
            List<char> lChars = new List<char>(13);

            ulong lNumber = pNumber;

            do
            {
                int lChar = (int)(lNumber % 36);
                lChars.Add(kChars[lChar]);
                lNumber = lNumber / 36;
            } while (lNumber > 0);

            return lChars;
        }
    }
}
