using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace Nekoxy2.Default.Certificate.Default
{
    /// <summary>
    /// 証明書ストア
    /// </summary>
    internal sealed class CertificateStore : ICertificateStore
    {
        /// <summary>
        /// X509証明書ストアファクトリー
        /// </summary>
        internal Func<StoreName, IX509Store> StoreFactory { get; set; }
            = storeName => new X509StoreWrapper(new X509Store(storeName, StoreLocation.CurrentUser));

        /// <summary>
        /// 証明書作成器
        /// </summary>
        public ICertificateFactory CertificateFactory { get; set; } = new BouncyCastleCertificateFactory();

        #region Root Certificate

        /// <summary>
        /// ルート証明書を作成
        /// </summary>
        /// <param name="issuerName">発行者名</param>
        /// <returns>ルート証明書</returns>
        public X509Certificate2 CreateRootCertificate(string issuerName)
            => this.CertificateFactory.CreateRootCertificate(issuerName);

        /// <summary>
        /// 発行者名を指定してルート証明書をアンインストール
        /// </summary>
        /// <param name="issuerName">発行者名</param>
        /// <returns>アンインストールされたルート証明書リスト</returns>
        public IEnumerable<X509Certificate2> UninstallRootCertificates(string issuerName)
        {
            var rootCerts = this.FindBySubjectName(StoreName.Root, issuerName);
            foreach (var cert in rootCerts)
            {
                this.UninstallFromRootStore(cert);
            }
            return rootCerts.Cast<X509Certificate2>();
        }

        /// <summary>
        /// 証明書を信頼されたルート証明機関ストアにインストール
        /// </summary>
        /// <param name="cert">証明書</param>
        public void InstallToRootStore(X509Certificate2 cert)
            => this.Install(StoreName.Root, cert);

        /// <summary>
        /// 証明書を信頼されたルート証明機関ストアからアンインストール
        /// </summary>
        /// <param name="cert">証明書</param>
        public void UninstallFromRootStore(X509Certificate2 cert)
            => this.Uninstall(StoreName.Root, cert);

        /// <summary>
        /// 発行者名からルート証明書を検索
        /// </summary>
        /// <param name="issuerName">発行者名</param>
        /// <returns>ルート証明書</returns>
        public X509Certificate2 FindRootCertificate(string issuerName) => this.FindBySubjectName(StoreName.Root, issuerName).Cast<X509Certificate2>().FirstOrDefault();

        #endregion

        #region Server Certificate

        /// <summary>
        /// 指定された発行者名のサーバー証明書をすべてアンインストール
        /// </summary>
        /// <param name="issuerName">発行者名</param>
        /// <returns>削除されたサーバー証明書</returns>
        public IEnumerable<X509Certificate2> UninstallAllServerCertificatesByIssuer(string issuerName)
        {
            var serverCerts = this.FindByIssuerName(StoreName.My, issuerName);
            foreach (var cert in serverCerts)
            {
                this.Uninstall(StoreName.My, cert);
            }
            return serverCerts.Cast<X509Certificate2>();
        }

        /// <summary>
        /// 証明書を個人ストアにインストール
        /// </summary>
        /// <param name="cert">証明書</param>
        public void InstallToPersonalStore(X509Certificate2 cert)
            => this.Install(StoreName.My, cert);

        /// <summary>
        /// 証明書を個人ストアからアンインストール
        /// </summary>
        /// <param name="cert">証明書</param>
        public void UninstallFromPersonalStore(X509Certificate2 cert)
            => this.Uninstall(StoreName.My, cert);

        /// <summary>
        /// ホスト名とルート証明書からサーバー証明書を検索
        /// </summary>
        /// <param name="host">ホスト名</param>
        /// <param name="rootCert">ルート証明書</param>
        /// <returns>サーバー証明書</returns>
        public X509Certificate2 FindServerCertificate(string host, X509Certificate2 rootCert)
        {
            // invalid なサーバー証明書も対象とする(実際に検証するのはクライアント依存なので、ここでは余計な検証は行わない)
            return this.FindByIssuerName(StoreName.My, rootCert.Issuer.RemoveCn())
                        .Find(X509FindType.FindBySubjectName, host, false)   // FindBySubjectName は部分一致くさい
                        .Cast<X509Certificate2>()
                        .FirstOrDefault(x => x.Subject == $"CN={host}");
        }

        #endregion

        /// <summary>
        /// 証明書をインストール
        /// </summary>
        /// <param name="storeName">インストールするストア名</param>
        /// <param name="cert">インストールする証明書</param>
        private void Install(StoreName storeName, X509Certificate2 cert)
        {
            using (var store = this.StoreFactory(storeName))
            {
                store.Open(OpenFlags.ReadWrite);
                store.Add(cert);
            }
        }

        /// <summary>
        /// 証明書をアンインストール
        /// </summary>
        /// <param name="storeName">アンインストールするストア名</param>
        /// <param name="cert">アンインストールする証明書</param>
        private void Uninstall(StoreName storeName, X509Certificate2 cert)
        {
            using (var store = this.StoreFactory(storeName))
            {
                store.Open(OpenFlags.ReadWrite);
                store.Remove(cert);
            }
        }

        /// <summary>
        /// サブジェクト名から証明書を検索
        /// </summary>
        /// <param name="storeName">検索するストア名</param>
        /// <param name="subjectName">サブジェクト名</param>
        /// <returns>検索された証明書リスト</returns>
        private X509Certificate2Collection FindBySubjectName(StoreName storeName, string subjectName)
        {
            using (var store = this.StoreFactory(storeName))
            {
                store.Open(OpenFlags.OpenExistingOnly);
                return store.Certificates.Find(X509FindType.FindBySubjectName, subjectName, false);
            }
        }

        /// <summary>
        /// 発行者名から証明書を検索
        /// </summary>
        /// <param name="storeName">検索するストア名</param>
        /// <param name="issuerName">発行者名</param>
        /// <returns>検索された証明書リスト</returns>
        private X509Certificate2Collection FindByIssuerName(StoreName storeName, string issuerName)
        {
            using (var store = this.StoreFactory(storeName))
            {
                store.Open(OpenFlags.OpenExistingOnly);
                return store.Certificates.Find(X509FindType.FindByIssuerName, issuerName, false);
            }
        }
    }
}
