using System.Collections.Generic;

namespace Nekoxy2.Entities.Http
{
    /// <summary>
    /// 読み取り専用 HTTP メッセージ
    /// </summary>
    public interface IReadOnlyHttpMessage : Spi.Entities.Http.IReadOnlyHttpMessage
    {
        /// <summary>
        /// ヘッダー
        /// </summary>
        new IReadOnlyHttpHeaders Headers { get; }

        /// <summary>
        /// メッセージボディー
        /// </summary>
        new IReadOnlyList<byte> Body { get; }

        /// <summary>
        /// トレイラーヘッダー
        /// </summary>
        new IReadOnlyHttpHeaders Trailers { get; }
    }

    /// <summary>
    /// HTTP メッセージ
    /// </summary>
    public interface IHttpMessage : IReadOnlyHttpMessage, Spi.Entities.Http.IHttpMessage
    {
        /// <summary>
        /// ヘッダー
        /// </summary>
        new IHttpHeaders Headers { get; }

        /// <summary>
        /// メッセージボディー
        /// </summary>
        new byte[] Body { get; }

        /// <summary>
        /// トレイラーヘッダー
        /// </summary>
        new IHttpHeaders Trailers { get; }
    }
}