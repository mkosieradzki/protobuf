using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.Examples.AddressBook;
using Google.Protobuf.Pipelines;
using Google.Protobuf.WellKnownTypes;

namespace TestProtoPiper
{
    class Program
    {
        async static Task Main(string[] args)
        {
            await Test1();
            //var summary = BenchmarkRunner.Run<ParseVarInt>();
        }

        static async Task Test1()
        {
            var pipe = new Pipe(new PipeOptions(minimumSegmentSize: 16));
            var reader = new CodedInputReader(pipe.Reader);

            var ab = new AddressBook
            {
                People =
                {
                    new Person
                    {
                        Id = 1,
                        Email = "asdadas@asdadas.com",
                        LastUpdated = Timestamp.FromDateTime(new DateTime(2016, 1, 1, 8, 0, 3, DateTimeKind.Utc)),
                        Name = "ASdasdsad sda sdasd SSADSA",
                    }
                }
            };
            var data = ab.ToByteArray();

            AddressBook.Parser.ParseFrom(data);
            await pipe.Writer.WriteAsync(data);
            pipe.Writer.Complete();

            var addressBook = await reader.ReadMessageAsync(AddressBookType.Instance);
        }

    }

    //TODO: Move to a separate library when .NET Standard 2.1 is released
    static class CompatUtils
    {
        public static string DecodeUtf8String(ReadOnlySequence<byte> sequence)
        {
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

        public object CompleteMessage(object message) => message;
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
                    return new FieldInfo(Google.Protobuf.ValueType.String);
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
            }
        }

        public object CompleteMessage(object message) => message;
    }
}
