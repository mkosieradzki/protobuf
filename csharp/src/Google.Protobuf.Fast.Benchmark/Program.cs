using System;
using System.Diagnostics;
using System.IO;
using Google.Protobuf.Examples.Fast.AddressBook;

namespace Google.Protobuf.Fast.Benchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            var buff = File.ReadAllBytes(@"C:\protobench\addressbook1.bin");
            var addressBook = new AddressBook();

            var arena = new SingleThreadedTrivialArena(new Memory<byte>(new byte[250000]));
            //addressBook.MergeFrom(inputStream, allocator);

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 10000; i++)
            {
                var inputStream = new CodedInputStream(buff);
                try
                {
                    addressBook = new AddressBook();
                    addressBook.MergeFrom(inputStream, arena);

                    //var test1 = addressBook.People.GetItemAt(arena, 100);
                    //var test2 = test1.Id;
                    //var test3 = test1.Phones.GetItemAt(arena, 1);
                    //var test4 = test3.Type;
                    //var test5 = test3.Number.AsString(arena);
                }
                finally
                {
                    inputStream.Dispose();
                }
                arena.Clear();
            }

            sw.Stop();
            Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds}ms GC0={GC.CollectionCount(0)} GC1={GC.CollectionCount(1)} GC2={GC.CollectionCount(2)} PeakMem={Process.GetCurrentProcess().PeakWorkingSet64}");
            Console.ReadLine();
        }
    }
}
