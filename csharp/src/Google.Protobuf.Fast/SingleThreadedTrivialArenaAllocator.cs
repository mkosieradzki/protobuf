using System;
using System.Runtime.CompilerServices;

namespace Google.Protobuf.Fast
{
    public sealed class SingleThreadedTrivialArenaAllocator : IAllocator
    {
        private unsafe void* memory;
        private int totalSize;
        private int pos;

        public unsafe SingleThreadedTrivialArenaAllocator(void* memory, int totalSize)
        {
            this.memory = memory;
            this.totalSize = totalSize;
        }

        public unsafe void Clear()
        {
            Unsafe.InitBlock(memory, 0, (uint)totalSize);
            pos = 0;
        }

        public unsafe ref T Alloc<T>() where T : struct
        {
            var size = Unsafe.SizeOf<T>();

            if (pos + size > totalSize)
                throw new Exception();
            var ptr = (byte*)memory + pos;
            pos += size;
            return ref Unsafe.AsRef<T>(ptr);
        }

        public unsafe IntPtr AllocMem(int size)
        {
            if (size <= 0)
                throw new ArgumentNullException(nameof(size));

            if (pos + size > totalSize)
                throw new Exception();
            var ptr = (byte*)memory + pos;
            pos += size;
            return (IntPtr)ptr;
        }
    }
}
