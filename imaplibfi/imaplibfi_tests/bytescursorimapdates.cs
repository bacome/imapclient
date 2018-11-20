using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using work.bacome.imapinternals;

namespace work.bacome.imapclient_tests
{
    [TestClass]
    public class Test_cBytesCursor_IMAPDates
    {
        [TestMethod]
        public void cBytesCursor_IMAPDate_Tests()
        {
            // RFC 3501 date-time

            var lCursor = new cBytesCursor("\" 4-apr-1968 23:59:59 +0000\"\"04-apr-1968 23:59:59 +1200\"\"28-apr-1968 23:59:59 +1130\"\"28-apr-1968 11:59:59 -1000\"\" 4-apr-1968 23:59:59 -0000\"");

            ZSkipDateTime(lCursor, new DateTime(1968, 4, 4, 23, 59, 59, DateTimeKind.Utc), false);
            ZSkipDateTime(lCursor, new DateTime(1968, 4, 4, 11, 59, 59, DateTimeKind.Utc), false);
            ZSkipDateTime(lCursor, new DateTime(1968, 4, 28, 12, 29, 59, DateTimeKind.Utc), false);
            ZSkipDateTime(lCursor, new DateTime(1968, 4, 28, 21, 59, 59, DateTimeKind.Utc), false);
            ZSkipDateTime(lCursor, new DateTime(1968, 4, 4, 23, 59, 59, DateTimeKind.Utc), true);

            // TODO ... edge cases


            // RFC 3339 date-time for URL expires (only) 

            lCursor = new cBytesCursor("1968-04-04T23:59:59Z,1968-04-04T23:59:59+12:00,1968-04-28T23:59:59+11:30,1968-04-28T11:59:59-10:00,1985-04-12T23:20:50.52-00:00");

            ZSkipTimestamp(lCursor, new DateTime(1968, 4, 4, 23, 59, 59, DateTimeKind.Utc), false);
            Assert.IsTrue(lCursor.SkipByte(cASCII.COMMA));
            ZSkipTimestamp(lCursor, new DateTime(1968, 4, 4, 11, 59, 59, DateTimeKind.Utc), false);
            Assert.IsTrue(lCursor.SkipByte(cASCII.COMMA));
            ZSkipTimestamp(lCursor, new DateTime(1968, 4, 28, 12, 29, 59, DateTimeKind.Utc), false);
            Assert.IsTrue(lCursor.SkipByte(cASCII.COMMA));
            ZSkipTimestamp(lCursor, new DateTime(1968, 4, 28, 21, 59, 59, DateTimeKind.Utc), false);
            Assert.IsTrue(lCursor.SkipByte(cASCII.COMMA));
            ZSkipTimestamp(lCursor, new DateTime(1985, 04, 12, 23, 20, 50, 520, DateTimeKind.Utc), true);

            // examples from rfc3339
            lCursor = new cBytesCursor("1985-04-12T23:20:50.52Z,1996-12-19T16:39:57-08:00,1990-12-31T23:59:60Z,1990-12-31T15:59:60-08:00,1937-01-01T12:00:27.87+00:20");

            ZSkipTimestamp(lCursor, new DateTime(1985, 04, 12, 23, 20, 50, 520, DateTimeKind.Utc), false);
            Assert.IsTrue(lCursor.SkipByte(cASCII.COMMA));
            ZSkipTimestamp(lCursor, new DateTime(1996, 12, 20, 00, 39, 57, DateTimeKind.Utc), false);
            Assert.IsTrue(lCursor.SkipByte(cASCII.COMMA));
            ZSkipTimestamp(lCursor, new DateTime(1990, 12, 31, 23, 59, 59, DateTimeKind.Utc), false);
            Assert.IsTrue(lCursor.SkipByte(cASCII.COMMA));
            ZSkipTimestamp(lCursor, new DateTime(1990, 12, 31, 23, 59, 59, DateTimeKind.Utc), false);
        }

        private void ZSkipDateTime(cBytesCursor pCursor, DateTime pExpectedDateTime, bool pUnknownLocalOffset)
        {
            Assert.IsTrue(pCursor.GetDateTime(out var lTimestamp));
            Assert.AreEqual(pExpectedDateTime, lTimestamp.UtcDateTime);
            Assert.AreEqual(pUnknownLocalOffset, lTimestamp.UnknownLocalOffset);
        }

        private void ZSkipTimestamp(cBytesCursor pCursor, DateTime pExpectedDateTime, bool pUnknownLocalOffset)
        {
            Assert.IsTrue(pCursor.GetTimeStamp(out var lTimestamp));
            Assert.AreEqual(pExpectedDateTime, lTimestamp.UtcDateTime);
            Assert.AreEqual(pUnknownLocalOffset, lTimestamp.UnknownLocalOffset);
        }
    }
}