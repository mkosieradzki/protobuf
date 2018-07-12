using System;

namespace Google.Protobuf.Compatibility
{
    public static class LowLevelCompat
    {
        //TODO: Provide either low unsafe or inefficient implementations for other platforms
        public static float Int32BitsToSingle(int value)
        {
#if NETCOREAPP2_1
            return BitConverter.Int32BitsToSingle(value);
#else
            throw new NotImplementedException();
#endif
        }

        public static string SpanToUtf8String(ReadOnlySpan<byte> data)
        {
#if NETCOREAPP2_1
            return CodedOutputStream.Utf8Encoding.GetString(data);
#else
            throw new NotImplementedException();
#endif
        }
    }
}
