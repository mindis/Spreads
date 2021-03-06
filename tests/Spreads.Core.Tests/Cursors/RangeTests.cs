﻿// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using NUnit.Framework;
using Spreads.Collections;
using System;
using System.Linq;
using System.Runtime;

namespace Spreads.Core.Tests.Cursors
{
    [TestFixture]
    public class RangeTests
    {
        [Test]
        public void CouldUseRangeValues()
        {
            SortedMap<int, double> sm = null;

            Assert.Throws<NullReferenceException>(() =>
            {
                var nullRange = sm.Range(0, Int32.MaxValue, true, true);
            });

            var empty = new SortedMap<int, double>();

            var range = empty.Range(0, Int32.MaxValue, true, true);
            Assert.False(range.Any());

            var nonEmpty = new SortedMap<int, double>
            {
                {1, 1}
            };
            var range1 = nonEmpty.Range(0, Int32.MaxValue, true, true);
            Assert.True(range1.Any());
        }

        [Test]
        public void CouldReuseRangeValuesCursor()
        {
            IReadOnlySeries<int, double> nonEmpty = new SortedMap<int, double>
            {
                {1, 1},
                {2, 2}
            };
            var range1 = nonEmpty.Range(0, Int32.MaxValue, true, true);

            //var e = range1.GetEnumerator();

            foreach (var keyValuePair in range1)
            {
                Assert.True(keyValuePair.Value > 0);
            }

            Console.WriteLine("Foreach is OK");

            Assert.True(range1.Count() > 0);

            Console.WriteLine("Count is OK");

            Assert.True(range1.Any());

            Console.WriteLine("Any is OK");

            Assert.True(range1.First.Value > 0);

            Console.WriteLine("Navigation is OK");
        }
    }
}