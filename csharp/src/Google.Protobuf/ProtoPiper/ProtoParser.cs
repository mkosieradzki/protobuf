using Google.Protobuf.Collections;
using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Protobuf.ProtoPiper
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
            ProtoPreconditions.CheckNotNull(input, nameof(input));

            this.input = input;
        }

        private readonly PipeReader input;

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

        /// <summary>
        /// Reads a field tag, returning the tag of 0 for "end of stream".
        /// </summary>
        /// <remarks>
        /// If this method returns 0, it doesn't necessarily mean the end of all
        /// the data in this CodedInputStream; it may be the end of the logical stream
        /// for an embedded message, for example.
        /// </remarks>
        /// <returns>The next field tag, or 0 for end of stream. (0 is never a valid tag.)</returns>
        public ValueTask<uint> ReadTag(CancellationToken cancellationToken = default)
        {
            if (hasNextTag)
            {
                lastTag = nextTag;
                hasNextTag = false;
                return new ValueTask<uint>(lastTag);
            }

            var readTask = input.ReadAsync(cancellationToken);
            if (readTask.IsCompletedSuccessfully)
            {
                var isAtEnd = readTask.Result.IsCompleted;
                // readTask.Result.IsCanceled?
                var res = TryReadTagFromBuffer(readTask.Result.Buffer, out var consumed);
                input.AdvanceTo(consumed);
                res.Value
                readTask.Result.Buffer.Slice(0, 5).ToSpan()
            }
            else
            {
                return readTask.AsTask().ContinueWith()
            }
            //new Span<byte>()

            var span = input.

            // Optimize for the incredibly common case of having at least two bytes left in the buffer,
            // and those two bytes being enough to get the tag. This will be true for fields up to 4095.
            if (bufferPos + 2 <= bufferSize)
            {
                int tmp = buffer[bufferPos++];
                if (tmp < 128)
                {
                    lastTag = (uint)tmp;
                }
                else
                {
                    int result = tmp & 0x7f;
                    if ((tmp = buffer[bufferPos++]) < 128)
                    {
                        result |= tmp << 7;
                        lastTag = (uint)result;
                    }
                    else
                    {
                        // Nope, rewind and go the potentially slow route.
                        bufferPos -= 2;
                        lastTag = ReadRawVarint32();
                    }
                }
            }
            else
            {
                if (IsAtEnd)
                {
                    lastTag = 0;
                    return 0; // This is the only case in which we return 0.
                }

                lastTag = ReadRawVarint32();
            }
            if (WireFormat.GetTagFieldNumber(lastTag) == 0)
            {
                // If we actually read a tag with a field of 0, that's not a valid tag.
                throw InvalidProtocolBufferException.InvalidTag();
            }
            return lastTag;
        }

        private static uint? TryReadTagFromBuffer(ReadOnlySequence<byte> sequence, out SequencePosition consumed)
        {

        }

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
