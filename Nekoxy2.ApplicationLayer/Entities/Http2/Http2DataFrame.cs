using System;
using System.Collections.Generic;
using System.Linq;

namespace Nekoxy2.ApplicationLayer.Entities.Http2
{
    /// <summary>
    /// リクエスト・レスポンスボディとなる可変長オクテット列を転送するフレーム
    /// RFC7540 6.1
    /// </summary>
    internal sealed class Http2DataFrame : IHttp2Frame
    {
        #region Header

        /// <summary>
        /// HTTP/2 フレームヘッダ
        /// </summary>
        public Http2FrameHeader Header { get; }

        /// <summary>
        /// エンドポイントが特定のストリームに送信する最後のフレームであることを示す
        /// </summary>
        public bool IsEndStream => this.HasFlag((byte)Flag.EndStream);

        /// <summary>
        /// パディングが設定されていることを示す
        /// </summary>
        public bool IsPadded => this.HasFlag((byte)Flag.Padded);

        private enum Flag : byte
        {
            EndStream = 0b00000001,
            Padded = 0b00001000,
        }

        #endregion

        /// <summary>
        /// パディング長
        /// </summary>
        private byte PadLength { get; }

        /// <summary>
        /// アプリケーションデータ
        /// </summary>
        public byte[] Data { get; }

        /// <summary>
        /// パディング
        /// </summary>
        private byte[] Padding { get; }

        public Http2DataFrame() { }

        public Http2DataFrame(Http2FrameHeader header, byte[] data)
        {
            if (data.Length != header.Length)
                throw new ArgumentException("Invalid Length.");
            this.Header = header;
            if (this.IsPadded)
            {
                this.PadLength = data[0];
                var dataLength = data.Length - this.PadLength - 1;
                this.Data = data.Skip(1).Take(dataLength).ToArray();
                this.Padding = data.Skip(1 + dataLength).ToArray();
            }
            else
            {
                this.PadLength = 0;
                this.Data = data;
                this.Padding = Array.Empty<byte>();
            }
        }

        public Http2DataFrame(Http2FrameHeader header, byte[] data, byte[] padding)
        {
            this.Header = header;
            this.PadLength = (byte)padding.Length;
            this.Data = data;
            this.Padding = padding;
        }

        public byte[] ToBytes()
        {
            var bytes = new List<byte>();
            bytes.AddRange(this.Header.ToBytes());
            if (this.IsPadded) bytes.Add(this.PadLength);
            bytes.AddRange(this.Data);
            if (this.IsPadded) bytes.AddRange(this.Padding);
            return bytes.ToArray();
        }

        public override string ToString()
            => $"{this.Header}, IsEndStream: {this.IsEndStream}";

        public static Http2DataFrame Create(
            int streamID,
            bool isEndStream,
            byte[] data)
        {
            var flags = isEndStream ? (byte)Flag.EndStream : (byte)0;
            var header = new Http2FrameHeader(
                data.Length,
                Http2FrameType.Data,
                flags,
                streamID);
            return new Http2DataFrame(header, data, Array.Empty<byte>());
        }
    }
}
