using System.Collections.Generic;
using System.Text;

namespace Nekoxy2.Entities.Http.Extensions
{
    public static class DecodeExtensions
    {
        /// <summary>
        /// メッセージボディを指定した文字エンコーディングで文字列として取得。
        /// Transfer-Encoding, Content-Encoding がある場合は同時にデコードします。
        /// </summary>
        /// <param name="message">HTTP メッセージ</param>
        /// <param name="encoding">文字エンコーディング</param>
        /// <returns>メッセージボディー文字列</returns>
        public static string GetBodyAsString(this IReadOnlyHttpMessage message, Encoding encoding = null)
            => ApplicationLayer.Entities.Http.DecodeExtensions.GetBodyAsString(message, encoding);

        /// <summary>
        /// Content-Encoding をデコード。
        /// Transfer-Encoding がある場合は同時にデコードします。
        /// </summary>
        /// <param name="message">HTTP メッセージ</param>
        /// <returns>Content-Encoding をデコードされたメッセージボディー</returns>
        public static IReadOnlyList<byte> GetContentDecodedBody(this IReadOnlyHttpMessage message)
            => ApplicationLayer.Entities.Http.DecodeExtensions.GetContentDecodedBody(message);

        /// <summary>
        /// Transfer-Encoding をデコード
        /// </summary>
        /// <param name="message">HTTP メッセージ</param>
        /// <returns>Transfer-Encoding をデコードされたメッセージボディー</returns>
        public static IReadOnlyList<byte> GetTransferDecodedBody(this IReadOnlyHttpMessage message)
            => ApplicationLayer.Entities.Http.DecodeExtensions.GetTransferDecodedBody(message);
    }
}
