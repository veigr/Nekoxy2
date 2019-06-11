using Nekoxy2.ApplicationLayer.Entities.Http;
using Nekoxy2.ApplicationLayer.Entities.Http2;
using Nekoxy2.ApplicationLayer.ProtocolReaders.Http2.Hpack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nekoxy2.ApplicationLayer.ProtocolReaders.Http2
{
    internal static partial class Http2StreamReaderExtensions
    {
        /// <summary>
        /// HTTP/2 リクエストヘッダーから HTTP/1.1 リクエストラインを構築
        /// </summary>
        /// <param name="requestHeaders">HTTP/2 リクエストヘッダー</param>
        /// <returns>HTTP/1.1 リクエストライン</returns>
        public static HttpRequestLine ToRequestLine(this IReadOnlyList<(string Name, string Value)> requestHeaders)
        {
            var method = requestHeaders.GetFirstValue(":method");
            var path = requestHeaders.GetFirstValue(":path");
            HttpRequestLine.TryParse($"{method} {path} HTTP/2.0\r\n", out var result);
            return result;
        }

        /// <summary>
        /// HTTP/2 レスポンスヘッダーから HTTP/1.1 ステータスラインを構築
        /// </summary>
        /// <param name="responseHeaders">HTTP/2 レスポンスヘッダー</param>
        /// <returns>HTTP/1.1 ステータスライン</returns>
        public static HttpStatusLine ToStatusLine(this IReadOnlyList<(string Name, string Value)> responseHeaders)
        {
            var status = responseHeaders.GetFirstValue(":status");
            HttpStatusLine.TryParse($"HTTP/2.0 {status} \r\n", out var result);
            return result;
        }

        /// <summary>
        /// HTTP/2 ヘッダーから HTTP/1.1 ヘッダーを構築
        /// </summary>
        /// <param name="headers">HTTP/2 ヘッダー</param>
        /// <returns>HTTP/1.1 ヘッダー</returns>
        public static HttpHeaders ToHttpHeaders(this IReadOnlyList<(string Name, string Value)> headers)
        {
            if (headers == null || headers.Count == 0)
                return HttpHeaders.Empty;

            var headersSource = string.Join("\r\n",
                headers
                    .Select(x =>
                    {
                        if (x.Name == ":authority")
                            return (Name: "host", x.Value);
                        else
                            return x;
                    })
                    .Where(x => !x.Name.StartsWith(":"))
                    .Select(x => $"{x.Name}: {x.Value}")
                ) + "\r\n\r\n";
            HttpHeaders.TryParse(headersSource, out var result, false);
            return result;
        }

        /// <summary>
        /// ヘッダーブロックフラグメントを結合
        /// </summary>
        /// <param name="frames">ヘッダーブロックフラグメントリスト</param>
        /// <returns>結合されたヘッダーブロックフラグメント</returns>
        public static byte[] BuildHeadersBlock(this IList<IHttp2Frame> frames)
        {
            var headersFrames = frames
                .OfType<IHttp2HeadersFrame>();
            var headers = headersFrames
                .Last(x => !(x is Http2ContinuationFrame));
            return headersFrames
                .SkipWhile(x => x != headers)
                .Select(x => (IEnumerable<byte>)x.HeaderBlockFragment)
                .Aggregate((a, b) => a.Concat(b))
                .ToArray();
        }

        /// <summary>
        /// ボディーを結合
        /// </summary>
        /// <param name="frames">ボディーを含むフレームリスト</param>
        /// <returns>ボディー</returns>
        public static byte[] BuildBody(this IList<IHttp2Frame> frames)
        {
            var dataFrames = frames.OfType<Http2DataFrame>();

            if (!dataFrames.Any())
                return Array.Empty<byte>();

            var length = dataFrames.Sum(x => (decimal)x.Data.Length);
            if (int.MaxValue < length)
                return Array.Empty<byte>();

            var result = new byte[(int)length];
            var index = 0;
            foreach (var dataFrame in dataFrames)
            {
                Buffer.BlockCopy(dataFrame.Data, 0, result, index, dataFrame.Data.Length);
                index += dataFrame.Data.Length;
            }
            return result;
        }
    }
}
