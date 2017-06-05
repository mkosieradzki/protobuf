#if !PROTOBUF_NO_ASYNC

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Protobuf
{
    /// <summary>
    /// Helper class used to detect synchronous operations in async-APIS
    /// </summary>
    internal sealed class AsyncOnlyStreamWrapper : Stream
    {
        private Stream inner;

        public AsyncOnlyStreamWrapper(byte[] buffer) : this(new MemoryStream(buffer)) { }

        public AsyncOnlyStreamWrapper(Stream inner)
        {
            if (inner == null)
            {
                throw new ArgumentNullException(nameof(inner));
            }
            this.inner = inner;
        }

        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => inner.CanSeek;
        public override bool CanWrite => inner.CanWrite;
        public override long Length => inner.Length;
        public override long Position { get => inner.Position; set => inner.Position = value; }
        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
        public override void SetLength(long value) => inner.SetLength(value);

        public override void Flush() => throw new NotSupportedException();
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override int ReadByte() => throw new NotSupportedException();
        public override void WriteByte(byte value) => throw new NotSupportedException();

        public override Task FlushAsync(CancellationToken cancellationToken) => inner.FlushAsync(cancellationToken);
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => inner.ReadAsync(buffer, offset, count, cancellationToken);
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => inner.WriteAsync(buffer, offset, count, cancellationToken);
    }
}

#endif