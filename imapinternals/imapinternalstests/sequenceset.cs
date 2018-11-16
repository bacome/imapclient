using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using work.bacome.imapclient;
using work.bacome.imapsupport;

namespace work.bacome.imapinternalstests
{
    [TestClass]
    public class cSequenceSetTests
    {
        [TestMethod]
        public void cSequenceSetIncludes_Tests()
        {
            ZTest_Includes(new uint[] { 1, 2, 3, 7, 11, 12, 13, 17, 21, 22, 23, 27 }, new uint[] { 1, 2, 3, 7, 11, 12, 13, 17, 21, 22, 23, 27 }, new uint[] { 4, 5, 6, 8, 9, 10, 14, 15, 16, 18, 19, 20, 24, 25, 26, 28 });

            var lSet1 = new cSequenceSet(
                new cSequenceSetItem[] {
                    new cSequenceSetRange(new cSequenceSetNumber(5), new cSequenceSetNumber(10)),
                    new cSequenceSetNumber(13),
                    new cSequenceSetRange(new cSequenceSetNumber(15), new cSequenceSetNumber(20)),
                    new cSequenceSetRange(new cSequenceSetNumber(25), cSequenceSetItem.Asterisk) });

            ZTest_Includes(lSet1, 25, new uint[] { 5, 6, 7, 8, 9, 10, 13, 15, 16, 17, 18, 19, 20, 25 }, new uint[] { 1, 2, 3, 4, 11, 12, 14, 21, 22, 23, 24, 26 });
            ZTest_Includes(lSet1, 30, new uint[] { 5, 6, 7, 8, 9, 10, 13, 15, 16, 17, 18, 19, 20, 25, 26, 27, 28, 29, 30 }, new uint[] { 1, 2, 3, 4, 11, 12, 14, 21, 22, 23, 24, 31 });

            var lSet2 = new cSequenceSet(
                new cSequenceSetItem[] {
                    new cSequenceSetRange(new cSequenceSetNumber(5), new cSequenceSetNumber(10)),
                    new cSequenceSetNumber(13),
                    new cSequenceSetRange(new cSequenceSetNumber(15), new cSequenceSetNumber(20)),
                    cSequenceSetItem.Asterisk });

            ZTest_Includes(lSet2, 25, new uint[] { 5, 6, 7, 8, 9, 10, 13, 15, 16, 17, 18, 19, 20, 25 }, new uint[] { 1, 2, 3, 4, 11, 12, 14, 21, 22, 23, 24, 26 });
            ZTest_Includes(lSet2, 30, new uint[] { 5, 6, 7, 8, 9, 10, 13, 15, 16, 17, 18, 19, 20, 30 }, new uint[] { 1, 2, 3, 4, 11, 12, 14, 21, 22, 23, 24, 25, 26, 27, 28, 29, 31 });
        }

        private void ZTest_Includes(IEnumerable<uint> pUInts, IEnumerable<uint> pIncludes, IEnumerable<uint> pExcludes) => ZTest_Includes(cSequenceSet.FromUInts(1000, pUInts), 0, pIncludes, pExcludes);

        private void ZTest_Includes(cSequenceSet pSequenceSet, uint pAsterisk, IEnumerable<uint> pIncludes, IEnumerable<uint> pExcludes)
        {
            foreach (var lUInt in pIncludes) Assert.IsTrue(pSequenceSet.Includes(lUInt, pAsterisk));
            foreach (var lUInt in pExcludes) Assert.IsFalse(pSequenceSet.Includes(lUInt, pAsterisk));
        }


        [TestMethod]
        public void cLimitingFactory_Tests()
        {
            ZTestLimitingFactory(1000, new uint[] { 1, 2, 3, 4 }, "1:4");
            ZTestLimitingFactory(1000, new uint[] { 1, 2, 3, 4, 7, 8, 9 }, "1:4,7:9");
            ZTestLimitingFactory(
                1000,
                new uint[] { 2000000000, 2000000002, 2000000004, 2000000006, 2000000008, 2000000010, 2000000012, 2000000014, 2000000016, 2000000018 },
                "2000000000,2000000002,2000000004,2000000006,2000000008,2000000010,2000000012,2000000014,2000000016,2000000018");

            ZTestLimitingFactory(
                99,
                new uint[] { 2000000000, 2000000002, 2000000004, 2000000006, 2000000008, 2000000010, 2000000012, 2000000014, 2000000016, 2000000018 },
                "2000000000,2000000002,2000000004,2000000006,2000000008,2000000010,2000000012,2000000014,2000000016,2000000018");

            ZTestLimitingFactory(
                98,
                new uint[] { 2000000000, 2000000002, 2000000004, 2000000006, 2000000008, 2000000010, 2000000012, 2000000014, 2000000016, 2000000018 },
                "2000000000,2000000002,2000000004,2000000006,2000000008,2000000010,2000000012,2000000014,2000000016");

            ZTestLimitingFactory(
                99,
                new uint[] { 2000000000, 2000000001, 2000000002, 2000000004, 2000000005, 2000000006, 2000000008, 2000000010, 2000000012, 2000000014, 2000000016, 2000000018 },
                "2000000000:2000000002,2000000004:2000000006,2000000008,2000000010,2000000012,2000000014,2000000016,2000000018");


            ZTestLimitingFactory(
                98,
                new uint[] { 1000000000, 1000000001, 1000000002, 2000000004, 2000000005, 2000000006, 2000000008, 2000000010, 2000000012, 2000000014, 2000000016, 2000000018 },
                "1000000000:1000000002,2000000004:2000000006,2000000008,2000000010,2000000012,2000000014,2000000016");


            ZTestLimitingFactory(
                98,
                new uint[] { 999999999, 1000000000, 1000000001, 1000000002, 2000000004, 2000000005, 2000000006, 2000000008, 2000000010, 2000000012, 2000000014, 2000000016, 2000000018 },
                "999999999:1000000002,2000000004:2000000006,2000000008,2000000010,2000000012,2000000014,2000000016,2000000018");


            /*
                        ZTestLimitingFactory(
                            99,
                            new uint[] { 2000000000, 2000000001, 2000000002, 2000000004, 2000000005, 2000000006, 2000000008, 2000000010, 2000000012, 2000000014, 2000000016, 2000000018 },
                            "cSequenceSet(cSequenceSetNumber(2000000000),cSequenceSetNumber(2000000002),cSequenceSetNumber(2000000004),cSequenceSetNumber(2000000006),cSequenceSetNumber(2000000008),cSequenceSetNumber(2000000010),cSequenceSetNumber(2000000012),cSequenceSetNumber(2000000014),cSequenceSetNumber(2000000016),cSequenceSetNumber(2000000018))");
                            */
        }

        private void ZTestLimitingFactory(int pASCIILengthLimit, IEnumerable<uint> pUInts, string pExpectedSequenceSet)
        {
            var lLimitingFactory = new cSequenceSet.cLimitingFactory(pASCIILengthLimit);
            foreach (var lUInt in pUInts) if (!lLimitingFactory.Add(lUInt)) break;
            Assert.AreEqual(pExpectedSequenceSet, lLimitingFactory.SequenceSet.ToCompactString(), false, "limiting factory");
        }

        [TestMethod]
        public void cSequenceSetFromUInts_Tests()
        {
            ZTestFromUInts(1000, new uint[] { 1, 2, 3, 4 }, "1:4");
            ZTestFromUInts(1000, new uint[] { 1, 2, 3, 4, 7, 8, 9 }, "1:4,7:9");
            ZTestFromUInts(
                1000,
                new uint[] { 2000000000, 2000000002, 2000000004, 2000000006, 2000000008, 2000000010, 2000000012, 2000000014, 2000000016, 2000000018 },
                "2000000000,2000000002,2000000004,2000000006,2000000008,2000000010,2000000012,2000000014,2000000016,2000000018");

            ZTestFromUInts(
                109,
                new uint[] { 2000000000, 2000000002, 2000000004, 2000000006, 2000000008, 2000000010, 2000000012, 2000000014, 2000000016, 2000000018 },
                "2000000000,2000000002,2000000004,2000000006,2000000008,2000000010,2000000012,2000000014,2000000016,2000000018");

            ZTestFromUInts(
                108,
                new uint[] { 2000000000, 2000000002, 2000000004, 2000000006, 2000000008, 2000000010, 2000000012, 2000000014, 2000000016, 2000000018 },
                "2000000000,2000000002,2000000004,2000000006,2000000008,2000000010,2000000012,2000000014:2000000018");

            // range range

            ZTestFromUInts(
                23,
                new uint[] { 10, 11, 12, 30, 31, 32, 40, 41, 42, 60, 61, 62 },
                "10:12,30:32,40:42,60:62"
                );

            ZTestFromUInts(
                22,
                new uint[] { 10, 11, 12, 30, 31, 32, 40, 41, 42, 60, 61, 62 },
                "10:12,30:42,60:62"
                );

            // range single


            ZTestFromUInts(
                26,
                new uint[] { 10, 11, 12, 19, 40, 41, 42, 48, 70, 71, 72, 79 },
                "10:12,19,40:42,48,70:72,79"
                );

            ZTestFromUInts(
                25,
                new uint[] { 10, 11, 12, 19, 40, 41, 42, 48, 70, 71, 72, 79 },
                "10:12,19,40:48,70:72,79"
                );


            // single range

            ZTestFromUInts(
                25,
                new uint[] { 2, 10, 11, 12, 33, 40, 41, 42, 62, 70, 71, 72 },
                "2,10:12,33,40:42,62,70:72"
                );

            ZTestFromUInts(
                24,
                new uint[] { 2, 10, 11, 12, 33, 40, 41, 42, 62, 70, 71, 72 },
                "2,10:12,33:42,62,70:72"
                );

            // single single single

            ZTestFromUInts(
                31,
                new uint[] { 5, 10, 15, 20, 25, 27, 30, 35, 40, 45, 50 },
                "5,10,15,20,25,27,30,35,40,45,50"
                );

            ZTestFromUInts(
                30,
                new uint[] { 5, 10, 15, 20, 25, 27, 30, 35, 40, 45, 50 },
                "5,10,15,20,25:30,35,40,45,50"
                );

            // cascades

            ZTestFromUInts(
                40,
                new uint[] { 5, 10, 15, 20, 25, 26, 27, 28, 29, 30, 35, 36, 37, 38, 39, 40, 45, 50, 55, 60, 65, 70 },
                "5,10,15,20,25:30,35:40,45,50,55,60,65,70"
                );

            ZTestFromUInts(
                39,
                new uint[] { 5, 10, 15, 20, 25, 26, 27, 28, 29, 30, 35, 36, 37, 38, 39, 40, 45, 50, 55, 60, 65, 70 },
                "5,10,15,20,25:40,45,50,55,60,65,70"
                );

            ZTestFromUInts(
                34,
                new uint[] { 5, 10, 15, 20, 25, 26, 27, 28, 29, 30, 35, 36, 37, 38, 39, 40, 45, 50, 55, 60, 65, 70 },
                "5,10,15,20,25:40,45,50,55,60,65,70"
                );

            ZTestFromUInts(
                33,
                new uint[] { 5, 10, 15, 20, 25, 26, 27, 28, 29, 30, 35, 36, 37, 38, 39, 40, 45, 50, 55, 60, 65, 70 },
                "5,10,15,20,25:45,50,55,60,65,70"
                );

            ZTestFromUInts(
                31,
                new uint[] { 5, 10, 15, 20, 25, 26, 27, 28, 29, 30, 35, 36, 37, 38, 39, 40, 45, 50, 55, 60, 65, 70 },
                "5,10,15,20,25:45,50,55,60,65,70"
                );

            ZTestFromUInts(
                30,
                new uint[] { 5, 10, 15, 20, 25, 26, 27, 28, 29, 30, 35, 36, 37, 38, 39, 40, 45, 50, 55, 60, 65, 70 },
                "5,10,15,20,25:50,55,60,65,70"
                );

            // length

            ZTestFromUInts(
                58,
                new uint[] { 5, 10, 15, 20, 25, 26, 27, 28, 29, 30, 35, 36, 37, 38, 39, 40, 45, 50, 55, 60, 65, 70, 10000, 10003, 10006 },
                "5,10,15,20,25:30,35:40,45,50,55,60,65,70,10000,10003,10006"
                );

            ZTestFromUInts(
                57,
                new uint[] { 5, 10, 15, 20, 25, 26, 27, 28, 29, 30, 35, 36, 37, 38, 39, 40, 45, 50, 55, 60, 65, 70, 10000, 10003, 10006 },
                "5,10,15,20,25:30,35:40,45,50,55,60,65,70,10000:10006"
                );

            ZTestFromUInts(
                52,
                new uint[] { 5, 10, 15, 20, 25, 26, 27, 28, 29, 30, 35, 36, 37, 38, 39, 40, 45, 50, 55, 60, 65, 70, 10000, 10003, 10006 },
                "5,10,15,20,25:30,35:40,45,50,55,60,65,70,10000:10006"
                );

            ZTestFromUInts(
                51,
                new uint[] { 5, 10, 15, 20, 25, 26, 27, 28, 29, 30, 35, 36, 37, 38, 39, 40, 45, 50, 55, 60, 65, 70, 10000, 10003, 10006 },
                "5,10,15,20,25:40,45,50,55,60,65,70,10000:10006"
                );


            ZTestFromUInts(
                68,
                new uint[] { 15, 16, 17, 18, 19, 20, 21, 25, 26, 27, 28, 29, 30, 31, 35, 36, 37, 38, 39, 40, 45, 50, 55, 60, 65, 70, 1000000000, 1000000003, 1000000006 },
                "15:21,25:31,35:40,45,50,55,60,65,70,1000000000,1000000003,1000000006"
                );

            ZTestFromUInts(
                67,
                new uint[] { 15, 16, 17, 18, 19, 20, 21, 25, 26, 27, 28, 29, 30, 31, 35, 36, 37, 38, 39, 40, 45, 50, 55, 60, 65, 70, 1000000000, 1000000003, 1000000006 },
                "15:21,25:40,45,50,55,60,65,70,1000000000,1000000003,1000000006"
                );

            ZTestFromUInts(
                62,
                new uint[] { 15, 16, 17, 18, 19, 20, 21, 25, 26, 27, 28, 29, 30, 31, 35, 36, 37, 38, 39, 40, 45, 50, 55, 60, 65, 70, 1000000000, 1000000003, 1000000006 },
                "15:21,25:40,45,50,55,60,65,70,1000000000,1000000003,1000000006"
                );

            //  "15:21,25:31,35:40,45,50,55,60,65,70,1000000000:1000000006"

            ZTestFromUInts(
                61,
                new uint[] { 15, 16, 17, 18, 19, 20, 21, 25, 26, 27, 28, 29, 30, 31, 35, 36, 37, 38, 39, 40, 45, 50, 55, 60, 65, 70, 1000000000, 1000000003, 1000000006 },
                "15:40,45,50,55,60,65,70,1000000000,1000000003,1000000006"
                );

            /*
            ZTestFromUInts(
                72,
                new uint[] { 5, 10, 15, 20, 25, 26, 27, 28, 29, 30, 31, 35, 36, 37, 38, 39, 40, 45, 50, 55, 60, 65, 70, 1000000000, 1000000003, 1000000006 },
                "5,10,15,20,25:40,45,50,55,60,65,70,1000000000,1000000003,1000000006"
                );

            ZTestFromUInts(
                67,
                new uint[] { 5, 10, 15, 20, 25, 26, 27, 28, 29, 30, 31, 35, 36, 37, 38, 39, 40, 45, 50, 55, 60, 65, 70, 1000000000, 1000000003, 1000000006 },
                "5,10,15,20,25:40,45,50,55,60,65,70,1000000000,1000000003,1000000006"
                );

            ZTestFromUInts(
                66,
                new uint[] { 5, 10, 15, 20, 25, 26, 27, 28, 29, 30, 31, 35, 36, 37, 38, 39, 40, 45, 50, 55, 60, 65, 70, 1000000000, 1000000003, 1000000006 },
                "5,10,15,20,25:31,35:40,45,50,55,60,65,70,1000000000:1000000006"
                ); */


            //
        }

        private void ZTestFromUInts(int pASCIILengthLimit, IEnumerable<uint> pUInts, string pExpectedSequenceSet)
        {
            Assert.AreEqual(pExpectedSequenceSet, cSequenceSet.FromUInts(pASCIILengthLimit, pUInts).ToCompactString(), false, "from uints");
        }

        [TestMethod]
        public void cSequenceSetFromUInts_Visual_Tests()
        {
            var lContext = kTrace.Root.NewMethod(nameof(cSequenceSetTests), nameof(cSequenceSetFromUInts_Visual_Tests));
            for (int i = 0; i < 100; i++) ZTestRandom(1, 99, 50, lContext);
        }


        [TestMethod]
        public void cSequenceSetFromUInts_Speed_Test()
        {
            var lContext = cTrace.cContext.None;
            for (int i = 0; i < 100; i++) ZTestRandom(1000, 100000, 40000, lContext);
        }

        private void ZTestRandom(int pMinValue, int pMaxValue, uint pCount, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cSequenceSetTests), nameof(ZTestRandom), pMinValue, pMaxValue, pCount);

            var lUInts = new List<uint>();

            var lRandom = new Random();
            for (int i = 0; i < pCount; i++) lUInts.Add((uint)lRandom.Next(pMinValue, pMaxValue));

            var lInitial = cSequenceSet.FromUInts(int.MaxValue, lUInts).ToCompactString();
            lContext.TraceVerbose("initial:{0}", lInitial);

            var lFinal = cSequenceSet.FromUInts(Math.Min(lInitial.Length / 2, 1000), lUInts);

            foreach (var lUInt in lUInts) Assert.IsTrue(lFinal.Includes(lUInt, 0));

            lContext.TraceVerbose("final:{0}", lFinal.ToCompactString());
        }
    }
}

