using System;

namespace Nekoxy2.ApplicationLayer.ProtocolReaders.Http
{
    /// <summary>
    /// ボディーが不完全な場合にスロー
    /// </summary>
    internal sealed class IncompleteBodyException : Exception
    {
    }
}
