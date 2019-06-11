using System.IO;
using System.Linq;

namespace Nekoxy2.ApplicationLayer
{
    internal static partial class StreamExtensions
    {
        /// <summary>
        /// 現在末尾が空行かどうか
        /// </summary>
        /// <param name="stream">対象のストリーム</param>
        /// <returns>現在末尾が空行かどうか</returns>
        public static bool CurrentWithEmptyLine(this Stream stream)
            => stream.IsMatchLast((byte)'\r', (byte)'\n', (byte)'\r', (byte)'\n')
            || stream.IsMatchLast((byte)'\n', (byte)'\n');

        /// <summary>
        /// 現在末尾が改行かどうか
        /// </summary>
        /// <param name="stream">対象のストリーム</param>
        /// <returns>現在末尾が改行かどうか</returns>
        public static bool CurrentWithNewLine(this Stream stream)
            => stream.IsMatchLast((byte)'\r', (byte)'\n');

        /// <summary>
        /// 現在末尾が指定バイト配列と一致するかどうか
        /// </summary>
        /// <param name="stream">対象のストリーム</param>
        /// <param name="expected">検査バイト配列</param>
        /// <returns>現在末尾が指定バイト配列と一致するかどうか</returns>
        private static bool IsMatchLast(this Stream stream, params byte[] expected)
        {
            lock (stream)
            {
                if (stream.Length < expected.Length)
                    return false;

                var position = stream.Position;
                var actual = new byte[expected.Length];
                stream.Seek(-expected.Length, SeekOrigin.End);
                stream.Read(actual, 0, actual.Length);

                if (stream.Position != position)
                    stream.Position = position;

                return actual.Zip(expected, (a, e) => (a, e)).All(x => x.a == x.e);
            }
        }

        /// <summary>
        /// 現在ストリームデータが改行のみかどうか
        /// </summary>
        /// <param name="stream">対象のストリーム</param>
        /// <returns>現在ストリームデータが改行のみかどうか</returns>
        public static bool IsNewLine(this Stream stream)
            => stream.Length == 2 && stream.CurrentWithNewLine();
    }
}
