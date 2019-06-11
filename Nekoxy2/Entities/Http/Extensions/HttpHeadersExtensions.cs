using System.Collections.Generic;

namespace Nekoxy2.Entities.Http.Extensions
{
    public static partial class HttpHeadersExtensions
    {
        /// <summary>
        /// 指定された名前のヘッダーを持つかどうか
        /// </summary>
        /// <param name="headers">HTTP ヘッダー</param>
        /// <param name="name">ヘッダー名</param>
        /// <returns>指定された名前のヘッダーを持つかどうか</returns>
        public static bool HasHeader(this IReadOnlyHttpHeaders headers, string name)
            => ApplicationLayer.Entities.Http.HttpHeadersExtensions.HasHeader(headers, name);

        /// <summary>
        /// 指定された名前のヘッダーの最初の値を取得
        /// </summary>
        /// <param name="headers">HTTP ヘッダー</param>
        /// <param name="name">ヘッダー名</param>
        /// <returns>最初のヘッダー値</returns>
        public static string GetFirstValue(this IReadOnlyHttpHeaders headers, string name)
            => ApplicationLayer.Entities.Http.HttpHeadersExtensions.GetFirstValue(headers, name);

        /// <summary>
        /// 指定された名前のヘッダーの値のリストを取得
        /// </summary>
        /// <param name="headers">HTTP ヘッダー</param>
        /// <param name="name">ヘッダー名</param>
        /// <returns>ヘッダー値のリスト</returns>
        public static IReadOnlyList<string> GetValues(this IReadOnlyHttpHeaders headers, string name)
            => ApplicationLayer.Entities.Http.HttpHeadersExtensions.GetValues(headers, name);

        /// <summary>
        /// 指定された名前のヘッダーフィールドを取得
        /// </summary>
        /// <param name="headers">HTTP ヘッダー</param>
        /// <param name="name">ヘッダー名</param>
        /// <returns>ヘッダーフィールド</returns>
        public static (string Name, string Value) GetHeaderField(this IEnumerable<(string Name, string Value)> headers, string name)
            => ApplicationLayer.Entities.Http.HttpHeadersExtensions.GetHeaderField(headers, name);

        /// <summary>
        /// 指定された名前のヘッダーフィールドの値を設定
        /// </summary>
        /// <param name="headers">HTTP ヘッダー</param>
        /// <param name="name">ヘッダー名</param>
        /// <param name="value">設定する値</param>
        public static void SetValue(this IList<(string Name, string Value)> headers, string name, string value)
            => ApplicationLayer.Entities.Http.HttpHeadersExtensions.SetValue(headers, name, value);

        /// <summary>
        /// Host ヘッダーの値を取得
        /// </summary>
        /// <param name="request">HTTP リクエスト</param>
        /// <returns>Host ヘッダー値</returns>
        public static string GetHost(this IReadOnlyHttpRequest request)
            => request?.Headers?.GetFirstValue("Host");

        /// <summary>
        /// Host ヘッダーの値を取得
        /// </summary>
        /// <param name="session">HTTP リクエスト・レスポンスペア</param>
        /// <returns>Host ヘッダー値</returns>
        public static string GetHost(this IReadOnlySession session)
            => session?.Request?.Headers?.GetFirstValue("Host");

        /// <summary>
        /// リクエストターゲットを取得
        /// </summary>
        /// <param name="request">HTTP リクエスト</param>
        /// <returns>リクエストターゲット</returns>
        public static string GetRequestTarget(this IReadOnlyHttpRequest request)
            => request?.RequestLine?.RequestTarget;

        /// <summary>
        /// リクエストターゲットを取得
        /// </summary>
        /// <param name="session">HTTP リクエスト・レスポンスペア</param>
        /// <returns>リクエストターゲット</returns>
        public static string GetRequestTarget(this IReadOnlySession session)
            => session?.Request?.RequestLine?.RequestTarget;
    }
}
