using Nekoxy2.Default.Certificate;
using Nekoxy2.Default.Certificate.Default;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Nekoxy2.Test.Default.Certificate
{
    public class BouncyCastleCertificateFactoryTest
    {
        [Fact]
        public void CreateCertificateTest()
        {
            var issuerName = "CN=DO_NOT_TRUST_NekoxyRoot";
            var factory = new BouncyCastleCertificateFactory();
            var rootCert = factory.CreateRootCertificate(issuerName);
            rootCert.Issuer.Is(issuerName);
            rootCert.Subject.Is(issuerName);
            rootCert.Extensions.Count.Is(1);
            rootCert.Extensions[0].GetType().Is(typeof(X509BasicConstraintsExtension));
            var rootExt = rootCert.Extensions[0] as X509BasicConstraintsExtension;
            rootExt.CertificateAuthority.IsTrue();
            rootExt.Critical.IsTrue();
            rootCert.HasPrivateKey.IsTrue();

            var serverCert = factory.CreateServerCertificate("*.example.com", rootCert);
            serverCert.Issuer.Is(issuerName);
            serverCert.Subject.Is("CN=*.example.com");
            // X509Extensionsの確認は面倒なのでスキップ……
            serverCert.HasPrivateKey.IsTrue();
        }
    }
}
