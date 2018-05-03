using System;
using System.Buffers;
using System.Buffers.Binary;

namespace Google.Protobuf
{
    public readonly ref struct ReadOnlySequenceState<T>
    {
        public ReadOnlySequence<T> Buffer { get; }
        public ReadOnlySpan<T> First { get; }

        public ReadOnlySequenceState(ReadOnlySequence<T> buffer)
        {
            Buffer = buffer;
            First = buffer.First.Span;
        }

        private ReadOnlySequenceState(ReadOnlySequence<T> buffer, ReadOnlySpan<T> first)
        {
            Buffer = buffer;
            First = first;
        }

        public ReadOnlySequenceState<T> Slice(int length) => First.Length > length ? new ReadOnlySequenceState<T>(Buffer.Slice(length), First.Slice(length)) : new ReadOnlySequenceState<T>(Buffer.Slice(length));
    }

    public static class CodedInputExpParser
    {
        public static int ReadLength(ref ReadOnlySequenceState<byte> buffer) => (int)ReadRawVarint32(ref buffer);

        public static ReadOnlySequenceState<byte> ReadLengthDelimited(ref ReadOnlySequenceState<byte> buffer)
        {
            var length = ReadLength(ref buffer);
            if (length > buffer.Buffer.Length)
                throw new Exception();
            var ret = new ReadOnlySequenceState<byte>(buffer.Buffer.Slice(0, length));
            buffer = buffer.Slice(length);
            return ret;
        }

        public static int ReadInt32(ref ReadOnlySequenceState<byte> buffer) => (int)ReadRawVarint32(ref buffer);

        public static uint ReadTag(ref ReadOnlySequenceState<byte> buffer)
        {
            var span = buffer.First;

            // Optimize for the incredibly common case of having at least two bytes left in the buffer,
            // and those two bytes being enough to get the tag. This will be true for fields up to 4095.
            if (span.Length >= 2)// bufferPos + 2 <= bufferSize)
            {
                uint tag;

                int tmp = span[0];
                if (tmp < 128)
                {
                    tag = (uint)tmp;
                    buffer = buffer.Slice(1);
                }
                else
                {
                    int result = tmp & 0x7f;
                    if ((tmp = span[1]) < 128)
                    {
                        result |= tmp << 7;
                        tag = (uint)result;
                        buffer = buffer.Slice(2);
                    }
                    else if (buffer.First.IsEmpty)
                    {
                        return 0;
                    }
                    else
                    {
                        return ReadRawVarint32(ref buffer);
                    }
                }
                if (WireFormat.GetTagFieldNumber(tag) == 0)
                {
                    // If we actually read a tag with a field of 0, that's not a valid tag.
                    //TODO: Fix
                    //throw InvalidProtocolBufferException.InvalidTag();
                    throw new Exception();
                }
                return tag;
            }
            else if (buffer.First.IsEmpty)
            {
                return 0;
            }
            else
            {
                return ReadRawVarint32(ref buffer);
            }
        }

        private static uint ReadRawVarint32(ref ReadOnlySequenceState<byte> buffer)
        {
            var span = buffer.First;
            if (span.Length < 5)
            {
                return SlowReadRawVarint32(ref buffer);
            }

            int tmp = span[0];
            if (tmp < 128)
            {
                buffer = buffer.Slice(1);
                return (uint)tmp;
            }
            int result = tmp & 0x7f;
            if ((tmp = span[1]) < 128)
            {
                buffer = buffer.Slice(2);
                result |= tmp << 7;
            }
            else
            {
                result |= (tmp & 0x7f) << 7;
                if ((tmp = span[2]) < 128)
                {
                    buffer = buffer.Slice(3);
                    result |= tmp << 14;
                }
                else
                {
                    result |= (tmp & 0x7f) << 14;
                    if ((tmp = span[3]) < 128)
                    {
                        buffer = buffer.Slice(4);
                        result |= tmp << 21;
                    }
                    else
                    {
                        result |= (tmp & 0x7f) << 21;
                        buffer = buffer.Slice(5);
                        result |= (tmp = span[4]) << 28;
                        if (tmp >= 128)
                        {
                            // Discard upper 32 bits.
                            DiscardUpperVarIntBits(ref buffer, 5);
                            return (uint)result;
                        }
                    }
                }
            }
            return (uint)result;
        }

        private static ulong ReadRawVarint64(ref ReadOnlySequenceState<byte> buffer) => SlowReadRawVarint64(ref buffer);

        private static ulong SlowReadRawVarint64(ref ReadOnlySequenceState<byte> buffer)
        {
            int shift = 0;
            ulong result = 0;
            while (shift < 64)
            {
                byte b = ReadRawByte(ref buffer);
                result |= (ulong)(b & 0x7F) << shift;
                if ((b & 0x80) == 0)
                {
                    return result;
                }
                shift += 7;
            }
            //TODO: Implement properly
            throw new Exception();
            //throw InvalidProtocolBufferException.MalformedVarint();
        }

        private static uint SlowReadRawVarint32(ref ReadOnlySequenceState<byte> buffer)
        {
            int tmp = ReadRawByte(ref buffer);
            if (tmp < 128)
            {
                return (uint)tmp;
            }
            int result = tmp & 0x7f;
            if ((tmp = ReadRawByte(ref buffer)) < 128)
            {
                result |= tmp << 7;
            }
            else
            {
                result |= (tmp & 0x7f) << 7;
                if ((tmp = ReadRawByte(ref buffer)) < 128)
                {
                    result |= tmp << 14;
                }
                else
                {
                    result |= (tmp & 0x7f) << 14;
                    if ((tmp = ReadRawByte(ref buffer)) < 128)
                    {
                        result |= tmp << 21;
                    }
                    else
                    {
                        result |= (tmp & 0x7f) << 21;
                        result |= (tmp = ReadRawByte(ref buffer)) << 28;
                        if (tmp >= 128)
                        {
                            // Discard upper 32 bits.
                            for (int i = 0; i < 5; i++)
                            {
                                if (ReadRawByte(ref buffer) < 128)
                                {
                                    return (uint)result;
                                }
                            }
                            //TODO: Fix
                            //throw InvalidProtocolBufferException.MalformedVarint();
                            throw new Exception();
                        }
                    }
                }
            }
            return (uint)result;
        }

        private static void DiscardUpperVarIntBits(ref ReadOnlySequenceState<byte> buffer, int count)
        {
            // Note that this has to use ReadRawByte() as we only ensure we've
            // got at least n bytes at the start of the method. This lets us
            // use the fast path in more cases, and we rarely hit this section of code.
            for (int i = 0; i < count; i++)
            {
                if (ReadRawByte(ref buffer) < 128)
                {
                    return;
                }
            }
            //TODO: Fix
            //throw InvalidProtocolBufferException.MalformedVarint();
            throw new Exception();
        }

        private static byte ReadRawByte(ref ReadOnlySequenceState<byte> buffer)
        {
            if (buffer.First.IsEmpty)
            {
                //TODO: Fix
                //throw InvalidProtocolBufferException.TruncatedMessage();
                throw new Exception();
            }
            var ret = buffer.First[0];
            buffer = buffer.Slice(1);
            return ret;
        }

        private static uint ReadFixed32(ref ReadOnlySequenceState<byte> buffer)
        {
            if (buffer.First.Length >= 4)
            {
                var span = buffer.First;
                buffer = buffer.Slice(4);
                return BinaryPrimitives.ReadUInt32LittleEndian(span);
            }
            else if (buffer.Buffer.Length >= 4)
            {
                Span<byte> span = stackalloc byte[4];
                buffer.Buffer.CopyTo(span);
                buffer = buffer.Slice(4);
                return BinaryPrimitives.ReadUInt32LittleEndian(span);
            }
            else
            {
                //TODO: Proper exception
                throw new Exception();
            }
        }

        private static ulong ReadFixed64(ref ReadOnlySequenceState<byte> buffer)
        {
            if (buffer.First.Length >= 8)
            {
                var span = buffer.First;
                buffer = buffer.Slice(8);
                return BinaryPrimitives.ReadUInt64LittleEndian(span);
            }
            else if (buffer.Buffer.Length >= 8)
            {
                Span<byte> span = stackalloc byte[8];
                buffer.Buffer.CopyTo(span);
                buffer = buffer.Slice(8);
                return BinaryPrimitives.ReadUInt64LittleEndian(span);
            }
            else
            {
                //TODO: Proper exception
                throw new Exception();
            }
        }

        private static void SkipField(ref ReadOnlySequenceState<byte> buffer, in uint tag, in int remainingRecursionLevels)
        {
            if (tag == 0)
            {
                throw new InvalidOperationException("SkipLastField cannot be called at the end of a stream");
            }

            switch (WireFormat.GetTagWireType(tag))
            {
                case WireFormat.WireType.StartGroup:
                    SkipGroup(ref buffer, tag, remainingRecursionLevels);
                    break;
                case WireFormat.WireType.EndGroup:
                    //TODO: Implement properly
                    throw new Exception();
                //throw new InvalidProtocolBufferException("SkipLastField called on an end-group tag, indicating that the corresponding start-group was missing");
                case WireFormat.WireType.Fixed32:
                    SkipRawBytes(ref buffer, 4);
                    break;
                case WireFormat.WireType.Fixed64:
                    SkipRawBytes(ref buffer, 8);
                    break;
                case WireFormat.WireType.LengthDelimited:
                    var length = ReadLength(ref buffer);
                    SkipRawBytes(ref buffer, length);
                    break;
                case WireFormat.WireType.Varint:
                    ReadRawVarint32(ref buffer);
                    break;
            }
        }

        private static void SkipRawBytes(ref ReadOnlySequenceState<byte> buffer, in int size)
        {
            if (buffer.Buffer.Length < size)
            {
                //TODO: Implement properly
                //throw InvalidProtocolBufferException.TruncatedMessage();
                throw new Exception();
            }

            buffer = buffer.Slice(size);
        }

        private static void SkipGroup(ref ReadOnlySequenceState<byte> buffer, in uint startGroupTag, in int remainingRecursionLevels)
        {
            // Note: Currently we expect this to be the way that groups are read. We could put the recursion
            // depth changes into the ReadTag method instead, potentially...
            if (remainingRecursionLevels <= 0)
            {
                //TODO: add proper exception
                throw new Exception();
                //throw InvalidProtocolBufferException.RecursionLimitExceeded();
            }
            uint tag;
            while (true)
            {
                tag = ReadTag(ref buffer);
                if (tag == 0)
                {
                    throw new Exception();
                    //TODO: Implement properly
                    //throw InvalidProtocolBufferException.TruncatedMessage();
                }
                // Can't call SkipLastField for this case- that would throw.
                if (WireFormat.GetTagWireType(tag) == WireFormat.WireType.EndGroup)
                {
                    break;
                }
                // This recursion will allow us to handle nested groups.
                SkipField(ref buffer, tag, remainingRecursionLevels - 1);
            }
            int startField = WireFormat.GetTagFieldNumber(startGroupTag);
            int endField = WireFormat.GetTagFieldNumber(tag);
            if (startField != endField)
            {
                throw new Exception();
                //TODO: Implement properly
                //throw new InvalidProtocolBufferException(
                //    $"Mismatched end-group tag. Started with field {startField}; ended with field {endField}");
            }
        }

        /// <summary>
        /// Decode a 32-bit value with ZigZag encoding.
        /// </summary>
        /// <remarks>
        /// ZigZag encodes signed integers into values that can be efficiently
        /// encoded with varint.  (Otherwise, negative values must be 
        /// sign-extended to 64 bits to be varint encoded, thus always taking
        /// 10 bytes on the wire.)
        /// </remarks>
        public static int DecodeZigZag32(uint n) => (int)(n >> 1) ^ -(int)(n & 1);

        /// <summary>
        /// Decode a 64-bit value with ZigZag encoding.
        /// </summary>
        /// <remarks>
        /// ZigZag encodes signed integers into values that can be efficiently
        /// encoded with varint.  (Otherwise, negative values must be 
        /// sign-extended to 64 bits to be varint encoded, thus always taking
        /// 10 bytes on the wire.)
        /// </remarks>
        public static long DecodeZigZag64(ulong n) => (long)(n >> 1) ^ -(long)(n & 1);

        //TODO: Use proper BitConverter.Int32BitsToSingle when .NET Standard is out - alternatively use unsafe implementation
#if UNSAFE
        internal static unsafe float Int32BitsToSingle(int n) => *((float*)&n);
#else
        internal static float Int32BitsToSingle(int n) => BitConverter.ToSingle(BitConverter.GetBytes(n), 0);
#endif
    }
}
