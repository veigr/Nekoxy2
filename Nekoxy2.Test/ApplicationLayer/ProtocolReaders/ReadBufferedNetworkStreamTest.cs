using Nekoxy2.ApplicationLayer.ProtocolReaders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Nekoxy2.Test.TestUtil;
using System.Net.Sockets;
using System.Net.Security;
using Nekoxy2.Default.Proxy.Tls;

namespace Nekoxy2.Test.ApplicationLayer.ProtocolReaders
{
    public class ReadBufferedNetworkStreamTest
    {
        [Fact]
        public void ReceiveTest()
        {
            var baseStream = new MemoryStream();
            baseStream.Write(Encoding.ASCII.GetBytes("hoge"), 0, 4);
            baseStream.Position = 0;

            var buffered = new ReadBufferedNetworkStream(baseStream);
            var task = buffered.ReceiveAsync();

            task.Result.IsTrue();

            buffered[0].Is((byte)'h');
            buffered[1].Is((byte)'o');
            buffered[2].Is((byte)'g');
            buffered[3].Is((byte)'e');
        }

        [Fact]
        public void ReadTest()
        {
            var baseStream = new MemoryStream();
            var source = Encoding.ASCII.GetBytes("hoge");
            baseStream.Write(source, 0, source.Length);
            baseStream.Position = 0;

            var buffered = new ReadBufferedNetworkStream(baseStream);
            var tcsRead = new TaskCompletionSource<(byte[] buffer, int size)>();
            buffered.ReadData += x => tcsRead.TrySetResult(x);

            var result = new byte[4];
            buffered.Read(result, 0, result.Length);
            result.Is(source);
            tcsRead.GetResult().buffer.Is(source);

            baseStream.Position = 0;

            var source2 = Encoding.ASCII.GetBytes("fugapiyo");
            baseStream.Write(source2, 0, source2.Length);
            baseStream.Position = 0;

            var result2 = new byte[4];
            buffered.Read(result2, 0, result2.Length);
            result2.Is(source2.Take(4).ToArray());

            buffered.Receive().IsTrue();

            var result3 = new byte[4];
            buffered.Read(result3, 0, result3.Length);
            result3.Is(source2.Skip(4).Take(4).ToArray());
        }

        [Fact]
        public void WriteTest()
        {
            var baseStream = new MemoryStream();
            var buffered = new ReadBufferedNetworkStream(baseStream);

            var source = Encoding.ASCII.GetBytes("hoge");
            buffered.Write(source, 0, source.Length);
            baseStream.Position = 0;
            var result = new byte[source.Length];
            baseStream.Read(result, 0, result.Length);
            result.Is(source);
        }

        [Fact]
        public void FullBufferTest()
        {
            var baseStream = new MemoryStream();
            var source = Encoding.ASCII.GetBytes("hogefuga");
            baseStream.Write(source, 0, source.Length);
            baseStream.Position = 0;

            var buffered = new ReadBufferedNetworkStream(baseStream, 4);
            var result = new byte[source.Length];
            var readSize = buffered.Read(result, 0, result.Length);
            readSize.Is(4);
            result.Is(source.Take(4).Concat(Enumerable.Repeat(new byte(), 4)).ToArray());

            var result2 = new byte[4];
            var readSize2 = buffered.Read(result2, 0, result2.Length);
            readSize2.Is(4);
            result2.Is(source.Skip(4).Take(4).ToArray());
        }

        //[Fact]
        //public void SslTest()
        //{
        //    var host = "www.dmm.com";
        //    var client = new TcpClient(host, 443);
        //    var buffer = new byte[1];
        //    var sourceStream = client.GetStream();
        //    var sslStream = new SslStream(new ReadBufferedNetworkStream(sourceStream));
        //    var outerStream = new ReadBufferedNetworkStream(sslStream);
        //    sslStream.AuthenticateAsClient(host);
        //}
    }
}
