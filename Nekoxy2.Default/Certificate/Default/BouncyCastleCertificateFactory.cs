using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.X509;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Nekoxy2.Default.Certificate.Default
{
    internal sealed class BouncyCastleCertificateFactory : ICertificateFactory
    {
        private static readonly int rsaKeyStrength = 2048;

        private static readonly DerObjectIdentifier signatureAlgorithm
            = PkcsObjectIdentifiers.Sha256WithRsaEncryption;

        public X509Certificate2 CreateServerCertificate(string hostName, X509Certificate2 rootCert)
            => this.CreateCertificate(rootCert.Subject, hostName.AddCn(), rootCert);

        public X509Certificate2 CreateRootCertificate(string issuerName)
        {
            var subject = issuerName.AddCn();
            return this.CreateCertificate(subject, subject);
        }

        private X509Certificate2 CreateCertificate(
            string issuer,
            string subject,
            X509Certificate2 rootCert = null,  // サーバー証明書に署名するCA証明書
            int validFromDays = -365,
            int validToDays = 3650
            )
        {
            var secureRandom = new SecureRandom(new CryptoApiRandomGenerator());

            var generator = new X509V3CertificateGenerator();
            generator.SetSerialNumber(BigIntegers.CreateRandomInRange(
                BigInteger.One,
                BigInteger.ValueOf(long.MaxValue),
                secureRandom));
            generator.SetIssuerDN(new X509Name(issuer));
            generator.SetSubjectDN(new X509Name(subject));
            generator.SetNotBefore(DateTime.UtcNow.AddDays(validFromDays));
            generator.SetNotAfter(DateTime.UtcNow.AddDays(validToDays));

            if (rootCert != null)
            {
                var host = subject.RemoveCn();
                var subjectAlternativeNames = new DerSequence(new[] { new GeneralName(GeneralName.DnsName, host) });
                generator.AddExtension(X509Extensions.SubjectAlternativeName.Id, false, subjectAlternativeNames);
            }

            var rsa = new RSACryptoServiceProvider(rsaKeyStrength);
            var subjectKeyPair = DotNetUtilities.GetKeyPair(rsa);
            generator.SetPublicKey(subjectKeyPair.Public);

            if (rootCert == null)
                // CA
                generator.AddExtension(X509Extensions.BasicConstraints.Id, true, new BasicConstraints(true));
            else
                // サーバー証明限定
                generator.AddExtension(X509Extensions.ExtendedKeyUsage.Id, false, new ExtendedKeyUsage(KeyPurposeID.IdKPServerAuth));

            var certificate = generator.Generate(new Asn1SignatureFactory(
                signatureAlgorithm.Id,
                // CA による署名 or 自己署名
                rootCert != null ? DotNetUtilities.GetKeyPair(rootCert.PrivateKey).Private : subjectKeyPair.Private,
                secureRandom));

            return certificate.AddPrivateKey(subjectKeyPair.Private);
        }

    }

    internal static partial class X509CertificateExtensions
    {
        private static readonly string privateKeyPassword = "password";

        public static X509Certificate2 AddPrivateKey(this Org.BouncyCastle.X509.X509Certificate cert, AsymmetricKeyParameter privateKey)
        {
            var keyInfo = PrivateKeyInfoFactory.CreatePrivateKeyInfo(privateKey);
            var seq = Asn1Object.FromByteArray(keyInfo.ParsePrivateKey().GetDerEncoded()) as Asn1Sequence;
            if (seq.Count != 9)
                throw new PemException("malformed sequence in RSA private key");
            var rsa = RsaPrivateKeyStructure.GetInstance(seq);
            var rsaParams = new RsaPrivateKeyStructure(
                                    rsa.Modulus,
                                    rsa.PublicExponent,
                                    rsa.PrivateExponent,
                                    rsa.Prime1,
                                    rsa.Prime2,
                                    rsa.Exponent1,
                                    rsa.Exponent2,
                                    rsa.Coefficient);
            var store = new Pkcs12Store();
            var certEntry = new X509CertificateEntry(cert);
            store.SetCertificateEntry(cert.SubjectDN.ToString(), certEntry);
            store.SetKeyEntry(cert.SubjectDN.ToString(), new AsymmetricKeyEntry(privateKey), new[] { certEntry });
            using (var stream = new MemoryStream())
            {
                store.Save(stream, privateKeyPassword.ToCharArray(), new SecureRandom(new CryptoApiRandomGenerator()));
                return new X509Certificate2(stream.ToArray(), privateKeyPassword, X509KeyStorageFlags.Exportable);
            }
        }
    }
}
