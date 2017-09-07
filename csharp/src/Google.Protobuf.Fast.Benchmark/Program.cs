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

            var inputStream = new CodedInputStream(buff);

            var allocator = new SingleThreadedTrivialArenaAllocator(100000);
            addressBook.MergeFrom(inputStream, allocator);
            ref var p = ref addressBook.People[5];
            var a = p.Phones[1].Number;
        }
    }
}
