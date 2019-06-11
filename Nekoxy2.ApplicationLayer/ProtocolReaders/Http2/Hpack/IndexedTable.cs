namespace Nekoxy2.ApplicationLayer.ProtocolReaders.Http2.Hpack
{
    /// <summary>
    /// インデックステーブル。
    /// 静的テーブルと動的テーブルを持ち、同じインデックスアドレス空間で結合。
    /// </summary>
    /// <remarks>
    /// RFC7541 2.3
    /// </remarks>
    internal sealed class IndexedTable
    {
        /// <summary>
        /// 動的テーブル
        /// </summary>
        internal DynamicTable DynamicTable { get; }
            = new DynamicTable(StaticTable.Entries.Count + 1);

        /// <summary>
        /// 解析されたバイナリーヘッダーフィールド表現からヘッダーフィールドを取得
        /// </summary>
        /// <param name="field">解析されたバイナリーヘッダーフィールド表現</param>
        /// <returns>ヘッダーフィールド</returns>
        public (string Name, string Value) Decode(HeaderField field)
        {
            if (0 < field.Index)
            {
                // インデックスされた名前
                if (StaticTable.Entries.TryGetValue(field.Index, out var staticField))
                {
                    // 静的テーブルのインデックス
                    if (field.IsIndexing)
                    {
                        this.DynamicTable.Add(staticField.Name, field.Value ?? staticField.Value);
                    }
                    // field.Value == null はインデックスヘッダーフィールド表現
                    return (staticField.Name, field.Value ?? staticField.Value);
                }
                else
                {
                    // 動的テーブルのインデックス
                    var (name, currentValue) = this.DynamicTable[field.Index];
                    if (field.IsIndexing)
                    {
                        name = this.DynamicTable.Add(field.Index, field.Value);
                    }
                    // field.Value == null はインデックスヘッダーフィールド表現
                    return (name, field.Value ?? currentValue);
                }
            }
            else
            {
                // 新しい名前
                if (field.IsIndexing)
                {
                    this.DynamicTable.Add(field.Name, field.Value);
                }
                return (field.Name, field.Value);
            }
        }

        /// <summary>
        /// 動的テーブルサイズを更新
        /// </summary>
        /// <param name="size">新しいサイズ</param>
        public void UpdateDynamicTableSize(uint size)
            => this.DynamicTable.UpdateTableSize(size);
    }
}
