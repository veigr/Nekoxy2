using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace Nekoxy2.Default.Certificate
{
    /// <summary>
    /// 証明書ストア操作の窓口
    /// </summary>
    internal static class CertificateStoreFacade
    {
        /// <summary>
        /// サーバー証明書のオンメモリキャッシュ
        /// </summary>
        internal static readonly ConcurrentDictionary<string, X509Certificate2> onMemoryCache = new ConcurrentDictionary<string, X509Certificate2>();

        /// <summary>
        /// 証明書解決をホスト単位でロックするためのセマフォ
        /// </summary>
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> hostLock = new ConcurrentDictionary<string, SemaphoreSlim>();

        /// <summary>
        /// ホスト名からサーバー証明書を取得
        /// </summary>
        /// <param name="host">ホスト名</param>
        /// <param name="config">復号化設定</param>
        /// <returns>サーバー証明書</returns>
        public static X509Certificate2 GetServerCertificate(string host, DecryptConfig config)
        {
            if ((config.RootCertificate = config.RootCertificate ?? config.CertificateStore.FindRootCertificate(config.IssuerName)) == null)
                throw new RootCertificateNotFoundException();

            var cacheResolvers = config.CacheLocations
                .Select(x =>
                {
                    switch (x)
                    {
                        case CertificateCacheLocation.Memory:
                            return h => onMemoryCache.TryGetValue(h, out var cached) ? cached : null;
                        case CertificateCacheLocation.Store:
                            return h => config.CertificateStore.FindServerCertificate(h, config.RootCertificate);
                        case CertificateCacheLocation.Custom:
                            return config.ServerCertificateCacheResolver;
                        default:
                            return h => null;
                    }
                });

            X509Certificate2 cert = null;
            var semaphore = hostLock.GetOrAdd(host, new SemaphoreSlim(1, 1));
            try
            {
                // 同時に同じホストの処理が実行されると重複した証明書が作成されてしまう
                semaphore.Wait();

                if (cacheResolvers.All(x => (cert = x?.Invoke(host)) == null))
                {
                    cert = config.CertificateFactory.CreateServerCertificate(host, config.RootCertificate);
                    if (config.CacheLocationFlags.HasFlag(CertificateCacheLocation.Memory))
                    {
                        onMemoryCache.TryAdd(host, cert);
                    }
                    if (config.CacheLocationFlags.HasFlag(CertificateCacheLocation.Store))
                    {
                        config.CertificateStore.InstallToPersonalStore(cert);
                    }
                    if (config.CacheLocationFlags.HasFlag(CertificateCacheLocation.Custom))
                    {
                        config.InvokeServerCertificateCreated(config.CertificateFactory, cert);
                    }
                }
            }
            finally
            {
                hostLock.TryRemove(host, out var _);
                semaphore.Release();
            }
            return cert;
        }
    }
}
