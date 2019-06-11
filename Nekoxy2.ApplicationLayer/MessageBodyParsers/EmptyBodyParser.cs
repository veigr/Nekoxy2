using System;

namespace Nekoxy2.ApplicationLayer.MessageBodyParsers
{
    /// <summary>
    /// 空ボディ解析器
    /// </summary>
    internal sealed class EmptyBodyParser : IMessageBodyParser
    {
        /// <summary>
        /// メッセージボディー
        /// </summary>
        public byte[] Body => Array.Empty<byte>();

        /// <summary>
        /// トレイラー
        /// </summary>
        public string Trailers => "";

        /// <summary>
        /// 終端に達したかどうか
        /// </summary>
        public bool IsTerminated => true;

        /// <summary>
        /// 1 バイト書き込み
        /// </summary>
        /// <param name="b">バイトデータ</param>
        public void WriteByte(byte b) { }

        /// <summary>
        /// LF まで書き込み
        /// </summary>
        /// <param name="buffer">書き込むバイト配列</param>
        /// <returns>書き込んだバイト数</returns>
        public int Write(byte[] buffer)
            => buffer.Length;

        public void Dispose() { }
    }
}
