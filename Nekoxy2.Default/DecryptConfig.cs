using Nekoxy2.Default.Certificate;
using Nekoxy2.Default.Certificate.Default;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Nekoxy2.Default
{
    /// <summary>
    /// HTTPS 復号化設定
    /// </summary>
    public interface IDecryptConfig
    {
        /// <summary>
        /// 復号化するかどうか
        /// </summary>
        bool IsDecrypt { get; set; }

        /// <summary>
        /// 有効化する SSL プロトコルバージョン
        /// </summary>
        SslProtocols EnabledSslProtocols { get; set; }

        /// <summary>
        /// 証明書作成器
        /// </summary>
        ICertificateFactory CertificateFactory { get; set; }

        /// <summary>
        /// 証明書ストア。
        /// 既定では BouncyCastle を用いて証明書を作成し、OS の個人証明書ストアにサーバー証明書をキャッシュします。
        /// </summary>
        ICertificateStore CertificateStore { get; set; }

        /// <summary>
        /// ルート証明書の証明書ストアからの検索方法。
        /// 既定では、設定された証明書ストアから既定の発行者名で検索します。
        /// </summary>
        Func<ICertificateStore, X509Certificate2> RootCertificateResolver { get; set; }

        /// <summary>
        /// 復号化対象のホストを絞り込むフィルタ
        /// </summary>
        Func<string, bool> HostFilterHandler { get; set; }

        /// <summary>
        /// カスタムキャッシュ場所が有効な場合に、ホスト名からサーバー証明書のキャッシュを解決し取得を行う関数
        /// </summary>
        Func<string, X509Certificate2> ServerCertificateCacheResolver { get; set; }

        /// <summary>
        /// サーバー証明書のキャッシュ場所。
        /// 指定された順序が優先度となります。
        /// </summary>
        IEnumerable<CertificateCacheLocation> CacheLocations { get; set; }

        /// <summary>
        /// カスタムキャッシュ場所が有効な場合に、サーバー証明書が作成された際に発生
        /// </summary>
        event EventHandler<X509Certificate2> ServerCertificateCreated;
    }

    /// <summary>
    /// HTTPS 復号化設定
    /// </summary>
    internal sealed class DecryptConfig : IDecryptConfig
    {
        /// <summary>
        /// 復号化するかどうか
        /// </summary>
        public bool IsDecrypt { get; set; } = false;

        /// <summary>
        /// 有効化する SSL プロトコルバージョン
        /// </summary>
        public SslProtocols EnabledSslProtocols { get; set; } = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls;

        /// <summary>
        /// 証明書作成器
        /// </summary>
        public ICertificateFactory CertificateFactory { get; set; } = new BouncyCastleCertificateFactory();

        /// <summary>
        /// 証明書ストア。
        /// 既定では BouncyCastle を用いて証明書を作成し、OS の個人証明書ストアにサーバー証明書をキャッシュします。
        /// </summary>
        public ICertificateStore CertificateStore { get; set; } = new CertificateStore();

        /// <summary>
        /// ルート証明書の証明書ストアからの検索方法。
        /// 既定では、設定された証明書ストアから既定の発行者名で検索します。
        /// </summary>
        public Func<ICertificateStore, X509Certificate2> RootCertificateResolver { get; set; } = store
               => store.FindRootCertificate(CertificateUtil.DEFAULT_ISSUER_NAME);

        /// <summary>
        /// 復号化対象のホストを絞り込むフィルタ
        /// </summary>
        public Func<string, bool> HostFilterHandler { get; set; } = host => true;

        /// <summary>
        /// カスタムキャッシュ場所が有効な場合に、ホスト名からサーバー証明書のキャッシュを解決し取得を行う関数
        /// </summary>
        public Func<string, X509Certificate2> ServerCertificateCacheResolver { get; set; } = null;

        /// <summary>
        /// サーバー証明書のキャッシュ場所。
        /// 指定された順序が優先度となります。
        /// </summary>
        public IEnumerable<CertificateCacheLocation> CacheLocations { get; set; }
            = new[] { CertificateCacheLocation.Memory, CertificateCacheLocation.Custom, CertificateCacheLocation.Store };

        /// <summary>
        /// カスタムキャッシュ場所が有効な場合に、サーバー証明書が作成された際に発生
        /// </summary>
        public event EventHandler<X509Certificate2> ServerCertificateCreated;

        /// <summary>
        /// (for Test) フラグ化されたキャッシュ場所
        /// </summary>
        internal CertificateCacheLocation CacheLocationFlags
            => (CertificateCacheLocation)this.CacheLocations.Distinct().Cast<int>().Sum();

        /// <summary>
        /// <see cref="ServerCertificateCreated"/> イベントを発生させます
        /// </summary>
        /// <param name="cert">サーバー証明書</param>
        internal void InvokeServerCertificateCreated(object sender, X509Certificate2 cert)
            => this.ServerCertificateCreated?.Invoke(sender, cert);
    }

    /// <summary>
    /// サーバー証明書のキャッシュ場所
    /// </summary>
    [Flags]
    public enum CertificateCacheLocation
    {
        /// <summary>
        /// オンメモリ
        /// </summary>
        Memory = 0b001,
        /// <summary>
        /// <see cref="DecryptConfig.CertificateStore"/> で指定された証明書ストア
        /// </summary>
        Store = 0b010,
        /// <summary>
        /// カスタム。
        /// <see cref="DecryptConfig.ServerCertificateCacheResolver"> 関数、<see cref="DecryptConfig.ServerCertificateCreated"/> イベントが利用されるようになります。
        /// </summary>
        Custom = 0b100,
    }
}
