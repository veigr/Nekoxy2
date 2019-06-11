using Nekoxy2.SazLoader.Entities;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Nekoxy2.SazLoader.Deserialization
{
    /// <summary>
    /// SAZ ファイルを展開
    /// </summary>
    internal static class SazFile
    {
        /// <summary>
        /// パスを指定し、SAZ ファイルを展開
        /// </summary>
        /// <param name="path">SAZ ファイルパス</param>
        /// <returns>展開されたセッションデータ</returns>
        public static IEnumerable<SazSession> Load(string path)
        {
            using (var zip = ZipFile.OpenRead(path))
            {
                return zip.Entries
                    .Where(x => x.FullName.StartsWith("raw/"))
                    .Where(x => !string.IsNullOrEmpty(x.Name))
                    .GroupBy(x => x.Name.Split(new[] { '_' }).First(),
                    (key, elements) => new
                    {
                        Number = int.Parse(key),
                        Metadata = elements.FirstOrDefault(x => x.Name.EndsWith("_m.xml")),
                        Request = elements.FirstOrDefault(x => x.Name.EndsWith("_c.txt")),
                        Response = elements.FirstOrDefault(x => x.Name.EndsWith("_s.txt")),
                        WebSocket = elements.FirstOrDefault(x => x.Name.EndsWith("_w.txt")),
                    })
                    .Select(x => new SazSession(
                        x.Number,
                        x.Metadata?.ToMetadata(),
                        x.Request?.ReadAllBytes(),
                        x.Response?.ReadAllBytes(),
                        x.WebSocket?.ReadAllBytes())
                    )
                    .ToArray();
            }
        }
    }

    internal static partial class ZipExtensions
    {
        /// <summary>
        /// <see cref="ZipArchiveEntry"/> を <see cref="byte[]"/> に読み込む
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public static byte[] ReadAllBytes(this ZipArchiveEntry entry)
        {
            using (var source = entry.Open())
            using (var dest = new MemoryStream())
            {
                source.CopyTo(dest);
                return dest.ToArray();
            }
        }
    }
}
