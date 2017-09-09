using System;

namespace Google.Protobuf.Fast
{
    public sealed class SingleThreadedTrivialArenaAllocator : IAllocator
    {
        private Span<byte> pinnedMemory;
        private Span<byte> freeMemory;

        public SingleThreadedTrivialArenaAllocator(Span<byte> pinnedMemory)
        {
            //NOTE: Replace this with System.Memory<byte> when available and use pinning!
            //IF this pointer is not pinned bad things gonna happe
            this.pinnedMemory = pinnedMemory;
            this.freeMemory = pinnedMemory;
        }

        public void Clear()
        {
            pinnedMemory.Clear();
            freeMemory = pinnedMemory;
        }

        public Span<T> Alloc<T>(int size) where T : struct
        {
            var ret = pinnedMemory.NonPortableCast<byte, T>().Slice(0, size);
            freeMemory = freeMemory.Slice(ret.AsBytes().Length);
            return ret;
        }
    }
}
