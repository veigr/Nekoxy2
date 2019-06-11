using System;

namespace Nekoxy2.ApplicationLayer.ProtocolReaders.WebSocket
{
    /// <summary>
    /// サポートしていない拡張が指定されている場合にスロー
    /// </summary>
    public sealed class UnsupportedExtensionException : Exception
    {
        public UnsupportedExtensionException(string message) : base(message) { }
    }
}
