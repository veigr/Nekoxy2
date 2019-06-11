using System;
using System.Collections.Generic;
using System.Linq;

namespace Nekoxy2.ApplicationLayer.Entities.Http2
{
    /// <summary>
    /// コネクション再利用可能なオリジンのリストを転送するフレーム
    /// RFC8336
    /// </summary>
    internal sealed class Http2OriginFrame : IHttp2Frame
    {
        #region Header

        /// <summary>
        /// HTTP/2 フレームヘッダー
        /// </summary>
        public Http2FrameHeader Header { get; }

        #endregion

        /// <summary>
        /// オリジンリスト
        /// </summary>
        public IReadOnlyList<OriginEntry> OriginEntries { get; }

        public sealed class OriginEntry
        {
            /// <summary>
            /// 長さ
            /// </summary>
            public ushort OriginLen { get; internal set; }

            /// <summary>
            /// オリジン
            /// </summary>
            public byte[] AsciiOrigin { get; internal set; }

            /// <summary>
            /// オリジン
            /// </summary>
            public string Origin => this.AsciiOrigin.ToASCII();
        }

        public Http2OriginFrame() { }

        public Http2OriginFrame(Http2FrameHeader header, byte[] data)
        {
            if (data.Length != header.Length)
                throw new ArgumentException("Invalid Length.");
            this.Header = header;

            var entries = new List<OriginEntry>();
            var index = 0;
            while (index < data.Length)
            {
                var entry = new OriginEntry();
                entry.OriginLen = data.ToUInt16(index);
                index += 2;
                entry.AsciiOrigin = data.Skip(index).Take(entry.OriginLen).ToArray();
                index += entry.OriginLen;
                entries.Add(entry);
            }
            this.OriginEntries = entries;
        }

        public Http2OriginFrame(Http2FrameHeader header, IReadOnlyList<OriginEntry> originEntries)
        {
            this.Header = header;
            this.OriginEntries = originEntries;
        }

        public byte[] ToBytes()
        {
            var bytes = new List<byte>();
            bytes.AddRange(this.Header.ToBytes());
            foreach (var entry in this.OriginEntries)
            {
                bytes.AddRange(BitConverter.GetBytes(entry.OriginLen).Reverse());
                bytes.AddRange(entry.AsciiOrigin);
            }
            return bytes.ToArray();
        }

        public override string ToString()
            => $"{this.Header}, Origins: \r\n{string.Join("\r\n", this.OriginEntries)}";
    }
}
