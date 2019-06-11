using System.Collections.Generic;
using System.Linq;

namespace Nekoxy2.ApplicationLayer.ProtocolReaders.Http2.Hpack
{
    /// <summary>
    /// 動的テーブル
    /// </summary>
    /// <remarks>
    /// RFC7541 2.3.2
    /// </remarks>
    internal sealed class DynamicTable
    {
        /// <summary>
        /// 動的テーブルの開始インデックス。
        /// 動的テーブルのインデックスは静的テーブルのインデックスの続きとなるため。
        /// </summary>
        private readonly int baseIndex;

        /// <summary>
        /// 動的テーブルサイズ
        /// </summary>
        /// <remarks>
        /// 初期値: https://www.iana.org/assignments/http2-parameters/http2-parameters.xhtml#settings
        /// </remarks>
        internal uint Size { get; private set; } = 4096;

        /// <summary>
        /// テーブルのエントリーリスト
        /// </summary>
        internal IList<(string Name, string Value)> Table { get; }
            = new List<(string Name, string Value)>();

        /// <summary>
        /// インデックスを指定してエントリーを取得
        /// </summary>
        /// <param name="index">インデックス</param>
        /// <returns>エントリー</returns>
        public (string Name, string Value) this[int index]
            => this.Table[index - this.baseIndex];

        /// <summary>
        /// 開始インデックスを指定してインスタンスを作成
        /// </summary>
        /// <param name="baseIndex">開始インデックス</param>
        public DynamicTable(int baseIndex)
            => this.baseIndex = baseIndex;

        /// <summary>
        /// ヘッダー名とヘッダー値を指定してエントリーを追加
        /// </summary>
        /// <param name="name">ヘッダー名</param>
        /// <param name="value">ヘッダー値</param>
        public void Add(string name, string value)
        {
            this.Table.Insert(0, (name, value));
            this.Trim();
        }

        /// <summary>
        /// ヘッダー名のインデックスとヘッダー値を指定してエントリーを追加
        /// </summary>
        /// <param name="index">ヘッダー名のインデックス</param>
        /// <param name="value">ヘッダー値</param>
        /// <returns>ヘッダー名</returns>
        public string Add(int index, string value)
        {
            var name = this[index].Name;
            this.Table.Insert(0, (name, value));
            this.Trim();
            return name;
        }

        /// <summary>
        /// テーブルサイズを更新
        /// </summary>
        /// <param name="size">新しいサイズ</param>
        public void UpdateTableSize(uint size)
        {
            this.Size = size;
            this.Trim();
        }

        /// <summary>
        /// テーブルサイズに合わせてエントリーを削除
        /// </summary>
        private void Trim()
        {
            while (this.Size < this.Table.TableSize())
            {
                this.Table.RemoveAt(this.Table.Count - 1);
            }
        }
    }

    internal static partial class DynamicTableExtentions
    {
        /// <summary>
        /// テーブルサイズを計算
        /// </summary>
        /// <param name="table">テーブル</param>
        /// <returns>サイズ</returns>
        /// <remarks>
        /// RFC7541 4.1
        /// </remarks>
        public static int TableSize(this IList<(string Name, string Value)> table)
            => table.Sum(x => x.Name.Length + x.Value.Length + 32);
    }
}
