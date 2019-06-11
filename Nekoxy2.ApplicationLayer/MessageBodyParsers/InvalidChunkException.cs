using System;

namespace Nekoxy2.ApplicationLayer.MessageBodyParsers
{
    /// <summary>
    /// 不正なチャンクがある場合にスローされる例外
    /// </summary>
    internal sealed class InvalidChunkException : Exception
    {
        public InvalidChunkException(string message)
            : base(message) { }

        public InvalidChunkException(string message, Exception innnerException)
            : base(message, innnerException) { }
    }
}
