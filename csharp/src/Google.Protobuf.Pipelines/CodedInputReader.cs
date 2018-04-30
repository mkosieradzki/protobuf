using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Protobuf.Pipelines
{
    /// <summary>
    /// Reads and decodes protocol message fields.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class is generally used by generated code to read appropriate
    /// primitives from the stream. It effectively encapsulates the lowest
    /// levels of protocol buffer format.
    /// </para>
    /// <para>
    /// Repeated fields and map fields are not handled by this class; use <see cref="RepeatedField{T}"/>
    /// and <see cref="MapField{TKey, TValue}"/> to serialize such fields.
    /// </para>
    /// </remarks>
    public sealed class CodedInputReader
    {
        /// <summary>
        /// Creates a new CodedInputReader reading data from the given pipe reader.
        /// </summary>
        public CodedInputReader(PipeReader input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            this.input = input;
        }

        private readonly PipeReader input;

        private ReadOnlySequence<byte> buffer;
        private bool isLastRead;
        private bool isInitialized;
        private SequencePosition consumed;

        /// <summary>
        /// The last tag we read. 0 indicates we've read to the end of the stream
        /// (or haven't read anything yet).
        /// </summary>
        private uint lastTag = 0;

        /// <summary>
        /// The next tag, used to store the value read by PeekTag.
        /// </summary>
        private uint nextTag = 0;
        private bool hasNextTag = false;

        public ValueTask<object> ReadMessageAsync(IReadableMessageType messageType, CancellationToken cancellationToken = default)
        {
            if (messageType == null)
                throw new ArgumentNullException(nameof(messageType));

            return SlowReadMessageAsync(messageType, cancellationToken);
        }

        private async ValueTask<object> SlowReadMessageAsync(IReadableMessageType messageType, CancellationToken cancellationToken)
        {
            var message = messageType.CreateMessage();

            uint tag;
            while ((tag = await ReadTagAsync(cancellationToken)) != 0)
            {
                var fieldInfo = messageType.GetFieldInfo(tag);

                switch (fieldInfo.ValueType)
                {
                    case ValueType.Unknown:
                        await SkipFieldAsync(tag, cancellationToken);
                        break;
                    case ValueType.Double:
                        messageType.ConsumeField(message, tag, BitConverter.Int64BitsToDouble((long)await ReadFixed64Async(cancellationToken)));
                        break;
                    case ValueType.Float:
                        messageType.ConsumeField(message, tag, Int32BitsToSingle((int)await ReadFixed32Async(cancellationToken)));
                        break;
                    case ValueType.Int32:
                        messageType.ConsumeField(message, tag, (int)await ReadRawVarint32Async(cancellationToken));
                        break;
                    case ValueType.Int64:
                        messageType.ConsumeField(message, tag, (int)await ReadRawVarint64Async(cancellationToken));
                        break;
                    case ValueType.UInt32:
                        messageType.ConsumeField(message, tag, await ReadRawVarint32Async(cancellationToken));
                        break;
                    case ValueType.UInt64:
                        messageType.ConsumeField(message, tag, await ReadRawVarint64Async(cancellationToken));
                        break;
                    case ValueType.SInt32:
                        messageType.ConsumeField(message, tag, DecodeZigZag32(await ReadRawVarint32Async(cancellationToken)));
                        break;
                    case ValueType.SInt64:
                        messageType.ConsumeField(message, tag, DecodeZigZag64(await ReadRawVarint64Async(cancellationToken)));
                        break;
                    case ValueType.Fixed32:
                        messageType.ConsumeField(message, tag, await ReadFixed32Async(cancellationToken));
                        break;
                    case ValueType.Fixed64:
                        messageType.ConsumeField(message, tag, await ReadFixed64Async(cancellationToken));
                        break;
                    case ValueType.SFixed32:
                        messageType.ConsumeField(message, tag, (int)await ReadFixed32Async(cancellationToken));
                        break;
                    case ValueType.SFixed64:
                        messageType.ConsumeField(message, tag, (int)await ReadFixed64Async(cancellationToken));
                        break;
                    case ValueType.Bool:
                        messageType.ConsumeField(message, tag, await ReadRawVarint32Async(cancellationToken) != 0);
                        break;
                    case ValueType.Bytes:
                    case ValueType.String:
                        messageType.ConsumeField(message, tag, await ReadLengthDelimitedAsync(cancellationToken));
                        break;
                    case ValueType.Enum:
                        messageType.ConsumeField(message, tag, (int)await ReadRawVarint32Async(cancellationToken));
                        break;
                    case ValueType.Message:
                        {
                            //TODO: If is packed?
                            var length = await ReadLengthAsync(cancellationToken);
                            //if (recursionDepth >= recursionLimit)
                            //{
                            //    throw InvalidProtocolBufferException.RecursionLimitExceeded();
                            //}
                            //int oldLimit = PushLimit(length);
                            //++recursionDepth;
                            var nestedMessage = await ReadMessageAsync(fieldInfo.MessageType, cancellationToken);
                            messageType.ConsumeField(message, tag, nestedMessage);
                            //builder.MergeFrom(this);
                            //CheckReadEndOfStreamTag();
                            //// Check that we've read exactly as much data as expected.
                            //if (!ReachedLimit)
                            //{
                            //    throw InvalidProtocolBufferException.TruncatedMessage();
                            //}
                            //--recursionDepth;
                            //PopLimit(oldLimit);
                            break;
                        }
                    default:
                        throw new NotSupportedException();
                }
            }

            return messageType.CompleteMessage(message);
        }

        private async ValueTask<ReadOnlySequence<byte>> ReadLengthDelimitedAsync(CancellationToken cancellationToken)
        {
            var length = await ReadLengthAsync(cancellationToken);
            if (buffer.Length < length)
            {
                await SlowEnsureMinBufferSizeAsync(length, cancellationToken);
            }

            var ret = buffer.Slice(0, length);

            consumed = buffer.GetPosition(length);
            buffer = buffer.Slice(length);

            return ret;
        }

        private async ValueTask SkipFieldAsync(uint tag, CancellationToken cancellationToken)
        {
            if (tag == 0)
            {
                throw new InvalidOperationException("SkipLastField cannot be called at the end of a stream");
            }

            switch (WireFormat.GetTagWireType(tag))
            {
                case WireFormat.WireType.StartGroup:
                    await SkipGroupAsync(tag, cancellationToken);
                    break;
                case WireFormat.WireType.EndGroup:
                    //TODO: Implement properly
                    throw new Exception();
                //throw new InvalidProtocolBufferException("SkipLastField called on an end-group tag, indicating that the corresponding start-group was missing");
                case WireFormat.WireType.Fixed32:
                    await SkipRawBytesAsync(4, cancellationToken);
                    break;
                case WireFormat.WireType.Fixed64:
                    await SkipRawBytesAsync(8, cancellationToken);
                    break;
                case WireFormat.WireType.LengthDelimited:
                    var length = await ReadLengthAsync(cancellationToken);
                    await SkipRawBytesAsync(length, cancellationToken);
                    break;
                case WireFormat.WireType.Varint:
                    await ReadRawVarint32Async(cancellationToken);
                    break;
            }
        }

        /// <summary>
        /// Skip a group.
        /// </summary>
        private async ValueTask SkipGroupAsync(uint startGroupTag, CancellationToken cancellationToken)
        {
            // Note: Currently we expect this to be the way that groups are read. We could put the recursion
            // depth changes into the ReadTag method instead, potentially...
            //recursionDepth++;
            //if (recursionDepth >= recursionLimit)
            //{
            //    throw InvalidProtocolBufferException.RecursionLimitExceeded();
            //}
            uint tag;
            while (true)
            {
                tag = await ReadTagAsync();
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
                await SkipFieldAsync(tag, cancellationToken);
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
            //recursionDepth--;
        }

        private ValueTask<bool> IsAtEndAsync(CancellationToken cancellationToken)
        {
            if (buffer.IsEmpty)
            {
                if (isLastRead)
                {
                    return new ValueTask<bool>(true);
                }
                else
                {
                    return SlowIsAtEndAsync(cancellationToken);
                }
            }
            else
            {
                return new ValueTask<bool>(false);
            }
        }

        private async ValueTask<bool> SlowIsAtEndAsync(CancellationToken cancellationToken)
        {
            await RefillBufferAsync(cancellationToken);
            return await IsAtEndAsync(cancellationToken);
        }

        private async ValueTask RefillBufferAsync(CancellationToken cancellationToken)
        {
            if (isInitialized)
            {
                input.AdvanceTo(consumed);
            }

            var result = await input.ReadAsync(cancellationToken);
            buffer = result.Buffer;
            isLastRead = result.IsCompleted;
            consumed = result.Buffer.Start;
            isInitialized = true;
        }

        //private ValueTask<object> ReadMessageFromBuffer(in object message, in IReadableMessageType messageType, in ReadOnlySequence<byte> buffer, in CancellationToken cancellationToken = default)
        //{
        //    buffer.

        //    return new ValueTask<object>(message);
        //}


        /// <summary>
        /// Reads a field tag, returning the tag of 0 for "end of stream".
        /// </summary>
        /// <remarks>
        /// If this method returns 0, it doesn't necessarily mean the end of all
        /// the data in this CodedInputStream; it may be the end of the logical stream
        /// for an embedded message, for example.
        /// </remarks>
        /// <returns>The next field tag, or 0 for end of stream. (0 is never a valid tag.)</returns>
        public ValueTask<uint> ReadTagAsync(CancellationToken cancellationToken = default)
        {
            if (hasNextTag)
            {
                lastTag = nextTag;
                hasNextTag = false;
                return new ValueTask<uint>(lastTag);
            }

            var span = buffer.First.Span;

            // Optimize for the incredibly common case of having at least two bytes left in the buffer,
            // and those two bytes being enough to get the tag. This will be true for fields up to 4095.
            if (span.Length >= 2)// bufferPos + 2 <= bufferSize)
            {
                int tmp = span[0];
                if (tmp < 128)
                {
                    lastTag = (uint)tmp;
                    consumed = buffer.GetPosition(1);
                    buffer = buffer.Slice(1);
                }
                else
                {
                    int result = tmp & 0x7f;
                    if ((tmp = span[1]) < 128)
                    {
                        result |= tmp << 7;
                        lastTag = (uint)result;
                        consumed = buffer.GetPosition(2);
                        buffer = buffer.Slice(2);
                    }
                    else
                    {
                        return SlowReadTagAsync(cancellationToken);
                    }
                }
                if (WireFormat.GetTagFieldNumber(lastTag) == 0)
                {
                    // If we actually read a tag with a field of 0, that's not a valid tag.
                    //TODO: Fix
                    //throw InvalidProtocolBufferException.InvalidTag();
                    throw new Exception();
                }
                return new ValueTask<uint>(lastTag);
            }
            else
            {
                return SlowReadTagAsync(cancellationToken);
            }
        }

        /// <summary>
        /// Reads a length for length-delimited data.
        /// </summary>
        /// <remarks>
        /// This is internally just reading a varint, but this method exists
        /// to make the calling code clearer.
        /// </remarks>
        public ValueTask<uint> ReadLengthAsync(CancellationToken cancellationToken = default) => ReadRawVarint32Async(cancellationToken);

        private async ValueTask<uint> SlowReadTagAsync(CancellationToken cancellationToken)
        {
            if (await IsAtEndAsync(cancellationToken))
            {
                lastTag = 0;
                return 0; // This is the only case in which we return 0.
            }

            lastTag = await ReadRawVarint32Async(cancellationToken);
            return lastTag;
        }

        /// <summary>
        /// Reads a raw Varint from the stream.  If larger than 32 bits, discard the upper bits.
        /// This method is optimised for the case where we've got lots of data in the buffer.
        /// That means we can check the size just once, then just read directly from the buffer
        /// without constant rechecking of the buffer length.
        /// </summary>
        private ValueTask<uint> ReadRawVarint32Async(CancellationToken cancellationToken)
        {
            var span = buffer.First.Span;
            if (span.Length < 5)
            {
                return SlowReadRawVarint32Async(cancellationToken);
            }

            int tmp = span[0];
            if (tmp < 128)
            {
                consumed = buffer.GetPosition(1);
                buffer = buffer.Slice(1);
                return new ValueTask<uint>((uint)tmp);
            }
            int result = tmp & 0x7f;
            if ((tmp = span[1]) < 128)
            {
                consumed = buffer.GetPosition(2);
                buffer = buffer.Slice(2);
                result |= tmp << 7;
            }
            else
            {
                result |= (tmp & 0x7f) << 7;
                if ((tmp = span[2]) < 128)
                {
                    consumed = buffer.GetPosition(3);
                    buffer = buffer.Slice(3);
                    result |= tmp << 14;
                }
                else
                {
                    result |= (tmp & 0x7f) << 14;
                    if ((tmp = span[3]) < 128)
                    {
                        consumed = buffer.GetPosition(4);
                        buffer = buffer.Slice(4);
                        result |= tmp << 21;
                    }
                    else
                    {
                        result |= (tmp & 0x7f) << 21;
                        consumed = buffer.GetPosition(5);
                        buffer = buffer.Slice(5);
                        result |= (tmp = span[4]) << 28;
                        if (tmp >= 128)
                        {
                            // Discard upper 32 bits.
                            return SlowDiscardUpperVarIntBitsAndReturn(5, (uint)result, cancellationToken);
                        }
                    }
                }
            }
            return new ValueTask<uint>((uint)result);
        }

        private ValueTask<ulong> ReadRawVarint64Async(CancellationToken cancellationToken) => SlowReadRawVarint64Async(cancellationToken);

        private async ValueTask<ulong> SlowReadRawVarint64Async(CancellationToken cancellationToken)
        {
            int shift = 0;
            ulong result = 0;
            while (shift < 64)
            {
                byte b = await ReadRawByteAsync(cancellationToken);
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

        private async ValueTask<T> SlowDiscardUpperVarIntBitsAndReturn<T>(int count, T result, CancellationToken cancellationToken)
        {
            // Note that this has to use ReadRawByte() as we only ensure we've
            // got at least n bytes at the start of the method. This lets us
            // use the fast path in more cases, and we rarely hit this section of code.
            for (int i = 0; i < count; i++)
            {
                if (await ReadRawByteAsync(cancellationToken) < 128)
                {
                    return result;
                }
            }
            //TODO: Fix
            //throw InvalidProtocolBufferException.MalformedVarint();
            throw new Exception();
        }

        /// <summary>
        /// Same code as ReadRawVarint32, but read each byte individually, checking for
        /// buffer overflow.
        /// </summary>
        private async ValueTask<uint> SlowReadRawVarint32Async(CancellationToken cancellationToken)
        {
            int tmp = await ReadRawByteAsync(cancellationToken);
            if (tmp < 128)
            {
                return (uint)tmp;
            }
            int result = tmp & 0x7f;
            if ((tmp = await ReadRawByteAsync(cancellationToken)) < 128)
            {
                result |= tmp << 7;
            }
            else
            {
                result |= (tmp & 0x7f) << 7;
                if ((tmp = await ReadRawByteAsync(cancellationToken)) < 128)
                {
                    result |= tmp << 14;
                }
                else
                {
                    result |= (tmp & 0x7f) << 14;
                    if ((tmp = await ReadRawByteAsync(cancellationToken)) < 128)
                    {
                        result |= tmp << 21;
                    }
                    else
                    {
                        result |= (tmp & 0x7f) << 21;
                        result |= (tmp = await ReadRawByteAsync(cancellationToken)) << 28;
                        if (tmp >= 128)
                        {
                            // Discard upper 32 bits.
                            for (int i = 0; i < 5; i++)
                            {
                                if (await ReadRawByteAsync(cancellationToken) < 128)
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

        /// <summary>
        /// Read one byte from the input.
        /// </summary>
        /// <exception cref="InvalidProtocolBufferException">
        /// the end of the stream or the current limit was reached
        /// </exception>
        internal ValueTask<byte> ReadRawByteAsync(CancellationToken cancellationToken)
        {
            if (buffer.IsEmpty)
            {
                return SlowReadRawByteAsync(cancellationToken);
            }
            var ret = buffer.First.Span[0];
            consumed = buffer.GetPosition(1);
            buffer = buffer.Slice(1);
            return new ValueTask<byte>(ret);
        }

        private async ValueTask<byte> SlowReadRawByteAsync(CancellationToken cancellationToken)
        {
            if (await IsAtEndAsync(cancellationToken))
            {
                //TODO: Fix
                //throw InvalidProtocolBufferException.TruncatedMessage();
                throw new Exception();
            }
            else
            {
                return buffer.First.Span[0];
            }
        }

        private ValueTask<uint> ReadFixed32Async(CancellationToken cancellationToken)
        {
            if (buffer.First.Length >= 4)
            {
                var span = buffer.First.Span;
                consumed = buffer.GetPosition(4);
                buffer = buffer.Slice(4);
                return new ValueTask<uint>(BinaryPrimitives.ReadUInt32LittleEndian(span));
            }
            else if (buffer.Length >= 4)
            {
                Span<byte> span = stackalloc byte[4];
                buffer.CopyTo(span);
                consumed = buffer.GetPosition(4);
                buffer = buffer.Slice(4);
                return new ValueTask<uint>(BinaryPrimitives.ReadUInt32LittleEndian(span));
            }
            else
            {
                return SlowReadFixed32Async(cancellationToken);
            }
        }

        private ValueTask<ulong> ReadFixed64Async(CancellationToken cancellationToken)
        {
            if (buffer.First.Length >= 8)
            {
                var span = buffer.First.Span;
                consumed = buffer.GetPosition(8);
                buffer = buffer.Slice(8);
                return new ValueTask<ulong>(BinaryPrimitives.ReadUInt64LittleEndian(span));
            }
            else if (buffer.Length >= 8)
            {
                Span<byte> span = stackalloc byte[8];
                buffer.CopyTo(span);
                consumed = buffer.GetPosition(8);
                buffer = buffer.Slice(8);
                return new ValueTask<ulong>(BinaryPrimitives.ReadUInt64LittleEndian(span));
            }
            else
            {
                return SlowReadFixed64Async(cancellationToken);
            }
        }

        private async ValueTask SlowEnsureMinBufferSizeAsync(uint bytes, CancellationToken cancellationToken)
        {
            while (buffer.Length < bytes)
            {
                if (isLastRead)
                {
                    //TODO: Proper exception
                    throw new Exception();
                }
                await RefillBufferAsync(cancellationToken);
            }
        }

        private async ValueTask<uint> SlowReadFixed32Async(CancellationToken cancellationToken)
        {
            await SlowEnsureMinBufferSizeAsync(4, cancellationToken);
            //NOTE: this time should succeed without entering slow path
            return await ReadFixed32Async(cancellationToken);
        }

        private async ValueTask<ulong> SlowReadFixed64Async(CancellationToken cancellationToken)
        {
            await SlowEnsureMinBufferSizeAsync(8, cancellationToken);
            //NOTE: this time should succeed without entering slow path
            return await ReadFixed64Async(cancellationToken);
        }

        //private static uint? TryReadTagFromBuffer(ReadOnlySequence<byte> sequence, out SequencePosition consumed)
        //{

        //}

        //private Task<> CompleteReadTag

        //public static async Task<T> ParseNextMessage<T>(PipeReader input, CancellationToken cancellationToken = default) where T : IMessage<T>
        //{
        //    if (input == null)
        //        throw new ArgumentNullException(nameof(input));

        //    var message = await input.ReadAsync(cancellationToken);

        //    message.Buffer.
        //}

        /// <summary>
        /// Reads and discards <paramref name="size"/> bytes.
        /// </summary>
        /// <exception cref="InvalidProtocolBufferException">the end of the stream
        /// or the current limit was reached</exception>
        private ValueTask SkipRawBytesAsync(uint size, CancellationToken cancellationToken)
        {
            if (buffer.Length < size)
            {
                return SlowSkipRawBytesAsync(size, cancellationToken);
            }

            consumed = buffer.GetPosition(size);
            buffer = buffer.Slice(size);
            return default;
        }

        private async ValueTask SlowSkipRawBytesAsync(uint size, CancellationToken cancellationToken)
        {
            while (size > 0)
            {
                if (await IsAtEndAsync(cancellationToken))
                {
                    //TODO: Implement properly
                    //throw InvalidProtocolBufferException.TruncatedMessage();
                    throw new Exception();
                }
                else if (size <= buffer.Length)
                {
                    consumed = buffer.GetPosition(size);
                    buffer = buffer.Slice(size);
                    break;
                }
                else
                {
                    consumed = buffer.End;
                    buffer = buffer.Slice(buffer.Length);
                    size -= (uint)buffer.Length;
                }
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
        private static int DecodeZigZag32(uint n) => (int)(n >> 1) ^ -(int)(n & 1);

        /// <summary>
        /// Decode a 64-bit value with ZigZag encoding.
        /// </summary>
        /// <remarks>
        /// ZigZag encodes signed integers into values that can be efficiently
        /// encoded with varint.  (Otherwise, negative values must be 
        /// sign-extended to 64 bits to be varint encoded, thus always taking
        /// 10 bytes on the wire.)
        /// </remarks>
        private static long DecodeZigZag64(ulong n) => (long)(n >> 1) ^ -(long)(n & 1);

        //TODO: Use proper BitConverter.Int32BitsToSingle when .NET Standard is out - alternatively use unsafe implementation
#if UNSAFE
        private static unsafe float Int32BitsToSingle(int n) => *((float*)&n);
#else
        private static float Int32BitsToSingle(int n) => BitConverter.ToSingle(BitConverter.GetBytes(n), 0);
#endif
    }
}
