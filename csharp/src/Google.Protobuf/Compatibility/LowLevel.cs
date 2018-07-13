using System;
using System.Security;

namespace Google.Protobuf.Compatibility
{
    internal static class LowLevel
    {
        public static float Int32BitsToSingle(int value)
        {
#if NETCOREAPP2_1
            return BitConverter.Int32BitsToSingle(value);
#else
            return BitConverter.ToSingle(BitConverter.GetBytes(value), 0);
#endif
        }

        [SecurityCritical]
        public static string ReadUtf8StringFromSpan(ReadOnlySpan<byte> span)
        {
#if NETCOREAPP2_1
            return CodedOutputStream.Utf8Encoding.GetString(span);
#else
            return CodedOutputStream.Utf8Encoding.GetString(span.ToArray(), 0, span.Length);
#endif
        }
    }
}
