using System;

namespace Google.Protobuf.Fast
{
    public interface IArena
    {
        Span<T> Get<T>(int handle, int count) where T : struct;
        ref T Get<T>(int handle) where T : struct;
    }

    public interface IAllocator
    {
        Span<T> Alloc<T>(int count, out int handle) where T : struct;
        ref T Alloc<T>(out int handle) where T : struct;
    }
}
