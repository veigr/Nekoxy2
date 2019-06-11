using Nekoxy2.ApplicationLayer.Entities.Http;
using Nekoxy2.Spi.Entities.Http;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Nekoxy2.SazLoader.Entities.Http
{
    /// <summary>
    /// ヘッダー
    /// </summary>
    internal sealed partial class SazHttpHeaders : IReadOnlyHttpHeaders
    {
        private readonly IList<(string Name, string Value)> headers;

        public int Count => this.headers.Count;

        private SazHttpHeaders(string source)
            => this.headers = source.ParseToKVPList();

        public IEnumerator<(string Name, string Value)> GetEnumerator()
            => this.headers.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => this.headers.GetEnumerator();

        public override string ToString()
        {
            return this.Any()
                ? this.Select(x => $"{x.Name}: {x.Value}\r\n").Aggregate((a, b) => a + b) + "\r\n"
                : "\r\n";
        }

        public static SazHttpHeaders Parse(string source)
            => new SazHttpHeaders(source);
    }
}
