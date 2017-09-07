using System.Collections.Generic;

namespace Google.Protobuf.Fast
{
    public delegate void ValueWriter<T>(CodedOutputStream codedOutputStream, ref T value);
    public delegate void ValueReader<T>(CodedInputStream codedInputStream, ref T value, IAllocator allocator);
    public delegate int ValueSizeCalculator<T>(ref T value);

    public static class FieldCodec
    {
        /// <summary>
        /// Retrieves a codec suitable for a message field with the given tag.
        /// </summary>
        /// <param name="tag">The tag.</param>
        /// <param name="parser">A parser to use for the message type.</param>
        /// <returns>A codec for the given tag.</returns>
        public static FieldCodec<T> ForMessage<T>(uint tag) where T : struct, IMessage<T> => new FieldCodec<T>(
            (CodedInputStream input, ref T message, IAllocator allocator) => input.ReadMessage(ref message, allocator),
            (CodedOutputStream output, ref T message) => output.WriteMessage(message),
            (ref T message) => CodedOutputStream.ComputeMessageSize(message),
            tag);
    }

    public sealed class FieldCodec<T> where T : struct
    {
        //NOTE: Only primitive types support packing
        private bool isPrimitiveType;

        internal bool PackedRepeatedField { get; }

        /// <summary>
        /// Returns a delegate to write a value (unconditionally) to a coded output stream.
        /// </summary>
        internal ValueWriter<T> ValueWriter { get; }

        /// <summary>
        /// Returns the size calculator for just a value.
        /// </summary>
        internal ValueSizeCalculator<T> ValueSizeCalculator { get; }

        /// <summary>
        /// Returns a delegate to read a value from a coded input stream. It is assumed that
        /// the stream is already positioned on the appropriate tag.
        /// </summary>
        internal ValueReader<T> ValueReader { get; }

        /// <summary>
        /// Returns the fixed size for an entry, or 0 if sizes vary.
        /// </summary>
        internal int FixedSize { get; }

        /// <summary>
        /// Gets the tag of the codec.
        /// </summary>
        /// <value>
        /// The tag of the codec.
        /// </value>
        internal uint Tag { get; }

        /// <summary>
        /// Default value for this codec. Usually the same for every instance of the same type, but
        /// for string/ByteString wrapper fields the codec's default value is null, whereas for
        /// other string/ByteString fields it's "" or ByteString.Empty.
        /// </summary>
        /// <value>
        /// The default value of the codec's type.
        /// </value>
        internal T DefaultValue { get; }

        private readonly int tagSize;

        internal FieldCodec(bool isPrimitiveType, ValueReader<T> reader, ValueWriter<T> writer, int fixedSize, uint tag) : this(reader, writer, (ref T val) => fixedSize, tag)
        {
            this.isPrimitiveType = isPrimitiveType;
            FixedSize = fixedSize;
        }

        internal FieldCodec(ValueReader<T> reader, ValueWriter<T> writer, ValueSizeCalculator<T> sizeCalculator, uint tag) : this(reader, writer, sizeCalculator, tag, default(T))
        {
        }

        internal FieldCodec(ValueReader<T> reader, ValueWriter<T> writer, ValueSizeCalculator<T> sizeCalculator, uint tag, T defaultValue)
        {
            ValueReader = reader;
            ValueWriter = writer;
            ValueSizeCalculator = sizeCalculator;
            FixedSize = 0;
            Tag = tag;
            DefaultValue = defaultValue;
            tagSize = CodedOutputStream.ComputeRawVarint32Size(tag);
            // Detect packed-ness once, so we can check for it within RepeatedField<T>.
            PackedRepeatedField = IsPackedRepeatedField(tag);
        }

        /// <summary>
        /// Reads a value of the codec type from the given <see cref="CodedInputStream"/>.
        /// </summary>
        /// <param name="input">The input stream to read from.</param>
        /// <returns>The value read from the stream.</returns>
        public void ReadValue(CodedInputStream codedInputStream, ref T dest, IAllocator allocator) => ValueReader(codedInputStream, ref dest, allocator);

        /// <summary>
        /// Write a tag and the given value, *if* the value is not the default.
        /// </summary>
        public void WriteTagAndValue(CodedOutputStream output, ref T value)
        {
            if (!IsDefault(ref value))
            {
                output.WriteTag(Tag);
                ValueWriter(output, ref value);
            }
        }

        public bool IsPackedRepeatedField(uint tag) => isPrimitiveType && WireFormat.GetTagWireType(tag) == WireFormat.WireType.LengthDelimited;

        private bool IsDefault(ref T value) => EqualityComparer<T>.Default.Equals(value, DefaultValue);
    }
}
