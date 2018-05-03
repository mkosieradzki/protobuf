using System;
using System.Buffers;
using System.Buffers.Binary;

namespace Google.Protobuf
{
    public static class CodedInputSeqParser
    {
        public static object ReadMessage(in ReadOnlySequence<byte> buffer, ref SequencePosition position, in IReadableMessageType messageType, in int maxRecursionLevels = 32)
        {
            var message = messageType.CreateMessage();

            //TODO: Add support for packed encoding
            uint tag, prevTag = 0;
            FieldInfo fieldInfo = default;
            WireFormat.WireType wireType = default;
            while ((tag = ReadTag(buffer, ref position)) != 0)
            {
                // Do not ask again for field info in case of a repeated field
                if (tag != prevTag)
                {
                    fieldInfo = messageType.GetFieldInfo(tag);
                    wireType = WireFormat.GetTagWireType(tag);
                    prevTag = tag;
                }

                switch (wireType)
                {
                    case WireFormat.WireType.Varint:
                        switch (fieldInfo.ValueType)
                        {
                            case ValueType.Unknown:
                                ReadRawVarint32(buffer, ref position);
                                break;
                            case ValueType.Int32:
                                messageType.ConsumeField(message, tag, (int)ReadRawVarint32(buffer, ref position));
                                break;
                            case ValueType.Int64:
                                messageType.ConsumeField(message, tag, (long)ReadRawVarint64(buffer, ref position));
                                break;
                            case ValueType.UInt32:
                                messageType.ConsumeField(message, tag, ReadRawVarint32(buffer, ref position));
                                break;
                            case ValueType.UInt64:
                                messageType.ConsumeField(message, tag, ReadRawVarint64(buffer, ref position));
                                break;
                            case ValueType.SInt32:
                                messageType.ConsumeField(message, tag, DecodeZigZag32(ReadRawVarint32(buffer, ref position)));
                                break;
                            case ValueType.SInt64:
                                messageType.ConsumeField(message, tag, DecodeZigZag64(ReadRawVarint64(buffer, ref position)));
                                break;
                            case ValueType.Bool:
                                messageType.ConsumeField(message, tag, ReadRawVarint32(buffer, ref position) != 0);
                                break;
                            case ValueType.Enum:
                                messageType.ConsumeField(message, tag, (int)ReadRawVarint32(buffer, ref position));
                                break;
                            default:
                                //TODO: Consider skipping instead
                                throw new Exception();
                        }
                        break;
                    case WireFormat.WireType.Fixed32:
                        switch (fieldInfo.ValueType)
                        {
                            case ValueType.Unknown:
                                SkipRawBytes(buffer, ref position, 4);
                                break;
                            case ValueType.Float:
                                messageType.ConsumeField(message, tag, Int32BitsToSingle((int)ReadFixed32(buffer, ref position)));
                                break;
                            case ValueType.Fixed32:
                                messageType.ConsumeField(message, tag, ReadFixed32(buffer, ref position));
                                break;
                            case ValueType.SFixed32:
                                messageType.ConsumeField(message, tag, (int)ReadFixed32(buffer, ref position));
                                break;
                            default:
                                throw new Exception();
                        }
                        break;
                    case WireFormat.WireType.Fixed64:
                        switch (fieldInfo.ValueType)
                        {
                            case ValueType.Unknown:
                                SkipRawBytes(buffer, ref position, 8);
                                break;
                            case ValueType.Double:
                                messageType.ConsumeField(message, tag, BitConverter.Int64BitsToDouble((long)ReadFixed64(buffer, ref position)));
                                break;
                            case ValueType.Fixed64:
                                messageType.ConsumeField(message, tag, ReadFixed64(buffer, ref position));
                                break;
                            case ValueType.SFixed64:
                                messageType.ConsumeField(message, tag, (long)ReadFixed64(buffer, ref position));
                                break;
                            default:
                                throw new Exception();
                        }
                        break;
                    case WireFormat.WireType.LengthDelimited:
                        var length = ReadLength(buffer, ref position);
                        if (maxRecursionLevels <= 0)
                        {
                            throw new Exception();
                            //TODO: Handle recursion limit
                            //throw InvalidProtocolBufferException.RecursionLimitExceeded();
                        }

                        if (length == 0)
                        {
                            break;
                        }

                        //TODO: Huge optimization potential
                        if (!buffer.TryGet(ref position, out var memory, false) || memory.IsEmpty)
                        {
                            throw new Exception();
                        }

                        if (memory.Length >= length)
                        {
                            var nestedSpan = memory.Span.Slice(0, length);

                            position = buffer.GetPosition(length, position);

                            switch (fieldInfo.ValueType)
                            {
                                case ValueType.Unknown:
                                    break;
                                case ValueType.Double:
                                    //TODO: Support packed enoding
                                    throw new NotImplementedException();
                                case ValueType.Float:
                                    throw new NotImplementedException();
                                case ValueType.Int32:
                                    throw new NotImplementedException();
                                case ValueType.Int64:
                                    throw new NotImplementedException();
                                case ValueType.UInt32:
                                    throw new NotImplementedException();
                                case ValueType.UInt64:
                                    throw new NotImplementedException();
                                case ValueType.SInt32:
                                    throw new NotImplementedException();
                                case ValueType.SInt64:
                                    throw new NotImplementedException();
                                case ValueType.Fixed32:
                                    throw new NotImplementedException();
                                case ValueType.Fixed64:
                                    throw new NotImplementedException();
                                case ValueType.SFixed32:
                                    throw new NotImplementedException();
                                case ValueType.SFixed64:
                                    throw new NotImplementedException();
                                case ValueType.Bool:
                                    throw new NotImplementedException();
                                case ValueType.Enum:
                                    throw new NotImplementedException();
                                case ValueType.String:
                                    messageType.ConsumeSpanField(message, tag, nestedSpan);
                                    break;
                                case ValueType.Bytes:
                                    messageType.ConsumeSpanField(message, tag, nestedSpan);
                                    break;
                                case ValueType.Message:
                                    var nestedPosition = 0;
                                    messageType.ConsumeField(message, tag, CodedInputSpanPosParser.ReadMessage(nestedSpan, ref nestedPosition, fieldInfo.MessageType, maxRecursionLevels - 1));
                                    break;
                                default:
                                    throw new Exception();
                            }
                            break;
                        }


                        //if (length > buffer.Length)
                        //{
                        //    throw new Exception();
                        //    //TODO: Better exception
                        //}
                        ReadOnlySequence<byte> nestedBuffer;
                        try
                        {
                            nestedBuffer = buffer.Slice(position, length);
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            //TODO: Add better exception
                            throw new Exception();
                        }
                        position = buffer.GetPosition(length, position);

                        switch (fieldInfo.ValueType)
                        {
                            case ValueType.Unknown:
                                break;
                            case ValueType.Double:
                                //TODO: Support packed enoding
                                throw new NotImplementedException();
                            case ValueType.Float:
                                throw new NotImplementedException();
                            case ValueType.Int32:
                                throw new NotImplementedException();
                            case ValueType.Int64:
                                throw new NotImplementedException();
                            case ValueType.UInt32:
                                throw new NotImplementedException();
                            case ValueType.UInt64:
                                throw new NotImplementedException();
                            case ValueType.SInt32:
                                throw new NotImplementedException();
                            case ValueType.SInt64:
                                throw new NotImplementedException();
                            case ValueType.Fixed32:
                                throw new NotImplementedException();
                            case ValueType.Fixed64:
                                throw new NotImplementedException();
                            case ValueType.SFixed32:
                                throw new NotImplementedException();
                            case ValueType.SFixed64:
                                throw new NotImplementedException();
                            case ValueType.Bool:
                                throw new NotImplementedException();
                            case ValueType.Enum:
                                throw new NotImplementedException();
                            case ValueType.String:
                                messageType.ConsumeField(message, tag, nestedBuffer);
                                break;
                            case ValueType.Bytes:
                                messageType.ConsumeField(message, tag, nestedBuffer);
                                break;
                            case ValueType.Message:
                                var nestedPosition = nestedBuffer.Start;
                                messageType.ConsumeField(message, tag, ReadMessage(nestedBuffer, ref nestedPosition, fieldInfo.MessageType, maxRecursionLevels - 1));
                                break;
                            default:
                                throw new Exception();
                        }
                        break;
                    case WireFormat.WireType.StartGroup:
                        SkipGroup(buffer, ref position, tag, maxRecursionLevels);
                        break;
                    case WireFormat.WireType.EndGroup:
                        //TODO: Add proper exception
                        throw new Exception();
                    default:
                        //TODO: Add proper exception
                        throw new Exception();
                }
            }

            return messageType.CompleteMessage(message);
        }

        public static int ReadLength(in ReadOnlySequence<byte> buffer, ref SequencePosition position) => (int)ReadRawVarint32(buffer, ref position);

        public static int ReadInt32(in ReadOnlySequence<byte> buffer, ref SequencePosition position) => (int)ReadRawVarint32(buffer, ref position);

        public static uint ReadTag(in ReadOnlySequence<byte> buffer, ref SequencePosition position)
        {
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
                    position = buffer.GetPosition(1, position);
                }
                else
                {
                    int result = tmp & 0x7f;
                    if ((tmp = span[1]) < 128)
                    {
                        result |= tmp << 7;
                        tag = (uint)result;
                        position = buffer.GetPosition(2, position);
                    }
                    else
                    {
                        return ReadRawVarint32(buffer, ref position);
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
                return ReadRawVarint32(buffer, ref position);
            }
        }

        private static uint ReadRawVarint32(in ReadOnlySequence<byte> buffer, ref SequencePosition position)
        {
            if (!buffer.TryGet(ref position, out var memory, false) || memory.IsEmpty)
            {
                throw new Exception();
            }
            var span = memory.Span;
            if (span.Length < 5)
            {
                return SlowReadRawVarint32(buffer, ref position);
            }

            int tmp = span[0];
            if (tmp < 128)
            {
                position = buffer.GetPosition(1, position);
                return (uint)tmp;
            }
            int result = tmp & 0x7f;
            if ((tmp = span[1]) < 128)
            {
                position = buffer.GetPosition(2, position);
                result |= tmp << 7;
            }
            else
            {
                result |= (tmp & 0x7f) << 7;
                if ((tmp = span[2]) < 128)
                {
                    position = buffer.GetPosition(3, position);
                    result |= tmp << 14;
                }
                else
                {
                    result |= (tmp & 0x7f) << 14;
                    if ((tmp = span[3]) < 128)
                    {
                        position = buffer.GetPosition(4, position);
                        result |= tmp << 21;
                    }
                    else
                    {
                        result |= (tmp & 0x7f) << 21;
                        position = buffer.GetPosition(5, position);
                        result |= (tmp = span[4]) << 28;
                        if (tmp >= 128)
                        {
                            // Discard upper 32 bits.
                            DiscardUpperVarIntBits(buffer, ref position, 5);
                            return (uint)result;
                        }
                    }
                }
            }
            return (uint)result;
        }

        private static ulong ReadRawVarint64(in ReadOnlySequence<byte> buffer, ref SequencePosition position) => SlowReadRawVarint64(buffer, ref position);

        private static ulong SlowReadRawVarint64(in ReadOnlySequence<byte> buffer, ref SequencePosition position)
        {
            int shift = 0;
            ulong result = 0;
            while (shift < 64)
            {
                byte b = ReadRawByte(buffer, ref position);
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

        private static uint SlowReadRawVarint32(in ReadOnlySequence<byte> buffer, ref SequencePosition position)
        {
            int tmp = ReadRawByte(buffer, ref position);
            if (tmp < 128)
            {
                return (uint)tmp;
            }
            int result = tmp & 0x7f;
            if ((tmp = ReadRawByte(buffer, ref position)) < 128)
            {
                result |= tmp << 7;
            }
            else
            {
                result |= (tmp & 0x7f) << 7;
                if ((tmp = ReadRawByte(buffer, ref position)) < 128)
                {
                    result |= tmp << 14;
                }
                else
                {
                    result |= (tmp & 0x7f) << 14;
                    if ((tmp = ReadRawByte(buffer, ref position)) < 128)
                    {
                        result |= tmp << 21;
                    }
                    else
                    {
                        result |= (tmp & 0x7f) << 21;
                        result |= (tmp = ReadRawByte(buffer, ref position)) << 28;
                        if (tmp >= 128)
                        {
                            // Discard upper 32 bits.
                            for (int i = 0; i < 5; i++)
                            {
                                if (ReadRawByte(buffer, ref position) < 128)
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

        private static void DiscardUpperVarIntBits(in ReadOnlySequence<byte> buffer, ref SequencePosition position, int count)
        {
            // Note that this has to use ReadRawByte() as we only ensure we've
            // got at least n bytes at the start of the method. This lets us
            // use the fast path in more cases, and we rarely hit this section of code.
            for (int i = 0; i < count; i++)
            {
                if (ReadRawByte(buffer, ref position) < 128)
                {
                    return;
                }
            }
            //TODO: Fix
            //throw InvalidProtocolBufferException.MalformedVarint();
            throw new Exception();
        }

        private static byte ReadRawByte(in ReadOnlySequence<byte> buffer, ref SequencePosition position)
        {
            if (!buffer.TryGet(ref position, out var memory, false) || memory.IsEmpty)
            {
                //TODO: Fix
                //throw InvalidProtocolBufferException.TruncatedMessage();
                throw new Exception();
            }
            position = buffer.GetPosition(1, position);
            return memory.Span[0];
        }

        private static uint ReadFixed32(in ReadOnlySequence<byte> buffer, ref SequencePosition position)
        {
            if (!buffer.TryGet(ref position, out var memory, false) || memory.IsEmpty)
            {
                //TODO: Proper exception
                throw new Exception();
            }
            else if (memory.Length >= 4)
            {
                position = buffer.GetPosition(4, position);
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
                position = buffer.GetPosition(4, position);
                Span<byte> span = stackalloc byte[4];
                remaining.CopyTo(span);
                return BinaryPrimitives.ReadUInt32LittleEndian(span);
            }
        }

        private static ulong ReadFixed64(in ReadOnlySequence<byte> buffer, ref SequencePosition position)
        {
            if (!buffer.TryGet(ref position, out var memory, false) || memory.IsEmpty)
            {
                //TODO: Proper exception
                throw new Exception();
            }
            else if (memory.Length >= 8)
            {
                position = buffer.GetPosition(8, position);
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
                position = buffer.GetPosition(8, position);
                Span<byte> span = stackalloc byte[8];
                remaining.CopyTo(span);
                return BinaryPrimitives.ReadUInt64LittleEndian(span);
            }
        }

        private static void SkipField(in ReadOnlySequence<byte> buffer, ref SequencePosition position, in uint tag, in int remainingRecursionLevels)
        {
            if (tag == 0)
            {
                throw new InvalidOperationException("SkipLastField cannot be called at the end of a stream");
            }

            switch (WireFormat.GetTagWireType(tag))
            {
                case WireFormat.WireType.StartGroup:
                    SkipGroup(buffer, ref position, tag, remainingRecursionLevels);
                    break;
                case WireFormat.WireType.EndGroup:
                    //TODO: Implement properly
                    throw new Exception();
                //throw new InvalidProtocolBufferException("SkipLastField called on an end-group tag, indicating that the corresponding start-group was missing");
                case WireFormat.WireType.Fixed32:
                    SkipRawBytes(buffer, ref position, 4);
                    break;
                case WireFormat.WireType.Fixed64:
                    SkipRawBytes(buffer, ref position, 8);
                    break;
                case WireFormat.WireType.LengthDelimited:
                    var length = ReadLength(buffer, ref position);
                    SkipRawBytes(buffer, ref position, length);
                    break;
                case WireFormat.WireType.Varint:
                    ReadRawVarint32(buffer, ref position);
                    break;
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

        private static void SkipGroup(in ReadOnlySequence<byte> buffer, ref SequencePosition position, in uint startGroupTag, in int remainingRecursionLevels)
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
                tag = ReadTag(buffer, ref position);
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
                SkipField(buffer, ref position, tag, remainingRecursionLevels - 1);
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
