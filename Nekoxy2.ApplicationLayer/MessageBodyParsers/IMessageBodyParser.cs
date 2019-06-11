using System;

namespace Nekoxy2.ApplicationLayer.MessageBodyParsers
{
    /// <summary>
    /// HTTP メッセージボディー解析器
    /// </summary>
    internal interface IMessageBodyParser : IDisposable
    {
        /// <summary>
        /// メッセージボディー
        /// </summary>
        byte[] Body { get; }

        /// <summary>
        /// トレイラー
        /// </summary>
        string Trailers { get; }

        /// <summary>
        /// 終端に達したかどうか
        /// </summary>
        bool IsTerminated { get; }

        /// <summary>
        /// 1 バイト書き込み
        /// </summary>
        /// <param name="b">バイトデータ</param>
        void WriteByte(byte b);

        /// <summary>
        /// 書き込み
        /// </summary>
        /// <param name="buffer">書き込むバイト配列</param>
        /// <returns>書き込んだバイト数</returns>
        int Write(byte[] buffer);
    }
}
