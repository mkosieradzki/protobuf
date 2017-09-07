using System;
using System.IO;
using Google.Protobuf.Examples.AddressBook;

namespace Google.ProtobufBenchmark
{
    class Program
    {
        static void Main(string[] args)
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
            {
                addressBook.WriteTo(codedOutputStream);
                //codedOutputStream
                //codedOutputStream.WriteTo();
            }
        }
    }
}
