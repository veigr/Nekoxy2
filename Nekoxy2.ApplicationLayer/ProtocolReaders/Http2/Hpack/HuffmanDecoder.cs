using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Nekoxy2.ApplicationLayer.ProtocolReaders.Http2.Hpack
{
    /// <summary>
    /// ハフマンコーディングデコーダー
    /// </summary>
    internal static class HuffmanDecoder
    {
        /// <summary>
        /// ハフマンコーディングされたバイト配列をデコード
        /// </summary>
        /// <param name="source">ハフマンコーディングされたバイト配列</param>
        /// <returns>デコードされたバイト配列</returns>
        public static byte[] Decode(byte[] source)
        {
            var result = new List<byte>();
            var node = HuffmanTree.Root;
            var bits = new Queue<bool>(source.SelectMany(x => x.ToBits()));
            while (0 < bits.Count)
            {
                var bit = bits.Dequeue();
                node = bit ? node.Child1 : node.Child0;
                if (node.Symbol != null)
                {
                    if (node.Symbol == 256)
                        throw new InvalidDataException("EOS symbol MUST be decoding error.");   // RFC7541 5.2
                    result.Add((byte)node.Symbol);
                    node = HuffmanTree.Root;
                }
            }
            return result.ToArray();
        }
    }

    internal static partial class HuffmanDecoderExtensions
    {
        /// <summary>
        /// バイトをビット配列に変換
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool[] ToBits(this byte value)
        {
            var result = new bool[8];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = value.HasFlag((byte)(1 << (7 - i)));
            }
            return result;
        }
    }
}
