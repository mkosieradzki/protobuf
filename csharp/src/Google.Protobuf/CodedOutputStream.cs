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

using Google.Protobuf.Collections;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;

namespace Google.Protobuf
{
    /// <summary>
    /// Encodes and writes protocol message fields.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class is generally used by generated code to write appropriate
    /// primitives to the stream. It effectively encapsulates the lowest
    /// levels of protocol buffer format. Unlike some other implementations,
    /// this does not include combined "write tag and value" methods. Generated
    /// code knows the exact byte representations of the tags they're going to write,
    /// so there's no need to re-encode them each time. Manually-written code calling
    /// this class should just call one of the <c>WriteTag</c> overloads before each value.
    /// </para>
    /// <para>
    /// Repeated fields and map fields are not handled by this class; use <c>RepeatedField&lt;T&gt;</c>
    /// and <c>MapField&lt;TKey, TValue&gt;</c> to serialize such fields.
    /// </para>
    /// </remarks>
    public sealed partial class CodedOutputStream : IDisposable
    {
        // "Local" copy of Encoding.UTF8, for efficiency. (Yes, it makes a difference.)
        internal static readonly Encoding Utf8Encoding = Encoding.UTF8;

        /// <summary>
        /// The buffer size used by CreateInstance(Stream).
        /// </summary>
        public static readonly int DefaultBufferSize = 4096;

        private readonly bool leaveOpen;
        private readonly byte[] buffer;
        private int limit;
        private int position;
        private readonly Stream output;

        private Memory<byte> nativeBuffer;
        private readonly IBufferWriter<byte> nativeOutput;
        private long nativeOutputPosition;

        #region Construction
        /// <summary>
        /// Creates a new CodedOutputStream that writes directly to the given
        /// byte array. If more bytes are written than fit in the array,
        /// OutOfSpaceException will be thrown.
        /// </summary>
        public CodedOutputStream(byte[] flatArray) : this(flatArray, 0, flatArray.Length)
        {
        }

        /// <summary>
        /// Creates a new CodedOutputStream that writes directly to the contiguous
        /// memory. If more bytes are written than fit in the memory,
        /// OutOfSpaceException will be thrown.
        /// </summary>
        [SecurityCritical]
        public CodedOutputStream(Memory<byte> nativeBuffer)
        {
            this.nativeBuffer = nativeBuffer;
            this.position = 0;
            this.limit = nativeBuffer.Length;
            leaveOpen = true; // Simple way of avoiding trying to dispose of a null reference
        }

        /// <summary>
        /// Creates a new CodedOutputStream that writes directly to the contiguous
        /// memory. If more bytes are written than fit in the memory,
        /// OutOfSpaceException will be thrown.
        /// </summary>
        [SecurityCritical]
        public CodedOutputStream(IBufferWriter<byte> nativeOutput)
        {
            this.nativeOutput = ProtoPreconditions.CheckNotNull(nativeOutput, nameof(nativeOutput));
            this.nativeBuffer = nativeOutput.GetMemory();
            this.position = 0;
            this.limit = nativeBuffer.Length;
            leaveOpen = true; // Simple way of avoiding trying to dispose of a null reference
        }

        /// <summary>
        /// Creates a new CodedOutputStream that writes directly to the given
        /// byte array slice. If more bytes are written than fit in the array,
        /// OutOfSpaceException will be thrown.
        /// </summary>
        private CodedOutputStream(byte[] buffer, int offset, int length)
        {
            this.buffer = buffer;
            this.position = offset;
            this.limit = offset + length;
            leaveOpen = true; // Simple way of avoiding trying to dispose of a null reference
        }

        private CodedOutputStream(Stream output, byte[] buffer, bool leaveOpen)
        {
            this.output = ProtoPreconditions.CheckNotNull(output, nameof(output));
            this.buffer = buffer;
            this.position = 0;
            this.limit = buffer.Length;
            this.leaveOpen = leaveOpen;
        }

        /// <summary>
        /// Creates a new <see cref="CodedOutputStream" /> which write to the given stream, and disposes of that
        /// stream when the returned <c>CodedOutputStream</c> is disposed.
        /// </summary>
        /// <param name="output">The stream to write to. It will be disposed when the returned <c>CodedOutputStream is disposed.</c></param>
        public CodedOutputStream(Stream output) : this(output, DefaultBufferSize, false)
        {
        }

        /// <summary>
        /// Creates a new CodedOutputStream which write to the given stream and uses
        /// the specified buffer size.
        /// </summary>
        /// <param name="output">The stream to write to. It will be disposed when the returned <c>CodedOutputStream is disposed.</c></param>
        /// <param name="bufferSize">The size of buffer to use internally.</param>
        public CodedOutputStream(Stream output, int bufferSize) : this(output, new byte[bufferSize], false)
        {
        }

        /// <summary>
        /// Creates a new CodedOutputStream which write to the given stream.
        /// </summary>
        /// <param name="output">The stream to write to.</param>
        /// <param name="leaveOpen">If <c>true</c>, <paramref name="output"/> is left open when the returned <c>CodedOutputStream</c> is disposed;
        /// if <c>false</c>, the provided stream is disposed as well.</param>
        public CodedOutputStream(Stream output, bool leaveOpen) : this(output, DefaultBufferSize, leaveOpen)
        {
        }

        /// <summary>
        /// Creates a new CodedOutputStream which write to the given stream and uses
        /// the specified buffer size.
        /// </summary>
        /// <param name="output">The stream to write to.</param>
        /// <param name="bufferSize">The size of buffer to use internally.</param>
        /// <param name="leaveOpen">If <c>true</c>, <paramref name="output"/> is left open when the returned <c>CodedOutputStream</c> is disposed;
        /// if <c>false</c>, the provided stream is disposed as well.</param>
        public CodedOutputStream(Stream output, int bufferSize, bool leaveOpen) : this(output, new byte[bufferSize], leaveOpen)
        {
        }
        #endregion

        /// <summary>
        /// Returns the current position in the stream, or the position in the output buffer
        /// </summary>
        public long Position
        {
            get
            {
                if (output != null)
                {
                    return output.Position + position;
                }
                return nativeOutputPosition + position;
            }
        }

        internal Span<byte> ImmediateBuffer
        {
            [SecurityCritical]
            get => buffer != null ? buffer : nativeBuffer.Span;
        }

        #region Writing of values (not including tags)

        /// <summary>
        /// Writes a double field value, without a tag, to the stream.
        /// </summary>
        /// <param name="value">The value to write</param>
        [SecuritySafeCritical]
        public void WriteDouble(double value)
        {
            var immediateBudder = ImmediateBuffer;
            WriteDouble(value, ref immediateBudder);
        }

        /// <summary>
        /// This supports the Protocol Buffers infrastructure and is not meant to be used directly from your code.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SecurityCritical]
        public void WriteDouble(double value, ref Span<byte> immediateBuffer) => WriteRawLittleEndian64((ulong)BitConverter.DoubleToInt64Bits(value), ref immediateBuffer);

        /// <summary>
        /// This supports the Protocol Buffers infrastructure and is not meant to be used directly from your code.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SecurityCritical]
        public void WriteWrappedDouble(double? value, ref Span<byte> immediateBuffer)
        {
            if (value.Value == default(double))
            {
                WriteLength(0, ref immediateBuffer);
            }
            else
            {
                WriteRawByte(LittleEndian64Size + 1, ref immediateBuffer);
                WriteRawByte(WellKnownTypes.WrappersReflection.WrapperValueFixed64TagByte, ref immediateBuffer);
                WriteDouble(value.Value, ref immediateBuffer);
            }
        }

        /// <summary>
        /// This supports the Protocol Buffers infrastructure and is not meant to be used directly from your code.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SecurityCritical]
#if NETCOREAPP2_1
        public void WriteFloat(float value, ref Span<byte> immediateBuffer) => WriteRawLittleEndian32((uint)BitConverter.SingleToInt32Bits(value), ref immediateBuffer);
#else
        public void WriteFloat(float value, ref Span<byte> immediateBuffer) => WriteRawLittleEndian32((uint)SingleToInt32BitsSlow(value), ref immediateBuffer);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int SingleToInt32BitsSlow(float value) => BitConverter.ToInt32(BitConverter.GetBytes(value), 0);
#endif

        /// <summary>
        /// Writes a float field value, without a tag, to the stream.
        /// </summary>
        /// <param name="value">The value to write</param>
        [SecuritySafeCritical]
        public void WriteFloat(float value)
        {
            var immediateBudder = ImmediateBuffer;
            WriteDouble(value, ref immediateBudder);
        }

        /// <summary>
        /// This supports the Protocol Buffers infrastructure and is not meant to be used directly from your code.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SecurityCritical]
        public void WriteWrappedFloat(float? value, ref Span<byte> immediateBuffer)
        {
            if (value.Value == default(float))
            {
                WriteLength(0, ref immediateBuffer);
            }
            else
            {
                WriteRawByte(LittleEndian32Size + 1, ref immediateBuffer);
                WriteRawByte(WellKnownTypes.WrappersReflection.WrapperValueFixed32TagByte, ref immediateBuffer);
                WriteFloat(value.Value, ref immediateBuffer);
            }
        }

        /// <summary>
        /// Writes a uint64 field value, without a tag, to the stream.
        /// </summary>
        /// <param name="value">The value to write</param>
        [SecuritySafeCritical]
        public void WriteUInt64(ulong value)
        {
            var immediateBudder = ImmediateBuffer;
            WriteUInt64(value, ref immediateBudder);
        }

        /// <summary>
        /// This supports the Protocol Buffers infrastructure and is not meant to be used directly from your code.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SecurityCritical]
        public void WriteUInt64(ulong value, ref Span<byte> immediateBuffer) => WriteRawVarint64(value, ref immediateBuffer);

        /// <summary>
        /// This supports the Protocol Buffers infrastructure and is not meant to be used directly from your code.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SecurityCritical]
        public void WriteWrappedUInt64(ulong? value, ref Span<byte> immediateBuffer) => WriteWrappedRawVarint64(value.Value, ref immediateBuffer);

        /// <summary>
        /// Writes an int64 field value, without a tag, to the stream.
        /// </summary>
        /// <param name="value">The value to write</param>
        [SecuritySafeCritical]
        public void WriteInt64(long value)
        {
            var immediateBudder = ImmediateBuffer;
            WriteInt64(value, ref immediateBudder);
        }

        /// <summary>
        /// This supports the Protocol Buffers infrastructure and is not meant to be used directly from your code.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SecurityCritical]
        public void WriteInt64(long value, ref Span<byte> immediateBuffer) => WriteRawVarint64((ulong)value, ref immediateBuffer);

        /// <summary>
        /// This supports the Protocol Buffers infrastructure and is not meant to be used directly from your code.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SecurityCritical]
        public void WriteWrappedInt64(long? value, ref Span<byte> immediateBuffer) => WriteWrappedRawVarint64((ulong)value.Value, ref immediateBuffer);
       
        /// <summary>
        /// This supports the Protocol Buffers infrastructure and is not meant to be used directly from your code.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SecurityCritical]
        public void WriteInt32(int value, ref Span<byte> immediateBuffer)
        {
            if (value >= 0)
            {
                WriteRawVarint32((uint)value, ref immediateBuffer);
            }
            else
            {
                // Must sign-extend.
                WriteRawVarint64((ulong)value, ref immediateBuffer);
            }
        }

        /// <summary>
        /// Writes an int32 field value, without a tag, to the stream.
        /// </summary>
        /// <param name="value">The value to write</param>
        [SecuritySafeCritical]
        public void WriteInt32(int value)
        {
            var immediateBuffer = ImmediateBuffer;
            WriteInt32(value, ref immediateBuffer);
        }

        /// <summary>
        /// This supports the Protocol Buffers infrastructure and is not meant to be used directly from your code.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SecurityCritical]
        public void WriteWrappedInt32(int? value, ref Span<byte> immediateBuffer)
        {
            if (value.Value >= 0)
            {
                WriteWrappedRawVarint32((uint)value, ref immediateBuffer);
            }
            else
            {
                // Must sign-extend.
                WriteWrappedRawVarint64((ulong)value, ref immediateBuffer);
            }
        }

        /// <summary>
        /// Writes a fixed64 field value, without a tag, to the stream.
        /// </summary>
        /// <param name="value">The value to write</param>
        [SecuritySafeCritical]
        public void WriteFixed64(ulong value)
        {
            var immediateBuffer = ImmediateBuffer;
            WriteFixed64(value, ref immediateBuffer);
        }

        /// <summary>
        /// This supports the Protocol Buffers infrastructure and is not meant to be used directly from your code.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SecurityCritical]
        public void WriteFixed64(ulong value, ref Span<byte> immediateBuffer) => WriteRawLittleEndian64(value, ref immediateBuffer);

        /// <summary>
        /// Writes a fixed32 field value, without a tag, to the stream.
        /// </summary>
        /// <param name="value">The value to write</param>
        [SecuritySafeCritical]
        public void WriteFixed32(uint value)
        {
            var immediateBudder = ImmediateBuffer;
            WriteFixed32(value, ref immediateBudder);
        }

        /// <summary>
        /// This supports the Protocol Buffers infrastructure and is not meant to be used directly from your code.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SecurityCritical]
        public void WriteFixed32(uint value, ref Span<byte> immediateBuffer) => WriteRawLittleEndian32(value, ref immediateBuffer);

        /// <summary>
        /// Writes a bool field value, without a tag, to the stream.
        /// </summary>
        /// <param name="value">The value to write</param>
        [SecuritySafeCritical]
        public void WriteBool(bool value)
        {
            var immediateBudder = ImmediateBuffer;
            WriteBool(value, ref immediateBudder);
        }

        /// <summary>
        /// This supports the Protocol Buffers infrastructure and is not meant to be used directly from your code.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SecurityCritical]
        public void WriteBool(bool value, ref Span<byte> immediateBuffer) => WriteRawByte(value ? (byte)1 : (byte)0, ref immediateBuffer);

        /// <summary>
        /// This supports the Protocol Buffers infrastructure and is not meant to be used directly from your code.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SecurityCritical]
        public void WriteWrappedBool(bool? value, ref Span<byte> immediateBuffer)
        {
            if (value.Value)
            {
                WriteRawByte(2, ref immediateBuffer);
                WriteRawByte(WellKnownTypes.WrappersReflection.WrapperValueVarintTagByte, ref immediateBuffer);
                WriteRawByte(1, ref immediateBuffer);
            }
            else
            {
                WriteRawByte(0, ref immediateBuffer);
            }
        }

        /// <summary>
        /// Writes a string field value, without a tag, to the stream.
        /// The data is length-prefixed.
        /// </summary>
        /// <param name="value">The value to write</param>
        [SecuritySafeCritical]
        public void WriteString(string value)
        {
            var immediateBudder = ImmediateBuffer;
            WriteString(value, ref immediateBudder);
        }

        /// <summary>
        /// This supports the Protocol Buffers infrastructure and is not meant to be used directly from your code.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SecurityCritical]
        public void WriteString(string value, ref Span<byte> immediateBuffer)
        {
            if (value.Length == 0)
            {
                WriteRawByte(0, ref immediateBuffer);
            }
            else
            {
                // Optimise the case where we have enough space to write
                // the string directly to the buffer, which should be common.
                int length = Utf8Encoding.GetByteCount(value);
                WriteLength(length, ref immediateBuffer);
                if (limit - position >= length)
                {
                    if (length == value.Length) // Must be all ASCII...
                    {
                        for (int i = 0; i < length; i++)
                        {
                            immediateBuffer[position + i] = (byte)value[i];
                        }
                    }
                    else
                    {
#if NETCOREAPP2_1
                        Utf8Encoding.GetBytes(value, immediateBuffer);
#else
                    if (buffer != null)
                    {
                        Utf8Encoding.GetBytes(value, 0, value.Length, buffer, position);
                    }
                    else
                    {
                        var temp = ArrayPool<byte>.Shared.Rent(length);
                        try
                        {
                            Utf8Encoding.GetBytes(value, 0, value.Length, temp, position);
                            temp.AsSpan(0, length).CopyTo(immediateBuffer);
                        }
                        finally
                        {
                            ArrayPool<byte>.Shared.Return(temp);
                        }
                    }
#endif
                    }
                    position += length;
                }
                else
                {
                    byte[] bytes = Utf8Encoding.GetBytes(value);
                    WriteRawBytes(bytes, ref immediateBuffer);
                }
            }
        }

        /// <summary>
        /// This supports the Protocol Buffers infrastructure and is not meant to be used directly from your code.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SecurityCritical]
        public void WriteWrappedString(string value, ref Span<byte> immediateBuffer)
        {
            if (value.Length == 0)
            {
                WriteRawByte(0, ref immediateBuffer);
            }
            else
            {
                var length = ComputeStringSize(value) + 1;
                WriteLength(length, ref immediateBuffer);
                WriteRawByte(WellKnownTypes.WrappersReflection.WrapperValueLengthDelimitedTagByte, ref immediateBuffer);
                WriteString(value, ref immediateBuffer);
            }
        }

        /// <summary>
        /// Writes a message, without a tag, to the stream.
        /// The data is length-prefixed.
        /// </summary>
        /// <param name="value">The value to write</param>
        [SecuritySafeCritical]
        public void WriteMessage(IMessage value)
        {
            var immediateBudder = ImmediateBuffer;
            WriteMessage(value, ref immediateBudder);
        }

        /// <summary>
        /// This supports the Protocol Buffers infrastructure and is not meant to be used directly from your code.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SecurityCritical]
        public void WriteMessage(IMessage value, ref Span<byte> immediateBuffer)
        {
            WriteLength(value.CalculateSize(), ref immediateBuffer);
            value.WriteTo(this, ref immediateBuffer);
        }

        /// <summary>
        /// Write a byte string, without a tag, to the stream.
        /// The data is length-prefixed.
        /// </summary>
        /// <param name="value">The value to write</param>
        [SecuritySafeCritical]
        public void WriteBytes(ByteString value)
        {
            var immediateBudder = ImmediateBuffer;
            WriteBytes(value, ref immediateBudder);
        }

        /// <summary>
        /// This supports the Protocol Buffers infrastructure and is not meant to be used directly from your code.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SecurityCritical]
        public void WriteBytes(ByteString value, ref Span<byte> immediateBuffer)
        {
            WriteLength(value.Length, ref immediateBuffer);
            value.WriteRawBytesTo(this, ref immediateBuffer);
        }

        /// <summary>
        /// This supports the Protocol Buffers infrastructure and is not meant to be used directly from your code.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SecurityCritical]
        public void WriteWrappedBytes(ByteString value, ref Span<byte> immediateBuffer)
        {
            if (value.Length == 0)
            {
                WriteRawByte(0, ref immediateBuffer);
            }
            else
            {
                var length = ComputeBytesSize(value) + 1;
                WriteLength(length, ref immediateBuffer);
                WriteRawByte(WellKnownTypes.WrappersReflection.WrapperValueLengthDelimitedTagByte, ref immediateBuffer);
                WriteBytes(value, ref immediateBuffer);
            }
        }

        /// <summary>
        /// Writes a uint32 value, without a tag, to the stream.
        /// </summary>
        /// <param name="value">The value to write</param>
        [SecuritySafeCritical]
        public void WriteUInt32(uint value)
        {
            var immediateBudder = ImmediateBuffer;
            WriteUInt32(value, ref immediateBudder);
        }

        /// <summary>
        /// This supports the Protocol Buffers infrastructure and is not meant to be used directly from your code.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SecurityCritical]
        public void WriteUInt32(uint value, ref Span<byte> immediateBuffer) => WriteRawVarint32(value, ref immediateBuffer);

        /// <summary>
        /// This supports the Protocol Buffers infrastructure and is not meant to be used directly from your code.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SecurityCritical]
        public void WriteWrappedUInt32(uint? value, ref Span<byte> immediateBuffer) => WriteWrappedRawVarint32(value.Value, ref immediateBuffer);

        /// <summary>
        /// Writes an enum value, without a tag, to the stream.
        /// </summary>
        /// <param name="value">The value to write</param>
        [SecuritySafeCritical]
        public void WriteEnum(int value)
        {
            var immediateBudder = ImmediateBuffer;
            WriteEnum(value, ref immediateBudder);
        }

        /// <summary>
        /// This supports the Protocol Buffers infrastructure and is not meant to be used directly from your code.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SecurityCritical]
        public void WriteEnum(int value, ref Span<byte> immediateBuffer) => WriteInt32(value, ref immediateBuffer);

        /// <summary>
        /// Writes an sfixed32 value, without a tag, to the stream.
        /// </summary>
        /// <param name="value">The value to write.</param>
        [SecuritySafeCritical]
        public void WriteSFixed32(int value)
        {
            var immediateBudder = ImmediateBuffer;
            WriteSFixed32(value, ref immediateBudder);
        }

        /// <summary>
        /// This supports the Protocol Buffers infrastructure and is not meant to be used directly from your code.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SecurityCritical]
        public void WriteSFixed32(int value, ref Span<byte> immediateBuffer) => WriteRawLittleEndian32((uint)value, ref immediateBuffer);

        /// <summary>
        /// Writes an sfixed64 value, without a tag, to the stream.
        /// </summary>
        /// <param name="value">The value to write</param>
        [SecuritySafeCritical]
        public void WriteSFixed64(long value)
        {
            var immediateBudder = ImmediateBuffer;
            WriteSFixed64(value, ref immediateBudder);
        }

        /// <summary>
        /// This supports the Protocol Buffers infrastructure and is not meant to be used directly from your code.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SecurityCritical]
        public void WriteSFixed64(long value, ref Span<byte> immediateBuffer) => WriteRawLittleEndian64((ulong)value, ref immediateBuffer);

        /// <summary>
        /// Writes an sint32 value, without a tag, to the stream.
        /// </summary>
        /// <param name="value">The value to write</param>
        [SecuritySafeCritical]
        public void WriteSInt32(int value)
        {
            var immediateBudder = ImmediateBuffer;
            WriteSInt32(value, ref immediateBudder);
        }

        /// <summary>
        /// This supports the Protocol Buffers infrastructure and is not meant to be used directly from your code.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SecurityCritical]
        public void WriteSInt32(int value, ref Span<byte> immediateBuffere) => WriteRawVarint32(EncodeZigZag32(value), ref immediateBuffere);

        /// <summary>
        /// Writes an sint64 value, without a tag, to the stream.
        /// </summary>
        /// <param name="value">The value to write</param>
        [SecuritySafeCritical]
        public void WriteSInt64(long value)
        {
            var immediateBudder = ImmediateBuffer;
            WriteSInt64(value, ref immediateBudder);
        }

        /// <summary>
        /// This supports the Protocol Buffers infrastructure and is not meant to be used directly from your code.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SecurityCritical]
        public void WriteSInt64(long value, ref Span<byte> immediateBuffer) => WriteRawVarint64(EncodeZigZag64(value), ref immediateBuffer);

        /// <summary>
        /// Writes a length (in bytes) for length-delimited data.
        /// </summary>
        /// <remarks>
        /// This method simply writes a rawint, but exists for clarity in calling code.
        /// </remarks>
        /// <param name="length">Length value, in bytes.</param>
        [SecuritySafeCritical]
        public void WriteLength(int length)
        {
            var immediateBuffer = ImmediateBuffer;
            WriteLength(length, ref immediateBuffer);
        }

        /// <summary>
        /// This supports the Protocol Buffers infrastructure and is not meant to be used directly from your code.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SecurityCritical]
        public void WriteLength(int length, ref Span<byte> immediateBuffer) => WriteRawVarint32((uint)length, ref immediateBuffer);

        #endregion

        #region Raw tag writing
        /// <summary>
        /// This supports the Protocol Buffers infrastructure and is not meant to be used directly from your code.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SecurityCritical]
        public void WriteTag(int fieldNumber, WireFormat.WireType type, ref Span<byte> immediateBuffer) => WriteRawVarint32(WireFormat.MakeTag(fieldNumber, type), ref immediateBuffer);

        /// <summary>
        /// Encodes and writes a tag.
        /// </summary>
        /// <param name="fieldNumber">The number of the field to write the tag for</param>
        /// <param name="type">The wire format type of the tag to write</param>
        [SecuritySafeCritical]
        public void WriteTag(int fieldNumber, WireFormat.WireType type)
        {
            var immediateBuffer = ImmediateBuffer;
            WriteTag(fieldNumber, type, ref immediateBuffer);
        }

        /// <summary>
        /// Writes an already-encoded tag.
        /// </summary>
        /// <param name="tag">The encoded tag</param>
        [SecuritySafeCritical]
        public void WriteTag(uint tag)
        {
            var immediateBuffer = ImmediateBuffer;
            WriteTag(tag, ref immediateBuffer);
        }

        /// <summary>
        /// This supports the Protocol Buffers infrastructure and is not meant to be used directly from your code.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SecurityCritical]
        public void WriteTag(uint tag, ref Span<byte> immediateBuffer) => WriteRawVarint32(tag, ref immediateBuffer);

        /// <summary>
        /// Writes the given single-byte tag directly to the stream.
        /// </summary>
        /// <param name="b1">The encoded tag</param>
        [SecuritySafeCritical]
        public void WriteRawTag(byte b1)
        {
            var immediateBudder = ImmediateBuffer;
            WriteRawTag(b1, ref immediateBudder);
        }

        /// <summary>
        /// This supports the Protocol Buffers infrastructure and is not meant to be used directly from your code.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SecurityCritical]
        public void WriteRawTag(byte b1, ref Span<byte> immediateBuffer) => WriteRawByte(b1, ref immediateBuffer);

        /// <summary>
        /// Writes the given two-byte tag directly to the stream.
        /// </summary>
        /// <param name="b1">The first byte of the encoded tag</param>
        /// <param name="b2">The second byte of the encoded tag</param>
        [SecuritySafeCritical]
        public void WriteRawTag(byte b1, byte b2)
        {
            var immediateBudder = ImmediateBuffer;
            WriteRawTag(b1, b2, ref immediateBudder);
        }

        /// <summary>
        /// This supports the Protocol Buffers infrastructure and is not meant to be used directly from your code.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SecurityCritical]
        public void WriteRawTag(byte b1, byte b2, ref Span<byte> immediateBuffer)
        {
            WriteRawByte(b1, ref immediateBuffer);
            WriteRawByte(b2, ref immediateBuffer);
        }

        /// <summary>
        /// Writes the given three-byte tag directly to the stream.
        /// </summary>
        /// <param name="b1">The first byte of the encoded tag</param>
        /// <param name="b2">The second byte of the encoded tag</param>
        /// <param name="b3">The third byte of the encoded tag</param>
        [SecuritySafeCritical]
        public void WriteRawTag(byte b1, byte b2, byte b3)
        {
            var immediateBudder = ImmediateBuffer;
            WriteRawTag(b1, b2, b3, ref immediateBudder);
        }

        /// <summary>
        /// This supports the Protocol Buffers infrastructure and is not meant to be used directly from your code.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SecurityCritical]
        public void WriteRawTag(byte b1, byte b2, byte b3, ref Span<byte> immediateBuffer)
        {
            WriteRawByte(b1, ref immediateBuffer);
            WriteRawByte(b2, ref immediateBuffer);
            WriteRawByte(b3, ref immediateBuffer);
        }

        /// <summary>
        /// Writes the given four-byte tag directly to the stream.
        /// </summary>
        /// <param name="b1">The first byte of the encoded tag</param>
        /// <param name="b2">The second byte of the encoded tag</param>
        /// <param name="b3">The third byte of the encoded tag</param>
        /// <param name="b4">The fourth byte of the encoded tag</param>
        [SecuritySafeCritical]
        public void WriteRawTag(byte b1, byte b2, byte b3, byte b4)
        {
            var immediateBudder = ImmediateBuffer;
            WriteRawTag(b1, b2, b3, b4, ref immediateBudder);
        }

        /// <summary>
        /// This supports the Protocol Buffers infrastructure and is not meant to be used directly from your code.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SecurityCritical]
        public void WriteRawTag(byte b1, byte b2, byte b3, byte b4, ref Span<byte> immediateBuffer)
        {
            WriteRawByte(b1, ref immediateBuffer);
            WriteRawByte(b2, ref immediateBuffer);
            WriteRawByte(b3, ref immediateBuffer);
            WriteRawByte(b4, ref immediateBuffer);
        }

        /// <summary>
        /// Writes the given five-byte tag directly to the stream.
        /// </summary>
        /// <param name="b1">The first byte of the encoded tag</param>
        /// <param name="b2">The second byte of the encoded tag</param>
        /// <param name="b3">The third byte of the encoded tag</param>
        /// <param name="b4">The fourth byte of the encoded tag</param>
        /// <param name="b5">The fifth byte of the encoded tag</param>
        [SecuritySafeCritical]
        public void WriteRawTag(byte b1, byte b2, byte b3, byte b4, byte b5)
        {
            var immediateBudder = ImmediateBuffer;
            WriteRawTag(b1, b2, b3, b4, b5, ref immediateBudder);
        }

        /// <summary>
        /// This supports the Protocol Buffers infrastructure and is not meant to be used directly from your code.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SecurityCritical]
        public void WriteRawTag(byte b1, byte b2, byte b3, byte b4, byte b5, ref Span<byte> immediateBuffer)
        {
            WriteRawByte(b1, ref immediateBuffer);
            WriteRawByte(b2, ref immediateBuffer);
            WriteRawByte(b3, ref immediateBuffer);
            WriteRawByte(b4, ref immediateBuffer);
            WriteRawByte(b5, ref immediateBuffer);
        }
        #endregion

        #region Underlying writing primitives
        /// <summary>
        /// Writes a 32 bit value as a varint. The fast route is taken when
        /// there's enough buffer space left to whizz through without checking
        /// for each byte; otherwise, we resort to calling WriteRawByte each time.
        /// </summary>
        [SecuritySafeCritical]
        internal void WriteRawVarint32(uint value)
        {
            var immediateBudder = ImmediateBuffer;
            WriteRawVarint32(value, ref immediateBudder);
        }

        /// <summary>
        /// This supports the Protocol Buffers infrastructure and is not meant to be used directly from your code.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SecurityCritical]
        internal void WriteRawVarint32(uint value, ref Span<byte> immediateBuffer)
        {
            // Optimize for the common case of a single byte value
            if (value < 128 && position < limit)
            {
                immediateBuffer[position++] = (byte)value;
                return;
            }

            while (value > 127 && position < limit)
            {
                immediateBuffer[position++] = (byte) ((value & 0x7F) | 0x80);
                value >>= 7;
            }
            while (value > 127)
            {
                WriteRawByte((byte)((value & 0x7F) | 0x80), ref immediateBuffer);
                value >>= 7;
            }
            if (position < limit)
            {
                immediateBuffer[position++] = (byte)value;
            }
            else
            {
                WriteRawByte((byte)value, ref immediateBuffer);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SecurityCritical]
        private void WriteWrappedRawVarint32(uint value, ref Span<byte> immediateBuffer)
        {
            if (value == default(uint))
            {
                WriteRawByte(0, ref immediateBuffer);
            }
            else
            {
                var length = ComputeRawVarint32Size(value) + 1;
                WriteRawByte((byte)length, ref immediateBuffer);
                WriteRawByte(WellKnownTypes.WrappersReflection.WrapperValueVarintTagByte, ref immediateBuffer);
                WriteRawVarint32(value, ref immediateBuffer);
            }
        }

        [SecurityCritical]
        internal void WriteRawVarint64(ulong value)
        {
            var immediateBuffer = ImmediateBuffer;
            WriteRawVarint64(value, ref immediateBuffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SecurityCritical]
        private void WriteRawVarint64(ulong value, ref Span<byte> immediateBuffer)
        {
            while (value > 127 && position < limit)
            {
                immediateBuffer[position++] = (byte)((value & 0x7F) | 0x80);
                value >>= 7;
            }
            while (value > 127)
            {
                WriteRawByte((byte) ((value & 0x7F) | 0x80), ref immediateBuffer);
                value >>= 7;
            }
            if (position < limit)
            {
                immediateBuffer[position++] = (byte)value;
            }
            else
            {
                WriteRawByte((byte)value, ref immediateBuffer);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SecurityCritical]
        private void WriteWrappedRawVarint64(ulong value, ref Span<byte> immediateBuffer)
        {
            if (value == default(ulong))
            {
                WriteRawByte(0, ref immediateBuffer);
            }
            else
            {
                var length = ComputeRawVarint64Size(value) + 1;
                WriteRawByte((byte)length, ref immediateBuffer);
                WriteRawByte(WellKnownTypes.WrappersReflection.WrapperValueVarintTagByte, ref immediateBuffer);
                WriteRawVarint64(value, ref immediateBuffer);
            }
        }

        [SecurityCritical]
        internal void WriteRawLittleEndian32(uint value)
        {
            var immediateBuffer = ImmediateBuffer;
            WriteRawLittleEndian32(value, ref immediateBuffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SecurityCritical]
        private void WriteRawLittleEndian32(uint value, ref Span<byte> immediateBuffer)
        {
            if (position + 4 > limit)
            {
                SlowWriteRawLittleEndian32(value, ref immediateBuffer);
            }
            else
            {
                BinaryPrimitives.WriteUInt32LittleEndian(immediateBuffer.Slice(position, 4), value);
                position += 4;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [SecurityCritical]
        private void SlowWriteRawLittleEndian32(uint value, ref Span<byte> immediateBuffer)
        {
            WriteRawByte((byte)value, ref immediateBuffer);
            WriteRawByte((byte)(value >> 8), ref immediateBuffer);
            WriteRawByte((byte)(value >> 16), ref immediateBuffer);
            WriteRawByte((byte)(value >> 24), ref immediateBuffer);
        }

        [SecurityCritical]
        internal void WriteRawLittleEndian64(ulong value)
        {
            var immediateBuffer = ImmediateBuffer;
            WriteRawLittleEndian64(value, ref immediateBuffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SecurityCritical]
        private void WriteRawLittleEndian64(ulong value, ref Span<byte> immediateBuffer)
        {
            if (position + 8 > limit)
            {
                SlowWriteRawLittleEndian64(value, ref immediateBuffer);
            }
            else
            {
                BinaryPrimitives.WriteUInt64LittleEndian(immediateBuffer.Slice(position, 8), value);
                position += 8;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [SecurityCritical]
        private void SlowWriteRawLittleEndian64(ulong value, ref Span<byte> immediateBuffer)
        {
            WriteRawByte((byte)value, ref immediateBuffer);
            WriteRawByte((byte)(value >> 8), ref immediateBuffer);
            WriteRawByte((byte)(value >> 16), ref immediateBuffer);
            WriteRawByte((byte)(value >> 24), ref immediateBuffer);
            WriteRawByte((byte)(value >> 32), ref immediateBuffer);
            WriteRawByte((byte)(value >> 40), ref immediateBuffer);
            WriteRawByte((byte)(value >> 48), ref immediateBuffer);
            WriteRawByte((byte)(value >> 56), ref immediateBuffer);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [SecurityCritical]
        internal void WriteRawByte(byte value)
        {
            var immediateBuffer = ImmediateBuffer;
            WriteRawByte(value, ref immediateBuffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SecurityCritical]
        private void WriteRawByte(byte value, ref Span<byte> immediateBuffer)
        {
            if (position == limit)
            {
                RefreshBuffer(ref immediateBuffer);
            }

            immediateBuffer[position++] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SecurityCritical]
        internal void WriteRawByte(uint value, ref Span<byte> immediateBuffer) => WriteRawByte((byte)value, ref immediateBuffer);

        /// <summary>
        /// Writes out part of an array of bytes.
        /// </summary>
        [SecurityCritical]
        internal void WriteRawBytes(ReadOnlySpan<byte> value)
        {
            var immediateBuffer = ImmediateBuffer;
            WriteRawBytes(value, ref immediateBuffer);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SecurityCritical]
        internal void WriteRawBytes(ReadOnlySpan<byte> value, ref Span<byte> immediateBuffer)
        {
            if (limit - position >= value.Length)
            {
                value.CopyTo(immediateBuffer.Slice(position));
                // We have room in the current buffer.
                position += value.Length;
            }
            else
            {
                SlowWriteRawBytes(value, ref immediateBuffer);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [SecurityCritical]
        private void SlowWriteRawBytes(ReadOnlySpan<byte> value, ref Span<byte> immediateBuffer)
        {
            int offset = 0;
            int length = value.Length;
            // Write extends past current buffer.  Fill the rest of this buffer and
            // flush.
            int bytesWritten = limit - position;
            value.Slice(0, bytesWritten).CopyTo(immediateBuffer.Slice(position));
            offset += bytesWritten;
            length -= bytesWritten;
            position = limit;
            RefreshBuffer(ref immediateBuffer);

            // Now deal with the rest.
            // Since we have an output stream, this is our buffer
            // and buffer offset == 0
            if (length <= limit)
            {
                // Fits in new buffer.
                value.Slice(offset, length).CopyTo(immediateBuffer);
                position = length;
            }
            else if (output != null)
            {
                // Write is very big.  Let's do it all at once.
#if NETCOREAPP2_1
                output.Write(value.Slice(offset, length));
#else
                var temp = ArrayPool<byte>.Shared.Rent(length);
                try
                {
                    value.Slice(offset, length).CopyTo(temp);
                    output.Write(temp, 0, length);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(temp);
                }
#endif
            }
            else
            {
                nativeOutput.Write(value.Slice(offset, length));
                nativeOutputPosition += length;
                nativeBuffer = nativeOutput.GetMemory();
                immediateBuffer = nativeBuffer.Span;
                limit = nativeBuffer.Length;
            }
        }

        #endregion

        /// <summary>
        /// Encode a 32-bit value with ZigZag encoding.
        /// </summary>
        /// <remarks>
        /// ZigZag encodes signed integers into values that can be efficiently
        /// encoded with varint.  (Otherwise, negative values must be 
        /// sign-extended to 64 bits to be varint encoded, thus always taking
        /// 10 bytes on the wire.)
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint EncodeZigZag32(int n)
        {
            // Note:  the right-shift must be arithmetic
            return (uint) ((n << 1) ^ (n >> 31));
        }

        /// <summary>
        /// Encode a 64-bit value with ZigZag encoding.
        /// </summary>
        /// <remarks>
        /// ZigZag encodes signed integers into values that can be efficiently
        /// encoded with varint.  (Otherwise, negative values must be 
        /// sign-extended to 64 bits to be varint encoded, thus always taking
        /// 10 bytes on the wire.)
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong EncodeZigZag64(long n) => (ulong) ((n << 1) ^ (n >> 63));

        [MethodImpl(MethodImplOptions.NoInlining)]
        [SecurityCritical]
        private void RefreshBuffer(ref Span<byte> immediateBuffer)
        {
            if (output != null)
            {
                // Since we have an output stream, this is our buffer
                // and buffer offset == 0
#if NETCOREAPP2_1
                output.Write(immediateBuffer.Slice(0, position));
#else
                //We always have buffer when using output
                output.Write(buffer, 0, position);
#endif
                position = 0;
            }
            else if (nativeOutput != null)
            {
                nativeOutput.Advance(position);
                nativeOutputPosition += position;
                nativeBuffer = nativeOutput.GetMemory();
                immediateBuffer = nativeBuffer.Span;
                position = 0;
                limit = nativeBuffer.Length;
            }
            else
            {
                // We're writing to a single buffer.
                throw new OutOfSpaceException();
            }
        }

        /// <summary>
        /// Indicates that a CodedOutputStream wrapping a flat byte array
        /// ran out of space.
        /// </summary>
        public sealed class OutOfSpaceException : IOException
        {
            internal OutOfSpaceException()
                : base("CodedOutputStream was writing to a flat byte array and ran out of space.")
            {
            }
        }

        /// <summary>
        /// Flushes any buffered data and optionally closes the underlying stream, if any.
        /// </summary>
        /// <remarks>
        /// <para>
        /// By default, any underlying stream is closed by this method. To configure this behaviour,
        /// use a constructor overload with a <c>leaveOpen</c> parameter. If this instance does not
        /// have an underlying stream, this method does nothing.
        /// </para>
        /// <para>
        /// For the sake of efficiency, calling this method does not prevent future write calls - but
        /// if a later write ends up writing to a stream which has been disposed, that is likely to
        /// fail. It is recommend that you not call any other methods after this.
        /// </para>
        /// </remarks>
        [SecuritySafeCritical]
        public void Dispose()
        {
            var immediateBuffer = ImmediateBuffer;
            Flush(ref immediateBuffer);
            if (!leaveOpen)
            {
                output.Dispose();
            }
        }

        /// <summary>
        /// Flushes any buffered data to the underlying stream (if there is one).
        /// </summary>
        [SecuritySafeCritical]
        public void Flush()
        {
            var immediateBuffer = ImmediateBuffer;
            Flush(ref immediateBuffer);
        }

        /// <summary>
        /// This supports the Protocol Buffers infrastructure and is not meant to be used directly from your code.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [SecurityCritical]
        public void Flush(ref Span<byte> immediateBuffer)
        {
            if (output != null)
            {
                RefreshBuffer(ref immediateBuffer);
            }
        }

        /// <summary>
        /// Verifies that SpaceLeft returns zero. It's common to create a byte array
        /// that is exactly big enough to hold a message, then write to it with
        /// a CodedOutputStream. Calling CheckNoSpaceLeft after writing verifies that
        /// the message was actually as big as expected, which can help bugs.
        /// </summary>
        public void CheckNoSpaceLeft()
        {
            if (SpaceLeft != 0)
            {
                throw new InvalidOperationException("Did not write as much data as expected.");
            }
        }

        /// <summary>
        /// If writing to a flat array, returns the space left in the array. Otherwise,
        /// throws an InvalidOperationException.
        /// </summary>
        public int SpaceLeft
        {
            [SecuritySafeCritical]
            get
            {
                if (output == null && nativeOutput == null)
                {
                    return limit - position;
                }
                else
                {
                    throw new InvalidOperationException(
                        "SpaceLeft can only be called on CodedOutputStreams that are " +
                        "writing to a flat array.");
                }
            }
        }
    }
}
