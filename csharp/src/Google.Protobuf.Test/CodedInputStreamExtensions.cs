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

using System;
using Google.Protobuf.Collections;
using Google.Protobuf.Reflection;
using NUnit.Framework;

namespace Google.Protobuf
{
    internal static class CodedInputStreamExtensions
    {
        public static void AssertNextTag(this CodedInputStream input, uint expectedTag)
        {
            var immediateBuffer = input.ImmediateBuffer;
            uint tag = input.ReadTag(ref immediateBuffer);
            Assert.AreEqual(expectedTag, tag);
        }

        public static T ReadMessage<T>(this CodedInputStream stream, MessageParser<T> parser)
            where T : IMessage<T>
        {
            var immediateBuffer = stream.ImmediateBuffer;
            var message = parser.CreateTemplate();
            stream.ReadMessage(message, ref immediateBuffer);
            return message;
        }

        public static uint ReadRawVarint32(this CodedInputStream stream)
        {
            var immediateBuffer = stream.ImmediateBuffer;
            return stream.ReadRawVarint32(ref immediateBuffer);
        }

        public static ulong ReadRawVarint64(this CodedInputStream stream)
        {
            var immediateBuffer = stream.ImmediateBuffer;
            return stream.ReadRawVarint64(ref immediateBuffer);
        }

        public static uint ReadRawLittleEndian32(this CodedInputStream stream)
        {
            var immediateBuffer = stream.ImmediateBuffer;
            return stream.ReadRawLittleEndian32(ref immediateBuffer);
        }

        public static ulong ReadRawLittleEndian64(this CodedInputStream stream)
        {
            var immediateBuffer = stream.ImmediateBuffer;
            return stream.ReadRawLittleEndian64(ref immediateBuffer);
        }

        public static uint ReadTag(this CodedInputStream stream)
        {
            var immediateBuffer = stream.ImmediateBuffer;
            return stream.ReadTag(ref immediateBuffer);
        }

        public static string ReadString(this CodedInputStream stream)
        {
            var immediateBuffer = stream.ImmediateBuffer;
            return stream.ReadString(ref immediateBuffer);
        }

        public static ByteString ReadBytes(this CodedInputStream stream)
        {
            var immediateBuffer = stream.ImmediateBuffer;
            return stream.ReadBytes(ref immediateBuffer);
        }

        public static int ReadEnum(this CodedInputStream stream)
        {
            var immediateBuffer = stream.ImmediateBuffer;
            return stream.ReadEnum(ref immediateBuffer);
        }

        public static int ReadInt32(this CodedInputStream stream)
        {
            var immediateBuffer = stream.ImmediateBuffer;
            return stream.ReadInt32(ref immediateBuffer);
        }

        public static int ReadLength(this CodedInputStream stream)
        {
            var immediateBuffer = stream.ImmediateBuffer;
            return stream.ReadLength(ref immediateBuffer);
        }

        public static int ReadSFixed32(this CodedInputStream stream)
        {
            var immediateBuffer = stream.ImmediateBuffer;
            return stream.ReadSFixed32(ref immediateBuffer);
        }

        public static void SkipLastField(this CodedInputStream stream)
        {
            var immediateBuffer = stream.ImmediateBuffer;
            stream.SkipLastField(ref immediateBuffer);
        }

        public static bool IsAtEnd(this CodedInputStream stream)
        {
            var immediateBuffer = stream.ImmediateBuffer;
            return stream.IsAtEnd(ref immediateBuffer);
        }

        public static void AddEntriesFrom<T>(this RepeatedField<T> field, CodedInputStream input, FieldCodec<T> codec)
        {
            var immediateBuffer = input.ImmediateBuffer;
            field.AddEntriesFrom(input, codec, ref immediateBuffer);
        }

        public static CustomOptions ReadOrSkipUnknownField(this CustomOptions customOptions, CodedInputStream input)
        {
            var immediateBuffer = input.ImmediateBuffer;
            return customOptions.ReadOrSkipUnknownField(input, ref immediateBuffer);
        }
    }
}
