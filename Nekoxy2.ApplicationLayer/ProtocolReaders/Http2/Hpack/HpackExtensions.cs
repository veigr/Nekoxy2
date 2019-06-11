using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nekoxy2.ApplicationLayer.ProtocolReaders.Http2.Hpack
{
    internal static partial class HpackExtensions
    {
        /// <summary>
        /// 整数表現を解析
        /// </summary>
        /// <param name="source">読み取り元バイト配列</param>
        /// <param name="startIndex">読み取り開始インデックス</param>
        /// <param name="mask">プレフィクス抽出用マスク</param>
        /// <param name="length">読み取ったバイト数</param>
        /// <returns>数値表現</returns>
        public static int DecodeInteger(this byte[] source, int startIndex, byte mask, out int length)
        {
            var index = startIndex;
            var first = source[index++] & mask;
            if (first < mask)
            {
                length = 1;
                return first;
            }

            var result = 0;
            var order = 0;
            var isContinue = true;
            while (isContinue)
            {
                var current = source[index++];
                result += (current & 0b01111111) * (int)Math.Pow(2, order);
                order += 7;
                isContinue = current.HasFlag(0b10000000);
            }
            length = order / 7 + 1;
            return result + first;
        }

        /// <summary>
        /// 文字列リテラル表現を解析
        /// </summary>
        /// <param name="source">読み取り元バイト配列</param>
        /// <param name="startIndex">読み取り開始インデックス</param>
        /// <param name="length">読み取ったバイト数</param>
        /// <returns>文字列リテラル表現</returns>
        public static string DecodeString(this byte[] source, int startIndex, out int length)
        {
            var isHuffman = source[startIndex].HasFlag(0b10000000);
            var stringLength = source.DecodeInteger(startIndex, 0b01111111, out var lengthSize);
            length = lengthSize + stringLength;
            if (!isHuffman)
            {
                return Encoding.ASCII.GetString(source, startIndex + 1, stringLength);
            }
            else
            {
                var target = source.Skip(startIndex + 1).Take(stringLength).ToArray();
                var decoded = HuffmanDecoder.Decode(target);
                return Encoding.ASCII.GetString(decoded);
            }
        }

        /// <summary>
        /// リテラルヘッダーフィールド表現を解析
        /// </summary>
        /// <param name="header">結合されたヘッダーブロックフラグメント</param>
        /// <param name="startIndex">読み取り開始インデックス</param>
        /// <param name="prefixMask">バイナリーフォーマットを識別するプレフィクス抽出用マスク</param>
        /// <param name="endIndex">読み取り終了後インデックス</param>
        /// <returns>リテラルヘッダーフィールド表現</returns>
        public static HeaderField ParseLiteralHeaderField(this byte[] header, int startIndex, byte prefixMask, bool isIndexing, out int endIndex)
        {
            var i = startIndex;
            var index = header.DecodeInteger(i - 1, prefixMask, out var indexLength);
            i += indexLength - 1;
            if (index != 0)
            {
                var value = header.DecodeString(i, out var valueLength);
                i += valueLength;
                endIndex = i;
                return new HeaderField(isIndexing, index, value);
            }
            else
            {
                var name = header.DecodeString(i, out var nameLength);
                i += nameLength;
                var value = header.DecodeString(i, out var valueLength);
                i += valueLength;
                endIndex = i;
                return new HeaderField(isIndexing, name, value);
            }
        }

        /// <summary>
        /// 指定した名前のヘッダーの最初の値を取得
        /// </summary>
        /// <param name="headers">ヘッダー</param>
        /// <param name="name">ヘッダー名</param>
        /// <returns>最初のヘッダー値</returns>
        public static string GetFirstValue(this IReadOnlyList<(string Name, string Value)> headers, string name)
            => headers.FirstOrDefault(x => x.Name == name).Value;

    }
}
