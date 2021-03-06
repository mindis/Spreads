﻿// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using NUnit.Framework;
using Spreads.Collections;
using Spreads.Cursors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Spreads.Core.Tests.Cursors
{
    [TestFixture]
    public class MapValuesTests
    {
        [Test]
        public void CouldMapValues()
        {
            var sm = new SortedMap<int, double>
            {
                { 1, 1 }
            };

            var map = new MapValuesSeries<int, double, double, SortedMapCursor<int, double>>(sm, i => i * 2);
            var map1 = new MapValuesSeries<int, double, double, MapValuesSeries<int, double, double, SortedMapCursor<int, double>>>(map, i => i * 2);
            var map2 = new MapValuesSeries<int, double, double, ICursor<int, double>>(map.Range(0, Int32.MaxValue, true, true), i => i * 2);
            var map3 = new MapValuesSeries<int, double, double, ICursor<int, double>>(map.Range(2, Int32.MaxValue, true, true), i => i * 2);

            Assert.AreEqual(2, map.First.Value);
            Assert.AreEqual(4, map1.First.Value);
            Assert.AreEqual(4, map2.First.Value);
            Assert.True(map3.IsEmpty);
        }

        [Test, Ignore]
        public void CouldMapValuesBenchmark()
        {
            var sm = new SortedMap<int, double>();
            var count = 10000000;
            sm.AddLast(0, 0);
            for (int i = 2; i < count; i++)
            {
                sm.AddLast(i, i);
            }

            for (int r = 0; r < 10; r++)
            {
                var sw = new Stopwatch();
                sw.Restart();
                var map = new MapValuesSeries<int, double, double, SortedMapCursor<int, double>>(sm, i => i * 2);
                var map2 = new MapValuesSeries<int, double, double, MapValuesSeries<int, double, double, SortedMapCursor<int, double>>>(map, i => i * 2);
                var sum = 0.0;
                foreach (var kvp in map2)
                {
                    sum += kvp.Value;
                }
                sw.Stop();
                Assert.IsTrue(sum > 0);

                Console.WriteLine($"Mops {sw.MOPS(count)}");
            }

            //for (int r = 0; r < 10; r++)
            //{
            //    var sw = new Stopwatch();
            //    sw.Restart();
            //    var map = sm
            //        //.Select(x => new KeyValuePair<int, double>(x.Key, x.Value * 2))
            //        .Select(x => new KeyValuePair<int, double>(x.Key, x.Value * 2));
            //    var sum = 0.0;
            //    foreach (var kvp in map)
            //    {
            //        sum += kvp.Value;
            //    }
            //    sw.Stop();
            //    Assert.IsTrue(sum > 0);

            //    Console.WriteLine($"LINQ Mops {sw.MOPS(count)}");
            //}
        }

        [Test]
        public void CouldMapValuesViaExtensionMethods()
        {
            var sm = new SortedMap<int, int>
            {
                { 1, 1 }
            };

            var map = sm.Map(i => i * 2);
            var map11 = map.Map(i => i * 2);
            var map111 = map11.Map(i => i * 2);
            var map2 = map.Range(0, Int32.MaxValue, true, true).Map(i => i * 2).Map(i => i * 2);
            Assert.AreEqual(2, map.First.Value);
            Assert.AreEqual(2 * 2, map11.First.Value);
            Assert.AreEqual(2 * 2 * 2, map111.First.Value);
            Assert.AreEqual(8, map2.First.Value);
        }

        [Test, Ignore]
        public void CouldMapValuesViaExtensionMethodsBenchmark()
        {
            var sm = new SortedMap<int, int>();
            var count = 10000000;
            for (int i = 0; i < count; i++)
            {
                sm.AddLast(i, i);
            }

            for (int r = 0; r < 10; r++)
            {
                var sw = new Stopwatch();
                sw.Restart();
                var map = sm.Map(i => i * 2).Range(0, int.MaxValue, true, true).Map(i => i * 2).Map(i => i * 2);
                long sum = 0;
                foreach (var kvp in map)
                {
                    sum += kvp.Value;
                }
                sw.Stop();
                Assert.IsTrue(sum > 0);

                Console.WriteLine($"Mops {sw.MOPS(count)}");
            }

            for (int r = 0; r < 10; r++)
            {
                var sw = new Stopwatch();
                sw.Restart();
                var map = sm
                    .Select(x => new KeyValuePair<int, int>(x.Key, x.Value * 2))
                    .Select(x => new KeyValuePair<int, int>(x.Key, x.Value * 2))
                    .Select(x => new KeyValuePair<int, int>(x.Key, x.Value * 2));
                long sum = 0;
                foreach (var kvp in map)
                {
                    sum += kvp.Value;
                }
                sw.Stop();
                Assert.IsTrue(sum > 0);

                Console.WriteLine($"LINQ Mops {sw.MOPS(count)}");
            }
        }


        [Test]
        public void CouldMapRangeSeriesViaExtensionMethods()
        {
            var sm = new SortedMap<int, int>
            {
                { 1, 1 }
            };

            var range = sm.Range(0, int.MaxValue, true, true);
            var map = range.Map(i => i * 2);
            var range2 = map.Range(0, int.MaxValue, true, true);
            var map2 = range2.Map(i => i * 2);
            var range3 = map2.Range(0, int.MaxValue, true, true);
            var map3 = range3.Map(i => i * 2);
            Assert.AreEqual(2, map.First.Value);
            Assert.AreEqual(4, map2.First.Value);
            Assert.AreEqual(8, map3.First.Value);
        }


        [Test, Ignore]
        public void CouldMapRangeSeriesViaExtensionMethodsBenchmark()
        {
            var sm = new SortedMap<int, int>();
            var count = 10000000;
            for (int i = 0; i < count; i++)
            {
                sm.AddLast(i, i);
            }

            for (int r = 0; r < 10; r++)
            {
                var sw = new Stopwatch();
                sw.Restart();
                var range = sm.After(0);
                var map = sm.Map(i => i * 2);
                //var range2 = map.Range(0, int.MaxValue, true, true);
                //var map2 = range2.Map(i => i * 2);
                //var range3 = map2.Range(0, int.MaxValue, true, true);
                //var map3 = range3.Map(i => i * 2);
                long sum = 0;
                foreach (var kvp in map)
                {
                    sum += kvp.Value;
                }
                sw.Stop();
                Assert.IsTrue(sum > 0);

                Console.WriteLine($"Mops {sw.MOPS(count)}");
            }

            //for (int r = 0; r < 10; r++)
            //{
            //    var sw = new Stopwatch();
            //    sw.Restart();
            //    var map = sm.Select(x => new KeyValuePair<int, int>(x.Key, x.Value * 2));
            //    long sum = 0;
            //    foreach (var kvp in map)
            //    {
            //        sum += kvp.Value;
            //    }
            //    sw.Stop();
            //    Assert.IsTrue(sum > 0);

            //    Console.WriteLine($"LINQ Mops {sw.MOPS(count)}");
            //}
        }
    }
}