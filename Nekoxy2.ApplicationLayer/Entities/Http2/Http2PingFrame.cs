using System;
using System.Linq;

namespace Nekoxy2.ApplicationLayer.Entities.Http2
{
    /// <summary>
    /// PING フレーム
    /// RFC7540 6.7
    /// </summary>
    internal sealed class Http2PingFrame : IHttp2Frame
    {
        #region Header

        /// <summary>
        /// HTTP/2 フレームヘッダー
        /// </summary>
        public Http2FrameHeader Header { get; }

        /// <summary>
        /// PING レスポンスであることを示す
        /// </summary>
        public bool IsAck => this.HasFlag((byte)Flag.Ack);

        private enum Flag : byte
        {
            Ack = 0b00000001,
        }

        #endregion

        /// <summary>
        /// 64bit の任意のデータ
        /// </summary>
        public byte[] OpaqueData { get; }

        public Http2PingFrame() { }

        public Http2PingFrame(Http2FrameHeader header, byte[] data)
        {
            if (data.Length != header.Length)
                throw new ArgumentException("Invalid Length.");
            this.Header = header;
            this.OpaqueData = data;
        }

        public byte[] ToBytes()
        {
            return this.Header.ToBytes()
                .Concat(this.OpaqueData)
                .ToArray();
        }

        public override string ToString()
            => $"{this.Header}, IsACK: {this.IsAck}, Data: {this.OpaqueData.ToUTF8()}";
    }
}
