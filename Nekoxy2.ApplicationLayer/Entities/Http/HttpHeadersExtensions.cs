using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace Nekoxy2.ApplicationLayer.Entities.Http
{
    internal static partial class HttpHeadersExtensions
    {
        /// <summary>
        /// 指定された名前のヘッダーを持つかどうか
        /// </summary>
        /// <param name="headers">HTTP ヘッダー</param>
        /// <param name="name">ヘッダー名</param>
        /// <returns>指定された名前のヘッダーを持つかどうか</returns>
        public static bool HasHeader(this IEnumerable<(string Name, string Value)> headers, string name)
            => headers.Any(kvp => kvp.Name.ToLower() == name.ToLower());

        /// <summary>
        /// 指定された名前のヘッダーの最初の値を取得
        /// </summary>
        /// <param name="headers">HTTP ヘッダー</param>
        /// <param name="name">ヘッダー名</param>
        /// <returns>最初のヘッダー値</returns>
        public static string GetFirstValue(this IEnumerable<(string Name, string Value)> headers, string name)
            => headers.HasHeader(name)
            ? headers.First(kvp => kvp.Name.ToLower() == name.ToLower()).Value
            : null;

        /// <summary>
        /// 指定された名前のヘッダーの値のリストを取得
        /// </summary>
        /// <param name="headers">HTTP ヘッダー</param>
        /// <param name="name">ヘッダー名</param>
        /// <returns>ヘッダー値のリスト</returns>
        public static IReadOnlyList<string> GetValues(this IEnumerable<(string Name, string Value)> headers, string name)
            => headers.HasHeader(name)
            ? headers.Where(kvp => kvp.Name.ToLower() == name.ToLower()).Select(x => x.Value).ToArray()
            : new string[0];

        /// <summary>
        /// 指定された名前のヘッダーフィールドを取得
        /// </summary>
        /// <param name="headers">HTTP ヘッダー</param>
        /// <param name="name">ヘッダー名</param>
        /// <returns>ヘッダーフィールド</returns>
        public static (string Name, string Value) GetHeaderField(this IEnumerable<(string Name, string Value)> headers, string name)
            => headers.FirstOrDefault(x => x.Name.ToLower() == name.ToLower());

        /// <summary>
        /// 指定された名前のヘッダーフィールドの値を設定
        /// </summary>
        /// <param name="headers">HTTP ヘッダー</param>
        /// <param name="name">ヘッダー名</param>
        /// <param name="value">設定する値</param>
        public static void SetValue(this IList<(string Name, string Value)> headers, string name, string value)
        {
            var target = headers.GetHeaderField(name);
            var index = headers.IndexOf(target);
            if(index < 0)
            {
                headers.Add((name, value));
            }
            else
            {
                headers.RemoveAt(index);
                target.Value = value;
                headers.Insert(index, target);
            }
        }

        /// <summary>
        /// 文字列を指定されたセパレーターで分割
        /// </summary>
        /// <param name="value">対象の文字列</param>
        /// <param name="separator">セパレーター</param>
        /// <returns>分割された文字列シーケンス</returns>
        public static IEnumerable<string> SplitValue(this string value, string separator = ",")
            => value
                ?.Split(new[] { separator }, StringSplitOptions.None)
                ?.Select(x => x?.Trim());

        /// <summary>
        /// Authority に一致するパターン
        /// </summary>
        private static readonly Regex authorityPattern
            = new Regex(@"^(.*@)?([a-zA-Z0-9-\.]+)(:(\d{1,5}))?$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        /// <summary>
        /// Host ヘッダーを Authority として解釈
        /// </summary>
        /// <param name="hostHeader">Host ヘッダー</param>
        /// <returns>解釈された Authority</returns>
        public static (string UserInfo, string Host, ushort? Port) ParseAuthority(this HttpHeaderField<string> hostHeader)
        {
            if (!hostHeader.Exists) return default;
            return hostHeader.Value.ParseAuthority();
        }

        /// <summary>
        /// Host ヘッダー値を Authority として解釈
        /// </summary>
        /// <param name="hostHeader">Host ヘッダー値</param>
        /// <returns>解釈された Authority</returns>
        private static (string UserInfo, string Host, ushort? Port) ParseAuthority(this string hostHeaderValue)
        {
            var match = authorityPattern.Match(hostHeaderValue);
            if (!match.Success) return default;
            var groups = match.Groups;
            ushort.TryParse(groups[4].Value, out var port);
            return (groups[1].Value, groups[2].Value, port);
        }

        /// <summary>
        /// Host ヘッダー値から左端がワイルドカード化されたドメイン名を取得
        /// </summary>
        /// <param name="hostHeaderValue">Host ヘッダー値</param>
        /// <returns>左端がワイルドカード化されたドメイン名</returns>
        public static string ToWildcardDomain(this string hostHeaderValue)
        {
            var host = hostHeaderValue.ParseAuthority().Host;
            if (IPAddress.TryParse(host, out var _))
                return host;    // IPアドレスの場合はそのまま
            var domains = host.Split(new[] { '.' });
            if (domains.Length <= 2)
                return host;
            // 最左ラベルだけ置き換え
            return string.Join(".", new[] { "*" }.Concat(domains.Skip(1)));
        }

        /// <summary>
        /// 最終 CR に合致するパターン
        /// </summary>
        private static Regex lastCr = new Regex(@"\r$", RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// 不正なフィールド空白に合致するパターン
        /// </summary>
        private static Regex invalidFieldSplitPattern = new Regex(@"^([^\s:]+)\s+:(.*)", RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// 複数行ヘッダー値(廃止)の2行目以降に合致するパターン
        /// </summary>
        private static Regex obsFoldPatter = new Regex(@"^\s+(.*)$", RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// ヘッダーフィールドに合致するパターン
        /// </summary>
        private static Regex field = new Regex(@"^([^\s]+):(.*)$", RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// HTTP ヘッダー文字列を <see cref="KeyValuePair{TKey, TValue}"/> として解釈
        /// </summary>
        /// <param name="headers">HTTP ヘッダー文字列</param>
        /// <returns>解釈された <see cref="KeyValuePair{TKey, TValue}"/></returns>
        public static IList<(string Name, string Value)> ParseToKVPList(this string headers)
        {
            // RFC7230 3.2.4
            // field-nameと:の間のスペースは除去する
            // obs-fold(行頭にSP or HTABを入れて改行とみなす)があった場合502かSPに置き換え → 置き換えにする

            var fields = headers
                .Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => lastCr.Replace(x, ""))
                .Where(x => !string.IsNullOrEmpty(x));
            var splitFixed = fields.Select(x => invalidFieldSplitPattern.Replace(x, "$1:$2"));
            var obsFoldFixed = splitFixed.Aggregate(new List<string>(), (a, b) =>
            {
                if (obsFoldPatter.IsMatch(b))
                    a[a.Count() - 1] += obsFoldPatter.Replace(b, " $1");
                else
                    a.Add(b);
                return a;
            });
            return obsFoldFixed.Select(x =>
            {
                var match = field.Match(x);
                if (!match.Success)
                    throw new InvalidHttpHeadersException("Invalid Header Field");
                var group = match.Groups;
                return (group[1].Value, group[2].Value.Trim());
            }).ToList();
        }

        /// <summary>
        /// 重複したヘッダーフィールドをマージして除去
        /// </summary>
        /// <param name="headers">HTTP ヘッダー</param>
        /// <returns>重複除去されたヘッダー</returns>
        public static IList<(string Name, string Value)> RemoveDuplicate(this IList<(string Name, string Value)> headers)
        {
            return headers.GroupBy(x => x.Name.ToLower())
                .SelectMany(Merge)
                .ToList();

        }

        /// <summary>
        /// 重複したヘッダーフィールドをマージ
        /// </summary>
        /// <param name="target">重複ヘッダー情報</param>
        /// <returns>マージされたヘッダー</returns>
        private static IEnumerable<(string Name, string Value)> Merge(IGrouping<string, (string Name, string Value)> target)
        {
            if (target.Count() < 2)
                return target.ToArray();

            // RFC7230 3.2.2
            // 重複ヘッダはマージしてもしなくても良いが、送信してはならない
            // Set-Cookie ヘッダは重複する可能性がある
            if (target.Key == "set-cookie")
                return target.ToArray();

            // RFC7230 5.4
            // Host の重複は 400
            if (target.Key == "host")
                throw new InvalidHttpHeadersException("Multiple Host Header");

            // RFC7230 3.2.2
            // ContentLength が複数ある場合、メッセージを却下するか妥当な値に修正しなければならない
            // →同じ値ならばマージ
            // →異なる値ならば回復不能なエラー
            // →Request→400→Close
            // →Response→サーバーは切断＆クライアントへは502
            if (target.Key == "Content-Length")
            {
                var distinct = target.Select(x => x.Value).Distinct();
                if (1 < distinct.Count())
                    throw new InvalidHttpHeadersException("Invalid Content-Length Header");
                return new[] { (target.First().Name, distinct.Single()) };
            }

            var values = string.Join(", ", target.Select(x => x.Value).Distinct());
            return new[] { (target.First().Name, values) };
        }

        /// <summary>
        /// 指定した名前のヘッダーフィールドを削除
        /// </summary>
        /// <param name="headers">ヘッダー</param>
        /// <param name="name">ヘッダー名</param>
        public static void RemoveByName(this IList<(string Name, string Value)> headers, string name)
        {
            lock (headers)
            {
                var target = headers.GetHeaderField(name);
                if (!target.Equals(default))
                    headers.Remove(target);
            }
        }

        /// <summary>
        /// 指定したヘッダーフィールドで追加または更新
        /// </summary>
        /// <param name="headers">ヘッダー</param>
        /// <param name="header">ヘッダーフィールド</param>
        public static void AddOrUpdate(this IList<(string Name, string Value)> headers, (string Name, string Value) header)
        {
            lock (headers)
            {
                var index = headers.IndexOf(headers.FirstOrDefault(x => x.Name == header.Name));
                if (-1 < index)
                {
                    headers.RemoveAt(index);
                    if (header.Value != null)
                        headers.Insert(index, header);
                }
                else
                {
                    headers.Add(header);
                }
            }
        }
    }
}
