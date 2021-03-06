﻿using System.Collections.Generic;
using DelftTools.Utils.Collections.Extensions;
using NUnit.Framework;

namespace DelftTools.Tests.Utils.Collections.Extensions
{
    [TestFixture]
    public class ListExtensionsTest
    {
        [TestFixtureSetUp]
        public void FixtureSetup()
        {
        }

        [Test]
        public void TestThatWeCanAddARangeOfItemsToAnIList()
        {
            var a = new[] {1, 2, 3};
            IList<int> intList = new List<int>();
            intList.AddRange(a);
            Assert.AreEqual(3, intList.Count);
        }

        [Test]
        public void TestThatWeCanAddARangeOfItemsToAnIListLeavingNullItemsOut()
        {
            var a = new[] { new object(), null, new object() };
            IList<object> intList = new List<object>();
            intList.AddRangeLeavingNullElementsOut(a);
            Assert.AreEqual(2, intList.Count);
        }

        [Test]
        public void TestThatWeCanAddARangeOfItemsToAnIListConditionally()
        {
            var a = new[] { 1, 2, 3 };
            IList<int> intList = new List<int>();
            intList.AddRangeConditionally(a, i => i > 2);
            Assert.AreEqual(1, intList.Count);
        }


        [TestFixtureTearDown]
        public void FixtureTearDown()
        {
        }
    }
}