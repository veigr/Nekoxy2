using Nekoxy2.Default;
using Nekoxy2.Default.Certificate;
using Nekoxy2.Default.Certificate.Default;
using Nekoxy2.Test.TestUtil;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Nekoxy2.Test.Default.Certificate
{
    public class CertificateStoreTest
    {
        [Fact]
        public void RootTest()
        {
            var rootStore = new X509TestStore();
            var myStore = new X509TestStore();
            var store = new CertificateStore
            {
                StoreFactory = name => name == StoreName.Root ? rootStore
                                            : name == StoreName.My ? myStore
                                            : null
            };

            var issuer = "hoge";
            var cert = store.CreateRootCertificate(issuer);

            store.InstallToRootStore(cert);
            store.FindRootCertificate(issuer).Is(cert);
            rootStore.Certificates.Count.Is(1);
            rootStore.Certificates[0].Is(cert);

            store.UninstallFromRootStore(cert);
            store.FindRootCertificate(issuer).IsNull();
            rootStore.Certificates.Count.Is(0);


            var cert2 = store.CreateRootCertificate(issuer);

            store.InstallToRootStore(cert);
            store.FindRootCertificate(issuer).Is(cert);
            rootStore.Certificates.Count.Is(1);
            rootStore.Certificates[0].Is(cert);

            store.UninstallRootCertificates(issuer);
            store.FindRootCertificate(issuer).IsNull();
            rootStore.Certificates.Count.Is(0);
        }

        [Fact]
        public void ServerTest()
        {
            var rootStore = new X509TestStore();
            var myStore = new X509TestStore();
            var store = new CertificateStore
            {
                StoreFactory = name => name == StoreName.Root ? rootStore
                                            : name == StoreName.My ? myStore
                                            : null
            };

            var issuer = "hoge";
            var root = store.CreateRootCertificate(issuer);

            var server1 = store.CertificateFactory.CreateServerCertificate("host1", root);
            store.InstallToPersonalStore(server1);
            store.FindServerCertificate("host1", root).Is(server1);

            var server2 = store.CertificateFactory.CreateServerCertificate("host2", root);
            store.InstallToPersonalStore(server2);
            store.FindServerCertificate("host2", root).Is(server2);
            myStore.Certificates.Count.Is(2);

            var server3 = store.CertificateFactory.CreateServerCertificate("host3", root);
            store.InstallToPersonalStore(server3);
            store.FindServerCertificate("host3", root).Is(server3);
            myStore.Certificates.Count.Is(3);

            store.UninstallFromPersonalStore(server2);
            myStore.Certificates.Count.Is(2);
            store.FindServerCertificate("host1", root).Is(server1);
            store.FindServerCertificate("host2", root).IsNull();
            store.FindServerCertificate("host3", root).Is(server3);

            store.UninstallAllServerCertificatesByIssuer(issuer);
            myStore.Certificates.Count.Is(0);
            store.FindServerCertificate("host1", root).IsNull();
            store.FindServerCertificate("host2", root).IsNull();
            store.FindServerCertificate("host3", root).IsNull();
        }

        [Fact]
        public void FacadeTest()
        {
            var rootStore = new X509TestStore();
            var myStore = new X509TestStore();
            var store = new CertificateStore
            {
                StoreFactory = name => name == StoreName.Root ? rootStore
                                            : name == StoreName.My ? myStore
                                            : null
            };

            var config = new DecryptConfig
            {
                CertificateStore = store
            };

            Assert.Throws<RootCertificateNotFoundException>(
                () => CertificateStoreFacade.GetServerCertificate($"host", config));
            
            store.InstallToRootStore(store.CreateRootCertificate(config.IssuerName));

            var bag = new ConcurrentBag<X509Certificate2>();
            // パラレルで100個要求してもホスト10種のみ作成される
            Parallel.For(0, 100, i =>
            {
                var num = i / 10;
                bag.Add(CertificateStoreFacade.GetServerCertificate($"host{num}", config));
            });
            var result = bag.ToArray();
            result.Length.Is(100);

            CertificateStoreFacade.onMemoryCache.Count.Is(10);

            myStore.Certificates.Count.Is(10);

            var certs = CertificateStoreFacade.onMemoryCache.Values.OrderBy(x => x.Subject).ToArray();
            for (int i = 0; i < 10; i++)
            {
                certs[i].Subject.Is($"CN=host{i}");
            }
        }
    }
}
