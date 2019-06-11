namespace Nekoxy2.Entities.Http
{
    /// <summary>
    /// 読み取り専用 HTTP リクエスト・レスポンスのペア
    /// </summary>
    public interface IReadOnlySession : Spi.Entities.Http.IReadOnlySession
    {
        /// <summary>
        /// ソース SPI インスタンス
        /// </summary>
        Spi.Entities.Http.IReadOnlySession Source { get; }

        /// <summary>
        /// HTTP リクエスト
        /// </summary>
        new IReadOnlyHttpRequest Request { get; }

        /// <summary>
        /// HTTP レスポンス
        /// </summary>
        new IReadOnlyHttpResponse Response { get; }
    }

    /// <summary>
    /// HTTP リクエスト・レスポンスのペア
    /// </summary>
    public interface ISession : IReadOnlySession, Spi.Entities.Http.ISession
    {
        /// <summary>
        /// ソース SPI インスタンス
        /// </summary>
        new Spi.Entities.Http.ISession Source { get; }

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
