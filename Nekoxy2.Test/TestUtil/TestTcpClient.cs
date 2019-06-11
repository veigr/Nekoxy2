using Nekoxy2.Default.Proxy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using Nekoxy2.Default.Proxy.Tcp;

namespace Nekoxy2.Test.TestUtil
{
    class TestTcpClient : ITcpClient
    {
        public bool NoDelay { get; set; }
        public bool Connected => true;
        public bool IsClosed { get; private set; }
        public bool IsDisposed { get; private set; }
        public string Host { get; }
        public int Port { get; }
        private readonly TestNetworkStream stream;

        public byte[] LastOutputBytes { get; private set; }
        public string LastOutputString { get; private set; }

        public CloseState CloseState { get; private set; }

        public TestTcpClient()
        {
            this.stream = new TestNetworkStream();
        }

        public TestTcpClient(string host, int port) : this()
        {
            this.Host = host;
            this.Port = port;
        }

        public void Shutdown(SocketShutdown how)
        {
            switch (how)
            {
                case SocketShutdown.Receive:
                    this.CloseState |= CloseState.ReceiveClosed;
                    break;
                case SocketShutdown.Send:
                    this.CloseState |= CloseState.SendClosed;
                    break;
                case SocketShutdown.Both:
                    this.CloseState |= CloseState.Both;
                    break;
            }
        }

        public void Close()
        {
            this.IsClosed = true;
            try
            {
                this.LastOutputBytes = this.ReadAllBytesFromOutput();
                this.LastOutputString = this.ReadAllStringFromOutput();
                this.stream.Close();
            }
            catch (ObjectDisposedException) { }
        }

        public void Dispose()
        {
            this.Close();
            this.IsDisposed = true;
            this.stream.Dispose();
        }

        public Stream GetStream()
        {
            return stream;
        }

        public int ReadInput(byte[] buffer, int offset, int count)
            => this.stream.InputStream.Read(buffer, offset, count);

        public int ReadOutput(byte[] buffer, int offset, int count)
            => this.stream.OutputStream.Read(buffer, offset, count);

        public byte[] ReadAllBytesFromOutput()
        {
            lock (this.stream)
            {
                var bytes = new byte[this.stream.OutputStream.Length];
                this.stream.OutputStream.Position = 0;
                this.stream.OutputStream.Read(bytes, 0, bytes.Length);
                return bytes;
            }
        }

        public string ReadAllStringFromOutput()
            => Encoding.UTF8.GetString(this.ReadAllBytesFromOutput());

        public void WriteToInput(byte[] bytes)
        {
            lock (this.stream)
            {
                this.stream.InputStream.Write(bytes, 0, bytes.Length);
                this.stream.InputStream.Flush();
                this.stream.InputStream.Position -= bytes.Length;
            }
        }

        public void WriteToInput(string value)
            => this.WriteToInput(Encoding.ASCII.GetBytes(value));

        public void WriteLineToInput(string value = "")
            => this.WriteToInput($"{value}\r\n");

        public void WriteFileToInput(string path)
            => this.WriteToInput(File.ReadAllBytes(path));
    }
}
