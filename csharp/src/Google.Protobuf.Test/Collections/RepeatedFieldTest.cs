#region Copyright notice and license
// Protocol Buffers - Google's data interchange format
// Copyright 2015 Google Inc.  All rights reserved.
// https://developers.google.com/protocol-buffers/
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are
// met:
//
//     * Redistributions of source code must retain the above copyright
// notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above
// copyright notice, this list of conditions and the following disclaimer
// in the documentation and/or other materials provided with the
// distribution.
//     * Neither the name of Google Inc. nor the names of its
// contributors may be used to endorse or promote products derived from
// this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
// OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Google.Protobuf.TestProtos;
using Google.Protobuf.WellKnownTypes;
using NUnit.Framework;

namespace Google.Protobuf.Collections
{
    public class RepeatedFieldTest
    {
        [Test]
        public void NullValuesRejected()
        {
            var list = new RepeatedField<string>();
            Assert.Throws<ArgumentNullException>(() => list.Add((string)null));
            Assert.Throws<ArgumentNullException>(() => list.Add((IEnumerable<string>)null));
            Assert.Throws<ArgumentNullException>(() => list.Add((RepeatedField<string>)null));
            Assert.Throws<ArgumentNullException>(() => list.Contains(null));
            Assert.Throws<ArgumentNullException>(() => list.IndexOf(null));
        }

        [Test]
        public void Add_SingleItem()
        {
            var list = new RepeatedField<string>();
            list.Add("foo");
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual("foo", list[0]);
        }

        [Test]
        public void Add_Sequence()
        {
            var list = new RepeatedField<string>();
            list.Add(new[] { "foo", "bar" });
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual("foo", list[0]);
            Assert.AreEqual("bar", list[1]);
        }

        [Test]
        public void AddRange_SlowPath()
        {
            var list = new RepeatedField<string>();
            list.AddRange(new[] { "foo", "bar" }.Select(x => x));
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual("foo", list[0]);
            Assert.AreEqual("bar", list[1]);
        }

        [Test]
        public void AddRange_SlowPath_NullsProhibited_ReferenceType()
        {
            var list = new RepeatedField<string>();
            // It's okay for this to throw ArgumentNullException if necessary.
            // It's not ideal, but not awful.
            Assert.Catch<ArgumentException>(() => list.AddRange(new[] { "foo", null }.Select(x => x)));
        }

        [Test]
        public void AddRange_SlowPath_NullsProhibited_NullableValueType()
        {
            var list = new RepeatedField<int?>();
            // It's okay for this to throw ArgumentNullException if necessary.
            // It's not ideal, but not awful.
            Assert.Catch<ArgumentException>(() => list.AddRange(new[] { 20, (int?)null }.Select(x => x)));
        }

        [Test]
        public void AddRange_Optimized_NonNullableValueType()
        {
            var list = new RepeatedField<int>();
            list.AddRange(new List<int> { 20, 30 });
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual(20, list[0]);
            Assert.AreEqual(30, list[1]);
        }

        [Test]
        public void AddRange_Optimized_ReferenceType()
        {
            var list = new RepeatedField<string>();
            list.AddRange(new List<string> { "foo", "bar" });
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual("foo", list[0]);
            Assert.AreEqual("bar", list[1]);
        }

        [Test]
        public void AddRange_Optimized_NullableValueType()
        {
            var list = new RepeatedField<int?>();
            list.AddRange(new List<int?> { 20, 30 });
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual((int?) 20, list[0]);
            Assert.AreEqual((int?) 30, list[1]);
        }

        [Test]
        public void AddRange_Optimized_NullsProhibited_ReferenceType()
        {
            // We don't just trust that a collection with a nullable element type doesn't contain nulls
            var list = new RepeatedField<string>();
            // It's okay for this to throw ArgumentNullException if necessary.
            // It's not ideal, but not awful.
            Assert.Catch<ArgumentException>(() => list.AddRange(new List<string> { "foo", null }));
        }

        [Test]
        public void AddRange_Optimized_NullsProhibited_NullableValueType()
        {
            // We don't just trust that a collection with a nullable element type doesn't contain nulls
            var list = new RepeatedField<int?>();
            // It's okay for this to throw ArgumentNullException if necessary.
            // It's not ideal, but not awful.
            Assert.Catch<ArgumentException>(() => list.AddRange(new List<int?> { 20, null }));
        }

        [Test]
        public void AddRange_AlreadyNotEmpty()
        {
            var list = new RepeatedField<int> { 1, 2, 3 };
            list.AddRange(new List<int> { 4, 5, 6 });
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5, 6 }, list);
        }

        [Test]
        public void AddRange_RepeatedField()
        {
            var list = new RepeatedField<string> { "original" };
            list.AddRange(new RepeatedField<string> { "foo", "bar" });
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual("original", list[0]);
            Assert.AreEqual("foo", list[1]);
            Assert.AreEqual("bar", list[2]);
        }

        [Test]
        public void RemoveAt_Valid()
        {
            var list = new RepeatedField<string> { "first", "second", "third" };
            list.RemoveAt(1);
            CollectionAssert.AreEqual(new[] { "first", "third" }, list);
            // Just check that these don't throw...
            list.RemoveAt(list.Count - 1); // Now the count will be 1...
            list.RemoveAt(0);
            Assert.AreEqual(0, list.Count);
        }

        [Test]
        public void RemoveAt_Invalid()
        {
            var list = new RepeatedField<string> { "first", "second", "third" };
            Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(3));
        }

        [Test]
        public void Insert_Valid()
        {
            var list = new RepeatedField<string> { "first", "second" };
            list.Insert(1, "middle");
            CollectionAssert.AreEqual(new[] { "first", "middle", "second" }, list);
            list.Insert(3, "end");
            CollectionAssert.AreEqual(new[] { "first", "middle", "second", "end" }, list);
            list.Insert(0, "start");
            CollectionAssert.AreEqual(new[] { "start", "first", "middle", "second", "end" }, list);
        }

        [Test]
        public void Insert_Invalid()
        {
            var list = new RepeatedField<string> { "first", "second" };
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(-1, "foo"));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(3, "foo"));
            Assert.Throws<ArgumentNullException>(() => list.Insert(0, null));
        }

        [Test]
        public void Equals_RepeatedField()
        {
            var list = new RepeatedField<string> { "first", "second" };
            Assert.IsFalse(list.Equals((RepeatedField<string>) null));
            Assert.IsTrue(list.Equals(list));
            Assert.IsFalse(list.Equals(new RepeatedField<string> { "first", "third" }));
            Assert.IsFalse(list.Equals(new RepeatedField<string> { "first" }));
            Assert.IsTrue(list.Equals(new RepeatedField<string> { "first", "second" }));
        }

        [Test]
        public void Equals_Object()
        {
            var list = new RepeatedField<string> { "first", "second" };
            Assert.IsFalse(list.Equals((object) null));
            Assert.IsTrue(list.Equals((object) list));
            Assert.IsFalse(list.Equals((object) new RepeatedField<string> { "first", "third" }));
            Assert.IsFalse(list.Equals((object) new RepeatedField<string> { "first" }));
            Assert.IsTrue(list.Equals((object) new RepeatedField<string> { "first", "second" }));
            Assert.IsFalse(list.Equals(new object()));
        }

        [Test]
        public void GetEnumerator_GenericInterface()
        {
            IEnumerable<string> list = new RepeatedField<string> { "first", "second" };
            // Select gets rid of the optimizations in ToList...
            CollectionAssert.AreEqual(new[] { "first", "second" }, list.Select(x => x).ToList());
        }

        [Test]
        public void GetEnumerator_NonGenericInterface()
        {
            IEnumerable list = new RepeatedField<string> { "first", "second" };
            CollectionAssert.AreEqual(new[] { "first", "second" }, list.Cast<object>().ToList());
        }

        [Test]
        public void CopyTo()
        {
            var list = new RepeatedField<string> { "first", "second" };
            string[] stringArray = new string[4];
            list.CopyTo(stringArray, 1);
            CollectionAssert.AreEqual(new[] { null, "first", "second", null }, stringArray);
        }

        [Test]
        public void Indexer_Get()
        {
            var list = new RepeatedField<string> { "first", "second" };
            Assert.AreEqual("first", list[0]);
            Assert.AreEqual("second", list[1]);
            Assert.Throws<ArgumentOutOfRangeException>(() => list[-1].GetHashCode());
            Assert.Throws<ArgumentOutOfRangeException>(() => list[2].GetHashCode());
        }

        [Test]
        public void Indexer_Set()
        {
            var list = new RepeatedField<string> { "first", "second" };
            list[0] = "changed";
            Assert.AreEqual("changed", list[0]);
            Assert.Throws<ArgumentNullException>(() => list[0] = null);
            Assert.Throws<ArgumentOutOfRangeException>(() => list[-1] = "bad");
            Assert.Throws<ArgumentOutOfRangeException>(() => list[2] = "bad");
        }

        [Test]
        public void Clone_ReturnsMutable()
        {
            var list = new RepeatedField<int> { 0 };
            var clone = list.Clone();
            clone[0] = 1;
        }

        [Test]
        public void Enumerator()
        {
            var list = new RepeatedField<string> { "first", "second" };
            using (var enumerator = list.GetEnumerator())
            {
                Assert.IsTrue(enumerator.MoveNext());
                Assert.AreEqual("first", enumerator.Current);
                Assert.IsTrue(enumerator.MoveNext());
                Assert.AreEqual("second", enumerator.Current);
                Assert.IsFalse(enumerator.MoveNext());
                Assert.IsFalse(enumerator.MoveNext());
            }
        }

        // Fairly perfunctory tests for the non-generic IList implementation
        [Test]
        public void IList_Indexer()
        {
            var field = new RepeatedField<string> { "first", "second" };
            IList list = field;
            Assert.AreEqual("first", list[0]);
            list[1] = "changed";
            Assert.AreEqual("changed", field[1]);
        }

        [Test]
        public void IList_Contains()
        {
            IList list = new RepeatedField<string> { "first", "second" };
            Assert.IsTrue(list.Contains("second"));
            Assert.IsFalse(list.Contains("third"));
            Assert.IsFalse(list.Contains(new object()));
        }

        [Test]
        public void IList_Add()
        {
            IList list = new RepeatedField<string> { "first", "second" };
            list.Add("third");
            CollectionAssert.AreEqual(new[] { "first", "second", "third" }, list);
        }

        [Test]
        public void IList_Remove()
        {
            IList list = new RepeatedField<string> { "first", "second" };
            list.Remove("third"); // No-op, no exception
            list.Remove(new object()); // No-op, no exception
            list.Remove("first");
            CollectionAssert.AreEqual(new[] { "second" }, list);
        }

        [Test]
        public void IList_IsFixedSize()
        {
            var field = new RepeatedField<string> { "first", "second" };
            IList list = field;
            Assert.IsFalse(list.IsFixedSize);
        }

        [Test]
        public void IList_IndexOf()
        {
            IList list = new RepeatedField<string> { "first", "second" };
            Assert.AreEqual(1, list.IndexOf("second"));
            Assert.AreEqual(-1, list.IndexOf("third"));
            Assert.AreEqual(-1, list.IndexOf(new object()));
        }

        [Test]
        public void IList_SyncRoot()
        {
            IList list = new RepeatedField<string> { "first", "second" };
            Assert.AreSame(list, list.SyncRoot);
        }

        [Test]
        public void IList_CopyTo()
        {
            IList list = new RepeatedField<string> { "first", "second" };
            string[] stringArray = new string[4];
            list.CopyTo(stringArray, 1);
            CollectionAssert.AreEqual(new[] { null, "first",  "second", null }, stringArray);

            object[] objectArray = new object[4];
            list.CopyTo(objectArray, 1);
            CollectionAssert.AreEqual(new[] { null, "first", "second", null }, objectArray);

            Assert.Throws<ArrayTypeMismatchException>(() => list.CopyTo(new StringBuilder[4], 1));
            Assert.Throws<ArrayTypeMismatchException>(() => list.CopyTo(new int[4], 1));
        }

        [Test]
        public void IList_IsSynchronized()
        {
            IList list = new RepeatedField<string> { "first", "second" };
            Assert.IsFalse(list.IsSynchronized);
        }

        [Test]
        public void IList_Insert()
        {
            IList list = new RepeatedField<string> { "first", "second" };
            list.Insert(1, "middle");
            CollectionAssert.AreEqual(new[] { "first", "middle", "second" }, list);
        }

        [Test]
        public void ToString_Integers()
        {
            var list = new RepeatedField<int> { 5, 10, 20 };
            var text = list.ToString();
            Assert.AreEqual("[ 5, 10, 20 ]", text);
        }

        [Test]
        public void ToString_Strings()
        {
            var list = new RepeatedField<string> { "x", "y", "z" };
            var text = list.ToString();
            Assert.AreEqual("[ \"x\", \"y\", \"z\" ]", text);
        }

        [Test]
        public void ToString_Messages()
        {
            var list = new RepeatedField<TestAllTypes> { new TestAllTypes { SingleDouble = 1.5 }, new TestAllTypes { SingleInt32 = 10 } };
            var text = list.ToString();
            Assert.AreEqual("[ { \"singleDouble\": 1.5 }, { \"singleInt32\": 10 } ]", text);
        }

        [Test]
        public void ToString_Empty()
        {
            var list = new RepeatedField<TestAllTypes> { };
            var text = list.ToString();
            Assert.AreEqual("[ ]", text);
        }

        [Test]
        public void ToString_InvalidElementType()
        {
            var list = new RepeatedField<decimal> { 15m };
            Assert.Throws<ArgumentException>(() => list.ToString());
        }

        [Test]
        public void ToString_Timestamp()
        {
            var list = new RepeatedField<Timestamp> { Timestamp.FromDateTime(new DateTime(2015, 10, 1, 12, 34, 56, DateTimeKind.Utc)) };
            var text = list.ToString();
            Assert.AreEqual("[ \"2015-10-01T12:34:56Z\" ]", text);
        }

        [Test]
        public void ToString_Struct()
        {
            var message = new Struct { Fields = { { "foo", new Value { NumberValue = 20 } } } };
            var list = new RepeatedField<Struct> { message };
            var text = list.ToString();
            Assert.AreEqual(text, "[ { \"foo\": 20 } ]", message.ToString());
        }

        [Test]
        public void NaNValuesComparedBitwise()
        {
            var list1 = new RepeatedField<double> { SampleNaNs.Regular, SampleNaNs.SignallingFlipped };
            var list2 = new RepeatedField<double> { SampleNaNs.Regular, SampleNaNs.PayloadFlipped };
            var list3 = new RepeatedField<double> { SampleNaNs.Regular, SampleNaNs.SignallingFlipped };

#if !NETCOREAPP2_1
            EqualityTester.AssertInequality(list1, list2);
#endif
            EqualityTester.AssertEquality(list1, list3);
            Assert.True(list1.Contains(SampleNaNs.SignallingFlipped));
            Assert.False(list2.Contains(SampleNaNs.SignallingFlipped));
        }
    }
}
