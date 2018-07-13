﻿using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Columns;
using BenchmarkDotNet.Attributes.Exporters;
using BenchmarkDotNet.Attributes.Jobs;
using Google.Protobuf;
using Google.Protobuf.Examples.AddressBook;
using Google.Protobuf.Pipelines;
using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Linq;
using System.Threading.Tasks;

namespace TestProtoPiper
{
    [CoreJob,ClrJob]
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
                    Enumerable.Range(1, 100).Select(x => new Person
                    {
                        Id = 1,
                        Email = "asdadas@asdadas.com",
                        //LastUpdated = Timestamp.FromDateTime(new DateTime(2016, 1, 1, 8, 0, 3, DateTimeKind.Utc)),
                        Name = "ASdasdsad sda sdasd SSADSA",
                    })
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

        //[Benchmark]
        //public async Task ParseUsingCodedInputReader()
        //{
        //    var pipe = new Pipe();
        //    pipe.Writer.WriteAsync(testData).GetAwaiter().GetResult();
        //    pipe.Writer.Complete();
        //    var reader = new CodedInputReader(pipe.Reader);

        //    var cpy = new AddressBook();
        //    await cpy.MergeFromAsync(reader);
        //}

        ////[Benchmark]
        ////public void ParseCodedInputSpanPosVirtCallParser()
        ////{
        ////    var buffer = new ReadOnlySpan<byte>(testData);
        ////    var pos = 0;
        ////    var cpy = CodedInputSpanPosParser.ReadMessage(in buffer, ref pos, AddressBookType.Instance);
        ////}

        //[Benchmark]
        //public void ParseUsingCodedInputParser()
        //{
        //    var buffer = new ReadOnlySequence<byte>(testData);

        //    var cpy = new AddressBook();
        //    cpy.MergeFrom(ref buffer);
        //}

        //[Benchmark]
        //public void ParseUsingCodedInputSpanParser()
        //{
        //    var buffer = new ReadOnlySpan<byte>(testData);

        //    var cpy = new AddressBook();
        //    cpy.MergeFrom(ref buffer);
        //}

        //[Benchmark]
        //public void ParseUsingCodedInputSpanPosParser()
        //{
        //    var buffer = new ReadOnlySpan<byte>(testData);

        //    var cpy = new AddressBook();
        //    int pos = 0;
        //    cpy.MergeFrom(buffer, ref pos);
        //}

        //[Benchmark]
        //public void ParseUsingCodedInputSpanPosParserReadOnlyMemory()
        //{
        //    var buffer = new ReadOnlyMemory<byte>(testData);

        //    var cpy = new AddressBook();
        //    int pos = 0;
        //    cpy.MergeFrom(buffer, ref pos);
        //}

        ////[Benchmark]
        ////public void ParseCodedInputSpanVirtCallParser()
        ////{
        ////    var buffer = new ReadOnlySpan<byte>(testData);

        ////    var cpy = CodedInputSpanParser.ReadMessage(ref buffer, AddressBookType.Instance);
        ////}

        ////[Benchmark]
        ////public void ParseCodedInputSmartVirtCallParser()
        ////{
        ////    var buffer = new ReadOnlySequence<byte>(testData);

        ////    var cpy = CodedInputParser.ReadMessage(buffer, AddressBookType.Instance);
        ////}

        ////[Benchmark]
        ////public void ParseCodedInputRefVirtCallParser()
        ////{
        ////    var buffer = new ReadOnlySpan<byte>(testData);

        ////    var cpy = RefAddressBookType.Parser.ReadMessage(ref buffer);
        ////}
    }
}