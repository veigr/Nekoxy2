using System;
using System.Collections.Generic;

namespace Nekoxy2.ApplicationLayer.ProtocolReaders.Http2.Hpack
{
    /// <summary>
    /// HPACK デコーダー
    /// </summary>
    /// <remarks>
    /// RFC7541
    /// ・Connection 単位＆リクエスト・レスポンス各方向ごとに動的テーブルは分離される RFC7541 2.2
    /// ・ヘッダーフィールドリストの HTTP2 -> HTTP1.1 変換はここでは行わない
    /// ・RFC7541 6.2.3 もし書き換えをサポートする場合は Without Indexing と Never Indexed の区別は
    /// 　デコード→書き換え→エンコードの間保持する必要がある。(Never は元の表現に戻せなければならない)
    /// </remarks>
    internal sealed class HpackDecoder
    {
        /// <summary>
        /// インデックステーブル
        /// </summary>
        internal IndexedTable Table { get; } = new IndexedTable();

        /// <summary>
        /// 結合されたヘッダーブロックフラグメントをデコードし、ヘッダーフィールドリストを作成
        /// </summary>
        /// <param name="header">結合されたヘッダーブロックフラグメント</param>
        /// <returns>ヘッダーフィールドリスト</returns>
        public IReadOnlyList<(string Name, string Value)> Decode(byte[] header)
        {
            var list = new List<(string Name, string Value)>();
            var i = 0;
            while (i < header.Length)
            {
                var first = header[i++];
                if ((first & (indexedPrefixMask ^ 0xff)) == (byte)BinaryFormatPrefix.Indexed)
                {
                    // インデックスヘッダーフィールド表現
                    var index = header.DecodeInteger(i - 1, indexedPrefixMask, out var indexLength);
                    i += indexLength - 1;
                    var field = new HeaderField(index);
                    list.Add(this.Table.Decode(field));
                }
                else if ((first & (withIndexingPrefixMask ^ 0xff)) == (byte)BinaryFormatPrefix.WithIndexing)
                {
                    // インデックス更新を伴うリテラルヘッダーフィールド表現
                    var field = header.ParseLiteralHeaderField(i, withIndexingPrefixMask, true, out i);
                    list.Add(this.Table.Decode(field));
                }
                else if ((first & (withoutIndexingPrefixMask ^ 0xff)) == (byte)BinaryFormatPrefix.WithoutIndexing
                || (first & (withoutIndexingPrefixMask ^ 0xff)) == (byte)BinaryFormatPrefix.FieldNeverIndexed)
                {
                    // インデックス更新を伴わないリテラルヘッダーフィールド表現・インデックスされないリテラルヘッダーフィールド表現
                    var field = header.ParseLiteralHeaderField(i, withoutIndexingPrefixMask, false, out i);
                    list.Add(this.Table.Decode(field));
                }
                else if ((first & (dynamicTableSizeUpdatePrefixMask ^ 0xff)) == (byte)BinaryFormatPrefix.DynamicTableSizeUpdate)
                {
                    // 動的テーブルサイズ更新
                    var size = header.DecodeInteger(i - 1, dynamicTableSizeUpdatePrefixMask, out var sizeLength);
                    i += sizeLength - 1;
                    this.Table.UpdateDynamicTableSize((uint)size);
                }
            }
            return list;
        }

        /// <summary>
        /// 動的テーブルサイズを更新
        /// </summary>
        /// <param name="size"></param>
        public void UpdateDynamicTableSize(uint size)
            => this.Table.UpdateDynamicTableSize(size);

        /// <summary>
        /// インデックスヘッダーフィールド表現を識別するプレフィックス抽出用マスク
        /// </summary>
        private static readonly byte indexedPrefixMask = 0b01111111;

        /// <summary>
        /// インデックス更新を伴うリテラルヘッダーフィールド表現を識別するプレフィックス抽出用マスク
        /// </summary>
        private static readonly byte withIndexingPrefixMask = 0b00111111;

        /// <summary>
        /// インデックス更新を伴わないリテラルヘッダーフィールド表現・インデックスされないリテラルヘッダーフィールド表現を識別するプレフィックス抽出用マスク
        /// </summary>
        private static readonly byte withoutIndexingPrefixMask = 0b00001111;

        /// <summary>
        /// 動的テーブルサイズ更新を識別するプレフィックス抽出用マスク
        /// </summary>
        private static readonly byte dynamicTableSizeUpdatePrefixMask = 0b00011111;

        /// <summary>
        /// バイナリーフォーマットを識別するプレフィクス
        /// </summary>
        /// <remarks>
        /// RFC7541 6
        /// </remarks>
        [Flags]
        private enum BinaryFormatPrefix : byte
        {
            /// <summary>
            /// インデックスヘッダーフィールド表現
            /// </summary>
            /// <remarks>
            /// RFC7541 6.1
            /// </remarks>
            Indexed = 0b10000000,
            /// <summary>
            /// インデックス更新を伴うリテラルヘッダーフィールド表現
            /// </summary>
            /// <remarks>
            /// RFC7541 6.2.1
            /// </remarks>
            WithIndexing = 0b01000000,
            /// <summary>
            /// インデックス更新を伴わないリテラルヘッダーフィールド表現
            /// </summary>
            /// <remarks>
            /// RFC7541 6.2.2
            /// </remarks>
            WithoutIndexing = 0b00000000,
            /// <summary>
            /// インデックスされないリテラルヘッダーフィールド表現
            /// </summary>
            /// <remarks>
            /// RFC7541 6.2.3
            /// </remarks>
            FieldNeverIndexed = 0b00010000,
            /// <summary>
            /// 動的テーブルサイズ更新
            /// </summary>
            /// <remarks>
            /// RFC7541 6.3
            /// </remarks>
            DynamicTableSizeUpdate = 0b00100000,
        }
    }
}
