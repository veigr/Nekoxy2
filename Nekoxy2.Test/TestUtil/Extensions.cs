using Nekoxy2.Default.Proxy;
using Nekoxy2.Default.Proxy.Tcp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Nekoxy2.Test.TestUtil
{
    static class Extensions
    {
        private static readonly object lockObject = new object();
        public static void WriteFile(this HttpConnection connection, string path)
        {
            byte[] bytes;
            lock (lockObject)
            {
                bytes = File.ReadAllBytes(path);
            }
            connection.Write(bytes);
        }

        public static void Write(this HttpConnection connection, string value)
            => connection.Write(Encoding.ASCII.GetBytes(value));

        private static void Write(this HttpConnection connection, byte[] bytes)
        {
            connection.Write(bytes, bytes.Length);
        }

        public static void Is(this byte[] bytes, string filePath)
        {
            Assert.Equal(bytes, File.ReadAllBytes(filePath));
        }

        public static byte[] HexToBytes(this string value, string splitter = "-")
        {
            return value
                .Replace(splitter, "")
                .Select((c, i) => (c, i))
                .GroupBy(x => x.i / 2, x => x.c)
                .Select(x => new string(x.ToArray()))
                .Select(x => Convert.ToByte(x, 16))
                .ToArray();
        }

        public static string ToBase64(this byte[] value)
            => Convert.ToBase64String(value);

        public static byte[] FromBase64(this string value)
            => Convert.FromBase64String(value);

        public static TestTcpClient AsTest(this ITcpClient client)
            => client as TestTcpClient;

        public static TestNetworkStream AsTest(this Stream stream)
            => stream as TestNetworkStream;

        public static T GetResult<T>(this TaskCompletionSource<T> source, int millisecondsTimeout = 5000)
        {
#if !DEBUG
            if (!source.Task.Wait(millisecondsTimeout))
                Assert.False(true, "timeout");
#endif
            return source.Task.Result;
        }

        public static void Is(this Stream stream, string expected, int millisecondsTimeout = 5000)
        {
            stream.Is(Encoding.ASCII.GetBytes(expected), millisecondsTimeout);
        }

        public static void Is(this Stream stream, byte[] expectedBytes, int millisecondsTimeout = 5000)
        {
            var start = DateTimeOffset.Now;

            while (DateTimeOffset.Now - start < TimeSpan.FromMilliseconds(millisecondsTimeout)
            && stream.Length < expectedBytes.Length)
            {
                Thread.Sleep(100);
            }

#if !DEBUG
            if (stream.Length < expectedBytes.Length)
            {
                Assert.False(true, "timeout");
                return;
            }
#endif
            var actualBytes = new byte[stream.Length];
            stream.Position = 0;
            stream.Read(actualBytes, 0, actualBytes.Length);
            actualBytes.Is(expectedBytes);
        }

        public static void IsEndWith(this Stream stream, byte[] expectedBytes, int millisecondsTimeout = 5000)
        {
            var start = DateTimeOffset.Now;

            while (DateTimeOffset.Now - start < TimeSpan.FromMilliseconds(millisecondsTimeout)
            && stream.Length < expectedBytes.Length)
            {
                Thread.Sleep(100);
            }

#if !DEBUG
            if (stream.Length < expectedBytes.Length)
            {
                Assert.False(true, "timeout");
                return;
            }
#endif
            var actualBytes = new byte[expectedBytes.Length];
            stream.Position = stream.Length - expectedBytes.Length;
            stream.Read(actualBytes, 0, actualBytes.Length);
            actualBytes.Is(expectedBytes);
        }

        public static void Is(this byte actual, byte expected)
        {
            Assert.Equal(expected, actual);
        }
    }
}
