using BenchmarkDotNet.Running;
using Google.Protobuf.ProtoPiper;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace TestProtoPiper
{
    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<ParseVarInt>();
        }

        static async Task Test1()
        {
            var pipe = new Pipe();
            var reader = new CodedInputReader(pipe.Reader);

            await pipe.Writer.WriteAsync(new byte[] { 8 });
            var a = await reader.ReadTagAsync();
            await pipe.Writer.WriteAsync(new byte[] { 96, 1 });
            var b = await reader.ReadTagAsync();
        }
    }
}
