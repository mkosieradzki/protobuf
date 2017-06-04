#region Copyright notice and license
// Protocol Buffers - Google's data interchange format
// Copyright 2008 Google Inc.  All rights reserved.
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

#if !PROTOBUF_NO_ASYNC
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.TestProtos;
using NUnit.Framework;

namespace Google.Protobuf
{
    public class CodedInputStreamAsyncTest
    {
        /// <summary>
        /// Helper to construct a byte array from a bunch of bytes.  The inputs are
        /// actually ints so that I can use hex notation and not get stupid errors
        /// about precision.
        /// </summary>
        private static byte[] Bytes(params int[] bytesAsInts)
        {
            byte[] bytes = new byte[bytesAsInts.Length];
            for (int i = 0; i < bytesAsInts.Length; i++)
            {
                bytes[i] = (byte) bytesAsInts[i];
            }
            return bytes;
        }

        /// <summary>
        /// Parses the given bytes using ReadRawVarint32() and ReadRawVarint64()
        /// </summary>
        private static async Task AssertReadVarint(byte[] data, ulong value)
        {
            CodedInputStream input = new CodedInputStream(data);
            Assert.AreEqual((uint) value, await input.ReadRawVarint32Async(CancellationToken.None));

            input = new CodedInputStream(data);
            Assert.AreEqual(value, await input.ReadRawVarint64Async(CancellationToken.None));
            Assert.IsTrue(input.IsAtEnd);

            // Try different block sizes.
            for (int bufferSize = 1; bufferSize <= 16; bufferSize *= 2)
            {
                input = new CodedInputStream(new SmallBlockInputStream(data, bufferSize));
                Assert.AreEqual((uint) value, await input.ReadRawVarint32Async(CancellationToken.None));

                input = new CodedInputStream(new SmallBlockInputStream(data, bufferSize));
                Assert.AreEqual(value, await input.ReadRawVarint64Async(CancellationToken.None));
                Assert.IsTrue(input.IsAtEnd);
            }

            // Try reading directly from a MemoryStream. We want to verify that it
            // doesn't read past the end of the input, so write an extra byte - this
            // lets us test the position at the end.
            MemoryStream memoryStream = new MemoryStream();
            memoryStream.Write(data, 0, data.Length);
            memoryStream.WriteByte(0);
            memoryStream.Position = 0;
            Assert.AreEqual((uint) value, await CodedInputStream.ReadRawVarint32Async(memoryStream, CancellationToken.None));
            Assert.AreEqual(data.Length, memoryStream.Position);
        }

        /// <summary>
        /// Parses the given bytes using ReadRawVarint32() and ReadRawVarint64() and
        /// expects them to fail with an InvalidProtocolBufferException whose
        /// description matches the given one.
        /// </summary>
        private static void AssertReadVarintFailure(InvalidProtocolBufferException expected, byte[] data)
        {
            CodedInputStream input = new CodedInputStream(data);
            var exception = Assert.ThrowsAsync<InvalidProtocolBufferException>(() => input.ReadRawVarint32Async(CancellationToken.None));
            Assert.AreEqual(expected.Message, exception.Message);

            input = new CodedInputStream(data);
            exception = Assert.ThrowsAsync<InvalidProtocolBufferException>(() => input.ReadRawVarint64Async(CancellationToken.None));
            Assert.AreEqual(expected.Message, exception.Message);

            // Make sure we get the same error when reading directly from a Stream.
            exception = Assert.ThrowsAsync<InvalidProtocolBufferException>(() => CodedInputStream.ReadRawVarint32Async(new MemoryStream(data), CancellationToken.None));
            Assert.AreEqual(expected.Message, exception.Message);
        }

        [Test]
        public async Task ReadVarint()
        {
            await AssertReadVarint(Bytes(0x00), 0);
            await AssertReadVarint(Bytes(0x01), 1);
            await AssertReadVarint(Bytes(0x7f), 127);
            // 14882
            await AssertReadVarint(Bytes(0xa2, 0x74), (0x22 << 0) | (0x74 << 7));
            // 2961488830
            await AssertReadVarint(Bytes(0xbe, 0xf7, 0x92, 0x84, 0x0b),
                             (0x3e << 0) | (0x77 << 7) | (0x12 << 14) | (0x04 << 21) |
                             (0x0bL << 28));

            // 64-bit
            // 7256456126
            await AssertReadVarint(Bytes(0xbe, 0xf7, 0x92, 0x84, 0x1b),
                             (0x3e << 0) | (0x77 << 7) | (0x12 << 14) | (0x04 << 21) |
                             (0x1bL << 28));
            // 41256202580718336
            await AssertReadVarint(Bytes(0x80, 0xe6, 0xeb, 0x9c, 0xc3, 0xc9, 0xa4, 0x49),
                             (0x00 << 0) | (0x66 << 7) | (0x6b << 14) | (0x1c << 21) |
                             (0x43L << 28) | (0x49L << 35) | (0x24L << 42) | (0x49L << 49));
            // 11964378330978735131
            await AssertReadVarint(Bytes(0x9b, 0xa8, 0xf9, 0xc2, 0xbb, 0xd6, 0x80, 0x85, 0xa6, 0x01),
                             (0x1b << 0) | (0x28 << 7) | (0x79 << 14) | (0x42 << 21) |
                             (0x3bUL << 28) | (0x56UL << 35) | (0x00UL << 42) |
                             (0x05UL << 49) | (0x26UL << 56) | (0x01UL << 63));

            // Failures
            AssertReadVarintFailure(
                InvalidProtocolBufferException.MalformedVarint(),
                Bytes(0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
                      0x00));
            AssertReadVarintFailure(
                InvalidProtocolBufferException.TruncatedMessage(),
                Bytes(0x80));
        }

        /// <summary>
        /// Parses the given bytes using ReadRawLittleEndian32() and checks
        /// that the result matches the given value.
        /// </summary>
        private static async Task AssertReadLittleEndian32(byte[] data, uint value)
        {
            CodedInputStream input = new CodedInputStream(data);
            Assert.AreEqual(value, await input.ReadRawLittleEndian32Async(CancellationToken.None));
            Assert.IsTrue(input.IsAtEnd);

            // Try different block sizes.
            for (int blockSize = 1; blockSize <= 16; blockSize *= 2)
            {
                input = new CodedInputStream(
                    new SmallBlockInputStream(data, blockSize));
                Assert.AreEqual(value, await input.ReadRawLittleEndian32Async(CancellationToken.None));
                Assert.IsTrue(input.IsAtEnd);
            }
        }

        /// <summary>
        /// Parses the given bytes using ReadRawLittleEndian64() and checks
        /// that the result matches the given value.
        /// </summary>
        private static async Task AssertReadLittleEndian64(byte[] data, ulong value)
        {
            CodedInputStream input = new CodedInputStream(data);
            Assert.AreEqual(value, await input.ReadRawLittleEndian64Async(CancellationToken.None));
            Assert.IsTrue(input.IsAtEnd);

            // Try different block sizes.
            for (int blockSize = 1; blockSize <= 16; blockSize *= 2)
            {
                input = new CodedInputStream(
                    new SmallBlockInputStream(data, blockSize));
                Assert.AreEqual(value, await input.ReadRawLittleEndian64Async(CancellationToken.None));
                Assert.IsTrue(input.IsAtEnd);
            }
        }

        [Test]
        public async Task ReadLittleEndian()
        {
            await AssertReadLittleEndian32(Bytes(0x78, 0x56, 0x34, 0x12), 0x12345678);
            await AssertReadLittleEndian32(Bytes(0xf0, 0xde, 0xbc, 0x9a), 0x9abcdef0);

            await AssertReadLittleEndian64(Bytes(0xf0, 0xde, 0xbc, 0x9a, 0x78, 0x56, 0x34, 0x12),
                                     0x123456789abcdef0L);
            await AssertReadLittleEndian64(
                Bytes(0x78, 0x56, 0x34, 0x12, 0xf0, 0xde, 0xbc, 0x9a), 0x9abcdef012345678UL);
        }
        
        [Test]
        public async Task ReadWholeMessage_VaryingBlockSizes()
        {
            TestAllTypes message = SampleMessages.CreateFullTestAllTypes();

            byte[] rawBytes = message.ToByteArray();
            Assert.AreEqual(rawBytes.Length, message.CalculateSize());
            TestAllTypes message2 = TestAllTypes.Parser.ParseFrom(rawBytes);
            Assert.AreEqual(message, message2);

            // Try different block sizes.
            for (int blockSize = 1; blockSize < 256; blockSize *= 2)
            {
                message2 = await TestAllTypes.Parser.ParseFromAsync(new SmallBlockInputStream(rawBytes, blockSize), CancellationToken.None);
                Assert.AreEqual(message, message2);
            }
        }

        [Test]
        public async Task ReadMaliciouslyLargeBlob()
        {
            MemoryStream ms = new MemoryStream();
            CodedOutputStream output = new CodedOutputStream(ms);

            uint tag = WireFormat.MakeTag(1, WireFormat.WireType.LengthDelimited);
            await output.WriteRawVarint32Async(tag, CancellationToken.None);
            await output.WriteRawVarint32Async(0x7FFFFFFF, CancellationToken.None);
            await output.WriteRawBytesAsync(new byte[32], CancellationToken.None); // Pad with a few random bytes.
            await output.FlushAsync(CancellationToken.None);
            ms.Position = 0;

            CodedInputStream input = new CodedInputStream(ms);
            Assert.AreEqual(tag, await input.ReadTagAsync(CancellationToken.None));

            Assert.ThrowsAsync<InvalidProtocolBufferException>(() => input.ReadBytesAsync(CancellationToken.None));
        }

        internal static TestRecursiveMessage MakeRecursiveMessage(int depth)
        {
            if (depth == 0)
            {
                return new TestRecursiveMessage { I = 5 };
            }
            else
            {
                return new TestRecursiveMessage { A = MakeRecursiveMessage(depth - 1) };
            }
        }

        internal static void AssertMessageDepth(TestRecursiveMessage message, int depth)
        {
            if (depth == 0)
            {
                Assert.IsNull(message.A);
                Assert.AreEqual(5, message.I);
            }
            else
            {
                Assert.IsNotNull(message.A);
                AssertMessageDepth(message.A, depth - 1);
            }
        }

        [Test]
        public void SizeLimit()
        {
            // Have to use a Stream rather than ByteString.CreateCodedInput as SizeLimit doesn't
            // apply to the latter case.
            MemoryStream ms = new MemoryStream(SampleMessages.CreateFullTestAllTypes().ToByteArray());
            CodedInputStream input = CodedInputStream.CreateWithLimits(ms, 16, 100);
            Assert.ThrowsAsync<InvalidProtocolBufferException>(() => TestAllTypes.Parser.ParseFromAsync(input, CancellationToken.None));
        }

        /// <summary>
        /// Tests that if we read an string that contains invalid UTF-8, no exception
        /// is thrown.  Instead, the invalid bytes are replaced with the Unicode
        /// "replacement character" U+FFFD.
        /// </summary>
        [Test]
        public async Task ReadInvalidUtf8()
        {
            MemoryStream ms = new MemoryStream();
            CodedOutputStream output = new CodedOutputStream(ms);

            uint tag = WireFormat.MakeTag(1, WireFormat.WireType.LengthDelimited);
            await output.WriteRawVarint32Async(tag, CancellationToken.None);
            await output.WriteRawVarint32Async(1, CancellationToken.None);
            await output.WriteRawBytesAsync(new byte[] {0x80}, CancellationToken.None);
            await output.FlushAsync(CancellationToken.None);
            ms.Position = 0;

            CodedInputStream input = new CodedInputStream(ms);

            Assert.AreEqual(tag, await input.ReadTagAsync(CancellationToken.None));
            string text = await input.ReadStringAsync(CancellationToken.None);
            Assert.AreEqual('\ufffd', text[0]);
        }

        /// <summary>
        /// A stream which limits the number of bytes it reads at a time.
        /// We use this to make sure that CodedInputStream doesn't screw up when
        /// reading in small blocks.
        /// </summary>
        private sealed class SmallBlockInputStream : MemoryStream
        {
            private readonly int blockSize;

            public SmallBlockInputStream(byte[] data, int blockSize)
                : base(data)
            {
                this.blockSize = blockSize;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return base.Read(buffer, offset, Math.Min(count, blockSize));
            }
        }

        [Test]
        public async Task TestNegativeEnum()
        {
            byte[] bytes = { 0xFE, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x01 };
            CodedInputStream input = new CodedInputStream(bytes);
            Assert.AreEqual((int)SampleEnum.NegativeValue, await input.ReadEnumAsync(CancellationToken.None));
            Assert.IsTrue(input.IsAtEnd);
        }

        //Issue 71:	CodedInputStream.ReadBytes go to slow path unnecessarily
        [Test]
        public async Task TestSlowPathAvoidance()
        {
            using (var ms = new MemoryStream())
            {
                CodedOutputStream output = new CodedOutputStream(ms);
                await output.WriteTagAsync(1, WireFormat.WireType.LengthDelimited, CancellationToken.None);
                await output.WriteBytesAsync(ByteString.CopyFrom(new byte[100]), CancellationToken.None);
                await output.WriteTagAsync(2, WireFormat.WireType.LengthDelimited, CancellationToken.None);
                await output.WriteBytesAsync(ByteString.CopyFrom(new byte[100]), CancellationToken.None);
                await output.FlushAsync(CancellationToken.None);

                ms.Position = 0;
                CodedInputStream input = new CodedInputStream(ms, new byte[ms.Length / 2], 0, 0);

                uint tag = await input.ReadTagAsync(CancellationToken.None);
                Assert.AreEqual(1, WireFormat.GetTagFieldNumber(tag));
                Assert.AreEqual(100, (await input.ReadBytesAsync(CancellationToken.None)).Length);

                tag = await input.ReadTagAsync(CancellationToken.None);
                Assert.AreEqual(2, WireFormat.GetTagFieldNumber(tag));
                Assert.AreEqual(100, (await input.ReadBytesAsync(CancellationToken.None)).Length);
            }
        }

        [Test]
        public void Tag0Throws()
        {
            var input = new CodedInputStream(new byte[] { 0 });
            Assert.ThrowsAsync<InvalidProtocolBufferException>(() => input.ReadTagAsync(CancellationToken.None));
        }

        [Test]
        public async Task SkipGroup()
        {
            // Create an output stream with a group in:
            // Field 1: string "field 1"
            // Field 2: group containing:
            //   Field 1: fixed int32 value 100
            //   Field 2: string "ignore me"
            //   Field 3: nested group containing
            //      Field 1: fixed int64 value 1000
            // Field 3: string "field 3"
            var stream = new MemoryStream();
            var output = new CodedOutputStream(stream);
            await output.WriteTagAsync(1, WireFormat.WireType.LengthDelimited, CancellationToken.None);
            await output.WriteStringAsync("field 1", CancellationToken.None);
            
            // The outer group...
            await output.WriteTagAsync(2, WireFormat.WireType.StartGroup, CancellationToken.None);
            await output.WriteTagAsync(1, WireFormat.WireType.Fixed32, CancellationToken.None);
            await output.WriteFixed32Async(100, CancellationToken.None);
            await output.WriteTagAsync(2, WireFormat.WireType.LengthDelimited, CancellationToken.None);
            await output.WriteStringAsync("ignore me", CancellationToken.None);
            // The nested group...
            await output.WriteTagAsync(3, WireFormat.WireType.StartGroup, CancellationToken.None);
            await output.WriteTagAsync(1, WireFormat.WireType.Fixed64, CancellationToken.None);
            await output.WriteFixed64Async(1000, CancellationToken.None);
            // Note: Not sure the field number is relevant for end group...
            await output.WriteTagAsync(3, WireFormat.WireType.EndGroup, CancellationToken.None);

            // End the outer group
            await output.WriteTagAsync(2, WireFormat.WireType.EndGroup, CancellationToken.None);

            await output.WriteTagAsync(3, WireFormat.WireType.LengthDelimited, CancellationToken.None);
            await output.WriteStringAsync("field 3", CancellationToken.None);
            await output.FlushAsync(CancellationToken.None);
            stream.Position = 0;

            // Now act like a generated client
            var input = new CodedInputStream(stream);
            Assert.AreEqual(WireFormat.MakeTag(1, WireFormat.WireType.LengthDelimited), await input.ReadTagAsync(CancellationToken.None));
            Assert.AreEqual("field 1", await input.ReadStringAsync(CancellationToken.None));
            Assert.AreEqual(WireFormat.MakeTag(2, WireFormat.WireType.StartGroup), await input.ReadTagAsync(CancellationToken.None));
            await input.SkipLastFieldAsync(CancellationToken.None); // Should consume the whole group, including the nested one.
            Assert.AreEqual(WireFormat.MakeTag(3, WireFormat.WireType.LengthDelimited), await input.ReadTagAsync(CancellationToken.None));
            Assert.AreEqual("field 3", await input.ReadStringAsync(CancellationToken.None));
        }

        [Test]
        public async Task SkipGroup_WrongEndGroupTag()
        {
            // Create an output stream with:
            // Field 1: string "field 1"
            // Start group 2
            //   Field 3: fixed int32
            // End group 4 (should give an error)
            var stream = new MemoryStream();
            var output = new CodedOutputStream(stream);
            await output.WriteTagAsync(1, WireFormat.WireType.LengthDelimited, CancellationToken.None);
            await output.WriteStringAsync("field 1", CancellationToken.None);

            // The outer group...
            await output.WriteTagAsync(2, WireFormat.WireType.StartGroup, CancellationToken.None);
            await output.WriteTagAsync(3, WireFormat.WireType.Fixed32, CancellationToken.None);
            await output.WriteFixed32Async(100, CancellationToken.None);
            await output.WriteTagAsync(4, WireFormat.WireType.EndGroup, CancellationToken.None);
            await output.FlushAsync(CancellationToken.None);
            stream.Position = 0;

            // Now act like a generated client
            var input = new CodedInputStream(stream);
            Assert.AreEqual(WireFormat.MakeTag(1, WireFormat.WireType.LengthDelimited), await input.ReadTagAsync(CancellationToken.None));
            Assert.AreEqual("field 1", await input.ReadStringAsync(CancellationToken.None));
            Assert.AreEqual(WireFormat.MakeTag(2, WireFormat.WireType.StartGroup), await input.ReadTagAsync(CancellationToken.None));
            Assert.ThrowsAsync<InvalidProtocolBufferException>(() => input.SkipLastFieldAsync(CancellationToken.None));
        }

        [Test]
        public async Task RogueEndGroupTag()
        {
            // If we have an end-group tag without a leading start-group tag, generated
            // code will just call SkipLastField... so that should fail.

            var stream = new MemoryStream();
            var output = new CodedOutputStream(stream);
            await output.WriteTagAsync(1, WireFormat.WireType.EndGroup, CancellationToken.None);
            await output.FlushAsync(CancellationToken.None);
            stream.Position = 0;

            var input = new CodedInputStream(stream);
            Assert.AreEqual(WireFormat.MakeTag(1, WireFormat.WireType.EndGroup), await input.ReadTagAsync(CancellationToken.None));
            Assert.ThrowsAsync<InvalidProtocolBufferException>(() => input.SkipLastFieldAsync(CancellationToken.None));
        }

        [Test]
        public async Task EndOfStreamReachedWhileSkippingGroup()
        {
            var stream = new MemoryStream();
            var output = new CodedOutputStream(stream);
            await output.WriteTagAsync(1, WireFormat.WireType.StartGroup, CancellationToken.None);
            await output.WriteTagAsync(2, WireFormat.WireType.StartGroup, CancellationToken.None);
            await output.WriteTagAsync(2, WireFormat.WireType.EndGroup, CancellationToken.None);

            await output.FlushAsync(CancellationToken.None);
            stream.Position = 0;

            // Now act like a generated client
            var input = new CodedInputStream(stream);
            await input.ReadTagAsync(CancellationToken.None);
            Assert.Throws<InvalidProtocolBufferException>(input.SkipLastField);
        }

        [Test]
        public async Task RecursionLimitAppliedWhileSkippingGroup()
        {
            var stream = new MemoryStream();
            var output = new CodedOutputStream(stream);
            for (int i = 0; i < CodedInputStream.DefaultRecursionLimit + 1; i++)
            {
                await output.WriteTagAsync(1, WireFormat.WireType.StartGroup, CancellationToken.None);
            }
            for (int i = 0; i < CodedInputStream.DefaultRecursionLimit + 1; i++)
            {
                await output.WriteTagAsync(1, WireFormat.WireType.EndGroup, CancellationToken.None);
            }
            await output.FlushAsync(CancellationToken.None);
            stream.Position = 0;

            // Now act like a generated client
            var input = new CodedInputStream(stream);
            Assert.AreEqual(WireFormat.MakeTag(1, WireFormat.WireType.StartGroup), input.ReadTag());
            Assert.ThrowsAsync<InvalidProtocolBufferException>(() => input.SkipLastFieldAsync(CancellationToken.None));
        }
    }
}
#endif