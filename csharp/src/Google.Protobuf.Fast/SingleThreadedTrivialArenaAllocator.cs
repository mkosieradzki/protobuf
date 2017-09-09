using System;
using System.Buffers;

namespace Google.Protobuf.Fast
{
    public sealed class SingleThreadedTrivialArena : IAllocator, IArena, IDisposable
    {
        private Memory<byte> memory;
        private MemoryHandle pinnedMemory;
        private int position = 0;

        public SingleThreadedTrivialArena(Memory<byte> memory)
        {
            this.memory = memory;
            pinnedMemory = memory.Retain(true);
            //NOTE: Replace this with System.Memory<byte> when available and use pinning!
            //IF this pointer is not pinned bad things gonna happe
        }

        public void Clear()
        {
            position = 0;
            memory.Span.Clear();
        }

        public void Dispose()
        {
            pinnedMemory.Dispose();
        }

        public Span<T> Alloc<T>(int count, out int handle) where T : struct
        {
            var span = memory.Span.Slice(position).NonPortableCast<byte, T>().Slice(0, count);
            handle = position;
            position += span.AsBytes().Length;
            return span;
        }

        public ref T Alloc<T>(out int handle) where T : struct
        {
            var span = memory.Span.Slice(position).NonPortableCast<byte, T>().Slice(0, 1);
            handle = position;
            position += span.AsBytes().Length;
            return ref span[0];
        }

        public Span<T> Get<T>(int handle, int count) where T : struct => memory.Span.Slice(handle).NonPortableCast<byte, T>().Slice(0, count);

        public ref T Get<T>(int handle) where T : struct => ref memory.Span.Slice(handle).NonPortableCast<byte, T>()[0];
    }
}
