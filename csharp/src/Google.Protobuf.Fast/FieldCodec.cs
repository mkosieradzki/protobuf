using System;
using System.Collections.Generic;

namespace Google.Protobuf.Fast
{
    public delegate void ValueWriter<T>(CodedOutputStream codedOutputStream, ref T value);
    public delegate void ValueReader<T>(CodedInputStream codedInputStream, ref T value);
    public delegate int ValueSizeCalculator<T>(ref T value);

    public sealed class FieldCodec<T> where T : struct
    {
        //private static readonly T DefaultDefault;
        //// Only non-nullable value types support packing. This is the simplest way of detecting that.
        //private static readonly bool TypeSupportsPacking = default(T) != null;

        //static FieldCodec()
        //{
        //    if (typeof(T) == typeof(string))
        //    {
        //        DefaultDefault = (T)(object)"";
        //    }
        //    else if (typeof(T) == typeof(ByteString))
        //    {
        //        DefaultDefault = (T)(object)ByteString.Empty;
        //    }
        //    // Otherwise it's the default value of the CLR type
        //}

        private bool supportsPacking;

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

        internal FieldCodec(ValueReader<T> reader, ValueWriter<T> writer, int fixedSize, uint tag) : this(reader, writer _ => fixedSize, tag)
        {
            FixedSize = fixedSize;
        }

        internal FieldCodec(ValueReader<T> reader, ValueWriter<T> writer, ValueSizeCalculator<T> sizeCalculator, uint tag) : this(reader, writer, sizeCalculator, tag, DefaultDefault)
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
        public void ReadValue(CodedInputStream codedInputStream, ref T dest) => ValueReader(codedInputStream, ref dest);

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

        public bool IsPackedRepeatedField(uint tag) => supportsPacking && WireFormat.GetTagWireType(tag) == WireFormat.WireType.LengthDelimited;

        private bool IsDefault(ref T value) => EqualityComparer<T>.Default.Equals(value, DefaultValue);
    }
}
