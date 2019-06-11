using System;
using System.Collections.Generic;
using System.Linq;

namespace Nekoxy2.ApplicationLayer.Entities.Http2
{
    /// <summary>
    /// HTTP ヘッダーを転送するフレーム
    /// RFC7540 6.2
    /// </summary>
    internal sealed class Http2HeadersFrame : IHttp2HeadersFrame
    {
        #region Header

        /// <summary>
        /// HTTP/2 フレームヘッダー
        /// </summary>
        public Http2FrameHeader Header { get; }

        /// <summary>
        /// エンドポイントが特定のストリームに送信する最後のフレームであることを示す
        /// </summary>
        public bool IsEndStream => this.HasFlag((byte)Flag.EndStream);

        /// <summary>
        /// このフレームの後には CONTINUATION フレームが続かないことを示す
        /// </summary>
        public bool IsEndHeaders => this.HasFlag((byte)Flag.EndHeaders);

        /// <summary>
        /// パディングが設定されていることを示す。
        /// </summary>
        public bool IsPadded => this.HasFlag((byte)Flag.Padded);

        /// <summary>
        /// Exclusive Flag (E)、StreamDependencyID、Weight フィールドが設定されていることを示す
        /// </summary>
        public bool IsPriority => this.HasFlag((byte)Flag.Priority);

        private enum Flag : byte
        {
            EndStream = 0b00000001,
            EndHeaders = 0b00000100,
            Padded = 0b00001000,
            Priority = 0b00100000,
        }

        #endregion

        /// <summary>
        /// パディング長
        /// </summary>
        public byte PadLength { get; }

        /// <summary>
        /// ストリームの依存関係の排他を示す (Exclusive Flag)
        /// </summary>
        public bool E { get; }

        /// <summary>
        /// 依存先ストリーム ID (unsigned 31bit)
        /// </summary>
        public int StreamDependencyID { get; }

        /// <summary>
        /// ストリーム優先度の重み
        /// </summary>
        public byte Weight { get; }

        /// <summary>
        /// ヘッダーブロックフラグメント
        /// </summary>
        public byte[] HeaderBlockFragment { get; }

        /// <summary>
        /// パディング
        /// </summary>
        public byte[] Padding { get; }

        public Http2HeadersFrame() { }

        public Http2HeadersFrame(Http2FrameHeader header, byte[] data)
        {
            if (data.Length != header.Length)
                throw new ArgumentException("Invalid Length.");
            this.Header = header;
            var index = 0;
            this.PadLength = this.IsPadded ? data[index++] : default;
            if (this.IsPriority)
            {
                this.E = data[index].HasFlag(0b10000000);
                this.StreamDependencyID = data.ToUInt31(index);
                index += 4;
                this.Weight = data[index++];
            }
            var fragmentLength = data.Length
                - (this.IsPadded ? 1 + this.PadLength : 0)
                - (this.IsPriority ? 5 : 0);
            this.HeaderBlockFragment = data.Skip(index).Take(fragmentLength).ToArray();
            index += fragmentLength;
            this.Padding = data.Skip(index).ToArray();
        }

        public Http2HeadersFrame(Http2FrameHeader header, bool e, int streamDependencyID, byte weight, byte[] headerBlockFragment, byte[] padding)
        {
            this.Header = header;
            this.E = e;
            this.StreamDependencyID = streamDependencyID;
            this.Weight = weight;
            this.HeaderBlockFragment = headerBlockFragment;
            this.Padding = padding;
        }

        public byte[] ToBytes()
        {
            var bytes = new List<byte>();
            bytes.AddRange(this.Header.ToBytes());
            if (this.IsPadded) bytes.Add(this.PadLength);
            if (this.IsPriority)
            {
                var e = this.E ? 0b10000000 : 0;
                var streamDependencyID = BitConverter.GetBytes(this.StreamDependencyID).Reverse().ToArray();
                bytes.Add((byte)(e & streamDependencyID[0]));
                bytes.Add(streamDependencyID[1]);
                bytes.Add(streamDependencyID[2]);
                bytes.Add(streamDependencyID[3]);
                bytes.Add(this.Weight);
            }
            bytes.AddRange(this.HeaderBlockFragment);
            if (this.IsPadded) bytes.AddRange(this.Padding);
            return bytes.ToArray();
        }

        /// <summary>
        /// バイト配列へ変換
        /// </summary>
        /// <returns></returns>
        /// 
        public override string ToString()
            => $"{this.Header}, IsEndStream: {this.IsEndStream}, IsEndHeaders: {this.IsEndHeaders}";

        public static Http2HeadersFrame Create(
            int streamID,
            bool isEndStream,
            bool isEndHeaders,
            byte[] headerBlockFragment)
        {
            var flags = isEndStream ? (byte)Flag.EndStream : (byte)0;
            flags &= isEndHeaders ? (byte)Flag.EndHeaders : (byte)0;
            var header = new Http2FrameHeader(
                headerBlockFragment.Length,
                Http2FrameType.Headers,
                flags,
                streamID);
            return new Http2HeadersFrame(header, default, default, default, headerBlockFragment, Array.Empty<byte>());
        }
    }
}
