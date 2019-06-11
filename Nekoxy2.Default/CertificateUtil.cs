using Nekoxy2.Default.Certificate;
using Nekoxy2.Default.Certificate.Default;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace Nekoxy2.Default
{
    /// <summary>
    /// 証明書操作ユーティリティ
    /// </summary>
    public static class CertificateUtil
    {
        /// <summary>
        /// 証明書ストア
        /// </summary>
        private static readonly ICertificateStore store = new CertificateStore();

        /// <summary>
        /// 既定の発行者名
        /// </summary>
        internal const string DEFAULT_ISSUER_NAME = "DO_NOT_TRUST_NekoxyRoot";

        /// <summary>
        /// 発行者名を指定してルート証明書を検索
        /// </summary>
        /// <param name="issuerName">発行者名</param>
        /// <returns>ルート証明書</returns>
        public static X509Certificate2 FindRootCertificate(string issuerName = DEFAULT_ISSUER_NAME)
            => store.FindRootCertificate(issuerName);

        /// <summary>
        /// 発行者名を指定してルート証明書を作成
        /// </summary>
        /// <param name="issuerName">発行者名</param>
        /// <returns>ルート証明書</returns>
        public static X509Certificate2 CreateRootCertificate(string issuerName = DEFAULT_ISSUER_NAME)
            => store.CreateRootCertificate(issuerName);

        /// <summary>
        /// 発行者名を指定してルート証明書を新規作成し、証明書ストアにインストール
        /// </summary>
        /// <param name="issuerName">発行者名</param>
        /// <returns>作成されたルート証明書</returns>
        public static X509Certificate2 InstallNewRootCertificate(string issuerName = DEFAULT_ISSUER_NAME)
        {
            var cert = store.CreateRootCertificate(issuerName);
            store.InstallToRootStore(cert);
            return cert;
        }

        /// <summary>
        /// 発行者名を指定してルート証明書を証明書ストアからアンインストール
        /// </summary>
        /// <param name="issuerName">発行者名</param>
        /// <returns>削除されたルート証明書リスト</returns>
        public static IEnumerable<X509Certificate2> UninstallRootCertificates(string issuerName = DEFAULT_ISSUER_NAME)
            => store.UninstallRootCertificates(issuerName);

        /// <summary>
        /// 指定された証明書を、証明書ストアの信頼されたルート証明機関にインストール
        /// </summary>
        /// <param name="cert">証明書</param>
        public static void InstallToRootStore(X509Certificate2 cert)
            => store.InstallToRootStore(cert);

        /// <summary>
        /// 指定された証明書を、証明書ストアの信頼されたルート証明機関からアンインストール
        /// </summary>
        /// <param name="cert">証明書</param>
        public static void UninstallFromRootStore(X509Certificate2 cert)
            => store.UninstallFromRootStore(cert);

        /// <summary>
        /// 指定した発行者名のサーバー証明書キャッシュを証明書ストアから全て削除
        /// </summary>
        /// <param name="issuerName">発行者名</param>
        /// <returns>削除された証明書リスト</returns>
        public static IEnumerable<X509Certificate2> UninstallAllServerCertificatesByIssuer(string issuerName = DEFAULT_ISSUER_NAME)
            => store.UninstallAllServerCertificatesByIssuer(issuerName);
    }
}
