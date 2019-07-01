using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nekoxy2.ApplicationLayer;

namespace Nekoxy2.Default.Proxy.Tls
{
    /// <summary>
    /// 読み取りバッファー内容を参照することができるネットワークストリーム
    /// </summary>
    internal sealed class ReadBufferedNetworkStream : Stream
    {
        /// <summary>
        /// 読み取りロック
        /// </summary>
        private readonly object readLock = new object();

        /// <summary>
        /// 書き込みロック
        /// </summary>
        private readonly object writeLock = new object();

        /// <summary>
        /// 非同期読み取りロック
        /// </summary>
        private readonly SemaphoreSlim readSemaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// 基となるストリーム
        /// </summary>
        private readonly Stream baseStream;

        /// <summary>
        /// 読み取りバッファー
        /// </summary>
        private byte[] readBuffer;

        /// <summary>
        /// 現在有効なバッファーの長さ
        /// </summary>
        public int BufferedLength { get; private set; }

        private bool disposedValue = false;

        private bool closed = false;

        public override bool CanRead => this.baseStream.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => this.baseStream.CanWrite;

        public override long Length => this.baseStream.Length;

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        /// <summary>
        /// 指定した位置のバッファー内容を取得
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public byte this[int index]
        {
            get => index < this.BufferedLength
                ? this.readBuffer[index]
                : throw new IndexOutOfRangeException();
        }

        public ReadBufferedNetworkStream(Stream baseStream, int bufferSize = 4096)
        {
            this.baseStream = baseStream;
            this.readBuffer = new byte[bufferSize];
        }

        public override void Flush()
            => this.baseStream.Flush();

        public override long Seek(long offset, SeekOrigin origin)
            => throw new NotSupportedException();

        public override void SetLength(long value)
            => throw new NotSupportedException();

        /// <summary>
        /// 指定した長さを超えるまでバッファーが溜まるのを待機
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public bool FillBuffer(int length)
        {
            this.ExpandBuffer(length);
            if (length <= this.BufferedLength) return true;

            while (this.Receive() && this.BufferedLength < length) { }
            return length <= this.BufferedLength;
        }

        /// <summary>
        /// データ受信を待機
        /// </summary>
        /// <returns>受信された場合 true、接続が閉じられた場合 false</returns>
        public bool Receive()
        {
            lock (this.readLock)
            {
                if (this.closed)
                    return false;

                try
                {
                    var readSize = this.baseStream.Read(this.readBuffer, 0, this.readBuffer.Length);
                    if (0 < readSize)
                    {
                        this.BufferedLength += readSize;
                        return true;
                    }
                    else if (0 < this.BufferedLength)
                    {
                        return true;
                    }
                    else
                    {
                        this.closed = true;
                        return false;
                    }
                }
                catch
                {
                    this.closed = true;
                    return false;
                }
            }
        }

        /// <summary>
        /// データ受信を待機
        /// </summary>
        /// <returns>受信された場合 true、接続が閉じられた場合 false</returns>
        public async Task<bool> ReceiveAsync()
        {
            await this.readSemaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                if (this.closed)
                    return false;

                var readSize = await this.baseStream.ReadAsync(this.readBuffer, 0, this.readBuffer.Length).ConfigureAwait(false);

                if (0 < readSize)
                {
                    this.BufferedLength += readSize;
                    return true;
                }
                else if (0 < this.BufferedLength)
                {
                    return true;
                }
                else
                {
                    this.closed = true;
                    return false;
                }
            }
            catch
            {
                this.closed = true;
                return false;
            }
            finally
            {
                this.readSemaphore.Release();
            }
        }

        /// <summary>
        /// バッファーされた内容を読み取るか、データ受信を待機して読み取り
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (this.closed)
                return 0;

            if (this.BufferedLength == 0 && !this.Receive())
                return 0;

            lock (this.readLock)
            {
                var length = Math.Min(count, this.BufferedLength);
                if (0 < length)
                {
                    Buffer.BlockCopy(this.readBuffer, 0, buffer, offset, length);
                    this.BufferedLength -= length;
                    Buffer.BlockCopy(this.readBuffer, length, this.readBuffer, 0, this.BufferedLength);
                    this.ReadData?.Invoke((buffer, length));
                }
                return length;
            }
        }

        /// <summary>
        /// バッファーされた内容を読み取るか、データ受信を待機して読み取り
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken _)
        {
            if (this.closed)
                return 0;

            if (this.BufferedLength == 0 && await this.ReceiveAsync().ConfigureAwait(false) == false)
                return 0;

            await this.readSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                var length = Math.Min(count, this.BufferedLength);
                if (0 < length)
                {
                    Buffer.BlockCopy(this.readBuffer, 0, buffer, offset, length);
                    this.BufferedLength -= length;
                    Buffer.BlockCopy(this.readBuffer, length, this.readBuffer, 0, this.BufferedLength);
                    this.ReadData?.Invoke((buffer, length));
                }
                return length;
            }
            finally
            {
                this.readSemaphore.Release();
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            lock (this.writeLock)
                this.baseStream.Write(buffer, offset, count);
        }

        /// <summary>
        /// バッファーの指定した位置からビッグエンディアンで符号なし16ビット整数を解釈し変換
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public ushort ReadBufferUInt16(int index)
            => this.readBuffer.ToUInt16(index);

        /// <summary>
        /// バッファーの指定した位置から指定した長さのバイト配列を取得
        /// </summary>
        /// <param name="startIndex"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public ArraySegment<byte> ReadBufferBytes(int startIndex, int length)
        {
            if (this.BufferedLength < startIndex + length)
                throw new IndexOutOfRangeException();
            return new ArraySegment<byte>(this.readBuffer, startIndex, length);
        }

        /// <summary>
        /// バッファーサイズを拡張。
        /// </summary>
        /// <param name="length"></param>
        private void ExpandBuffer(int length)
        {
            if (length <= this.readBuffer.Length)
                return;
            Array.Resize(ref this.readBuffer, length);
        }

        protected override void Dispose(bool disposing)
        {
            lock (this.writeLock)
            {
                if (!this.disposedValue)
                {
                    if (disposing)
                    {
                        // マネージ状態を破棄します (マネージ オブジェクト)。
                        this.baseStream.Dispose();
                        this.readSemaphore.Dispose();
                    }

                    // アンマネージ リソース (アンマネージ オブジェクト) を解放し、下のファイナライザーをオーバーライドします。
                    // 大きなフィールドを null に設定します。
                    this.disposedValue = true;
                }
            }
        }

        /// <summary>
        /// データを読み取った際に発生
        /// </summary>
        public event Action<(byte[] buffer, int readSize)> ReadData;
    }
}