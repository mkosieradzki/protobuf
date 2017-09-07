using System;
using System.Diagnostics;
using System.IO;
using Google.Protobuf;
using Google.Protobuf.Examples.AddressBook;

namespace Google.ProtobufBenchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            var buff = File.ReadAllBytes(@"C:\protobench\addressbook1.bin");
            var addressBook = new AddressBook();

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 10000; i++)
            {
                using (var inputStream = new CodedInputStream(buff))
                {
                    addressBook = new AddressBook();
                    addressBook.MergeFrom(inputStream);
                }
            }

            sw.Stop();

            Console.WriteLine($"Ellapsed: {sw.ElapsedMilliseconds}ms");
            Console.ReadLine();
        }
    }
}
