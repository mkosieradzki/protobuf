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

        public async ValueTask<object> ReadMessageAsync(IReadableMessageType messageType, CancellationToken cancellationToken = default)
        {
            if (messageType == null)
                throw new ArgumentNullException(nameof(messageType));

            var message = messageType.CreateMessage();

            uint tag;
            while ((tag = await ReadTagAsync(cancellationToken)) != 0)
            {
                var fieldInfo = messageType.GetFieldInfo(tag);

                switch (fieldInfo.ValueType)
                {
                    //case ValueType.Unknown:
                    //    break;
                    //case ValueType.Double:
                    //    break;
                    //case ValueType.Float:
                    //    break;
                    case ValueType.Int32:
                        messageType.ConsumeInt32(message, tag, (int)await ReadRawVarint32Async(cancellationToken));
                        break;
                    //case ValueType.Int64:
                    //    break;
                    case ValueType.UInt32:
                        messageType.ConsumeUInt32(message, tag, await ReadRawVarint32Async(cancellationToken));
                        break;
                    //case ValueType.UInt64:
                    //    break;
                    //case ValueType.SInt32:
                    //    break;
                    //case ValueType.SInt64:
                    //    break;
                    case ValueType.Fixed32:
                        messageType.ConsumeUInt32(message, tag, await ReadFixed32Async(cancellationToken));
                        break;
                    //case ValueType.Fixed64:
                    //    break;
                    //case ValueType.SFixed32:
                    //    break;
                    //case ValueType.SFixed64:
                    //    break;
                    //case ValueType.Bool:
                    //    break;
                    //case ValueType.String:
                    //    break;
                    //case ValueType.Bytes:
                    //    break;
                    //case ValueType.Enum:
                    //    break;
                    //case ValueType.Message:
                    //    break;
                    default:
                        throw new NotSupportedException();
                }

                //await input.ReadAsync(cancellationToken);
            }

            return messageType;
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
        internal ValueTask<uint> ReadRawVarint32Async(CancellationToken cancellationToken)
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
                return new ValueTask<uint>(BinaryPrimitives.ReadUInt32BigEndian(buffer.First.Span));
            }
            else if (buffer.Length >= 4)
            {
                Span<byte> span = stackalloc byte[4];
                buffer.CopyTo(span);
                return new ValueTask<uint>(BinaryPrimitives.ReadUInt32BigEndian(span));
            }
            else
            {
                return SlowReadFixed32Async(cancellationToken);
            }
        }

        private async ValueTask SlowEnsureMinBufferSizeAsync(int bytes, CancellationToken cancellationToken)
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
    }
}
