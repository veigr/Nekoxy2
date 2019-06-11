using Nekoxy2.Default.Certificate.Default;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Nekoxy2.Test.TestUtil
{
    class X509TestStore : IX509Store
    {
        private readonly object lockObject = new object();

        public X509Certificate2Collection Certificates { get; }
            = new X509Certificate2Collection();

        public OpenFlags Flags { get; private set; }

        public bool IsOpen { get; private set; }

        public void Add(X509Certificate2 certificate)
        {
            if (!this.IsOpen)
                throw new Exception("Store is not open.");

            lock(this.lockObject)
                this.Certificates.Add(certificate);
        }

        public void Remove(X509Certificate2 certificate)
        {
            if (!this.IsOpen)
                throw new Exception("Store is not open.");

            lock (this.lockObject)
                this.Certificates.Remove(certificate);
        }

        public void Open(OpenFlags flags)
        {
            this.Flags = flags;
            this.IsOpen = true;
        }

        public void Dispose()
        {
            this.IsOpen = false;
        }
    }
}
