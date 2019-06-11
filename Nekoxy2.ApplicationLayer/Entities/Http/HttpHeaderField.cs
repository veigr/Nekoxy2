using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Nekoxy2.ApplicationLayer.Entities.Http
{
    /// <summary>
    /// HTTP ヘッダーフィールド
    /// </summary>
    /// <typeparam name="T">ヘッダー値の型</typeparam>
    internal sealed class HttpHeaderField<T> : IEnumerable<T>
    {
        /// <summary>
        /// 所属する HTTP ヘッダー
        /// </summary>
        private readonly IList<(string Name, string Value)> headers;

        /// <summary>
        /// ヘッダー名
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 存在するかどうか
        /// </summary>
        public bool Exists
            => this.headers.HasHeader(this.Name);

        /// <summary>
        /// 基となる文字列
        /// </summary>
        public string RawValue
            => this.headers.GetFirstValue(this.Name);

        /// <summary>
        /// ヘッダー値リスト
        /// </summary>
        private IEnumerable<T> Values
            => this.Splitter(this.RawValue)?.Select(x => this.ToConverter(x));

        /// <summary>
        /// 最初のヘッダー値
        /// </summary>
        public T Value
        {
            get => (this.Values != null) ? this.Values.FirstOrDefault() : default;
            set => this.headers.AddOrUpdate((this.Name, this.FromConverter(value)));
        }

        /// <summary>
        /// ヘッダーフィールドのインスタンスを作成
        /// </summary>
        /// <param name="headers">所属する HTTP ヘッダー</param>
        /// <param name="name">ヘッダー名</param>
        /// <param name="splitter">ヘッダー値を分割する文字列</param>
        /// <param name="toConverter">ヘッダー値文字列を <see cref="T"/> 型に変換するコンバーター</param>
        /// <param name="fromConverter"><see cref="T"/> 型ヘッダー値を文字列に変換するコンバーター</param>
        public HttpHeaderField(
            IList<(string Name, string Value)> headers,
            string name,
            Func<string, IEnumerable<string>> splitter = null,
            Func<string, T> toConverter = null,
            Func<T, string> fromConverter = null)
        {
            this.headers = headers;
            this.Name = name;
            if (splitter != null)
                this.Splitter = splitter;
            if (toConverter != null)
                this.ToConverter = toConverter;
            if (fromConverter != null)
                this.FromConverter = fromConverter;
        }

        public IEnumerator<T> GetEnumerator()
            => this.Values?.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => this.Values?.GetEnumerator();

        public override string ToString()
            => $"{this.RawValue}";

        /// <summary>
        /// ヘッダー値を分割する文字列
        /// </summary>
        private Func<string, IEnumerable<string>> Splitter { get; }
            = value => value.SplitValue();

        /// <summary>
        /// ヘッダー値文字列を <see cref="T"/> 型に変換するコンバーター
        /// </summary>
        private Func<string, T> ToConverter { get; }
            = value =>
            {
                if (value is T t) return t;

                var type = typeof(T);

                var constructor = type.GetConstructor(new[] { typeof(string) });
                if (constructor != null)
                    return (T)constructor.Invoke(new[] { value });

                var parser = type.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
                if (parser != null)
                    return (T)parser.Invoke(null, new[] { value });

                throw new FormatException($"'{value}' can not convert to {type.FullName}");
            };

        /// <summary>
        /// <see cref="T"/> 型ヘッダー値を文字列に変換するコンバーター
        /// </summary>
        private Func<T, string> FromConverter { get; }
            = value => value?.ToString();

        /// <summary>
        /// 文字列への暗黙の型変換
        /// </summary>
        /// <param name="field">ヘッダーフィールド</param>
        public static explicit operator string(HttpHeaderField<T> field)
            => field.RawValue;

        /// <summary>
        /// <see cref="T"/> 型への暗黙の型変換
        /// </summary>
        /// <param name="field"></param>
        public static explicit operator T(HttpHeaderField<T> field)
            => field.FirstOrDefault();
    }

    internal static partial class HttpHeaderValueExtensions
    {
        /// <summary>
        /// ヘッダーフィールドが指定されたヘッダー値を持つかどうか
        /// </summary>
        /// <param name="source">ヘッダーフィールド</param>
        /// <param name="value">ヘッダー値</param>
        /// <returns>指定されたヘッダー値を持つかどうか</returns>
        public static bool ContainsValue(this HttpHeaderField<string> source, string value)
            => source.Select(x => x.ToLower()).Contains(value.ToLower());
    }
}
