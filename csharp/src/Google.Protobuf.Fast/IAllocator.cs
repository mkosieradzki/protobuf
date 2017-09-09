using System;

namespace Google.Protobuf.Fast
{
    public interface IAllocator
    {
        Span<T> Alloc<T>(int size) where T : struct;
    }
}
