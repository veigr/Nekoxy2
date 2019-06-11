using Nekoxy2.Default;
using Nekoxy2.ApplicationLayer.Entities.WebSocket;
using Nekoxy2.ApplicationLayer.ProtocolReaders.WebSocket;
using Nekoxy2.Spi.Entities.WebSocket;
using Nekoxy2.Test.TestUtil;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Nekoxy2.Test.ApplicationLayer.ProtocolReaders.WebSocket
{
    public class PerMessageDeflateExtensionTest
    {
        [Fact]
        public void DecompressTest()
        {
            var source = "hogefugapiyo";
            byte[] destBytes;
            using (var stream = new MemoryStream())
            {
                using (var deflate = new DeflateStream(stream, CompressionMode.Compress, true))
                {
                    var sourceBytes = Encoding.UTF8.GetBytes(source);
                    deflate.Write(sourceBytes, 0, sourceBytes.Length);
                }
                destBytes = stream.ToArray();
            }

            var builder = new WebSocketFrameBuilder(ProxyConfig.MaxByteArrayLength);
            var data = new byte[]
            {
                0b11000010,
                0b00001110,
            };
            builder.TryAddData(data, 0, data.Length, out var readSize, out _).IsFalse();
            readSize.Is(data.Length);

            builder.TryAddData(destBytes, 0, destBytes.Length, out readSize, out var frame).IsTrue();
            readSize.Is(destBytes.Length);

            var ex = new PerMessageDeflateExtension();
            var message = ex.Decompress(new WebSocketMessage(null, frame));
            Encoding.UTF8.GetString(message.PayloadData).Is(source);
        }
    }
}
