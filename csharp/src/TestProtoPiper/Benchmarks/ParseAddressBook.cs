using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Columns;
using BenchmarkDotNet.Attributes.Exporters;
using BenchmarkDotNet.Attributes.Jobs;
using Google.Protobuf;
using Google.Protobuf.Examples.AddressBook;
using System;
using System.Buffers;

namespace TestProtoPiper
{
    [CoreJob()]
    [RPlotExporter, RankColumn]
    public class ParseAddressBook
    {
        [GlobalSetup]
        public void Setup()
        {
            var ab = new AddressBook
            {
                People =
                {
                    new Person
                    {
                        Id = 1,
                        Email = "asdadas@asdadas.com",
                        //LastUpdated = Timestamp.FromDateTime(new DateTime(2016, 1, 1, 8, 0, 3, DateTimeKind.Utc)),
                        Name = "ASdasdsad sda sdasd SSADSA",
                    }
                }
            };
            testData = ab.ToByteArray();
        }

        private byte[] testData;


        [Benchmark]
        public void ParseUsingClassic()
        {
            AddressBook.Parser.ParseFrom(testData);
        }


        [Benchmark]
        public void ParseUsingCodedInputParser()
        {
            var buffer = new ReadOnlySequence<byte>(testData);

            var cpy = new AddressBook();
            cpy.MergeFrom(ref buffer);
        }

        [Benchmark]
        public void ParseUsingCodedInputSpanParser()
        {
            var buffer = new ReadOnlySpan<byte>(testData);

            var cpy = new AddressBook();
            cpy.MergeFrom(ref buffer);
        }

        [Benchmark]
        public void ParseUsingCodedInputExpParser()
        {
            var buffer = new ReadOnlySequenceState<byte>(new ReadOnlySequence<byte>(testData));

            var cpy = new AddressBook();
            cpy.MergeFrom(ref buffer);
        }

        [Benchmark]
        public void ParseCodedInputVirtCallParser()
        {
            var buffer = new ReadOnlySpan<byte>(testData);

            var cpy = CodedInputSpanParser.ReadMessage(ref buffer, AddressBookType.Instance);
        }

        [Benchmark]
        public void ParseCodedInputRefVirtCallParser()
        {
            var buffer = new ReadOnlySpan<byte>(testData);

            var cpy = RefAddressBookType.Parser.ReadMessage(ref buffer);
        }
    }
}
