using System;

namespace Nekoxy2.ApplicationLayer.Entities.Http
{
    /// <summary>
    /// 不正な HTTP ヘッダーがある場合にスローされる例外
    /// </summary>
    internal sealed class InvalidHttpHeadersException : Exception
    {
        public InvalidHttpHeadersException(string message) : base(message) { }
    }
}
