using Nekoxy2.Default;
using Nekoxy2.ApplicationLayer.Entities.WebSocket;
using Nekoxy2.ApplicationLayer.ProtocolReaders.WebSocket;
using Nekoxy2.Spi.Entities.WebSocket;
using Nekoxy2.Test.TestUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Nekoxy2.Test.ApplicationLayer.ProtocolReaders.WebSocket
{
    public class WebSocketFrameBuilderTest
    {
        [Fact]
        public void PayloadLengthPart7bitsWithoutMaskTest()
        {
            var builder = new WebSocketFrameBuilder(ProxyConfig.MaxByteArrayLength);
            var data = new byte[]
            {
                0b10000010,  // FIN, Binary
                0b00000000,  // noMask, 0bytes
            };

            builder.TryAddData(data, 0, data.Length, out var readSize, out var frame).IsTrue();
            readSize.Is(data.Length);

            frame.Fin.IsTrue();
            frame.Opcode.Is(WebSocketOpcode.Binary);
            frame.Mask.IsFalse();
            frame.PayloadLength.Is(0);
            frame.PayloadData.Length.Is(0);
        }

        [Fact]
        public void PayloadLengthPart7bitsWitMaskTest()
        {
            var builder = new WebSocketFrameBuilder(ProxyConfig.MaxByteArrayLength);
            var data = new byte[]
            {
                0b10000010, // FIN, Binary
                0b11111101, // Mask, 125bytes
                0b10101010, // Mask key
                0b10101010, // Mask key
                0b10101010, // Mask key
                0b10101010, // Mask key
            };

            builder.TryAddData(data, 0, data.Length, out var readSize, out _).IsFalse();
            readSize.Is(data.Length);

            var payload = Enumerable.Repeat((byte)'a', 125).ToArray();
            builder.TryAddData(payload, 0, payload.Length, out readSize, out var frame).IsTrue();
            readSize.Is(payload.Length);

            frame.Fin.IsTrue();
            frame.Opcode.Is(WebSocketOpcode.Binary);
            frame.Mask.IsTrue();
            frame.MaskKey.Is(new byte[] { 0b10101010, 0b10101010, 0b10101010, 0b10101010 });
            frame.PayloadLength.Is(125);
            frame.PayloadData.Length.Is(125);
        }

        [Fact]
        public void PayloadLengthExPart2bytesWitoutMaskTest()
        {
            var builder = new WebSocketFrameBuilder(ProxyConfig.MaxByteArrayLength);
            var data = new byte[]
            {
                0b10000010, // FIN, Binary
                0b01111110, // noMask, 2byteExPart
                0b00000000, // BigEndian
                0b01111110, // 126bytes
            };

            builder.TryAddData(data, 0, data.Length, out var readSize, out _).IsFalse();
            readSize.Is(data.Length);

            var payload = Enumerable.Repeat((byte)'a', 126).ToArray();
            builder.TryAddData(payload, 0, payload.Length, out readSize, out var frame).IsTrue();
            readSize.Is(payload.Length);

            frame.Fin.IsTrue();
            frame.Opcode.Is(WebSocketOpcode.Binary);
            frame.Mask.IsFalse();
            frame.PayloadLength.Is(126);
            frame.PayloadData.Length.Is(126);
        }

        [Fact]
        public void PayloadLengthExPart2bytesWitMaskTest()
        {
            var builder = new WebSocketFrameBuilder(ProxyConfig.MaxByteArrayLength);
            var data = new byte[]
            {
                0b10000010,  // FIN, Binary
                0b11111110,  // Mask, 2byteExPart
                0b00000000,  // BigEndian
                0b01111110,  // 126bytes
                0b10101010,  // Mask key
                0b10101010,  // Mask key
                0b10101010,  // Mask key
                0b10101010,  // Mask key
            };

            builder.TryAddData(data, 0, data.Length, out var readSize, out _).IsFalse();
            readSize.Is(data.Length);

            var payload = Enumerable.Repeat((byte)'a', 126).ToArray();
            builder.TryAddData(payload, 0, payload.Length, out readSize, out var frame).IsTrue();
            readSize.Is(payload.Length);

            frame.Fin.IsTrue();
            frame.Opcode.Is(WebSocketOpcode.Binary);
            frame.Mask.IsTrue();
            frame.MaskKey.Is(new byte[] { 0b10101010, 0b10101010, 0b10101010, 0b10101010 });
            frame.PayloadLength.Is(126);
            frame.PayloadData.Length.Is(126);
        }

        [Fact]
        public void PayloadLengthExPart8bytesWithoutMask()
        {
            var builder = new WebSocketFrameBuilder(ProxyConfig.MaxByteArrayLength);
            var data = new byte[]
            {
                0b10000010,  // FIN, Binary
                0b01111111,  // noMask, 8byteExPart
                0b00000000,  // BigEndian
                0b00000000,
                0b00000000,
                0b00000000,
                0b00000000,
                0b00000001,
                0b00000000,
                0b00000000,  // 65536bytes
            };

            builder.TryAddData(data, 0, data.Length, out var readSize, out _).IsFalse();
            readSize.Is(data.Length);

            var payload = Enumerable.Repeat((byte)'a', 65536).ToArray();
            builder.TryAddData(payload, 0, payload.Length, out readSize, out var frame).IsTrue();
            readSize.Is(payload.Length);

            frame.Fin.IsTrue();
            frame.Opcode.Is(WebSocketOpcode.Binary);
            frame.Mask.IsFalse();
            frame.PayloadLength.Is(65536);
            frame.PayloadData.Length.Is(65536);
        }

        [Fact]
        public void PayloadLengthExPart8bytesWithMask()
        {
            var builder = new WebSocketFrameBuilder(ProxyConfig.MaxByteArrayLength);
            var data = new byte[]
            {
                0b10000010,  // FIN, Binary
                0b11111111,  // Mask, 8byteExPart
                0b00000000,  // BigEndian
                0b00000000,
                0b00000000,
                0b00000000,
                0b00010000,
                0b00000000,
                0b00000000,
                0b00000000,  // 256MB
                0b10101010,  // Mask key
                0b10101010,  // Mask key
                0b10101010,  // Mask key
                0b10101010,  // Mask key
            };

            builder.TryAddData(data, 0, data.Length, out var readSize, out _).IsFalse();
            readSize.Is(data.Length);

            var frame = builder.Frame;
            frame.Fin.IsTrue();
            frame.Opcode.Is(WebSocketOpcode.Binary);
            frame.Mask.IsTrue();
            frame.MaskKey.Is(new byte[] { 0b10101010, 0b10101010, 0b10101010, 0b10101010 });
            frame.PayloadLength.Is(256 * 1024 * 1024);
            frame.PayloadData.IsNull();
        }

        [Fact]
        public void DemaskTest()
        {
            var builder = new WebSocketFrameBuilder(ProxyConfig.MaxByteArrayLength);
            var data = new byte[]
            {
                0b10000010,  // FIN, Binary
                0b10000110,  // Mask, 6bytes
                0b00000000,  // Mask key
                0b11001100,  // Mask key
                0b11111111,  // Mask key
                0b00110011,  // Mask key
            };

            builder.TryAddData(data, 0, data.Length, out var readSize, out _).IsFalse();
            readSize.Is(data.Length);

            var payload = new byte[]
            {
                0b11111111,
                0b11111111,
                0b11111111,
                0b11111111,
                0b11111111,
                0b11111111,
            };
            builder.TryAddData(payload, 0, payload.Length, out readSize, out var frame).IsTrue();
            readSize.Is(payload.Length);

            frame.PayloadData[0].Is(0b11111111);
            frame.PayloadData[1].Is(0b00110011);
            frame.PayloadData[2].Is(0b00000000);
            frame.PayloadData[3].Is(0b11001100);
            frame.PayloadData[4].Is(0b11111111);
            frame.PayloadData[5].Is(0b00110011);
        }
    }
}
