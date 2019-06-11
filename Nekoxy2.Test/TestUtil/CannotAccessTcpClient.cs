using Nekoxy2.Default.Proxy;
using Nekoxy2.Default.Proxy.Tcp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Nekoxy2.Test.TestUtil
{
    class CannotAccessTcpClient : ITcpClient
    {
        public CannotAccessTcpClient()
        {
            throw new SocketException();
        }

        public bool NoDelay
        {
            get => true;
            set { }
        }

        public bool Connected => true;

        public CloseState CloseState => CloseState.Both;

        public void Close()
        {
        }

        public void Dispose()
        {
        }

        public Stream GetStream()
        {
            return null;
        }

        public void Shutdown(SocketShutdown how)
        {
        }
    }
}
