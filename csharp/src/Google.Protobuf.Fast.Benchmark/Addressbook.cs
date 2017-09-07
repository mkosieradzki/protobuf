﻿// Generated by the protocol buffer compiler.  DO NOT EDIT!
// source: addressbook.proto
#pragma warning disable 1591, 0612, 3021
#region Designer generated code

using Google.Protobuf.Fast;
using pb = global::Google.Protobuf.Fast;
using pbc = global::Google.Protobuf.Fast.Collections;
//using pbr = global::Google.Protobuf.Fast.Reflection;
using scg = global::System.Collections.Generic;
namespace Google.Protobuf.Examples.Fast.AddressBook
{
    #region Messages
    /// <summary>
    /// [START messages]
    /// </summary>
    public struct Person : pb::IMessage<Person>
    {
        //         private static readonly pb::MessageParser<Person> _parser = new pb::MessageParser<Person>(() => new Person());
        //         [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        //         public static pb::MessageParser<Person> Parser { get { return _parser; } }

        //         //[global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        //         //public static pbr::MessageDescriptor Descriptor
        //         //{
        //         //    get { return global::Google.Protobuf.Examples.AddressBook.AddressbookReflection.Descriptor.MessageTypes[0]; }
        //         //}

        //         //[global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        //         //pbr::MessageDescriptor pb::IMessage.Descriptor
        //         //{
        //         //    get { return Descriptor; }
        //         //}

        //         [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        //         public Person()
        //         {
        //             OnConstruction();
        //         }

        //         partial void OnConstruction();

        //[global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        //public Person(Person other)
        //{
        //    name_ = other.name_;
        //    id_ = other.id_;
        //    email_ = other.email_;
        //    phones_ = other.phones_.Clone();
        //}

        //[global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        //public Person Clone()
        //{
        //    return new Person(this);
        //}

        /// <summary>Field number for the "name" field.</summary>
        public const int NameFieldNumber = 1;
        private pb::Utf8String name_;
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public pb::Utf8String Name
        {
            get { return name_; }
            set { name_ = value; }
        }

        /// <summary>Field number for the "id" field.</summary>
        public const int IdFieldNumber = 2;
        private int id_;
        /// <summary>
        /// Unique ID number for this person.
        /// </summary>
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public int Id
        {
            get { return id_; }
            set { id_ = value; }
        }

        /// <summary>Field number for the "email" field.</summary>
        public const int EmailFieldNumber = 3;
        private pb::Utf8String email_;
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public pb::Utf8String Email
        {
            get { return email_; }
            set { email_ = value; }
        }

        /// <summary>Field number for the "phones" field.</summary>
        public const int PhonesFieldNumber = 4;
        private static readonly pb::FieldCodec<global::Google.Protobuf.Examples.Fast.AddressBook.Person.Types.PhoneNumber> _repeated_phones_codec = pb::FieldCodec.ForMessage<global::Google.Protobuf.Examples.Fast.AddressBook.Person.Types.PhoneNumber>(34);
        //NOTE: Extremely important to not be readonly
        private pbc::RepeatedField<global::Google.Protobuf.Examples.Fast.AddressBook.Person.Types.PhoneNumber> phones_;
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public pbc::RepeatedField<global::Google.Protobuf.Examples.Fast.AddressBook.Person.Types.PhoneNumber> Phones
        {
            get { return phones_; }
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public override bool Equals(object other) => other is Person v && Equals(v);

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public bool Equals(Person other)
        {
            if (Name != other.Name) return false;
            if (Id != other.Id) return false;
            if (Email != other.Email) return false;
            if (!phones_.Equals(other.phones_)) return false;
            return true;
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public override int GetHashCode()
        {
            int hash = 1;
            if (Name.Length != 0) hash ^= Name.GetHashCode();
            if (Id != 0) hash ^= Id.GetHashCode();
            if (Email.Length != 0) hash ^= Email.GetHashCode();
            hash ^= phones_.GetHashCode();
            return hash;
        }

        //         [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        //         public override string ToString()
        //         {
        //             return pb::JsonFormatter.ToDiagnosticString(this);
        //         }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public void WriteTo(pb::CodedOutputStream output)
        {
            if (Name.Length != 0)
            {
                output.WriteRawTag(10);
                output.WriteString(Name);
            }
            if (Id != 0)
            {
                output.WriteRawTag(16);
                output.WriteInt32(Id);
            }
            if (Email.Length != 0)
            {
                output.WriteRawTag(26);
                output.WriteString(Email);
            }
            phones_.WriteTo(output, _repeated_phones_codec);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public int CalculateSize()
        {
            int size = 0;
            if (Name.Length != 0)
            {
                size += 1 + pb::CodedOutputStream.ComputeStringSize(Name);
            }
            if (Id != 0)
            {
                size += 1 + pb::CodedOutputStream.ComputeInt32Size(Id);
            }
            if (Email.Length != 0)
            {
                size += 1 + pb::CodedOutputStream.ComputeStringSize(Email);
            }
            size += phones_.CalculateSize(_repeated_phones_codec);
            return size;
        }

        //         [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        //         public void MergeFrom(Person other)
        //         {
        //             if (other == null)
        //             {
        //                 return;
        //             }
        //             if (other.Name.Length != 0)
        //             {
        //                 Name = other.Name;
        //             }
        //             if (other.Id != 0)
        //             {
        //                 Id = other.Id;
        //             }
        //             if (other.Email.Length != 0)
        //             {
        //                 Email = other.Email;
        //             }
        //             phones_.Add(other.phones_);
        //         }

        //[global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public void MergeFrom(pb::CodedInputStream input, IAllocator allocator)
        {
            uint tag;
            while ((tag = input.ReadTag()) != 0)
            {
                switch (tag)
                {
                    default:
                        input.SkipLastField();
                        break;
                    case 10:
                        {
                            Name = input.ReadString();
                            break;
                        }
                    case 16:
                        {
                            Id = input.ReadInt32();
                            break;
                        }
                    case 26:
                        {
                            Email = input.ReadString();
                            break;
                        }
                    case 34:
                        {
                            phones_.AddEntriesFrom(input, _repeated_phones_codec, allocator);
                            break;
                        }
                }
            }
        }

        #region Nested types
        /// <summary>Container for nested types declared in the Person message type.</summary>
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static class Types
        {
            public enum PhoneType
            {
                /*[pbr::OriginalName("MOBILE")] */Mobile = 0,
                /*[pbr::OriginalName("HOME")] */Home = 1,
                /*[pbr::OriginalName("WORK")] */Work = 2,
            }

            public struct PhoneNumber : pb::IMessage<PhoneNumber>
            {
                // private static readonly pb::MessageParser<PhoneNumber> _parser = new pb::MessageParser<PhoneNumber>(() => new PhoneNumber());
                // [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
                // public static pb::MessageParser<PhoneNumber> Parser { get { return _parser; } }

                // [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
                // public static pbr::MessageDescriptor Descriptor
                // {
                //     get { return global::Google.Protobuf.Examples.AddressBook.Person.Descriptor.NestedTypes[0]; }
                // }

                // [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
                // pbr::MessageDescriptor pb::IMessage.Descriptor
                // {
                //     get { return Descriptor; }
                // }

                // [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
                // public PhoneNumber()
                // {
                //     OnConstruction();
                // }

                // partial void OnConstruction();

                [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
                public PhoneNumber(PhoneNumber other) : this()
                {
                    number_ = other.number_;
                    type_ = other.type_;
                }

                // [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
                // public PhoneNumber Clone()
                // {
                //     return new PhoneNumber(this);
                // }

                /// <summary>Field number for the "number" field.</summary>
                public const int NumberFieldNumber = 1;
                private pb::Utf8String number_;
                [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
                public pb::Utf8String Number
                {
                    get { return number_; }
                    set { number_ = value; }
                }

                /// <summary>Field number for the "type" field.</summary>
                public const int TypeFieldNumber = 2;
                private global::Google.Protobuf.Examples.Fast.AddressBook.Person.Types.PhoneType type_;
                [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
                public global::Google.Protobuf.Examples.Fast.AddressBook.Person.Types.PhoneType Type
                {
                    get { return type_; }
                    set { type_ = value; }
                }

                [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
                public override bool Equals(object other) => other is PhoneNumber v && Equals(v);

                [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
                public bool Equals(PhoneNumber other)
                {
                    if (Number != other.Number) return false;
                    if (Type != other.Type) return false;
                    return true;
                }

                [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
                public override int GetHashCode()
                {
                    int hash = 1;
                    if (Number.Length != 0) hash ^= Number.GetHashCode();
                    if (Type != 0) hash ^= Type.GetHashCode();
                    return hash;
                }

                // [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
                // public override string ToString()
                // {
                //     return pb::JsonFormatter.ToDiagnosticString(this);
                // }

                [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
                public void WriteTo(pb::CodedOutputStream output)
                {
                    if (Number.Length != 0)
                    {
                        output.WriteRawTag(10);
                        output.WriteString(Number);
                    }
                    if (Type != 0)
                    {
                        output.WriteRawTag(16);
                        output.WriteEnum((int)Type);
                    }
                }

                [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
                public int CalculateSize()
                {
                    int size = 0;
                    if (Number.Length != 0)
                    {
                        size += 1 + pb::CodedOutputStream.ComputeStringSize(Number);
                    }
                    if (Type != 0)
                    {
                        size += 1 + pb::CodedOutputStream.ComputeEnumSize((int)Type);
                    }
                    return size;
                }

                // [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
                // public void MergeFrom(PhoneNumber other)
                // {
                //     if (other == null)
                //     {
                //         return;
                //     }
                //     if (other.Number.Length != 0)
                //     {
                //         Number = other.Number;
                //     }
                //     if (other.Type != 0)
                //     {
                //         Type = other.Type;
                //     }
                // }

                [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
                public void MergeFrom(pb::CodedInputStream input, IAllocator allocator)
                {
                    uint tag;
                    while ((tag = input.ReadTag()) != 0)
                    {
                        switch (tag)
                        {
                            default:
                                input.SkipLastField();
                                break;
                            case 10:
                                {
                                    Number = input.ReadString();
                                    break;
                                }
                            case 16:
                                {
                                    type_ = (global::Google.Protobuf.Examples.Fast.AddressBook.Person.Types.PhoneType)input.ReadEnum();
                                    break;
                                }
                        }
                    }
                }

            }

        }
        #endregion

    }

    /// <summary>
    /// Our address book file is just one of these.
    /// </summary>
    public sealed partial class AddressBook : pb::IMessage<AddressBook>
    {
        //         private static readonly pb::MessageParser<AddressBook> _parser = new pb::MessageParser<AddressBook>(() => new AddressBook());
        //         [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        //         public static pb::MessageParser<AddressBook> Parser { get { return _parser; } }

        //         [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        //         public static pbr::MessageDescriptor Descriptor
        //         {
        //             get { return global::Google.Protobuf.Examples.AddressBook.AddressbookReflection.Descriptor.MessageTypes[1]; }
        //         }

        //         [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        //         pbr::MessageDescriptor pb::IMessage.Descriptor
        //         {
        //             get { return Descriptor; }
        //         }

        //         [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        //         public AddressBook()
        //         {
        //             OnConstruction();
        //         }

        //         partial void OnConstruction();

        //         [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        //         public AddressBook(AddressBook other) : this()
        //         {
        //             people_ = other.people_.Clone();
        //         }

        //         [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        //         public AddressBook Clone()
        //         {
        //             return new AddressBook(this);
        //         }

        /// <summary>Field number for the "people" field.</summary>
        public const int PeopleFieldNumber = 1;
        private static readonly pb::FieldCodec<global::Google.Protobuf.Examples.Fast.AddressBook.Person> _repeated_people_codec = pb::FieldCodec.ForMessage<global::Google.Protobuf.Examples.Fast.AddressBook.Person>(10);
        private pbc::RepeatedField<global::Google.Protobuf.Examples.Fast.AddressBook.Person> people_;
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public pbc::RepeatedField<global::Google.Protobuf.Examples.Fast.AddressBook.Person> People
        {
            get { return people_; }
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public override bool Equals(object other)
        {
            return Equals(other as AddressBook);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public bool Equals(AddressBook other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }
            if (ReferenceEquals(other, this))
            {
                return true;
            }
            if (!people_.Equals(other.people_)) return false;
            return true;
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public override int GetHashCode()
        {
            int hash = 1;
            hash ^= people_.GetHashCode();
            return hash;
        }

        //         [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        //         public override string ToString()
        //         {
        //             return pb::JsonFormatter.ToDiagnosticString(this);
        //         }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public void WriteTo(pb::CodedOutputStream output)
        {
            people_.WriteTo(output, _repeated_people_codec);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public int CalculateSize()
        {
            int size = 0;
            size += people_.CalculateSize(_repeated_people_codec);
            return size;
        }

        //         [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        //         public void MergeFrom(AddressBook other)
        //         {
        //             if (other == null)
        //             {
        //                 return;
        //             }
        //             people_.Add(other.people_);
        //         }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public void MergeFrom(pb::CodedInputStream input, IAllocator allocator)
        {
            uint tag;
            while ((tag = input.ReadTag()) != 0)
            {
                switch (tag)
                {
                    default:
                        input.SkipLastField();
                        break;
                    case 10:
                        {
                            people_.AddEntriesFrom(input, _repeated_people_codec, allocator);
                            break;
                        }
                }
            }
        }

    }

    #endregion

}

#endregion Designer generated code
