using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using work.bacome.imapclient;
using work.bacome.imapsupport;

namespace work.bacome.imapclient_tests
{
    [TestClass]
    public class Test_cHeaderFieldNames
    {
        [TestMethod]
        public void cHeaderFieldNames_Basic_Tests()
        {
            cHeaderFieldNameList lNames1;
            cHeaderFieldNameList lNames2;
            cHeaderFieldNameList lNames3;
            cHeaderFieldNameList lNames4;

            Assert.IsTrue(cHeaderFieldNameList.TryConstruct(new string[] { }, out lNames1));
            Assert.AreEqual(0, lNames1.Count);

            Assert.IsTrue(cHeaderFieldNameList.TryConstruct(new string[] { "fred", "angus" }, out lNames1));
            lNames2 = new cHeaderFieldNameList("AnGuS", "ANGUS", "FrEd");
            Assert.IsTrue(lNames1.Contains(lNames2));
            Assert.IsTrue(lNames2.Contains(lNames1));
            Assert.AreEqual((cHeaderFieldNames)lNames1, lNames2);

            lNames3 = new cHeaderFieldNameList("fred", "charlie");
            lNames4 = new cHeaderFieldNameList("CHARLie", "mAx");
            Assert.IsFalse(lNames3.Contains(lNames4));
            Assert.IsFalse(lNames4.Contains(lNames3));
            Assert.AreNotEqual((cHeaderFieldNames)lNames3, lNames4);

            Assert.IsFalse(lNames2.Contains("max"));
            Assert.IsTrue(lNames2.Contains("FREd"));
            Assert.IsTrue(lNames4.Contains("max"));
            Assert.IsFalse(lNames4.Contains("FREd"));

            lNames2 = new cHeaderFieldNameList(lNames1);
            lNames2.Add("fReD");
            Assert.AreEqual((cHeaderFieldNames)lNames1, lNames2);
            lNames2.Add("charlie");
            Assert.AreNotEqual((cHeaderFieldNames)lNames1, lNames2);
            Assert.AreEqual(3, lNames2.Count);
            Assert.IsTrue(lNames2.Contains("Fred"));
            Assert.IsTrue(lNames2.Contains("ANgUS"));
            Assert.IsTrue(lNames2.Contains("CHArLIE"));

            var lNames5 = lNames1.Union(lNames3); // fred, angus union fred charlie
            Assert.AreEqual((cHeaderFieldNames)lNames2, lNames5);

            lNames2 = lNames1.Intersect(lNames3); // fred angus intersect fred charlie
            Assert.AreEqual(1, lNames2.Count);
            Assert.IsTrue(lNames2.Contains("fReD"));

            lNames2 = lNames5.Except(lNames4); // fred, angus, charlie except charlie max
            Assert.AreEqual(2, lNames2.Count);
            Assert.AreEqual((cHeaderFieldNames)lNames1, lNames2);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void cHeaderFieldNames_InvalidFieldName1()
        {
            cHeaderFieldNames lF = new cHeaderFieldNames("dd ff");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void cHeaderFieldNames_InvalidFieldName2()
        {
            cHeaderFieldNames lF = new cHeaderFieldNames("dd:ff");
        }
    }
}