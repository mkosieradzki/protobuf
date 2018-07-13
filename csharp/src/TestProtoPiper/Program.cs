﻿using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.Examples.AddressBook;
using Google.Protobuf.Pipelines;

namespace TestProtoPiper
{
    class Program
    {
        async static Task Main(string[] args)
        {
            //await Test1();
            //var summary = BenchmarkRunner.Run<ParseVarInt>();
            BenchmarkDotNet.Running.BenchmarkRunner.Run<ParseAddressBook>();
            //await ToProfile();
        }

        static async Task ToProfile()
        {
            var ab = new AddressBook
            {
                People =
                {
                    Enumerable.Range(1, 100).Select(x => new Person
                    {
                        Id = 1,
                        Email = "asdadas@asdadas.com",
                        //LastUpdated = Timestamp.FromDateTime(new DateTime(2016, 1, 1, 8, 0, 3, DateTimeKind.Utc)),
                        Name = "ASdasdsad sda sdasd SSADSA",
                    })
                }
            };
            var testData = ab.ToByteArray();

            var pipe = new Pipe();
            await pipe.Writer.WriteAsync(testData);
            pipe.Writer.Complete();

            var reader = new CodedInputReader(pipe.Reader);

            var cpy = new AddressBook();
            await cpy.MergeFromAsync(reader);

            //var buffer = new ReadOnlySequence<byte>(testData);

            //var pos = buffer.Start;

            //var cpy = CodedInputSeqParser.ReadMessage(buffer, ref pos, AddressBookType.Instance);

            //for (int i = 0; i < 1000000; i++)
            //{
            //    var buffer = new ReadOnlySequenceState<byte>(new ReadOnlySequence<byte>(testData));

            //    var cpy = new AddressBook();
            //    cpy.MergeFrom(ref buffer);

            //    //CodedInputParser.ReadMessage(ref buffer, AddressBookType.Instance);
            //}
        }

        static void Test2()
        {
            Span<object> a = new Span<object>();
            List<object> b = new List<object>();
        }

        //static async Task Test1()
        //{
        //    var pipe = new Pipe(new PipeOptions(minimumSegmentSize: 16));
        //    var reader = new CodedInputReader(pipe.Reader);

        //    var ab = new AddressBook
        //    {
        //        People =
        //        {
        //            new Person
        //            {
        //                Id = 1,
        //                Email = "asdadas@asdadas.com",
        //                //LastUpdated = Timestamp.FromDateTime(new DateTime(2016, 1, 1, 8, 0, 3, DateTimeKind.Utc)),
        //                Name = "ASdasdsad sda sdasd SSADSA",
        //            }
        //        }
        //    };
        //    var data = ab.ToByteArray();

        //    AddressBook.Parser.ParseFrom(data);
        //    await pipe.Writer.WriteAsync(data);
        //    pipe.Writer.Complete();

        //    var addressBook = await reader.ReadMessageAsync(AddressBookType.Instance);
        //}

    }

    //TODO: Move to a separate library when .NET Standard 2.1 is released
    static class CompatUtils
    {
        public static string DecodeUtf8String(ReadOnlySequence<byte> sequence)
        {
#if NETCOREAPP2_1
            if (sequence.IsEmpty)
            {
                return String.Empty;
            }
            else if (sequence.IsSingleSegment)
            {
                return Encoding.UTF8.GetString(sequence.First.Span);
            }
            else if (sequence.Length < 128)
            {
                Span<byte> span = stackalloc byte[(int)sequence.Length];
                sequence.CopyTo(span);
                return Encoding.UTF8.GetString(span);
            }
            else
            {
                throw new NotImplementedException();
            }
#else
            throw new NotImplementedException();
#endif
        }

        public static string DecodeUtf8String(ReadOnlySpan<byte> span)
        {
#if NETCOREAPP2_1
            if (span.IsEmpty)
            {
                return String.Empty;
            }
            else
            {
                return Encoding.UTF8.GetString(span);
            }
#else
            throw new NotImplementedException();
#endif
        }

        public static FieldInfo GetUnknownFieldInfo(uint tag)
        {
            switch (WireFormat.GetTagWireType(tag))
            {
                //throw new InvalidProtocolBufferException("Merge an unknown field of end-group tag, indicating that the corresponding start-group was missing.");
                case WireFormat.WireType.Fixed32:
                    return new FieldInfo(Google.Protobuf.ValueType.Fixed32);
                case WireFormat.WireType.Fixed64:
                    return new FieldInfo(Google.Protobuf.ValueType.Fixed64);
                case WireFormat.WireType.LengthDelimited:
                    return new FieldInfo(Google.Protobuf.ValueType.Bytes);
                case WireFormat.WireType.Varint:
                    return new FieldInfo(Google.Protobuf.ValueType.Int64);
                default:
                    return default;
            }
        }

        public static RefFieldInfo GetUnknownRefFieldInfo(in uint tag)
        {
            switch (WireFormat.GetTagWireType(tag))
            {
                //throw new InvalidProtocolBufferException("Merge an unknown field of end-group tag, indicating that the corresponding start-group was missing.");
                case WireFormat.WireType.Fixed32:
                    return new RefFieldInfo(Google.Protobuf.ValueType.Fixed32);
                case WireFormat.WireType.Fixed64:
                    return new RefFieldInfo(Google.Protobuf.ValueType.Fixed64);
                case WireFormat.WireType.LengthDelimited:
                    return new RefFieldInfo(Google.Protobuf.ValueType.Bytes);
                case WireFormat.WireType.Varint:
                    return new RefFieldInfo(Google.Protobuf.ValueType.Int64);
                default:
                    return default;
            }
        }
    }

    sealed class ObjPool<T>
    {
        private T[] arr;

        public ObjPool()
        {
            arr = new T[10];
        }

        public ref T Alloc()
        {
            return ref arr[0];
        }
    }

    sealed class AddressBookType : IMessageType
    {
        public static AddressBookType Instance { get; } = new AddressBookType();

        public object CreateMessage() => new AddressBook();

        public FieldInfo GetFieldInfo(uint tag)
        {
            switch (tag)
            {
                default:
                    return default;
                case 10:
                    return new FieldInfo(PersonType.Instance);
            }
        }

        public void ConsumeField(object message, uint tag, object value)
        {
            var obj = (AddressBook)message;
            switch (tag)
            {
                case 10:
                    obj.People.Add((Person)value);
                    break;
            }
        }

        public void ConsumeSpanField(object message, uint tag, ReadOnlySpan<byte> value)
        { }

        public object CompleteMessage(object message) => message;
    }

    sealed class RefAddressBookType : IRefMessageType<AddressBook>
    {
        public static RefMessageParser<AddressBook> Parser { get; } = new RefMessageParser<AddressBook>(new RefAddressBookType());

        public ref AddressBook CreateMessage() => ref (new AddressBook[] { new AddressBook() })[0];

        public RefFieldInfo GetFieldInfo(in uint tag)
        {
            switch (tag)
            {
                default:
                    return default;
                case 10:
                    return new RefFieldInfo(RefPersonType.Parser);
            }
        }

        public void ConsumeField(ref AddressBook message, in uint tag, in object value)
        {
            switch (tag)
            {
                case 10:
                    message.People.Add((Person)value);
                    break;
            }
        }

        public void CompleteMessage(ref AddressBook message) { }
    }

    sealed class PersonType : IMessageType
    {
        public static PersonType Instance { get; } = new PersonType();

        public object CreateMessage() => new Person();

        public FieldInfo GetFieldInfo(uint tag)
        {
            switch (tag)
            {
                default:
                    return CompatUtils.GetUnknownFieldInfo(tag);
                    //NOTE: If you want to handle unknown fields return compatible info - otherwise default
                    //return default;
                case 10:
                case 26:
                    return new FieldInfo(Google.Protobuf.ValueType.String);
                case 16:
                    return new FieldInfo(Google.Protobuf.ValueType.Int32);
            }
        }

        public void ConsumeField(object message, uint tag, object value)
        {
            var obj = (Person)message;
            switch (tag)
            {
                case 10:
                    obj.Name = CompatUtils.DecodeUtf8String((ReadOnlySequence<byte>)value);
                    break;
                case 16:
                    obj.Id = (int)value;
                    break;
                case 26:
                    obj.Email = CompatUtils.DecodeUtf8String((ReadOnlySequence<byte>)value);
                    break;
            }
        }

        public void ConsumeSpanField(object message, uint tag, ReadOnlySpan<byte> value)
        {
            var obj = (Person)message;
            switch (tag)
            {
                case 10:
                    obj.Name = CompatUtils.DecodeUtf8String(value);
                    break;
                case 26:
                    obj.Email = CompatUtils.DecodeUtf8String(value);
                    break;
            }
        }

        public object CompleteMessage(object message) => message;
    }

    sealed class RefPersonType : IRefMessageType<Person>
    {
        public static RefMessageParser<Person> Parser { get; } = new RefMessageParser<Person>(new RefPersonType());

        public ref Person CreateMessage() => ref (new Person[] { new Person() }[0]);

        public RefFieldInfo GetFieldInfo(in uint tag)
        {
            switch (tag)
            {
                default:
                    return CompatUtils.GetUnknownRefFieldInfo(tag);
                //NOTE: If you want to handle unknown fields return compatible info - otherwise default
                //return default;
                case 10:
                case 26:
                    return new RefFieldInfo(Google.Protobuf.ValueType.String);
                case 16:
                    return new RefFieldInfo(Google.Protobuf.ValueType.Int32);
            }
        }

        public void ConsumeField(ref Person message, in uint tag, in object value)
        {
            switch (tag)
            {
                case 10:
                    message.Name = CompatUtils.DecodeUtf8String((ReadOnlySequence<byte>)value);
                    break;
                case 16:
                    message.Id = (int)value;
                    break;
                case 26:
                    message.Email = CompatUtils.DecodeUtf8String((ReadOnlySequence<byte>)value);
                    break;
            }
        }

        public void CompleteMessage(ref Person message) { }
    }
}