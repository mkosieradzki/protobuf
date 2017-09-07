using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Google.Protobuf.Fast
{
    public unsafe struct Utf8String : IEquatable<Utf8String>
    {
        public int ByteLength { get; private set; }
        private byte* buff;

        public bool IsEmpty => ByteLength == 0;

        internal void Initialize(byte *ptr, int length)
        {
            buff = ptr;
            ByteLength = length;
        }

        public string AsString()
        {
            if (ByteLength == 0) return String.Empty;

            var arr = new byte[ByteLength];
            Unsafe.CopyBlock(ref arr[0], ref Unsafe.AsRef<byte>(buff), (uint)ByteLength);
            return Encoding.UTF8.GetString(arr, 0, arr.Length);
        }

        public override bool Equals(object obj) => obj is Utf8String v && Equals(v);

        public bool Equals(Utf8String other)
        {
            if (other.ByteLength != ByteLength) return false;
            if (ByteLength == 0) return true;

            for (int i = 0; i < ByteLength; i++)
                if (buff[i] != other.buff[i])
                    return false;

            return true;
        }

        public override int GetHashCode()
        {
            int ret = 23;
            for (int i = 0; i < ByteLength; i++)
                ret = (ret * 31) + buff[ByteLength];
            return ret;
        }

        public static bool operator ==(Utf8String s1, Utf8String s2) => s1.Equals(s2);
        public static bool operator !=(Utf8String s1, Utf8String s2) => !s1.Equals(s2);
    }
}
