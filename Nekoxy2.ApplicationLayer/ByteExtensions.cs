using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nekoxy2.ApplicationLayer
{
    internal static partial class ByteExtensions
    {
        public static bool HasFlag(this byte flags, byte flag)
            => (flags & flag) == flag;

        /// <summary>
        /// ビッグエンディアンで符号なし16ビット整数を解釈し変換
        /// </summary>
        /// <param name="source">対象のバイトシーケンス</param>
        /// <param name="startIndex">開始位置</param>
        /// <returns>符号なし16ビット整数</returns>
        public static ushort ToUInt16(this IEnumerable<byte> source, int startIndex)
        {
            var target = source.Skip(startIndex).Take(2).Reverse().ToArray();
            return BitConverter.ToUInt16(target, 0);
        }

        /// <summary>
        /// ビッグエンディアンで符号なし24ビット整数を解釈し変換
        /// </summary>
        /// <param name="source">対象のバイトシーケンス</param>
        /// <param name="startIndex">開始位置</param>
        /// <returns>符号なし24ビット整数</returns>
        public static int ToUInt24(this IEnumerable<byte> source, int startIndex)
        {
            var target = source.Skip(startIndex).Take(3).Reverse().Concat(new byte[] { 0x00 }).ToArray();
            return BitConverter.ToInt32(target, 0);
        }

        /// <summary>
        /// ビッグエンディアンで符号なし31ビット整数を解釈し変換
        /// </summary>
        /// <param name="source">対象のバイトシーケンス</param>
        /// <param name="startIndex">開始位置</param>
        /// <returns>符号なし31ビット整数</returns>
        public static int ToUInt31(this IEnumerable<byte> source, int startIndex)
        {
            var target = source.Skip(startIndex).Take(4).Reverse().ToArray();
            target[3] = (byte)(target[3] & 0b01111111);
            return BitConverter.ToInt32(target, 0);
        }

        /// <summary>
        /// ビッグエンディアンで符号なし32ビット整数を解釈し変換
        /// </summary>
        /// <param name="source">対象のバイトシーケンス</param>
        /// <param name="startIndex">開始位置</param>
        /// <returns>符号なし32ビット整数</returns>
        public static uint ToUInt32(this IEnumerable<byte> source, int startIndex)
        {
            var target = source.Skip(startIndex).Take(4).Reverse().ToArray();
            return BitConverter.ToUInt32(target, 0);
        }

        /// <summary>
        /// ビッグエンディアンで符号なし64ビット整数を解釈し変換
        /// </summary>
        /// <param name="source">対象のバイトシーケンス</param>
        /// <param name="startIndex">開始位置</param>
        /// <returns>符号なし64ビット整数</returns>
        public static long ToInt64(this IEnumerable<byte> source, int startIndex)
        {
            var target = source.Skip(startIndex).Take(8).Reverse().ToArray();
            return BitConverter.ToInt64(target, 0);
        }

        public static string ToASCII(this byte[] source)
            => Encoding.ASCII.GetString(source);

        public static string ToUTF8(this byte[] source)
            => Encoding.UTF8.GetString(source);
    }
}
