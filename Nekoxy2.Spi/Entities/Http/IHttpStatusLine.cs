using System;
using System.Net;

namespace Nekoxy2.Spi.Entities.Http
{
    /// <summary>
    /// 読み取り専用 HTTP ステータスライン
    /// </summary>
    public interface IReadOnlyHttpStatusLine
    {
        /// <summary>
        /// HTTP バージョン
        /// </summary>
        Version HttpVersion { get; }

        /// <summary>
        /// HTTP ステータスコード
        /// </summary>
        HttpStatusCode StatusCode { get; }

        /// <summary>
        /// HTTP ステータスの説明
        /// </summary>
        string ReasonPhrase { get; }
    }

    /// <summary>
    /// HTTP ステータスライン
    /// </summary>
    public interface IHttpStatusLine : IReadOnlyHttpStatusLine
    {
        /// <summary>
        /// HTTP バージョン
        /// </summary>
        new Version HttpVersion { get; set; }

        /// <summary>
        /// HTTP ステータスコード
        /// </summary>
        new HttpStatusCode StatusCode { get; set; }

        /// <summary>
        /// HTTP ステータスの説明
        /// </summary>
        new string ReasonPhrase { get; set; }
    }
}
