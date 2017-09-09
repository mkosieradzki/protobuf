using System;

namespace Google.Protobuf.Fast
{
    public interface IAllocator
    {
        ref T Alloc<T>() where T : struct;
        IntPtr AllocMem(int size);
    }
}
