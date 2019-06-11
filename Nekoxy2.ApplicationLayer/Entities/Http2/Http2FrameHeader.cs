using System;

namespace Nekoxy2.ApplicationLayer.Entities.Http2
{
    /// <summary>
    /// HTTP/2 フレームヘッダー
    /// RFC7540 4
    /// </summary>
    internal sealed class Http2FrameHeader
    {
        /// <summary>
        /// ヘッダを含まないフレーム長 (unsigned 24bit)
        /// RFC7540 4.1
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// フレームタイプ (8bit)
        /// </summary>
        public Http2FrameType Type { get; }

        /// <summary>
        /// フレームタイプ固有のフラグ (8bit)
        /// </summary>
        public byte Flags { get; }

        /// <summary>
        /// 予約済みフィールド (1bit)
        /// </summary>
        private bool R { get; }

        /// <summary>
        /// ストリーム ID (unsigned 31bit)
        /// </summary>
        public int StreamID { get; }

        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        public Http2FrameHeader() { }

        /// <summary>
        /// バイナリ配列を使用して初期化
        /// </summary>
        /// <param name="data"></param>
        public Http2FrameHeader(byte[] data)
        {
            if (data.Length != HeaderSize)
                throw new ArgumentException("Invalid Length.");
            this.Length = data.ToUInt24(0);
            this.Type = (Http2FrameType)data[3];
            this.Flags = data[4];
            this.R = data[5].HasFlag(0b10000000);
            this.StreamID = data.ToUInt31(5);
        }

        public Http2FrameHeader(
            int length,
            Http2FrameType type,
            byte flags,
            int streamID)
        {
            this.Length = length;
            this.Type = type;
            this.Flags = flags;
            this.R = false;
            this.StreamID = streamID;
        }

        /// <summary>
        /// バイト配列へ変換
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            var bytes = new byte[9];
            var length = BitConverter.GetBytes(this.Length);
            bytes[0] = length[2];
            bytes[1] = length[1];
            bytes[2] = length[0];
            bytes[3] = (byte)this.Type;
            bytes[4] = this.Flags;
            var id = BitConverter.GetBytes(this.StreamID);
            bytes[5] = id[3];
            bytes[6] = id[2];
            bytes[7] = id[1];
            bytes[8] = id[0];
            return bytes;
        }

        /// <summary>
        /// ペイロードデータから HTTP/2 フレームを作成
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        public IHttp2Frame CreateFrame(byte[] payload)
        {
            if (payload.Length != this.Length)
                throw new ArgumentException("Invalid Length.");

            IHttp2Frame frame = null;
            switch (this.Type)
            {
                case Http2FrameType.Data:
                    frame = new Http2DataFrame(this, payload);
                    break;
                case Http2FrameType.Headers:
                    frame = new Http2HeadersFrame(this, payload);
                    break;
                case Http2FrameType.Priority:
                    frame = new Http2PriorityFrame(this, payload);
                    break;
                case Http2FrameType.RstStream:
                    frame = new Http2RstStreamFrame(this, payload);
                    break;
                case Http2FrameType.Settings:
                    frame = new Http2SettingsFrame(this, payload);
                    break;
                case Http2FrameType.PushPromise:
                    frame = new Http2PushPromiseFrame(this, payload);
                    break;
                case Http2FrameType.Ping:
                    frame = new Http2PingFrame(this, payload);
                    break;
                case Http2FrameType.Goaway:
                    frame = new Http2GoawayFrame(this, payload);
                    break;
                case Http2FrameType.WindowUpdate:
                    frame = new Http2WindowUpdateFrame(this, payload);
                    break;
                case Http2FrameType.Continuation:
                    frame = new Http2ContinuationFrame(this, payload);
                    break;
                case Http2FrameType.Altsvc:
                    frame = new Http2AltsvcFrame(this, payload);
                    break;
                case Http2FrameType.Origin:
                    frame = new Http2OriginFrame(this, payload);
                    break;
                default:
                    frame = new Http2UnknownFrame(this, payload);
                    break;
            }
            return frame;
        }

        public override string ToString()
            => $"ID: {this.StreamID}, Type: {this.Type}, Length:{this.Length}";

        /// <summary>
        /// フレームヘッダのオクテットサイズ
        /// </summary>
        public static int HeaderSize => 9;  // フレームヘッダは固定長9オクテット RFC7540 4.1
    }

    internal static partial class FramesExtensions
    {
        public static bool HasFlag(this IHttp2Frame frame, byte flag)
            => (frame.Header.Flags & flag) == flag;
    }
}
