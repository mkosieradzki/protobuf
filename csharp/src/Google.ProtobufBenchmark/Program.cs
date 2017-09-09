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
            Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds}ms GC0={GC.CollectionCount(0)} GC1={GC.CollectionCount(1)} GC2={GC.CollectionCount(2)} PeakMem={Process.GetCurrentProcess().PeakWorkingSet64}");
            Console.ReadLine();
        }

        static void CreateTestFile()
        {
            var addressBook = new AddressBook();
            for (int i = 0; i < 1000; i++)
            {
                addressBook.People.Add(new Person
                {
                    Id = i,
                    Email = $"person{i}@contoso.com",
                    Name = $"Person Named {i}",
                    Phones =
                    {
                        new Person.Types.PhoneNumber
                        {
                            Type = Person.Types.PhoneType.Home,
                            Number = $"1234567{i}"
                        },
                        new Person.Types.PhoneNumber
                        {
                            Type = Person.Types.PhoneType.Mobile,
                            Number = $"001234567{i}"
                        },
                        new Person.Types.PhoneNumber
                        {
                            Type = Person.Types.PhoneType.Work,
                            Number = $"234567{i}"
                        }
                    }
                });
            }

            using (var stream = File.Create(@"C:\protobench\addressbook1.bin"))
            using (var codedOutputStream = new Protobuf.CodedOutputStream(stream))
                addressBook.WriteTo(codedOutputStream);
        }
    }
}
