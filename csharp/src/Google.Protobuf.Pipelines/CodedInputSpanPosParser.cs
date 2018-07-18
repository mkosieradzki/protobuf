using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Google.Protobuf
{
    public interface IMessageSupportsSpan
    {
        void MergeFrom(in ReadOnlySpan<byte> span, ref int pos);
    }

    public static class CodedInputSpanPosParser
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ReadMessage<T>(in ReadOnlySpan<byte> span, ref int pos, T message) where T : IMessageSupportsSpan
        {
            var nestedBuffer = ReadLengthDelimited(in span, ref pos);
            var nestedPos = 0;
            message.MergeFrom(in nestedBuffer, ref nestedPos);
            return message;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ReadMessage<T>(in ReadOnlySpan<byte> span, ref int pos, Func<T> factory) where T : IMessageSupportsSpan
        {
            var message = factory();
            var nestedBuffer = ReadLengthDelimited(in span, ref pos);
            var nestedPos = 0;
            message.MergeFrom(in nestedBuffer, ref nestedPos);
            return message;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadLength(in ReadOnlySpan<byte> span, ref int pos) => (int)ReadRawVarint32(in span, ref pos);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<byte> ReadLengthDelimited(in ReadOnlySpan<byte> span, ref int pos)
        {
            var length = ReadLength(in span, ref pos);
            if (pos + length > span.Length)
                throw new Exception();
            var ret = span.Slice(pos, length);
            pos += length;
            return ret;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadInt32(in ReadOnlySpan<byte> span, ref int pos) => (int)ReadRawVarint32(in span, ref pos);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReadTag(in ReadOnlySpan<byte> span, ref int pos)
        {
            // Optimize for the incredibly common case of having at least two bytes left in the buffer,
            // and those two bytes being enough to get the tag. This will be true for fields up to 4095.
            if (span.Length >= pos + 2)// bufferPos + 2 <= bufferSize)
            {
                uint tag;

                int tmp = span[pos];
                if (tmp < 128)
                {
                    tag = (uint)tmp;
                    pos++;
                }
                else
                {
                    int result = tmp & 0x7f;
                    if ((tmp = span[pos + 1]) < 128)
                    {
                        result |= tmp << 7;
                        tag = (uint)result;
                        pos += 2;
                    }
                    else
                    {
                        return ReadRawVarint32(in span, ref pos);
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
            else if (pos >= span.Length)
            {
                return 0;
            }
            else
            {
                return ReadRawVarint32(in span, ref pos);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReadRawVarint32(in ReadOnlySpan<byte> span, ref int pos)
        {
            int tmp = span[pos++];
            if (tmp < 128)
            {
                return (uint)tmp;
            }
            int result = tmp & 0x7f;
            if ((tmp = span[pos++]) < 128)
            {
                result |= tmp << 7;
            }
            else
            {
                result |= (tmp & 0x7f) << 7;
                if ((tmp = span[pos++]) < 128)
                {
                    result |= tmp << 14;
                }
                else
                {
                    result |= (tmp & 0x7f) << 14;
                    if ((tmp = span[pos++]) < 128)
                    {
                        result |= tmp << 21;
                    }
                    else
                    {
                        result |= (tmp & 0x7f) << 21;
                        result |= (tmp = span[pos++]) << 28;
                        if (tmp >= 128)
                        {
                            // Discard upper 32 bits.
                            DiscardUpperVarIntBits(in span, ref pos, 5);
                            return (uint)result;
                        }
                    }
                }
            }
            return (uint)result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ReadRawVarint64(in ReadOnlySpan<byte> span, ref int pos) => SlowReadRawVarint64(in span, ref pos);

        private static ulong SlowReadRawVarint64(in ReadOnlySpan<byte> span, ref int pos)
        {
            int shift = 0;
            ulong result = 0;
            while (shift < 64)
            {
                byte b = ReadRawByte(in span, ref pos);
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

        private static uint SlowReadRawVarint32(in ReadOnlySpan<byte> span, ref int pos)
        {
            int tmp = ReadRawByte(in span, ref pos);
            if (tmp < 128)
            {
                return (uint)tmp;
            }
            int result = tmp & 0x7f;
            if ((tmp = ReadRawByte(in span, ref pos)) < 128)
            {
                result |= tmp << 7;
            }
            else
            {
                result |= (tmp & 0x7f) << 7;
                if ((tmp = ReadRawByte(in span, ref pos)) < 128)
                {
                    result |= tmp << 14;
                }
                else
                {
                    result |= (tmp & 0x7f) << 14;
                    if ((tmp = ReadRawByte(in span, ref pos)) < 128)
                    {
                        result |= tmp << 21;
                    }
                    else
                    {
                        result |= (tmp & 0x7f) << 21;
                        result |= (tmp = ReadRawByte(in span, ref pos)) << 28;
                        if (tmp >= 128)
                        {
                            // Discard upper 32 bits.
                            for (int i = 0; i < 5; i++)
                            {
                                if (ReadRawByte(in span, ref pos) < 128)
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

        private static void DiscardUpperVarIntBits(in ReadOnlySpan<byte> span, ref int pos, int count)
        {
            // Note that this has to use ReadRawByte() as we only ensure we've
            // got at least n bytes at the start of the method. This lets us
            // use the fast path in more cases, and we rarely hit this section of code.
            for (int i = 0; i < count; i++)
            {
                if (ReadRawByte(in span, ref pos) < 128)
                {
                    return;
                }
            }
            //TODO: Fix
            //throw InvalidProtocolBufferException.MalformedVarint();
            throw new Exception();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ReadRawByte(in ReadOnlySpan<byte> span, ref int pos)
        {
            if (pos >= span.Length)
            {
                //TODO: Fix
                //throw InvalidProtocolBufferException.TruncatedMessage();
                throw new Exception();
            }
            var ret = span[pos++];
            return ret;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReadFixed32(in ReadOnlySpan<byte> span, ref int pos)
        {
            if (span.Length >= pos + 4)
            {
                var ret = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(pos));
                pos += 4;
                return ret;
            }
            else
            {
                //TODO: Proper exception
                throw new Exception();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ReadFixed64(in ReadOnlySpan<byte> span, ref int pos)
        {
            if (span.Length >= pos + 8)
            {
                var ret =  BinaryPrimitives.ReadUInt64LittleEndian(span.Slice(pos));
                pos += 8;
                return ret;
            }
            else
            {
                //TODO: Proper exception
                throw new Exception();
            }
        }

        private static void SkipField(in ReadOnlySpan<byte> span, ref int pos, in uint tag, int remainingRecursionLevels)
        {
            if (tag == 0)
            {
                throw new InvalidOperationException("SkipLastField cannot be called at the end of a stream");
            }

            switch (WireFormat.GetTagWireType(tag))
            {
                case WireFormat.WireType.StartGroup:
                    SkipGroup(in span, ref pos, tag, remainingRecursionLevels);
                    break;
                case WireFormat.WireType.EndGroup:
                    //TODO: Implement properly
                    throw new Exception();
                //throw new InvalidProtocolBufferException("SkipLastField called on an end-group tag, indicating that the corresponding start-group was missing");
                case WireFormat.WireType.Fixed32:
                    SkipRawBytes(in span, ref pos, 4);
                    break;
                case WireFormat.WireType.Fixed64:
                    SkipRawBytes(in span, ref pos, 8);
                    break;
                case WireFormat.WireType.LengthDelimited:
                    var length = ReadLength(in span, ref pos);
                    SkipRawBytes(in span, ref pos, (int)length);
                    break;
                case WireFormat.WireType.Varint:
                    ReadRawVarint32(in span, ref pos);
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SkipRawBytes(in ReadOnlySpan<byte> span, ref int pos, int size)
        {
            if (span.Length < pos + size)
            {
                //TODO: Implement properly
                //throw InvalidProtocolBufferException.TruncatedMessage();
                throw new Exception();
            }

            pos += size;
        }

        public static void SkipGroup(in ReadOnlySpan<byte> buffer, ref int pos, in uint startGroupTag, int remainingRecursionLevels)
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
                tag = ReadTag(in buffer, ref pos);
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
                SkipField(in buffer, ref pos, tag, remainingRecursionLevels - 1);
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long DecodeZigZag64(ulong n) => (long)(n >> 1) ^ -(long)(n & 1);

        //TODO: Use proper BitConverter.Int32BitsToSingle when .NET Standard is out - alternatively use unsafe implementation
#if UNSAFE
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe float Int32BitsToSingle(int n) => *((float*)&n);
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Int32BitsToSingle(int n) => BitConverter.ToSingle(BitConverter.GetBytes(n), 0);
#endif
    }
}
