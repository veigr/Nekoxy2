using Nekoxy2.ApplicationLayer;
using System;
using System.Collections.Generic;
using System.IO;

namespace Nekoxy2.SazLoader.Entities.WebSocket
{
    /// <summary>
    /// SAZ ファイルの WebSocket ファイルフォーマットを解析
    /// </summary>
    internal static class SazWebSocketFormatParser
    {
        /// <summary>
        /// WebSocket 記録ファイルを解析
        /// </summary>
        /// <param name="bytes">WebSocket 記録ファイルデータ</param>
        /// <returns>SAZ WebSocket フレームリスト</returns>
        public static IReadOnlyList<SazWebSocketFrame> Parse(byte[] bytes)
        {
            if (bytes == null) return null;

            var list = new List<SazWebSocketFrame>();
            using (var ms = new MemoryStream(bytes))
            {
                while (ms.Position < ms.Length)
                {
                    var firstEmptyLine = ms.ReadLine();
                    if (ms.Length <= ms.Position) break;

                    var lengthLine = ms.ReadLine();
                    var id = ms.ReadLine().ParseIntValue();
                    var bitFlagLine = ms.ReadLine();
                    var doneRead = ms.ReadLine().ParseDateTimeOffsetValue();
                    var beginSend = ms.ReadLine().ParseDateTimeOffsetValue();
                    var doneSend = ms.ReadLine().ParseDateTimeOffsetValue();
                    ms.ReadLine();

                    var direction = lengthLine.StartsWith("Request") ? Direction.Request : Direction.Response;
                    var length = lengthLine.ParseIntValue();
                    var frameBytes = new byte[length];
                    ms.Read(frameBytes, 0, frameBytes.Length);

                    list.Add(new SazWebSocketFrame(direction, id, doneRead, beginSend, doneSend, frameBytes));
                }
            }
            return list;
        }

        /// <summary>
        /// LF まで ASCII として読み取り
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        private static string ReadLine(this MemoryStream stream)
        {
            var bytes = new List<byte>();
            int read;
            while (-1 < (read = stream.ReadByte()))
            {
                bytes.Add((byte)read);
                if (read == '\n') break;
            }
            return bytes.ToArray().ToASCII();
        }

        /// <summary>
        /// : で区切られた Key-Value 行の Value を取得
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private static string GetValue(this string line)
            => line.Split(new[] { ": " }, StringSplitOptions.None)[1];

        /// <summary>
        /// : で区切られた Key-Value 行の Value を <see cref="int"/> に変換
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private static int ParseIntValue(this string line)
            => int.Parse(line.GetValue());

        /// <summary>
        /// : で区切られた Key-Value 行の Value を <see cref="DateTimeOffset"/> に変換
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private static DateTimeOffset ParseDateTimeOffsetValue(this string line)
            => DateTimeOffset.Parse(line.GetValue());
    }
}
