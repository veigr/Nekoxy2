using Nekoxy2.ApplicationLayer.MessageBodyParsers;
using Nekoxy2.Spi.Entities.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Nekoxy2.ApplicationLayer.Entities.Http
{
    internal static partial class DecodeExtensions
    {
        /// <summary>
        /// メッセージボディを指定した文字エンコーディングで文字列として取得。
        /// Transfer-Encoding, Content-Encoding がある場合は同時にデコードします。
        /// </summary>
        /// <param name="message">HTTP メッセージ</param>
        /// <param name="encoding">文字エンコーディング</param>
        /// <returns>メッセージボディー文字列</returns>
        public static string GetBodyAsString(this IReadOnlyHttpMessage message, Encoding encoding = null)
        {
            var body = message.GetContentDecodedBody();
            if (body == null)
                return "";
            if (encoding != null)
                encoding.GetString(body);
            return body.AsString(message.Headers.GetCharset());
        }

        /// <summary>
        /// Content-Encoding をデコード。
        /// Transfer-Encoding がある場合は同時にデコードします。
        /// </summary>
        /// <param name="message">HTTP メッセージ</param>
        /// <returns>Content-Encoding をデコードされたメッセージボディー</returns>
        public static byte[] GetContentDecodedBody(this IReadOnlyHttpMessage message)
        {
            var body = message.GetTransferDecodedBody();
            if (body == null)
                return null;
            if (!message.Headers.HasHeader("Content-Encoding"))
                return body;
            return body.Decode(message.Headers.GetFirstValue("Content-Encoding").SplitValue().ToArray());
        }

        /// <summary>
        /// Transfer-Encoding をデコード
        /// </summary>
        /// <param name="message">HTTP メッセージ</param>
        /// <returns>Transfer-Encoding をデコードされたメッセージボディー</returns>
        public static byte[] GetTransferDecodedBody(this IReadOnlyHttpMessage message)
        {
            if (message.Body == null)
                return null;
            if (!message.Headers.HasHeader("Transfer-Encoding"))
                return message.Body;
            return message.Body.Decode(message.Headers.GetFirstValue("Transfer-Encoding").SplitValue().ToArray());
        }

        /// <summary>
        /// バイト配列を指定した文字エンコーディングで文字列化
        /// </summary>
        /// <param name="bytes">対象バイト配列</param>
        /// <param name="encoding">文字エンコーディング</param>
        /// <returns>指定された文字エンコーディングで解釈された文字列</returns>
        public static string AsString(this byte[] bytes, Encoding encoding = null)
        {
            if (bytes == null)
                return null;
            if (encoding != null)
                return encoding.GetString(bytes);
            return Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// Content-Type から charset を取得し、<see cref="Encoding"/>として返す
        /// </summary>
        /// <param name="headers">HTTP ヘッダー</param>
        /// <returns>charset から作成された<see cref="Encoding"/></returns>
        private static Encoding GetCharset(this IReadOnlyHttpHeaders headers)
        {
            if (!headers.HasHeader("Content-Type"))
                return null;
            var charset = headers.GetFirstValue("Content-Type")
                .ToLower()
                .Split(';')
                .Select(x => x.Trim())
                .FirstOrDefault(x => x.StartsWith("charset="))
                ?.Split('=')
                .Last();
            if (string.IsNullOrWhiteSpace(charset))
                return null;
            try
            {
                var e = Encoding.GetEncoding(charset);
                return e;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <summary>
        /// Encode されたボディーをデコード
        /// </summary>
        /// <param name="body">HTTP メッセージボディー</param>
        /// <param name="encodings">Transfer-Encoding, Content-Encoding に指定されたエンコーディング</param>
        /// <returns>デコードされたボディー</returns>
        private static byte[] Decode(this byte[] body, IEnumerable<string> encodings)
        {
            if (body == null)
                return null;

            using (var stream = encodings
                .Reverse()
                .Aggregate((Stream)new MemoryStream(body), (source, encoding) =>
                {
                    switch (encoding)
                    {
                        case "chunked":
                            return source.Dechunk();
                        case "deflate":
                            return new DeflateStream(source, CompressionMode.Decompress);
                        case "gzip":
                            return new GZipStream(source, CompressionMode.Decompress);
                        default:
                            throw new NotSupportedException($"'{encoding}' is not a supported encoding.");
                    }
                }))
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// chunked エンコーディングをデコード
        /// </summary>
        /// <param name="source">対象のストリーム</param>
        /// <returns>デコードされたストリーム</returns>
        private static Stream Dechunk(this Stream source)
        {
            using (source)
            using (var c = new ChunkedBodyParser(true, int.MaxValue, true))
            {
                source.Position = 0;
                int b;
                while (-1 != (b = source.ReadByte()))
                    c.WriteByte((byte)b);
                return new MemoryStream(c.Body);
            }
        }
    }
}
