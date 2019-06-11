using System;

namespace Nekoxy2.Default.Certificate
{
    /// <summary>
    /// ルート証明書が見つからない場合にスローされる例外
    /// </summary>
    internal sealed class RootCertificateNotFoundException : Exception
    {
        public RootCertificateNotFoundException() : base() { }
    }
}
