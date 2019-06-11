using System;
using System.Net.Http;

namespace Nekoxy2.Spi.Entities.Http
{
    /// <summary>
    /// 読み取り専用 HTTP リクエストライン
    /// </summary>
    public interface IReadOnlyHttpRequestLine
    {
        /// <summary>
        /// HTTP メソッド
        /// </summary>
        HttpMethod Method { get; }

        /// <summary>
        /// リクエストターゲット
        /// </summary>
        string RequestTarget { get; }

        /// <summary>
        /// HTTP バージョン
        /// </summary>
        Version HttpVersion { get; }
    }

    /// <summary>
    /// HTTP リクエストライン
    /// </summary>
    public interface IHttpRequestLine : IReadOnlyHttpRequestLine
    {
        /// <summary>
        /// HTTP メソッド
        /// </summary>
        new HttpMethod Method { get; set; }

        /// <summary>
        /// リクエストターゲット
        /// </summary>
        new string RequestTarget { get; set; }

        /// <summary>
        /// HTTP バージョン
        /// </summary>
        new Version HttpVersion { get; set; }
    }
}
