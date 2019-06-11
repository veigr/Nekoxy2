using System.Collections.Generic;

namespace Nekoxy2.Spi.Entities.Http
{
    /// <summary>
    /// 読み取り専用 HTTP ヘッダー
    /// </summary>
    public interface IReadOnlyHttpHeaders : IReadOnlyCollection<(string Name, string Value)>
    {
    }

    /// <summary>
    /// HTTP ヘッダー
    /// </summary>
    public interface IHttpHeaders : IList<(string Name, string Value)>, IReadOnlyHttpHeaders
    {
        new int Count { get; }
    }
}
