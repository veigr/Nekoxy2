using System;
using System.Collections.Generic;
using System.Linq;

namespace Nekoxy2.ApplicationLayer.Entities.Http2
{
    /// <summary>
    /// サーバープッシュを通知するフレーム
    /// RFC7540 6.6
    /// </summary>
    internal sealed class Http2PushPromiseFrame : IHttp2HeadersFrame
    {
        #region Header

        /// <summary>
        /// HTTP/2 フレームヘッダー
        /// </summary>
        public Http2FrameHeader Header { get; }

        /// <summary>
        /// このフレームの後には CONTINUATION フレームが続かないことを示す
        /// </summary>
        public bool IsEndHeaders => this.HasFlag((byte)Flag.EndHeaders);

        /// <summary>
        /// パディングが設定されていることを示す
        /// </summary>
        public bool IsPadded => this.HasFlag((byte)Flag.Padded);

        private enum Flag : byte
        {
            EndHeaders = 0b00000100,
            Padded = 0b00001000,
        }
        #endregion

        /// <summary>
        /// パディング長
        /// </summary>
        private byte PadLength { get; }

        /// <summary>
        /// 予約済みビット
        /// </summary>
        private bool R { get; }

        /// <summary>
        /// プッシュレスポンス用の予約ストリーム ID (unsigned 31bit)
        /// </summary>
        public int PromisedStreamID { get; }

        /// <summary>
        /// プッシュリクエストのヘッダーブロックフラグメント
        /// </summary>
        public byte[] HeaderBlockFragment { get; }

        /// <summary>
        /// パディング
        /// </summary>
        private byte[] Padding { get; }

        public Http2PushPromiseFrame() { }

        public Http2PushPromiseFrame(Http2FrameHeader header, byte[] data)
        {
            if (data.Length != header.Length)
                throw new ArgumentException("Invalid Length.");
            this.Header = header;
            if (this.IsPadded)
            {
                this.PadLength = data[0];
                this.R = data[1].HasFlag(0b10000000);
                this.PromisedStreamID = data.ToUInt31(1);
                var fragmentLength = data.Length - 5 - this.PadLength;
                this.HeaderBlockFragment = data.Skip(5).Take(fragmentLength).ToArray();
                this.Padding = data.Skip(5 + fragmentLength).ToArray();
            }
            else
            {
                this.R = data[0].HasFlag(0b10000000);
                this.PromisedStreamID = data.ToUInt31(0);
                this.HeaderBlockFragment = data.Skip(4).ToArray();
            }
        }

        public Http2PushPromiseFrame(Http2FrameHeader header, int promisedStreamID, byte[] headerBlockFragment, byte[] padding)
        {
            this.Header = header;
            this.PadLength = (byte)padding.Length;
            this.R = false;
            this.PromisedStreamID = promisedStreamID;
            this.HeaderBlockFragment = headerBlockFragment;
            this.Padding = padding;
        }

        public byte[] ToBytes()
        {
            var bytes = new List<byte>();
            bytes.AddRange(this.Header.ToBytes());
            if (this.IsPadded) bytes.Add(this.PadLength);
            bytes.AddRange(BitConverter.GetBytes(this.PromisedStreamID).Reverse());
            bytes.AddRange(this.HeaderBlockFragment);
            if (this.IsPadded) bytes.AddRange(this.Padding);
            return bytes.ToArray();
        }

        public override string ToString()
            => $"{this.Header}, PromisedID: {this.PromisedStreamID}, IsEndHeaders: {this.IsEndHeaders}";

        public static Http2PushPromiseFrame Create(
            int streamID,
            bool isEndHeaders,
            int promisedStreamID,
            byte[] headerBlockFragment)
        {
            var flags = isEndHeaders ? (byte)Flag.EndHeaders : (byte)0;
            var header = new Http2FrameHeader(
                4 + headerBlockFragment.Length,
                Http2FrameType.PushPromise,
                flags,
                streamID);
            return new Http2PushPromiseFrame(header, promisedStreamID, headerBlockFragment, Array.Empty<byte>());
        }
    }
}
