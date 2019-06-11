using Nekoxy2.Default;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Nekoxy2.Test.Default
{
    public class ProxyConfigTest
    {
        [Fact]
        public void CacheLocationsTest()
        {
            var config = new DecryptConfig();
            config.CacheLocationFlags.HasFlag(CertificateCacheLocation.Memory).IsTrue();
            config.CacheLocationFlags.HasFlag(CertificateCacheLocation.Store).IsTrue();
            config.CacheLocationFlags.HasFlag(CertificateCacheLocation.Custom).IsTrue();

            config.CacheLocations = new CertificateCacheLocation[0];
            config.CacheLocationFlags.HasFlag(CertificateCacheLocation.Memory).IsFalse();
            config.CacheLocationFlags.HasFlag(CertificateCacheLocation.Store).IsFalse();
            config.CacheLocationFlags.HasFlag(CertificateCacheLocation.Custom).IsFalse();

            config.CacheLocations = new[] { CertificateCacheLocation.Memory };
            config.CacheLocationFlags.HasFlag(CertificateCacheLocation.Memory).IsTrue();
            config.CacheLocationFlags.HasFlag(CertificateCacheLocation.Store).IsFalse();
            config.CacheLocationFlags.HasFlag(CertificateCacheLocation.Custom).IsFalse();

            config.CacheLocations = new[] { CertificateCacheLocation.Memory, CertificateCacheLocation.Custom, CertificateCacheLocation.Store };
            config.CacheLocationFlags.HasFlag(CertificateCacheLocation.Memory).IsTrue();
            config.CacheLocationFlags.HasFlag(CertificateCacheLocation.Store).IsTrue();
            config.CacheLocationFlags.HasFlag(CertificateCacheLocation.Custom).IsTrue();

            config.CacheLocations = new[] { CertificateCacheLocation.Memory, CertificateCacheLocation.Store, CertificateCacheLocation.Store };
            config.CacheLocationFlags.HasFlag(CertificateCacheLocation.Memory).IsTrue();
            config.CacheLocationFlags.HasFlag(CertificateCacheLocation.Store).IsTrue();
            config.CacheLocationFlags.HasFlag(CertificateCacheLocation.Custom).IsFalse();
        }
    }
}
