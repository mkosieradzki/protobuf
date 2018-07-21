#region Copyright notice and license
// Protocol Buffers - Google's data interchange format
// Copyright 2008 Google Inc.  All rights reserved.
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

using System;
using System.Runtime.CompilerServices;

namespace Google.Protobuf
{
    // This part of CodedOutputStream provides all the static entry points that are used
    // by generated code and internally to compute the size of messages prior to being
    // written to an instance of CodedOutputStream.
    public sealed partial class CodedOutputStream
    {
        private const int LittleEndian64Size = 8;
        private const int LittleEndian32Size = 4;        

        /// <summary>
        /// Computes the number of bytes that would be needed to encode a
        /// double field, including the tag.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ComputeDoubleSize(double value) => LittleEndian64Size;

        /// <summary>
        /// Computes the number of bytes that would be needed to encode a
        /// wrapped double field, including the tag.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ComputeWrappedDoubleSize(double? value) => value.Value == default(double) ? 1 : 2 + LittleEndian64Size;

        /// <summary>
        /// Computes the number of bytes that would be needed to encode a
        /// float field, including the tag.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ComputeFloatSize(float value) => LittleEndian32Size;

        /// <summary>
        /// Computes the number of bytes that would be needed to encode a
        /// float field, including the tag.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ComputeWrappedFloatSize(float? value) => value.Value == default(float) ? 1 : 2 + LittleEndian32Size;

        /// <summary>
        /// Computes the number of bytes that would be needed to encode a
        /// uint64 field, including the tag.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ComputeUInt64Size(ulong value) => ComputeRawVarint64Size(value);

        /// <summary>
        /// Computes the number of bytes that would be needed to encode a
        /// wrapped uint64 field, including the tag.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ComputeWrappedUInt64Size(ulong? value) => value.Value == default(ulong) ? 1 : 2 + ComputeRawVarint64Size(value.Value);

        /// <summary>
        /// Computes the number of bytes that would be needed to encode an
        /// int64 field, including the tag.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ComputeInt64Size(long value) => ComputeRawVarint64Size((ulong) value);

        /// <summary>
        /// Computes the number of bytes that would be needed to encode an
        /// wrapped int64 field, including the tag.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ComputeWrappedInt64Size(long? value) => value.Value == default(long) ? 1 : 2 + ComputeRawVarint64Size((ulong)value.Value);

        /// <summary>
        /// Computes the number of bytes that would be needed to encode an
        /// int32 field, including the tag.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ComputeInt32Size(int value)
        {
            if (value >= 0)
            {
                return ComputeRawVarint32Size((uint) value);
            }
            else
            {
                // Must sign-extend.
                return 10;
            }
        }

        /// <summary>
        /// Computes the number of bytes that would be needed to encode an
        /// wrapped int32 field, including the tag.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ComputeWrappedInt32Size(int? value)
        {
            if (value.Value == 0)
            {
                return 1;
            }
            else if (value.Value > 0)
            {
                return 2 + ComputeRawVarint32Size((uint)value.Value);
            }
            else
            {
                // Must sign-extend.
                return 12;
            }
        }

        /// <summary>
        /// Computes the number of bytes that would be needed to encode a
        /// fixed64 field, including the tag.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ComputeFixed64Size(ulong value) => LittleEndian64Size;

        /// <summary>
        /// Computes the number of bytes that would be needed to encode a
        /// fixed32 field, including the tag.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ComputeFixed32Size(uint value) => LittleEndian32Size;

        /// <summary>
        /// Computes the number of bytes that would be needed to encode a
        /// bool field, including the tag.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ComputeBoolSize(bool value) => 1;

        /// <summary>
        /// Computes the number of bytes that would be needed to encode a
        /// wrapped bool field, including the tag.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ComputeWrappedBoolSize(bool? value) => value.Value == default(bool) ? 1 : 3;

        /// <summary>
        /// Computes the number of bytes that would be needed to encode a
        /// string field, including the tag.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ComputeStringSize(String value)
        {
            int byteArraySize = Utf8Encoding.GetByteCount(value);
            return ComputeLengthSize(byteArraySize) + byteArraySize;
        }

        /// <summary>
        /// Computes the number of bytes that would be needed to encode a
        /// wrapped string field, including the tag.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ComputeWrappedStringSize(String value)
        {
            if (value == String.Empty)
                return 1;

            int byteArraySize = Utf8Encoding.GetByteCount(value);
            var wrappedByteArraySize = ComputeLengthSize(byteArraySize) + byteArraySize;
            return 1 + ComputeLengthSize(wrappedByteArraySize) + wrappedByteArraySize;
        }

        /// <summary>
        /// Computes the number of bytes that would be needed to encode a
        /// group field, including the tag.
        /// </summary>
        public static int ComputeGroupSize(IMessage value) => value.CalculateSize();

        /// <summary>
        /// Computes the number of bytes that would be needed to encode an
        /// embedded message field, including the tag.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ComputeMessageSize(IMessage value)
        {
            int size = value.CalculateSize();
            return ComputeLengthSize(size) + size;
        }

        /// <summary>
        /// Computes the number of bytes that would be needed to encode a
        /// bytes field, including the tag.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ComputeBytesSize(ByteString value) => ComputeLengthSize(value.Length) + value.Length;

        /// <summary>
        /// Computes the number of bytes that would be needed to encode a
        /// wrapped bytes field, including the tag.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ComputeWrappedBytesSize(ByteString value)
        {
            if (value.IsEmpty)
                return 1;

            var wrappedBytesSize = ComputeLengthSize(value.Length) + value.Length;
            return 1 + ComputeLengthSize(wrappedBytesSize) + wrappedBytesSize;
        }

        /// <summary>
        /// Computes the number of bytes that would be needed to encode a
        /// uint32 field, including the tag.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ComputeUInt32Size(uint value) => ComputeRawVarint32Size(value);

        /// <summary>
        /// Computes the number of bytes that would be needed to encode a
        /// wrapped uint32 field, including the tag.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ComputeWrappedUInt32Size(uint? value) => value.Value == default(uint) ? 1 : 2 + ComputeRawVarint32Size(value.Value);

        /// <summary>
        /// Computes the number of bytes that would be needed to encode a
        /// enum field, including the tag. The caller is responsible for
        /// converting the enum value to its numeric value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ComputeEnumSize(int value) => ComputeInt32Size(value);

        /// <summary>
        /// Computes the number of bytes that would be needed to encode an
        /// sfixed32 field, including the tag.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ComputeSFixed32Size(int value) => LittleEndian32Size;

        /// <summary>
        /// Computes the number of bytes that would be needed to encode an
        /// sfixed64 field, including the tag.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ComputeSFixed64Size(long value) => LittleEndian64Size;

        /// <summary>
        /// Computes the number of bytes that would be needed to encode an
        /// sint32 field, including the tag.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ComputeSInt32Size(int value) => ComputeRawVarint32Size(EncodeZigZag32(value));

        /// <summary>
        /// Computes the number of bytes that would be needed to encode an
        /// sint64 field, including the tag.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ComputeSInt64Size(long value) => ComputeRawVarint64Size(EncodeZigZag64(value));

        /// <summary>
        /// Computes the number of bytes that would be needed to encode a length,
        /// as written by <see cref="WriteLength(int)"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ComputeLengthSize(int length) => ComputeRawVarint32Size((uint) length);

        /// <summary>
        /// Computes the number of bytes that would be needed to encode a varint.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ComputeRawVarint32Size(uint value)
        {
            if ((value & (0xffffffff << 7)) == 0)
            {
                return 1;
            }
            if ((value & (0xffffffff << 14)) == 0)
            {
                return 2;
            }
            if ((value & (0xffffffff << 21)) == 0)
            {
                return 3;
            }
            if ((value & (0xffffffff << 28)) == 0)
            {
                return 4;
            }
            return 5;
        }

        /// <summary>
        /// Computes the number of bytes that would be needed to encode a varint.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ComputeRawVarint64Size(ulong value)
        {
            if ((value & (0xffffffffffffffffL << 7)) == 0)
            {
                return 1;
            }
            if ((value & (0xffffffffffffffffL << 14)) == 0)
            {
                return 2;
            }
            if ((value & (0xffffffffffffffffL << 21)) == 0)
            {
                return 3;
            }
            if ((value & (0xffffffffffffffffL << 28)) == 0)
            {
                return 4;
            }
            if ((value & (0xffffffffffffffffL << 35)) == 0)
            {
                return 5;
            }
            if ((value & (0xffffffffffffffffL << 42)) == 0)
            {
                return 6;
            }
            if ((value & (0xffffffffffffffffL << 49)) == 0)
            {
                return 7;
            }
            if ((value & (0xffffffffffffffffL << 56)) == 0)
            {
                return 8;
            }
            if ((value & (0xffffffffffffffffL << 63)) == 0)
            {
                return 9;
            }
            return 10;
        }

        /// <summary>
        /// Computes the number of bytes that would be needed to encode a tag.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ComputeTagSize(int fieldNumber) => ComputeRawVarint32Size(WireFormat.MakeTag(fieldNumber, 0));
    }
}