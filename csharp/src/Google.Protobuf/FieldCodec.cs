#region Copyright notice and license
// Protocol Buffers - Google's data interchange format
// Copyright 2015 Google Inc.  All rights reserved.
// https://developers.google.com/protocol-buffers/
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are
// met:
//
//     * Redistributions of source code must retain the above copyright
// notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above
// copyright notice, this list of conditions and the following disclaimer
// in the documentation and/or other materials provided with the
// distribution.
//     * Neither the name of Google Inc. nor the names of its
// contributors may be used to endorse or promote products derived from
// this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
// OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#endregion

using Google.Protobuf.Collections;
using Google.Protobuf.Compatibility;
using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.Security;

namespace Google.Protobuf
{
    /// <summary>
    /// Factory methods for <see cref="FieldCodec{T}"/>.
    /// </summary>
    public static class FieldCodec
    {
        // TODO: Avoid the "dual hit" of lambda expressions: create open delegates instead. (At least test...)

        /// <summary>
        /// Retrieves a codec suitable for a string field with the given tag.
        /// </summary>
        /// <param name="tag">The tag.</param>
        /// <returns>A codec for the given tag.</returns>
        [SecuritySafeCritical]
        public static FieldCodec<string> ForString(uint tag) => new FieldCodec<string>(ParseString, (output, value) => output.WriteString(value), CodedOutputStream.ComputeStringSize, tag);

        [SecurityCritical]
        private static string ParseString(CodedInputStream input, ref ReadOnlySpan<byte> immediateBuffer) => input.ReadString(ref immediateBuffer);

        /// <summary>
        /// Retrieves a codec suitable for a bytes field with the given tag.
        /// </summary>
        /// <param name="tag">The tag.</param>
        /// <returns>A codec for the given tag.</returns>
        [SecuritySafeCritical]
        public static FieldCodec<ByteString> ForBytes(uint tag) => new FieldCodec<ByteString>(ParseBytes, (output, value) => output.WriteBytes(value), CodedOutputStream.ComputeBytesSize, tag);

        [SecurityCritical]
        private static ByteString ParseBytes(CodedInputStream input, ref ReadOnlySpan<byte> immediateBuffer) => input.ReadBytes(ref immediateBuffer);

        /// <summary>
        /// Retrieves a codec suitable for a bool field with the given tag.
        /// </summary>
        /// <param name="tag">The tag.</param>
        /// <returns>A codec for the given tag.</returns>
        [SecuritySafeCritical]
        public static FieldCodec<bool> ForBool(uint tag) => new FieldCodec<bool>(ParseBool, (output, value) => output.WriteBool(value), CodedOutputStream.ComputeBoolSize, tag);

        [SecurityCritical]
        private static bool ParseBool(CodedInputStream input, ref ReadOnlySpan<byte> immediateBuffer) => input.ReadBool(ref immediateBuffer);

        /// <summary>
        /// Retrieves a codec suitable for an int32 field with the given tag.
        /// </summary>
        /// <param name="tag">The tag.</param>
        /// <returns>A codec for the given tag.</returns>
        [SecuritySafeCritical]
        public static FieldCodec<int> ForInt32(uint tag) => new FieldCodec<int>(ParseInt32, (output, value) => output.WriteInt32(value), CodedOutputStream.ComputeInt32Size, tag);

        [SecurityCritical]
        private static int ParseInt32(CodedInputStream input, ref ReadOnlySpan<byte> immediateBuffer) => input.ReadInt32(ref immediateBuffer);

        /// <summary>
        /// Retrieves a codec suitable for an sint32 field with the given tag.
        /// </summary>
        /// <param name="tag">The tag.</param>
        /// <returns>A codec for the given tag.</returns>
        [SecuritySafeCritical]
        public static FieldCodec<int> ForSInt32(uint tag) => new FieldCodec<int>(ParseSInt32, (output, value) => output.WriteSInt32(value), CodedOutputStream.ComputeSInt32Size, tag);

        [SecurityCritical]
        private static int ParseSInt32(CodedInputStream input, ref ReadOnlySpan<byte> immediateBuffer) => input.ReadSInt32(ref immediateBuffer);

        /// <summary>
        /// Retrieves a codec suitable for a fixed32 field with the given tag.
        /// </summary>
        /// <param name="tag">The tag.</param>
        /// <returns>A codec for the given tag.</returns>
        [SecuritySafeCritical]
        public static FieldCodec<uint> ForFixed32(uint tag) => new FieldCodec<uint>(ParseFixed32, (output, value) => output.WriteFixed32(value), 4, tag);

        [SecurityCritical]
        private static uint ParseFixed32(CodedInputStream input, ref ReadOnlySpan<byte> immediateBuffer) => input.ReadFixed32(ref immediateBuffer);

        /// <summary>
        /// Retrieves a codec suitable for an sfixed32 field with the given tag.
        /// </summary>
        /// <param name="tag">The tag.</param>
        /// <returns>A codec for the given tag.</returns>
        [SecuritySafeCritical]
        public static FieldCodec<int> ForSFixed32(uint tag) => new FieldCodec<int>(ParseSFixed32, (output, value) => output.WriteSFixed32(value), 4, tag);

        [SecurityCritical]
        private static int ParseSFixed32(CodedInputStream input, ref ReadOnlySpan<byte> immediateBuffer) => input.ReadSFixed32(ref immediateBuffer);

        /// <summary>
        /// Retrieves a codec suitable for a uint32 field with the given tag.
        /// </summary>
        /// <param name="tag">The tag.</param>
        /// <returns>A codec for the given tag.</returns>
        [SecuritySafeCritical]
        public static FieldCodec<uint> ForUInt32(uint tag) => new FieldCodec<uint>(ParseUInt32, (output, value) => output.WriteUInt32(value), CodedOutputStream.ComputeUInt32Size, tag);

        [SecurityCritical]
        private static uint ParseUInt32(CodedInputStream input, ref ReadOnlySpan<byte> immediateBuffer) => input.ReadUInt32(ref immediateBuffer);

        /// <summary>
        /// Retrieves a codec suitable for an int64 field with the given tag.
        /// </summary>
        /// <param name="tag">The tag.</param>
        /// <returns>A codec for the given tag.</returns>
        [SecuritySafeCritical]
        public static FieldCodec<long> ForInt64(uint tag) => new FieldCodec<long>(ParseInt64, (output, value) => output.WriteInt64(value), CodedOutputStream.ComputeInt64Size, tag);

        [SecurityCritical]
        private static long ParseInt64(CodedInputStream input, ref ReadOnlySpan<byte> immediateBuffer) => input.ReadInt64(ref immediateBuffer);

        /// <summary>
        /// Retrieves a codec suitable for an sint64 field with the given tag.
        /// </summary>
        /// <param name="tag">The tag.</param>
        /// <returns>A codec for the given tag.</returns>
        [SecuritySafeCritical]
        public static FieldCodec<long> ForSInt64(uint tag) => new FieldCodec<long>(ParseSInt64, (output, value) => output.WriteSInt64(value), CodedOutputStream.ComputeSInt64Size, tag);

        [SecurityCritical]
        private static long ParseSInt64(CodedInputStream input, ref ReadOnlySpan<byte> immediateBuffer) => input.ReadSInt64(ref immediateBuffer);

        /// <summary>
        /// Retrieves a codec suitable for a fixed64 field with the given tag.
        /// </summary>
        /// <param name="tag">The tag.</param>
        /// <returns>A codec for the given tag.</returns>
        [SecuritySafeCritical]
        public static FieldCodec<ulong> ForFixed64(uint tag) => new FieldCodec<ulong>(ParseFixed64, (output, value) => output.WriteFixed64(value), 8, tag);

        [SecurityCritical]
        private static ulong ParseFixed64(CodedInputStream input, ref ReadOnlySpan<byte> immediateBuffer) => input.ReadFixed64(ref immediateBuffer);

        /// <summary>
        /// Retrieves a codec suitable for an sfixed64 field with the given tag.
        /// </summary>
        /// <param name="tag">The tag.</param>
        /// <returns>A codec for the given tag.</returns>
        [SecuritySafeCritical]
        public static FieldCodec<long> ForSFixed64(uint tag) => new FieldCodec<long>(ParseSFixed64, (output, value) => output.WriteSFixed64(value), 8, tag);

        [SecurityCritical]
        private static long ParseSFixed64(CodedInputStream input, ref ReadOnlySpan<byte> immediateBuffer) => input.ReadSFixed64(ref immediateBuffer);

        /// <summary>
        /// Retrieves a codec suitable for a uint64 field with the given tag.
        /// </summary>
        /// <param name="tag">The tag.</param>
        /// <returns>A codec for the given tag.</returns>
        [SecuritySafeCritical]
        public static FieldCodec<ulong> ForUInt64(uint tag) => new FieldCodec<ulong>(ParseUInt64, (output, value) => output.WriteUInt64(value), CodedOutputStream.ComputeUInt64Size, tag);

        [SecurityCritical]
        private static ulong ParseUInt64(CodedInputStream input, ref ReadOnlySpan<byte> immediateBuffer) => input.ReadUInt64(ref immediateBuffer);

        /// <summary>
        /// Retrieves a codec suitable for a float field with the given tag.
        /// </summary>
        /// <param name="tag">The tag.</param>
        /// <returns>A codec for the given tag.</returns>
        [SecuritySafeCritical]
        public static FieldCodec<float> ForFloat(uint tag) => new FieldCodec<float>(ParseFloat, (output, value) => output.WriteFloat(value), CodedOutputStream.ComputeFloatSize, tag);

        [SecurityCritical]
        private static float ParseFloat(CodedInputStream input, ref ReadOnlySpan<byte> immediateBuffer) => input.ReadFloat(ref immediateBuffer);

        /// <summary>
        /// Retrieves a codec suitable for a double field with the given tag.
        /// </summary>
        /// <param name="tag">The tag.</param>
        /// <returns>A codec for the given tag.</returns>
        [SecuritySafeCritical]
        public static FieldCodec<double> ForDouble(uint tag) => new FieldCodec<double>(ParseDouble, (output, value) => output.WriteDouble(value), CodedOutputStream.ComputeDoubleSize, tag);

        [SecurityCritical]
        private static double ParseDouble(CodedInputStream input, ref ReadOnlySpan<byte> immediateBuffer) => input.ReadDouble(ref immediateBuffer);

        // Enums are tricky. We can probably use expression trees to build these delegates automatically,
        // but it's easy to generate the code for it.

        /// <summary>
        /// Retrieves a codec suitable for an enum field with the given tag.
        /// </summary>
        /// <param name="tag">The tag.</param>
        /// <param name="toInt32">A conversion function from <see cref="Int32"/> to the enum type.</param>
        /// <param name="fromInt32">A conversion function from the enum type to <see cref="Int32"/>.</param>
        /// <returns>A codec for the given tag.</returns>
        [SecuritySafeCritical]
        public static FieldCodec<T> ForEnum<T>(uint tag, Func<T, int> toInt32, Func<int, T> fromInt32)
        {
            var enumCodec = new EnumCodec<T>(toInt32, fromInt32);
            return new FieldCodec<T>(enumCodec.ParseEnum, enumCodec.WriteEnum, value => CodedOutputStream.ComputeEnumSize(toInt32(value)), tag);
        }

        private sealed class EnumCodec<T>
        {
            private Func<T, int> toInt32;
            private Func<int, T> fromInt32;

            public EnumCodec(Func<T, int> toInt32, Func<int, T> fromInt32)
            {
                this.fromInt32 = fromInt32;
                this.toInt32 = toInt32;
            }

            [SecurityCritical]
            public T ParseEnum(CodedInputStream input, ref ReadOnlySpan<byte> immediateBuffer) => fromInt32(input.ReadEnum(ref immediateBuffer));
            public void WriteEnum(CodedOutputStream output, T value) => output.WriteEnum(toInt32(value));
        }

        /// <summary>
        /// Retrieves a codec suitable for a message field with the given tag.
        /// </summary>
        /// <param name="tag">The tag.</param>
        /// <param name="parser">A parser to use for the message type.</param>
        /// <returns>A codec for the given tag.</returns>
        [SecuritySafeCritical]
        public static FieldCodec<T> ForMessage<T>(uint tag, MessageParser<T> parser) where T : IMessage<T>
        {
            var codec = new MessageCodec<T>(parser);
            return new FieldCodec<T>(codec.ParseMessage, codec.WriteMessage, message => CodedOutputStream.ComputeMessageSize(message), tag);
        }

        private sealed class MessageCodec<T> where T : IMessage<T>
        {
            private MessageParser<T> parser;

            public MessageCodec(MessageParser<T> parser)
            {
                this.parser = parser;
            }

            [SecurityCritical]
            public T ParseMessage(CodedInputStream input, ref ReadOnlySpan<byte> immediateBuffer)
            {
                T message = parser.CreateTemplate();
                input.ReadMessage(message, ref immediateBuffer);
                return message;
            }

            public void WriteMessage(CodedOutputStream output, T value) => output.WriteMessage(value);
        }

        /// <summary>
        /// Creates a codec for a wrapper type of a class - which must be string or ByteString.
        /// </summary>
        [SecuritySafeCritical]
        public static FieldCodec<T> ForClassWrapper<T>(uint tag) where T : class
        {
            var nestedCodec = WrapperCodecs.GetCodec<T>();
            var wrapperCodec = new ClassWrapperCodec<T>(nestedCodec);
            return new FieldCodec<T>(wrapperCodec.ParseMessage, wrapperCodec.WriteMessage,
                value => WrapperCodecs.CalculateSize<T>(value, nestedCodec),
                tag,
                null); // Default value for the wrapper
        }

        private sealed class ClassWrapperCodec<T> where T : class
        {
            private FieldCodec<T> nestedCodec;

            public ClassWrapperCodec(FieldCodec<T> nestedCodec)
            {
                this.nestedCodec = nestedCodec;
            }

            [SecurityCritical]
            public T ParseMessage(CodedInputStream input, ref ReadOnlySpan<byte> immediateBuffer) => WrapperCodecs.Read(input, nestedCodec, ref immediateBuffer);

            public void WriteMessage(CodedOutputStream output, T value) => WrapperCodecs.Write(output, value, nestedCodec);
        }


        /// <summary>
        /// Creates a codec for a wrapper type of a struct - which must be Int32, Int64, UInt32, UInt64,
        /// Bool, Single or Double.
        /// </summary>
        [SecuritySafeCritical]
        public static FieldCodec<T?> ForStructWrapper<T>(uint tag) where T : struct
        {
            var nestedCodec = WrapperCodecs.GetCodec<T>();
            var wrapperCodec = new StructWrapperCodec<T>(nestedCodec);
            return new FieldCodec<T?>(wrapperCodec.ParseMessage, wrapperCodec.WriteMessage,
                value => value == null ? 0 : WrapperCodecs.CalculateSize<T>(value.Value, nestedCodec),
                tag,
                null); // Default value for the wrapper
        }

        private sealed class StructWrapperCodec<T> where T : struct
        {
            private FieldCodec<T> nestedCodec;

            public StructWrapperCodec(FieldCodec<T> nestedCodec)
            {
                this.nestedCodec = nestedCodec;
            }

            [SecurityCritical]
            public T? ParseMessage(CodedInputStream input, ref ReadOnlySpan<byte> immediateBuffer) => WrapperCodecs.Read(input, nestedCodec, ref immediateBuffer);

            public void WriteMessage(CodedOutputStream output, T? value) => WrapperCodecs.Write(output, value.Value, nestedCodec);
        }

        /// <summary>
        /// Helper code to create codecs for wrapper types.
        /// </summary>
        /// <remarks>
        /// Somewhat ugly with all the static methods, but the conversions involved to/from nullable types make it
        /// slightly tricky to improve. So long as we keep the public API (ForClassWrapper, ForStructWrapper) in place,
        /// we can refactor later if we come up with something cleaner.
        /// </remarks>
        private static class WrapperCodecs
        {
            private static readonly Dictionary<System.Type, object> Codecs = new Dictionary<System.Type, object>
            {
                { typeof(bool), ForBool(WireFormat.MakeTag(WrappersReflection.WrapperValueFieldNumber, WireFormat.WireType.Varint)) },
                { typeof(int), ForInt32(WireFormat.MakeTag(WrappersReflection.WrapperValueFieldNumber, WireFormat.WireType.Varint)) },
                { typeof(long), ForInt64(WireFormat.MakeTag(WrappersReflection.WrapperValueFieldNumber, WireFormat.WireType.Varint)) },
                { typeof(uint), ForUInt32(WireFormat.MakeTag(WrappersReflection.WrapperValueFieldNumber, WireFormat.WireType.Varint)) },
                { typeof(ulong), ForUInt64(WireFormat.MakeTag(WrappersReflection.WrapperValueFieldNumber, WireFormat.WireType.Varint)) },
                { typeof(float), ForFloat(WireFormat.MakeTag(WrappersReflection.WrapperValueFieldNumber, WireFormat.WireType.Fixed32)) },
                { typeof(double), ForDouble(WireFormat.MakeTag(WrappersReflection.WrapperValueFieldNumber, WireFormat.WireType.Fixed64)) },
                { typeof(string), ForString(WireFormat.MakeTag(WrappersReflection.WrapperValueFieldNumber, WireFormat.WireType.LengthDelimited)) },
                { typeof(ByteString), ForBytes(WireFormat.MakeTag(WrappersReflection.WrapperValueFieldNumber, WireFormat.WireType.LengthDelimited)) }
            };

            /// <summary>
            /// Returns a field codec which effectively wraps a value of type T in a message.
            /// 
            /// </summary>
            internal static FieldCodec<T> GetCodec<T>()
            {
                object value;
                if (!Codecs.TryGetValue(typeof(T), out value))
                {
                    throw new InvalidOperationException("Invalid type argument requested for wrapper codec: " + typeof(T));
                }
                return (FieldCodec<T>) value;
            }

            [SecurityCritical]
            internal static T Read<T>(CodedInputStream input, FieldCodec<T> codec, ref ReadOnlySpan<byte> immediateBuffer)
            {
                int length = input.ReadLength(ref immediateBuffer);
                int oldLimit = input.PushLimit(length);

                uint tag;
                T value = codec.DefaultValue;
                while ((tag = input.ReadTag(ref immediateBuffer)) != 0)
                {
                    if (tag == codec.Tag)
                    {
                        value = codec.Read(input, ref immediateBuffer);
                    }
                    else
                    {
                        input.SkipLastField(ref immediateBuffer);
                    }

                }
                input.CheckReadEndOfStreamTag();
                input.PopLimit(oldLimit);

                return value;
            }

            internal static void Write<T>(CodedOutputStream output, T value, FieldCodec<T> codec)
            {
                output.WriteLength(codec.CalculateSizeWithTag(value));
                codec.WriteTagAndValue(output, value);
            }

            internal  static int CalculateSize<T>(T value, FieldCodec<T> codec)
            {
                int fieldLength = codec.CalculateSizeWithTag(value);
                return CodedOutputStream.ComputeLengthSize(fieldLength) + fieldLength;
            }
        }
    }

    internal delegate T ValueReaderDelegate<T>(CodedInputStream input, ref ReadOnlySpan<byte> buffer);

    /// <summary>
    /// <para>
    /// An encode/decode pair for a single field. This effectively encapsulates
    /// all the information needed to read or write the field value from/to a coded
    /// stream.
    /// </para>
    /// <para>
    /// This class is public and has to be as it is used by generated code, but its public
    /// API is very limited - just what the generated code needs to call directly.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This never writes default values to the stream, and does not address "packedness"
    /// in repeated fields itself, other than to know whether or not the field *should* be packed.
    /// </remarks>
    public sealed class FieldCodec<T>
    {
        private static readonly EqualityComparer<T> EqualityComparer = ProtobufEqualityComparers.GetEqualityComparer<T>();
        private static readonly T DefaultDefault;
        // Only non-nullable value types support packing. This is the simplest way of detecting that.
        private static readonly bool TypeSupportsPacking = default(T) != null;

        static FieldCodec()
        {
            if (typeof(T) == typeof(string))
            {
                DefaultDefault = (T)(object)"";
            }
            else if (typeof(T) == typeof(ByteString))
            {
                DefaultDefault = (T)(object)ByteString.Empty;
            }
            // Otherwise it's the default value of the CLR type
        }

        internal static bool IsPackedRepeatedField(uint tag) =>
            TypeSupportsPacking && WireFormat.GetTagWireType(tag) == WireFormat.WireType.LengthDelimited;

        internal bool PackedRepeatedField { get; }

        /// <summary>
        /// Returns a delegate to write a value (unconditionally) to a coded output stream.
        /// </summary>
        internal Action<CodedOutputStream, T> ValueWriter { get; }

        /// <summary>
        /// Returns the size calculator for just a value.
        /// </summary>
        internal Func<T, int> ValueSizeCalculator { get; }

        /// <summary>
        /// Returns a delegate to read a value from a coded input stream. It is assumed that
        /// the stream is already positioned on the appropriate tag.
        /// </summary>
        internal ValueReaderDelegate<T> ValueReader { get; }

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
        
        internal FieldCodec(
                ValueReaderDelegate<T> reader,
                Action<CodedOutputStream, T> writer,
                int fixedSize,
                uint tag) : this(reader, writer, _ => fixedSize, tag)
        {
            FixedSize = fixedSize;
        }

        internal FieldCodec(
            ValueReaderDelegate<T> reader,
            Action<CodedOutputStream, T> writer,
            Func<T, int> sizeCalculator,
            uint tag) : this(reader, writer, sizeCalculator, tag, DefaultDefault)
        {
        }

        internal FieldCodec(
            ValueReaderDelegate<T> reader,
            Action<CodedOutputStream, T> writer,
            Func<T, int> sizeCalculator,
            uint tag,
            T defaultValue)
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
        /// Write a tag and the given value, *if* the value is not the default.
        /// </summary>
        public void WriteTagAndValue(CodedOutputStream output, T value)
        {
            if (!IsDefault(value))
            {
                output.WriteTag(Tag);
                ValueWriter(output, value);
            }
        }

        /// <summary>
        /// Reads a value of the codec type from the given <see cref="CodedInputStream"/>.
        /// </summary>
        /// <param name="input">The input stream to read from.</param>
        /// <returns>The value read from the stream.</returns>
        [SecurityCritical]
        public T Read(CodedInputStream input, ref ReadOnlySpan<byte> immediateBuffer) => ValueReader(input, ref immediateBuffer);

        /// <summary>
        /// Calculates the size required to write the given value, with a tag,
        /// if the value is not the default.
        /// </summary>
        public int CalculateSizeWithTag(T value) => IsDefault(value) ? 0 : ValueSizeCalculator(value) + tagSize;

        private bool IsDefault(T value) => EqualityComparer.Equals(value, DefaultValue);
    }
}
