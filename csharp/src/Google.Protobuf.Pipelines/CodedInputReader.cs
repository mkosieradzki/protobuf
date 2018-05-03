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
        private const int DefaultRecursionLimit = 64;
        private const int DefaultSizeLimit = Int32.MaxValue;

        /// <summary>
        /// Creates a new CodedInputReader reading data from the given pipe reader.
        /// </summary>
        public CodedInputReader(PipeReader input, int recursionLimit = DefaultRecursionLimit, int sizeLimit = DefaultSizeLimit)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            this.input = input;
            this.recursionLimit = recursionLimit;
            this.remainingBytesToLimit = sizeLimit;
            this.position = buffer.Start;
        }

        private readonly PipeReader input;

        private ReadOnlySequence<byte> buffer;
        private SequencePosition position;
        private int remainingBytesToLimit;
        private bool isLastRead;
        private bool isInitialized;

        private readonly int recursionLimit;
        private readonly int sizeLimit;

        private int recursionDepth;

        private void Progress(int count)
        {
            if (count > remainingBytesToLimit)
            {
                throw new Exception();
                //TODO: proper exception
            }
            remainingBytesToLimit -= count;
            position = buffer.GetPosition(count, position);
        }

        public async ValueTask<int> ReadLengthAndPushAsLimitAsync(CancellationToken cancellationToken = default)
        {
            var length = (int)await ReadLengthAsync(cancellationToken);
            if (length > remainingBytesToLimit)
            {
                //TODO: Better exception
                throw new Exception();
            }
            var toPop = remainingBytesToLimit - length;
            remainingBytesToLimit = length;
            return toPop;
        }

        public void PopLimit(int toPop)
        {
            remainingBytesToLimit = toPop;
        }

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
            // Optimize for the incredibly common case of having at least two bytes left in the buffer,
            // and those two bytes being enough to get the tag. This will be true for fields up to 4095.
            if (remainingBytesToLimit >= 10 && buffer.TryGet(ref position, out var memory, false) && memory.Length >= 10)
            {
                int pos = 0;
                var tag = CodedInputSpanPosParser.ReadTag(memory.Span, ref pos);
                Progress(pos);
                return new ValueTask<uint>(tag);
            }
            else if (memory.IsEmpty && isLastRead)
            {
                return new ValueTask<uint>(0);
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
                return 0; // This is the only case in which we return 0.
            }

            return await ReadRawVarint32Async(cancellationToken);
        }

        private ValueTask<bool> IsAtEndAsync(CancellationToken cancellationToken)
        {
            if (remainingBytesToLimit == 0)
            {
                return new ValueTask<bool>(true);
            }

            if (position == buffer.End)
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
                    var length = (int)await ReadLengthAsync(cancellationToken);
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
            recursionDepth++;
            if (recursionDepth >= recursionLimit)
            {
                //TODO: add proper exception
                throw new Exception();
                //throw InvalidProtocolBufferException.RecursionLimitExceeded();
            }
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
            recursionDepth--;
        }

        private async ValueTask RefillBufferAsync(CancellationToken cancellationToken)
        {
            if (isInitialized)
            {
                input.AdvanceTo(position);
            }

            var result = await input.ReadAsync(cancellationToken);
            buffer = result.Buffer;
            position = buffer.Start;
            isLastRead = result.IsCompleted;
            isInitialized = true;
        }

        /// <summary>
        /// Reads a length for length-delimited data.
        /// </summary>
        /// <remarks>
        /// This is internally just reading a varint, but this method exists
        /// to make the calling code clearer.
        /// </remarks>
        private ValueTask<uint> ReadLengthAsync(CancellationToken cancellationToken) => ReadRawVarint32Async(cancellationToken);

        public async ValueTask<ReadOnlySequence<byte>> ReadLengthDelimited(CancellationToken cancellationToken = default)
        {
            var length = (int)await ReadLengthAsync(cancellationToken);
            if (length > remainingBytesToLimit)
            {
                //TODO: better exception
                throw new Exception();
            }
            await EnsureMinBufferSizeAsync((uint)length, cancellationToken);
            var ret = buffer.Slice(position, length);
            Progress(length);
            return ret;
        }

        /// <summary>
        /// Reads a raw Varint from the stream.  If larger than 32 bits, discard the upper bits.
        /// This method is optimised for the case where we've got lots of data in the buffer.
        /// That means we can check the size just once, then just read directly from the buffer
        /// without constant rechecking of the buffer length.
        /// </summary>
        public ValueTask<uint> ReadRawVarint32Async(CancellationToken cancellationToken)
        {
            if (buffer.TryGet(ref position, out var memory, false) && memory.Length >= 10)
            {
                int pos = 0;
                var ret = CodedInputSpanPosParser.ReadRawVarint32(memory.Span, ref pos);
                Progress(pos);
                return new ValueTask<uint>(ret);
            }
            else
            {
                return SlowReadRawVarint32Async(cancellationToken);
            }
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

        public ValueTask<ulong> ReadRawVarint64Async(CancellationToken cancellationToken)
        {
            if (buffer.TryGet(ref position, out var memory, false) && memory.Length >= 10)
            {
                int pos = 0;
                var ret = CodedInputSpanPosParser.ReadRawVarint64(memory.Span, ref pos);
                Progress(pos);
                return new ValueTask<ulong>(ret);
            }
            else
            {
                return SlowReadRawVarint64Async(cancellationToken);
            }
        }

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

        /// <summary>
        /// Read one byte from the input.
        /// </summary>
        /// <exception cref="InvalidProtocolBufferException">
        /// the end of the stream or the current limit was reached
        /// </exception>
        private ValueTask<byte> ReadRawByteAsync(CancellationToken cancellationToken)
        {
            if (buffer.TryGet(ref position, out var memory, false) && !memory.IsEmpty)
            {
                var ret = memory.Span[0];
                Progress(1);
                return new ValueTask<byte>(ret);
            }
            else
            {
                return SlowReadRawByteAsync(cancellationToken);
            }
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
                buffer.TryGet(ref position, out var memory, false);
                return memory.Span[0];
            }
        }

        private ValueTask<uint> ReadFixed32Async(CancellationToken cancellationToken)
        {
            if (buffer.TryGet(ref position, out var memory, false) && memory.Length >= 4)
            {
                var ret = BinaryPrimitives.ReadUInt32LittleEndian(memory.Span);
                Progress(4);
                return new ValueTask<uint>(ret);
            }
            else
            {
                return SlowReadFixed32Async(cancellationToken);
            }
        }

        private ValueTask<ulong> ReadFixed64Async(CancellationToken cancellationToken)
        {
            if (buffer.TryGet(ref position, out var memory, false) && memory.Length >= 8)
            {
                var ret = BinaryPrimitives.ReadUInt64LittleEndian(memory.Span);
                Progress(8);
                return new ValueTask<ulong>(ret);
            }
            else
            {
                return SlowReadFixed64Async(cancellationToken);
            }
        }

        private ValueTask EnsureMinBufferSizeAsync(uint bytes, CancellationToken cancellationToken)
        {
            buffer = buffer.Slice(position);
            if (bytes > remainingBytesToLimit || isLastRead && bytes > buffer.Length)
            {
                //TODO: Proper exception
                throw new Exception();
            }
            else
            {
                return SlowEnsureMinBufferSizeAsync(bytes, cancellationToken);
            }
        }

        private async ValueTask SlowEnsureMinBufferSizeAsync(uint bytes, CancellationToken cancellationToken)
        {
            //Assuming buffer has been rewinded to position && checked for remainingBytesToLimit
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
            await EnsureMinBufferSizeAsync(4, cancellationToken);
            return ReadFixed32FromResizedBuffer();
        }

        private uint ReadFixed32FromResizedBuffer()
        {
            Span<byte> span = stackalloc byte[4];
            buffer.CopyTo(span);
            var ret = BinaryPrimitives.ReadUInt32LittleEndian(span);
            Progress(4);
            return ret;
        }

        private ulong ReadFixed64FromResizedBuffer()
        {
            Span<byte> span = stackalloc byte[8];
            buffer.CopyTo(span);
            var ret = BinaryPrimitives.ReadUInt64LittleEndian(span);
            Progress(8);
            return ret;
        }

        private async ValueTask<ulong> SlowReadFixed64Async(CancellationToken cancellationToken)
        {
            await EnsureMinBufferSizeAsync(8, cancellationToken);
            return ReadFixed64FromResizedBuffer();
        }

        /// <summary>
        /// Reads and discards <paramref name="size"/> bytes.
        /// </summary>
        /// <exception cref="InvalidProtocolBufferException">the end of the stream
        /// or the current limit was reached</exception>
        private ValueTask SkipRawBytesAsync(int size, CancellationToken cancellationToken)
        {
            if (buffer.TryGet(ref position, out var memory, false) && memory.Length >= size)
            {
                Progress(size);
                return default;
            }
            else
            {
                return SlowSkipRawBytesAsync(size, cancellationToken);
            }
        }

        private async ValueTask SlowSkipRawBytesAsync(int size, CancellationToken cancellationToken)
        {
            while (size > 0)
            {
                if (await IsAtEndAsync(cancellationToken))
                {
                    //TODO: Implement properly
                    //throw InvalidProtocolBufferException.TruncatedMessage();
                    throw new Exception();
                }
                else if (buffer.TryGet(ref position, out var memory, false) && memory.Length >= size)
                {
                    Progress(size);
                    break;
                }
                else
                {
                    Progress(memory.Length);
                    size -= memory.Length;
                }
            }
        }
    }
}
