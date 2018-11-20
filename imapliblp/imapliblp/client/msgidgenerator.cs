using System;
using System.Text;

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

        public static string MessageId()
        {
            var lBuilder = new StringBuilder();

            lBuilder.Append("<");

            ZMessageIdWorker((ulong)DateTime.Now.Ticks, lBuilder);

            lBuilder.Append(".");

            byte[] lRandomBytes = new byte[8];
            mRandom.NextBytes(lRandomBytes);
            ZMessageIdWorker(BitConverter.ToUInt64(lRandomBytes, 0), lBuilder);

            lBuilder.Append("@");

            lBuilder.Append(Environment.MachineName);

            lBuilder.Append(">");

            return lBuilder.ToString();
        }

        private static void ZMessageIdWorker(ulong pNumber, StringBuilder pBuilder)
        {
            ulong lNumber = pNumber;

            do
            {
                int lChar = (int)(lNumber % 36);
                pBuilder.Append(kChars[lChar]);
                lNumber = lNumber / 36;
            } while (lNumber > 0);
        }
    }
}
