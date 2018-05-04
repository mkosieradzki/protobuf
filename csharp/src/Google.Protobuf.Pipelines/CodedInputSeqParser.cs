using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Google.Protobuf
{
    public static class CodedInputSeqParser
    {
        public static int ReadLength(in ReadOnlySequence<byte> buffer, ref SequencePosition position, out int bytesConsumed) => (int)ReadRawVarint32(buffer, ref position, out bytesConsumed);

        public static int ReadInt32(in ReadOnlySequence<byte> buffer, ref SequencePosition position, out int bytesConsumed) => (int)ReadRawVarint32(buffer, ref position, out bytesConsumed);

        public static uint ReadTag(in ReadOnlySequence<byte> buffer, ref SequencePosition position, out int bytesConsumed, bool advance = true)
        {
            bytesConsumed = 0;
            if (!buffer.TryGet(ref position, out var memory, false) || memory.IsEmpty)
            {
                return 0;
            }
            var span = memory.Span;

            // Optimize for the incredibly common case of having at least two bytes left in the buffer,
            // and those two bytes being enough to get the tag. This will be true for fields up to 4095.
            if (span.Length >= 2)// bufferPos + 2 <= bufferSize)
            {
                uint tag;

                int tmp = span[0];
                if (tmp < 128)
                {
                    tag = (uint)tmp;
                    bytesConsumed = 1;
                    if (advance)
                    {
                        position = buffer.GetPosition(1, position);
                    }
                }
                else
                {
                    int result = tmp & 0x7f;
                    if ((tmp = span[1]) < 128)
                    {
                        result |= tmp << 7;
                        tag = (uint)result;
                        bytesConsumed = 2;
                        if (advance)
                        {
                            position = buffer.GetPosition(2, position);
                        }
                    }
                    else
                    {
                        return ReadRawVarint32(buffer, ref position, out bytesConsumed, advance);
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
            else
            {
                return ReadRawVarint32(buffer, ref position, out bytesConsumed, advance);
            }
        }

        public static uint ReadRawVarint32(in ReadOnlySequence<byte> buffer, ref SequencePosition position, out int bytesConsumed, bool advance = true)
        {
            bytesConsumed = 0;
            if (!buffer.TryGet(ref position, out var memory, false) || memory.IsEmpty)
            {
                throw new Exception();
            }
            var span = memory.Span;
            if (span.Length < 5)
            {
                return SlowReadRawVarint32(buffer, ref position, out bytesConsumed, advance);
            }

            int tmp = span[0];
            if (tmp < 128)
            {
                if (advance)
                {
                    position = buffer.GetPosition(1, position);
                }
                bytesConsumed += 1;
                return (uint)tmp;
            }
            int result = tmp & 0x7f;
            if ((tmp = span[1]) < 128)
            {
                bytesConsumed += 2;
                result |= tmp << 7;
            }
            else
            {
                result |= (tmp & 0x7f) << 7;
                if ((tmp = span[2]) < 128)
                {
                    bytesConsumed += 3;
                    result |= tmp << 14;
                }
                else
                {
                    result |= (tmp & 0x7f) << 14;
                    if ((tmp = span[3]) < 128)
                    {
                        bytesConsumed += 4;
                        result |= tmp << 21;
                    }
                    else
                    {
                        result |= (tmp & 0x7f) << 21;
                        bytesConsumed += 5;
                        result |= (tmp = span[4]) << 28;
                        if (tmp >= 128)
                        {
                            if (advance)
                            {
                                position = buffer.GetPosition(bytesConsumed, position);
                            }
                            // Discard upper 32 bits.
                            DiscardUpperVarIntBits(buffer, ref position, 5, ref bytesConsumed, advance);
                            return (uint)result;
                        }
                    }
                }
            }
            if (advance)
            {
                position = buffer.GetPosition(bytesConsumed, position);
            }
            return (uint)result;
        }

        public static ulong ReadRawVarint64(in ReadOnlySequence<byte> buffer, ref SequencePosition position, out int bytesConsumed, bool advance = true) => SlowReadRawVarint64(buffer, ref position, out bytesConsumed, advance);

        private static ulong SlowReadRawVarint64(in ReadOnlySequence<byte> buffer, ref SequencePosition position, out int bytesConsumed, bool advance)
        {
            bytesConsumed = 0;
            int shift = 0;
            ulong result = 0;
            while (shift < 64)
            {
                byte b = ReadRawByte(buffer, ref position, ref bytesConsumed, advance);
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

        private static uint SlowReadRawVarint32(in ReadOnlySequence<byte> buffer, ref SequencePosition position, out int bytesConsumed, bool advance)
        {
            bytesConsumed = 0;
            int tmp = ReadRawByte(buffer, ref position, ref bytesConsumed, advance);
            if (tmp < 128)
            {
                return (uint)tmp;
            }
            int result = tmp & 0x7f;
            if ((tmp = ReadRawByte(buffer, ref position, ref bytesConsumed, advance)) < 128)
            {
                result |= tmp << 7;
            }
            else
            {
                result |= (tmp & 0x7f) << 7;
                if ((tmp = ReadRawByte(buffer, ref position, ref bytesConsumed, advance)) < 128)
                {
                    result |= tmp << 14;
                }
                else
                {
                    result |= (tmp & 0x7f) << 14;
                    if ((tmp = ReadRawByte(buffer, ref position, ref bytesConsumed, advance)) < 128)
                    {
                        result |= tmp << 21;
                    }
                    else
                    {
                        result |= (tmp & 0x7f) << 21;
                        result |= (tmp = ReadRawByte(buffer, ref position, ref bytesConsumed, advance)) << 28;
                        if (tmp >= 128)
                        {
                            // Discard upper 32 bits.
                            for (int i = 0; i < 5; i++)
                            {
                                if (ReadRawByte(buffer, ref position, ref bytesConsumed, advance) < 128)
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

        private static void DiscardUpperVarIntBits(in ReadOnlySequence<byte> buffer, ref SequencePosition position, int count, ref int bytesConsumed, bool advance)
        {
            // Note that this has to use ReadRawByte() as we only ensure we've
            // got at least n bytes at the start of the method. This lets us
            // use the fast path in more cases, and we rarely hit this section of code.
            for (int i = 0; i < count; i++)
            {
                if (ReadRawByte(buffer, ref position, ref bytesConsumed, advance) < 128)
                {
                    return;
                }
            }
            //TODO: Fix
            //throw InvalidProtocolBufferException.MalformedVarint();
            throw new Exception();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte ReadRawByte(in ReadOnlySequence<byte> buffer, ref SequencePosition position, ref int bytesConsumed, bool advance)
        {
            if (!buffer.TryGet(ref position, out var memory, false) || memory.IsEmpty)
            {
                //TODO: Fix
                //throw InvalidProtocolBufferException.TruncatedMessage();
                throw new Exception();
            }
            if (advance)
            {
                position = buffer.GetPosition(1, position);
            }
            bytesConsumed += 1;
            return memory.Span[0];
        }

        public static uint ReadFixed32(in ReadOnlySequence<byte> buffer, ref SequencePosition position, bool advance = true)
        {
            if (!buffer.TryGet(ref position, out var memory, false) || memory.IsEmpty)
            {
                //TODO: Proper exception
                throw new Exception();
            }
            else if (memory.Length >= 4)
            {
                if (advance)
                {
                    position = buffer.GetPosition(4, position);
                }
                return BinaryPrimitives.ReadUInt32LittleEndian(memory.Span);
            }
            else
            {
                var remaining = buffer.Slice(position);
                if (remaining.Length < 4)
                {
                    //TODO: Proper exception
                    throw new Exception();
                }
                if (advance)
                {
                    position = buffer.GetPosition(4, position);
                }
                Span<byte> span = stackalloc byte[4];
                remaining.CopyTo(span);
                return BinaryPrimitives.ReadUInt32LittleEndian(span);
            }
        }

        public static ulong ReadFixed64(in ReadOnlySequence<byte> buffer, ref SequencePosition position, bool advance = true)
        {
            if (!buffer.TryGet(ref position, out var memory, false) || memory.IsEmpty)
            {
                //TODO: Proper exception
                throw new Exception();
            }
            else if (memory.Length >= 8)
            {
                if (advance)
                {
                    position = buffer.GetPosition(8, position);
                }
                return BinaryPrimitives.ReadUInt64LittleEndian(memory.Span);
            }
            else
            {
                var remaining = buffer.Slice(position);
                if (remaining.Length < 8)
                {
                    //TODO: Proper exception
                    throw new Exception();
                }
                if (advance)
                {
                    position = buffer.GetPosition(8, position);
                }
                Span<byte> span = stackalloc byte[8];
                remaining.CopyTo(span);
                return BinaryPrimitives.ReadUInt64LittleEndian(span);
            }
        }

        private static void SkipField(in ReadOnlySequence<byte> buffer, ref SequencePosition position, in uint tag, in int remainingRecursionLevels, out int bytesConsumed)
        {
            if (tag == 0)
            {
                throw new InvalidOperationException("SkipLastField cannot be called at the end of a stream");
            }

            switch (WireFormat.GetTagWireType(tag))
            {
                case WireFormat.WireType.StartGroup:
                    SkipGroup(buffer, ref position, tag, remainingRecursionLevels, out bytesConsumed);
                    break;
                case WireFormat.WireType.EndGroup:
                    //TODO: Implement properly
                    throw new Exception();
                //throw new InvalidProtocolBufferException("SkipLastField called on an end-group tag, indicating that the corresponding start-group was missing");
                case WireFormat.WireType.Fixed32:
                    SkipRawBytes(buffer, ref position, 4);
                    bytesConsumed = 4;
                    break;
                case WireFormat.WireType.Fixed64:
                    SkipRawBytes(buffer, ref position, 8);
                    bytesConsumed = 8;
                    break;
                case WireFormat.WireType.LengthDelimited:
                    var length = ReadLength(buffer, ref position, out bytesConsumed);
                    SkipRawBytes(buffer, ref position, length);
                    bytesConsumed += length;
                    break;
                case WireFormat.WireType.Varint:
                    ReadRawVarint32(buffer, ref position, out bytesConsumed);
                    break;
                default:
                    throw new Exception();
            }
        }

        private static void SkipRawBytes(in ReadOnlySequence<byte> buffer, ref SequencePosition position, int count)
        {
            while (count >= 0)
            {
                var savedPosition = position;
                if (!buffer.TryGet(ref position, out var memory, true) || memory.IsEmpty)
                {
                    //TODO: Implement properly
                    //throw InvalidProtocolBufferException.TruncatedMessage();
                    throw new Exception();
                }
                if (count < memory.Length)
                {
                    position = buffer.GetPosition(count, savedPosition);
                }
                else
                {
                    count -= memory.Length;
                }
            }
        }

        private static void SkipGroup(in ReadOnlySequence<byte> buffer, ref SequencePosition position, in uint startGroupTag, in int remainingRecursionLevels, out int bytesConsumed)
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
                tag = ReadTag(buffer, ref position, out bytesConsumed);
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
                SkipField(buffer, ref position, tag, remainingRecursionLevels - 1, out bytesConsumed);
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
