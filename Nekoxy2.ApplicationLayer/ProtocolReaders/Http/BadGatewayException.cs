using System;

namespace Nekoxy2.ApplicationLayer.ProtocolReaders.Http
{
    /// <summary>
    /// Bad Gateway エラーとなる際にスロー
    /// </summary>
    internal sealed class BadGatewayException : Exception
    {
        public BadGatewayException(string message) : base(message) { }
    }
}
