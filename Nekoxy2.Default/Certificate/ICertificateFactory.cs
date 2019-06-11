using System.Security.Cryptography.X509Certificates;

namespace Nekoxy2.Default.Certificate
{
    /// <summary>
    /// 証明書作成器
    /// </summary>
    public interface ICertificateFactory
    {
        /// <summary>
        /// サーバー証明書を作成
        /// </summary>
        /// <param name="hostName">ホスト名</param>
        /// <param name="rootCert">ルート証明書</param>
        /// <returns>サーバー証明書</returns>
        X509Certificate2 CreateServerCertificate(string hostName, X509Certificate2 rootCert);

        /// <summary>
        /// ルート証明書を作成
        /// </summary>
        /// <param name="issuerName">発行者名</param>
        /// <returns>ルート証明書</returns>
        X509Certificate2 CreateRootCertificate(string issuerName);
    }
}
