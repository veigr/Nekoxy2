using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nekoxy2.Test.TestUtil
{
    class TestNetworkStream : Stream
    {
        public MemoryStream InputStream { get; } = new MemoryStream();
        public MemoryStream OutputStream { get; } = new MemoryStream();

        public override bool CanRead => this.InputStream.CanRead;

        public override bool CanSeek => this.InputStream.CanSeek;

        public override bool CanWrite => this.OutputStream.CanWrite;

        public override long Length => this.InputStream.Length;

        public override long Position
        {
            get => this.InputStream.Position;
            set => this.InputStream.Position = value;
        }

        public override void Flush()
        {
            this.OutputStream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return this.InputStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            this.InputStream.SetLength(value);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (this.disposedValue)
                return 0;
            while (this.InputStream.Length - this.InputStream.Position <= 0)
            {
                Thread.Sleep(10);
                if (this.disposedValue)
                    return 0;
            }
            return this.InputStream.Read(buffer, offset, count);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return Task.Run(() => this.Read(buffer, offset, count), cancellationToken);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            lock (this.OutputStream)
            {
                if (this.disposedValue)
                    return;
                Debug.WriteLine("# Write");
                Debug.WriteLine(Encoding.ASCII.GetString(buffer));
                Debug.WriteLine("# WriteEnd");
                try
                {
                    this.OutputStream.Write(buffer, offset, count);
                }
                catch (ObjectDisposedException)
                {
                    throw;
                }
            }
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return Task.Run(() => this.Write(buffer, offset, count), cancellationToken);
        }

        #region IDisposable Support
        private bool disposedValue = false; // 重複する呼び出しを検出するには

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    lock (this)
                    {
                        this.InputStream.Dispose();
                        this.OutputStream.Dispose();
                    }
                }
                disposedValue = true;
            }
        }
        #endregion

    }
}
