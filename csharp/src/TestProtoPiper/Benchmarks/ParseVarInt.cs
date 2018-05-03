using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Columns;
using BenchmarkDotNet.Attributes.Exporters;
using BenchmarkDotNet.Attributes.Jobs;
using System;
using System.Buffers.Binary;

namespace TestProtoPiper
{
    [CoreJob()]
    [RPlotExporter, RankColumn]
    public class ParseVarInt
    {
        [Params(1000, 10000)]
        public int N;

        [Benchmark]
        public void ParseAsInt4()
        {
            var buff = new byte[] { 150, 200, 202, 60 };
            for (int i = 0; i < N; i++)
            {
                ParseAsIntCore(buff);
            }
        }

        [Benchmark]
        public void ParseAsInt2()
        {
            var buff = new byte[] { 150, 30, 255, 255 };
            for (int i = 0; i < N; i++)
            {
                ParseAsIntCore(buff);
            }
        }

        static uint ParseAsIntCore(ReadOnlySpan<byte> buff)
        {
            var test = BinaryPrimitives.ReadUInt32LittleEndian(buff);
            if ((test & 0x80) == 0)
                return test & 0x7f;

            uint result = test & 0x7f;
            result |= (test & 0x7f00) >> 1;
            if ((test & 0x8000) > 0)
            {
                result |= (test & 0x7f0000) >> 2;
                if ((test & 0x800000) > 0)
                {
                    result |= (test & 0x7f000000) >> 3;
                    if ((test & 0x80000000) > 0)
                    {
                        throw new Exception();
                    }
                }
            }
            return result;
        }

        [Benchmark]
        public void ParseAsIntT4()
        {
            var buff = new byte[] { 150, 200, 202, 60 };
            for (int i = 0; i < N; i++)
            {
                ParseAsIntTCore(buff);
            }
        }

        [Benchmark]
        public void ParseAsIntT2()
        {
            var buff = new byte[] { 150, 30, 255, 255 };
            for (int i = 0; i < N; i++)
            {
                ParseAsIntTCore(buff);
            }
        }

        static uint ParseAsIntTCore(ReadOnlySpan<byte> buff)
        {
            var test = BinaryPrimitives.ReadUInt32LittleEndian(buff);
            if ((test & 0x80808080) == 0x80808080)
                throw new Exception();
            else if ((test & 0x808080) == 0x808080)
                return (test & 0x7f000000) >> 3 | (test & 0x7f0000) >> 2 | (test & 0x7f00) >> 1 | (test & 0x7f);
            else if ((test & 0x8080) == 0x8080)
                return (test & 0x7f0000) >> 2 | (test & 0x7f00) >> 1 | (test & 0x7f);
            else if ((test & 0x80) == 0x80)
                return (test & 0x7f00) >> 1 | (test & 0x7f);
            else
                return (test & 0x7f);
        }

        [Benchmark]
        public void ParseAsArray4()
        {
            var buff = new byte[] { 150, 200, 202, 60 };
            for (int i = 0; i < N; i++)
            {
                ParseAsArrayCore(buff);
            }
        }

        [Benchmark]
        public void ParseAsArray2()
        {
            var buff = new byte[] { 150, 30, 255, 255 };
            for (int i = 0; i < N; i++)
            {
                ParseAsArrayCore(buff);
            }
        }

        static uint ParseAsArrayCore(ReadOnlySpan<byte> span)
        {
            uint tmp = span[0];
            if (tmp < 128)
                return tmp;
            uint result = tmp & 0x7f;
            if ((tmp = span[1]) < 128)
            {
                result |= tmp << 7;
            }
            else
            {
                result |= (tmp & 0x7f) << 7;
                if ((tmp = span[2]) < 128)
                {
                    result |= tmp << 14;
                }
                else
                {
                    result |= (tmp & 0x7f) << 14;
                    if ((tmp = span[3]) < 128)
                    {
                        result |= tmp << 21;
                    }
                    else
                    {
                        throw new Exception();
                        //result |= (tmp & 0x7f) << 21;
                        //result |= (tmp = span[4]) << 28;
                        //if (tmp >= 128)
                        //{
                        //    // Discard upper 32 bits.
                        //    return SlowDiscardUpperVarIntBitsAndReturn(5, (uint)result, cancellationToken);
                        //}
                    }
                }
            }
            return result;
        }
    }
}
