namespace Nekoxy2.Spi.Entities.Http
{
    /// <summary>
    /// 読み取り専用 HTTP リクエスト・レスポンスのペア
    /// </summary>
    public interface IReadOnlySession
    {
        /// <summary>
        /// HTTP リクエスト
        /// </summary>
        IReadOnlyHttpRequest Request { get; }

        /// <summary>
        /// HTTP レスポンス
        /// </summary>
        IReadOnlyHttpResponse Response { get; }
    }

    /// <summary>
    /// HTTP リクエスト・レスポンスのペア
    /// </summary>
    public interface ISession : IReadOnlySession
    {
        /// <summary>
        /// HTTP リクエスト
        /// </summary>
        new IReadOnlyHttpRequest Request { get; }

        /// <summary>
        /// HTTP レスポンス
        /// </summary>
        new IHttpResponse Response { get; set; }
    }
}
