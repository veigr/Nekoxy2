using Nekoxy2.Entities.Http;
using Nekoxy2.Entities.Http.Delegations;
using System.Collections.Generic;

namespace Nekoxy2.Entities.WebSocket.Delegations
{
    internal sealed class ReadOnlyWebSocketMessage : IReadOnlyWebSocketMessage
    {
        public Spi.Entities.WebSocket.IReadOnlyWebSocketMessage Source { get; private set; }

        Spi.Entities.Http.IReadOnlySession Spi.Entities.WebSocket.IReadOnlyWebSocketMessage.HandshakeSession
            => this.Source.HandshakeSession;

        Spi.Entities.WebSocket.WebSocketOpcode Spi.Entities.WebSocket.IReadOnlyWebSocketMessage.Opcode
            => this.Source.Opcode;

        byte[] Spi.Entities.WebSocket.IReadOnlyWebSocketMessage.PayloadData => this.Source.PayloadData;

        public IReadOnlySession HandshakeSession { get; private set; }

        public WebSocketOpcode Opcode { get; private set; }

        public IReadOnlyList<byte> PayloadData => this.Source.PayloadData;

        internal static IReadOnlyWebSocketMessage Convert(Spi.Entities.WebSocket.IReadOnlyWebSocketMessage source)
            => new ReadOnlyWebSocketMessage
            {
                Source = source,
                HandshakeSession = ReadOnlySession.Convert(source.HandshakeSession),
                Opcode = (WebSocketOpcode)source.Opcode,
            };
    }

    internal sealed class WebSocketMessage : IWebSocketMessage
    {
        public Spi.Entities.WebSocket.IWebSocketMessage Source { get; private set; }

        Spi.Entities.Http.IReadOnlySession Spi.Entities.WebSocket.IWebSocketMessage.HandshakeSession
            => this.Source.HandshakeSession;

        Spi.Entities.Http.IReadOnlySession Spi.Entities.WebSocket.IReadOnlyWebSocketMessage.HandshakeSession
            => this.Source.HandshakeSession;

        IReadOnlySession IWebSocketMessage.HandshakeSession => this.HandshakeSession;

        public IReadOnlySession HandshakeSession { get; private set; }

        Spi.Entities.WebSocket.WebSocketOpcode Spi.Entities.WebSocket.IWebSocketMessage.Opcode
        {
            get => this.Source.Opcode;
            set => this.Source.Opcode = value;
        }

        Spi.Entities.WebSocket.WebSocketOpcode Spi.Entities.WebSocket.IReadOnlyWebSocketMessage.Opcode
            => this.Source.Opcode;

        byte[] Spi.Entities.WebSocket.IWebSocketMessage.PayloadData
        {
            get => this.Source.PayloadData;
            set => this.Source.PayloadData = value;
        }

        public WebSocketOpcode Opcode
        {
            get => (WebSocketOpcode)this.Source.Opcode;
            set => this.Source.Opcode = (Spi.Entities.WebSocket.WebSocketOpcode)value;
        }

        public byte[] PayloadData
        {
            get => this.Source.PayloadData;
            set => this.Source.PayloadData = value;
        }

        internal static IWebSocketMessage Convert(Spi.Entities.WebSocket.IWebSocketMessage source)
            => new WebSocketMessage
            {
                Source = source,
                HandshakeSession = ReadOnlySession.Convert(source.HandshakeSession),
                Opcode = (WebSocketOpcode)source.Opcode,
            };
    }
}
