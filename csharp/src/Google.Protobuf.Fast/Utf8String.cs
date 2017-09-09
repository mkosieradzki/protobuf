using System;
using System.Text;

namespace Google.Protobuf.Fast
{
    public struct Utf8String// : IEquatable<Utf8String>
    {
        public int ByteLength { get; private set; }
        private int handle;

        public bool IsEmpty => ByteLength == 0;

        internal void Initialize(int handle, int byteLength)
        {
            this.handle = handle;
            ByteLength = byteLength;
        }

        public ReadOnlySpan<byte> GetBytes(IArena arena)
        {
            if (arena == null)
                throw new ArgumentNullException(nameof(arena));

            if (ByteLength == 0) return ReadOnlySpan<byte>.Empty;

            return arena.Get<byte>(handle, ByteLength).ToArray();
        }

        public string AsString(IArena arena)
        {
            if (ByteLength == 0) return String.Empty;

            var arr = arena.Get<byte>(handle, ByteLength).ToArray();
            //TODO: Use encoding function operating on byte spans
            return Encoding.UTF8.GetString(arr, 0, arr.Length);
        }

        //public override bool Equals(object obj) => obj is Utf8String v && Equals(v);

        public bool Equals(Utf8String other, IArena arena)
        {
            if (arena == null)
                throw new ArgumentNullException(nameof(arena));

            if (other.ByteLength != ByteLength) return false;
            if (ByteLength == 0) return true;

            return arena.Get<byte>(handle, ByteLength).SequenceEqual(arena.Get<byte>(other.handle, ByteLength));
        }

        //public override int GetHashCode()
        //{
        //    int ret = 23;
        //    unsafe
        //    {
        //        for (int i = 0; i < ByteLength; i++)
        //            ret = (ret * 31) + buff[ByteLength];
        //    }
        //    return ret;
        //}
    }
}
