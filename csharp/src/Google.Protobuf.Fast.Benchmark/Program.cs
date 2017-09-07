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

            var allocator = new SingleThreadedTrivialArenaAllocator(250000);
            //addressBook.MergeFrom(inputStream, allocator);

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 10000; i++)
            {
                using (var inputStream = new CodedInputStream(buff))
                {
                    addressBook = new AddressBook();
                    addressBook.MergeFrom(inputStream, allocator);
                }
                allocator.Clear();
            }

            sw.Stop();
            Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds}ms GC0={GC.CollectionCount(0)} GC1={GC.CollectionCount(1)} GC2={GC.CollectionCount(2)} PeakMem={Process.GetCurrentProcess().PeakWorkingSet64}");
        }
    }
}
