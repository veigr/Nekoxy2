using Nekoxy2.Default.Proxy;
using Nekoxy2.Default.Proxy.Tcp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Nekoxy2.Test.TestUtil
{
    class TestTcpServer : ITcpServer
    {
        public int Port { get; private set; }
        public bool IsStartupCalled { get; private set; }
        public bool IsShutdownCalled { get; private set; }

        public event Action<ITcpClient> AcceptTcpClient;
        public event EventHandler<Exception> FatalException;

        public void Startup(IPAddress localAddress, ushort port)
        {
            this.Port = port;
            this.IsStartupCalled = true;
        }

        public void Shutdown()
        {
            this.IsShutdownCalled = true;
        }

        public void AcceptTcp(ITcpClient client)
            => this.AcceptTcpClient?.Invoke(client);
    }
}
