using System.Linq;

namespace Nekoxy2.ApplicationLayer.Entities.Http2
{
    /// <summary>
    /// 未対応フレーム
    /// </summary>
    internal sealed class Http2UnknownFrame : IHttp2Frame
    {
        /// <summary>
        /// HTTP/2 フレームヘッダ
        /// </summary>
        public Http2FrameHeader Header { get; }

        /// <summary>
        /// ペイロードデータ
        /// </summary>
        public byte[] Payload { get; }

        public Http2UnknownFrame() { }

        public Http2UnknownFrame(Http2FrameHeader header, byte[] data)
        {
            this.Header = header;
            this.Payload = data;
        }

        public byte[] ToBytes()
        {
            return this.Header.ToBytes()
                .Concat(this.Payload)
                .ToArray();
        }

        public override string ToString()
            => this.Header.ToString();
    }
}
