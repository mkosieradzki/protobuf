using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Google.Protobuf.Fast
{
    public struct Utf8String : IEquatable<Utf8String>
    {
        public int ByteLength => buff.Length;
        private Span<byte> buff;

        public bool IsEmpty => ByteLength == 0;

        internal void Initialize(Span<byte> ptr)
        {
            buff = ptr;
        }

        public string AsString()
        {
            if (ByteLength == 0) return String.Empty;

            var arr = buff.ToArray();
            //TODO: Use encoding function operating on byte spans
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
