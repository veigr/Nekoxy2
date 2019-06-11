using Nekoxy2.ApplicationLayer.Entities.Http;
using System;

namespace Nekoxy2.ApplicationLayer.ProtocolReaders.Http
{
    /// <summary>
    /// Bad Request となる際にスロー
    /// </summary>
    internal sealed class BadRequestException : Exception
    {
        public HttpRequest Request { get; }

        public BadRequestException(string message, HttpRequest request) : base(message) => this.Request = request;
    }
}
