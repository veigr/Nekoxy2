namespace Nekoxy2.ApplicationLayer.ProtocolReaders.Http2.Hpack
{
    /// <summary>
    /// バイナリーヘッダーフィールド表現
    /// </summary>
    /// <remarks>
    /// RFC7541 6
    /// </remarks>
    internal sealed class HeaderField
    {
        /// <summary>
        /// インデックス更新を伴うかどうか<
        /// </summary>
        public bool IsIndexing { get; }

        /// <summary>
        /// インデックス
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// ヘッダー名
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// ヘッダー値
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// インデックスヘッダーフィールド表現からインスタンスを作成
        /// </summary>
        /// <param name="index">インデックス</param>
        public HeaderField(int index)
        {
            this.IsIndexing = false;
            this.Index = index;
        }

        /// <summary>
        /// ヘッダー名インデックス済みのリテラルヘッダーフィールド表現からインスタンスを作成
        /// </summary>
        /// <param name="isIndexing">インデックス更新を伴うかどうか</param>
        /// <param name="index">ヘッダー名のインデックス</param>
        /// <param name="value">ヘッダー値</param>
        public HeaderField(bool isIndexing, int index, string value)
        {
            this.IsIndexing = isIndexing;
            this.Index = index;
            this.Name = "";
            this.Value = value;
        }

        /// <summary>
        /// リテラルヘッダーフィールド表現からインスタンスを作成
        /// </summary>
        /// <param name="isIndexing">インデックス更新を伴うかどうか</param>
        /// <param name="name">ヘッダー名</param>
        /// <param name="value">ヘッダー値</param>
        public HeaderField(bool isIndexing, string name, string value)
        {
            this.IsIndexing = isIndexing;
            this.Index = 0;
            this.Name = name;
            this.Value = value;
        }
    }
}
