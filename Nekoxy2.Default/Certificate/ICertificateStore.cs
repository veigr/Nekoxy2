using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace Nekoxy2.Default.Certificate
{
    /// <summary>
    /// 証明書ストア
    /// </summary>
    public interface ICertificateStore
    {
        /// <summary>
        /// 証明書作成器
        /// </summary>
        ICertificateFactory CertificateFactory { get; set; }

        #region Root Certificate

        /// <summary>
        /// 発行者名からルート証明書を検索
        /// </summary>
        /// <param name="issuerName">発行者名</param>
        /// <returns>ルート証明書</returns>
        X509Certificate2 FindRootCertificate(string issuerName);

        /// <summary>
        /// ルート証明書を作成
        /// </summary>
        /// <param name="issuerName">発行者名</param>
        /// <returns>ルート証明書</returns>
        X509Certificate2 CreateRootCertificate(string issuerName);

        /// <summary>
        /// 発行者名を指定してルート証明書をアンインストール
        /// </summary>
        /// <param name="issuerName">発行者名</param>
        /// <returns>アンインストールされたルート証明書リスト</returns>
        IEnumerable<X509Certificate2> UninstallRootCertificates(string issuerName);

        /// <summary>
        /// 証明書を信頼されたルート証明機関ストアにインストール
        /// </summary>
        /// <param name="cert">証明書</param>
        void InstallToRootStore(X509Certificate2 cert);

        /// <summary>
        /// 証明書を信頼されたルート証明機関ストアからアンインストール
        /// </summary>
        /// <param name="cert">証明書</param>
        void UninstallFromRootStore(X509Certificate2 cert);

        #endregion

        #region Server Certificate

        /// <summary>
        /// ホスト名とルート証明書からサーバー証明書を検索
        /// </summary>
        /// <param name="host">ホスト名</param>
        /// <param name="rootCert">ルート証明書</param>
        /// <returns>サーバー証明書</returns>
        X509Certificate2 FindServerCertificate(string host, X509Certificate2 rootCert);

        /// <summary>
        /// 証明書を個人ストアにインストール
        /// </summary>
        /// <param name="cert">証明書</param>
        void InstallToPersonalStore(X509Certificate2 cert);

        /// <summary>
        /// 証明書を個人ストアからアンインストール
        /// </summary>
        /// <param name="cert">証明書</param>
        void UninstallFromPersonalStore(X509Certificate2 cert);

        /// <summary>
        /// 指定された発行者名のサーバー証明書をすべてアンインストール
        /// </summary>
        /// <param name="issuerName">発行者名</param>
        /// <returns>削除されたサーバー証明書</returns>
        IEnumerable<X509Certificate2> UninstallAllServerCertificatesByIssuer(string issuerName);

        #endregion
    }
}