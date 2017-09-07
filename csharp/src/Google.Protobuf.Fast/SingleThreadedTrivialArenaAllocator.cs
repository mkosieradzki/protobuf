using System;
using System.Runtime.CompilerServices;

namespace Google.Protobuf.Fast
{
    public sealed class SingleThreadedTrivialArenaAllocator : IAllocator
    {
        private byte[] memory;
        private uint pos;

        public SingleThreadedTrivialArenaAllocator(uint totalSize)
        {
            memory = new byte[totalSize];
        }

        public unsafe ref T Alloc<T>() where T : struct
        {
            var size = (uint)Unsafe.SizeOf<T>();

            if (pos + size > memory.Length)
                throw new Exception();
            var ptr = Unsafe.AsPointer(ref memory[pos]);
            pos += size;
            return ref Unsafe.AsRef<T>(ptr);
        }

        public unsafe IntPtr AllocMem(uint size)
        {
            if (pos + size > memory.Length)
                throw new Exception();
            var ptr = Unsafe.AsPointer(ref memory[pos]);
            pos += size;
            return (IntPtr)ptr;
        }
    }
}
