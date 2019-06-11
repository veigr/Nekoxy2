namespace Nekoxy2.Spi.Entities.Http
{
    /// <summary>
    /// 読み取り専用 HTTP メッセージ
    /// </summary>
    public interface IReadOnlyHttpMessage
    {
        /// <summary>
        /// ヘッダー
        /// </summary>
        IReadOnlyHttpHeaders Headers { get; }

        /// <summary>
        /// メッセージボディー
        /// </summary>
        byte[] Body { get; }

        /// <summary>
        /// トレイラーヘッダー
        /// </summary>
        IReadOnlyHttpHeaders Trailers { get; }
    }

    /// <summary>
    /// HTTP メッセージ
    /// </summary>
    public interface IHttpMessage : IReadOnlyHttpMessage
    {
        /// <summary>
        /// ヘッダー
        /// </summary>
        new IHttpHeaders Headers { get; set; }

        /// <summary>
        /// メッセージボディー
        /// </summary>
        new byte[] Body { get; set; }

        /// <summary>
        /// トレイラーヘッダー
        /// </summary>
        new IHttpHeaders Trailers { get; set; }
    }
}