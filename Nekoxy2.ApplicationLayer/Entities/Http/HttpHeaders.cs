using Nekoxy2.Spi.Entities.Http;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Nekoxy2.ApplicationLayer.Entities.Http
{
    /// <summary>
    /// HTTP ヘッダー
    /// </summary>
    internal sealed partial class HttpHeaders : IReadOnlyHttpHeaders
    {
        /// <summary>
        /// ヘッダーフィールドリスト
        /// </summary>
        private readonly IList<(string Name, string Value)> headers;

        /// <summary>
        /// ヘッダーフィールドリスト数
        /// </summary>
        public int Count => this.headers.Count;

        /// <summary>
        /// Transfer-Encoding ヘッダに chunked が指定されているかどうか
        /// </summary>
        public bool IsChunked => this.TransferEncoding.Exists
                            && this.TransferEncoding.ContainsValue("chunked");

        /// <summary>
        /// Connection ヘッダに close が設定されているかどうか
        /// </summary>
        public bool IsClose => this.Connection.Exists
                            && this.Connection.ContainsValue("close");

        /// <summary>
        /// ソース文字列
        /// </summary>
        public string Source { get; }

        internal bool IsValid { get; }

        /// <summary>
        /// <see cref="IsValid"/> が false の場合、その理由
        /// </summary>
        internal string InvalidReason { get; }

        private readonly object lockObject = new object();

        private HttpHeaders(string source, IList<(string Name, string Value)> headers)
        {
            this.Source = source;
            this.headers = headers ?? new List<(string Name, string Value)>();
            this.IsValid = (headers != null);
        }

        private HttpHeaders(string source, Exception e)
        {
            this.Source = source;
            this.IsValid = false;
            this.InvalidReason = e.Message;
        }

        public IEnumerator<(string Name, string Value)> GetEnumerator()
            => this.headers.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => this.headers.GetEnumerator();

        public override string ToString()
        {
            if (!this.IsValid)
                return this.Source;

            return this.Any()
                ? this.Select(x => $"{x.Name}: {x.Value}\r\n").Aggregate((a, b) => a + b) + "\r\n"
                : "\r\n";
        }

        /// <summary>
        /// 操作による改変前のヘッダー
        /// </summary>
        /// <returns></returns>
        public HttpHeaders GetOrigin()
        {
            TryParse(this.Source, out var headers, false);
            return headers;
        }

        internal void AddOrUpdate((string Name, string Value) header)
            => this.headers.AddOrUpdate(header);

        /// <summary>
        /// Via ヘッダーを追加
        /// </summary>
        /// <param name="protocol">送信されてきたプロトコル</param>
        /// <param name="version">送信されてきたプロトコルのバージョン</param>
        /// <param name="receivedBy">回送者の host, port または仮名</param>
        internal void AddVia(string protocol, Version version, string receivedBy)
        {
            lock (this.lockObject)
            {
                var exists = this.GetFirstValue("Via");
                var via = (exists == null) ? "" : exists + ", ";
                if (!string.IsNullOrEmpty(protocol) && protocol.ToUpper() != "HTTP")
                    via += protocol + "/";
                via += $"{version.ToString()} {receivedBy}";
                this.headers.AddOrUpdate(("Via", via));
            }
        }

        /// <summary>
        /// 現在日時で Date ヘッダーを追加
        /// </summary>
        internal void AddDate()
        {
            lock (this.lockObject)
            {
                if (this.HasHeader("Date"))
                    return;
                var dateValue = Now().ToString(@"ddd, dd MMM yyyy HH:mm:ss G\MT",
                    CultureInfo.GetCultureInfo("en-US").DateTimeFormat);
                this.headers.Insert(0, ("Date", dateValue));
            }
        }

        /// <summary>
        /// Connection ヘッダーおよびヘッダー値で指定されたヘッダー群を削除
        /// RFC 7230 6.1
        /// </summary>
        internal void RemoveConnectionHeaders()
        {
            lock (this.lockObject)
            {
                if (!this.Connection.Exists)
                    return;

                foreach (var header in this.Connection)
                {
                    this.headers.RemoveByName(header);
                }
                this.Connection.Value = this.IsClose ? "close" : null;
            }
        }

        /// <summary>
        /// 解析
        /// </summary>
        /// <param name="source">ソース文字列</param>
        /// <param name="headers">作成されたヘッダー</param>
        /// <param name="normalize">重複除去等を行うかどうか</param>
        /// <returns>解析成否</returns>
        public static bool TryParse(string source, out HttpHeaders headers, bool normalize = true)
        {
            try
            {
                var headersList = source.ParseToKVPList();

                if (normalize)
                {
                    headersList = headersList.RemoveDuplicate();
                    // RFC7230 A.1.2 Proxy-Connection は送信しないことが推奨されるため削除
                    headersList.RemoveByName("Proxy-Connection");
                }

                headers = new HttpHeaders(source, headersList);
                headers.ParseHeaders();
            }
            catch (Exception e)
            {
                headers = new HttpHeaders(source, e);
            }
            return headers.IsValid;
        }

        public static HttpHeaders Empty
        {
            get
            {
                TryParse("", out var headers);
                return headers;
            }
        }

        /// <summary>
        /// Date ヘッダーに追加する日時。
        /// テスト実行時に置換可能。
        /// </summary>
        internal static Func<DateTimeOffset> Now = () => DateTimeOffset.Now;
    }
}
