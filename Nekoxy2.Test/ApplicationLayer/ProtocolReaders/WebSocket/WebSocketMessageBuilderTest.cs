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
    public class WebSocketMessageBuilderTest
    {
        [Fact]
        public void BuildMessageTest()
        {
            var frameBuilder = new WebSocketFrameBuilder(ProxyConfig.MaxByteArrayLength);

            var data1 = new byte[]
            {
                0b00000010, // notFIN, Binary
                0b00000001, // noMask, 1bytes
                0b11111111, // Payload
            };
            frameBuilder.TryAddData(data1, 0, data1.Length, out var readSize1, out var frame1).IsTrue();
            readSize1.Is(data1.Length);

            var data2 = new byte[]
            {
                0b00000000, // notFIN, Continuation
                0b00000001, // noMask, 1bytes
                0b00000000, // Payload
            };
            frameBuilder.TryAddData(data2, 0, data2.Length, out var readSize2, out var frame2).IsTrue();
            readSize2.Is(data2.Length);

            var data3 = new byte[]
            {
                0b10000000, // FIN, Continuation
                0b00000001, // noMask, 1bytes
                0b01010101, // Payload
            };
            frameBuilder.TryAddData(data3, 0, data3.Length, out var readSize3, out var frame3).IsTrue();
            readSize3.Is(data3.Length);

            var builder = new WebSocketMessageBuilder(null, ProxyConfig.MaxByteArrayLength);
            builder.TryCreateOrAdd(frame1, out _).IsFalse();
            builder.TryCreateOrAdd(frame2, out _).IsFalse();
            builder.TryCreateOrAdd(frame3, out var message).IsTrue();

            message.HandshakeSession.IsNull();
            message.Opcode.Is(WebSocketOpcode.Binary);
            message.PayloadData[0].Is(0b11111111);
            message.PayloadData[1].Is(0b00000000);
            message.PayloadData[2].Is(0b01010101);
        }
    }
}
