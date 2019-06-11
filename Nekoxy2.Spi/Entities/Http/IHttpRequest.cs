namespace Nekoxy2.Spi.Entities.Http
{
    /// <summary>
    /// 読み取り専用 HTTP リクエスト
    /// </summary>
    public interface IReadOnlyHttpRequest : IReadOnlyHttpMessage
    {
        /// <summary>
        /// リクエストライン
        /// </summary>
        IReadOnlyHttpRequestLine RequestLine { get; }
    }

    /// <summary>
    /// HTTP リクエスト
    /// </summary>
    public interface IHttpRequest : IReadOnlyHttpRequest, IHttpMessage
    {
        /// <summary>
        /// リクエストライン
        /// </summary>
        new IHttpRequestLine RequestLine { get; set; }
    }
}
