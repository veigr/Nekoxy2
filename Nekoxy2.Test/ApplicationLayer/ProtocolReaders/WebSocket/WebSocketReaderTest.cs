using Nekoxy2.Default;
using Nekoxy2.ApplicationLayer.Entities.WebSocket;
using Nekoxy2.ApplicationLayer.ProtocolReaders.WebSocket;
using Nekoxy2.Test.TestUtil;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Nekoxy2.Test.ApplicationLayer.ProtocolReaders.WebSocket
{
    public class WebSocketReaderTest
    {
        [Fact]
        public void FragmentationWithCompressTest()
        {
            var reader = WebSocketReader.Create(true, ProxyConfig.MaxByteArrayLength);
            var tcs1 = new TaskCompletionSource<WebSocketMessage>();
            reader.MessageReceived += message => tcs1.TrySetResult(message);

            var source = "hogefugapiyo";
            byte[] payloadData1;
            using (var stream = new MemoryStream())
            {
                using (var deflate = new DeflateStream(stream, CompressionMode.Compress, true))
                {
                    var sourceBytes = Encoding.UTF8.GetBytes(source);
                    deflate.Write(sourceBytes, 0, sourceBytes.Length);
                }
                payloadData1 = stream.ToArray();
            }

            var frameData1 = new List<byte>
            {
                0b01000010,
                0b00000100
            };
            frameData1.AddRange(payloadData1.Take(4));
            reader.HandleReceive(frameData1.ToArray(), frameData1.Count);

            var frameData2 = new List<byte>
            {
                0b10000000,
                0b00001010
            };
            frameData2.AddRange(payloadData1.Skip(4).ToArray());
            reader.HandleReceive(frameData2.ToArray(), frameData2.Count);

            var message1 = tcs1.GetResult();
            Encoding.UTF8.GetString(message1.PayloadData).Is(source);


            var message2data = new List<byte>
            {
                0b10000010,
                0b00001100
            };
            message2data.AddRange(Encoding.UTF8.GetBytes(source));

            var tcs2 = new TaskCompletionSource<WebSocketMessage>();
            reader.MessageReceived += message => tcs2.TrySetResult(message);
            reader.HandleReceive(message2data.ToArray(), message2data.Count);
            var message2 = tcs2.GetResult();
            Encoding.UTF8.GetString(message2.PayloadData).Is(source);
        }

        [Fact]
        public void CompressTest()
        {
            var reader = WebSocketReader.Create(true, ProxyConfig.MaxByteArrayLength);
            var tcs1 = new TaskCompletionSource<WebSocketMessage>();
            reader.MessageReceived += message => tcs1.TrySetResult(message);

            var source = "hogefugapiyo";
            byte[] payloadData1;
            using (var stream = new MemoryStream())
            {
                using (var deflate = new DeflateStream(stream, CompressionMode.Compress, true))
                {
                    var sourceBytes = Encoding.UTF8.GetBytes(source);
                    deflate.Write(sourceBytes, 0, sourceBytes.Length);
                }
                payloadData1 = stream.ToArray();
            }

            var frameData1 = new List<byte>
            {
                0b11000010,
                0b00001110
            };
            frameData1.AddRange(payloadData1);
            reader.HandleReceive(frameData1.ToArray(), frameData1.Count);

            var message1 = tcs1.GetResult();
            Encoding.UTF8.GetString(message1.PayloadData).Is(source);


            var message2data = new List<byte>
            {
                0b10000010,
                0b00001100
            };
            message2data.AddRange(Encoding.UTF8.GetBytes(source));

            var tcs2 = new TaskCompletionSource<WebSocketMessage>();
            reader.MessageReceived += message => tcs2.TrySetResult(message);
            reader.HandleReceive(message2data.ToArray(), message2data.Count);
            var message2 = tcs2.GetResult();
            Encoding.UTF8.GetString(message2.PayloadData).Is(source);
        }

        [Fact]
        public void CompressWithMaskTest()
        {
            var reader = WebSocketReader.Create(true, ProxyConfig.MaxByteArrayLength);
            var tcs1 = new TaskCompletionSource<WebSocketMessage>();
            reader.MessageReceived += message => tcs1.TrySetResult(message);

            var source = "hogefugapiyo";
            byte[] payloadData1;
            using (var stream = new MemoryStream())
            {
                using (var deflate = new DeflateStream(stream, CompressionMode.Compress, true))
                {
                    var sourceBytes = Encoding.UTF8.GetBytes(source);
                    deflate.Write(sourceBytes, 0, sourceBytes.Length);
                }
                payloadData1 = stream.ToArray();
            }


            var maskKey = new byte[] { 0b11001100, 0b11001100, 0b11111111, 0b00000000 };
            var frameData1 = new List<byte>
            {
                0b11000010,
                0b10001110,
                maskKey[0],
                maskKey[1],
                maskKey[2],
                maskKey[3]
            };

            for (int i = 0; i < payloadData1.Length; i++)
            {
                payloadData1[i] ^= maskKey[i % 4];
            }
            frameData1.AddRange(payloadData1);
            reader.HandleReceive(frameData1.ToArray(), frameData1.Count);

            var message1 = tcs1.GetResult();
            Encoding.UTF8.GetString(message1.PayloadData).Is(source);


            var message2data = new List<byte>
            {
                0b10000010,
                0b10001100,
                maskKey[0],
                maskKey[1],
                maskKey[2],
                maskKey[3]
            };

            var payloadData2 = Encoding.UTF8.GetBytes(source);
            for (int i = 0; i < payloadData2.Length; i++)
            {
                payloadData2[i] ^= maskKey[i % 4];
            }
            message2data.AddRange(payloadData2);

            var tcs2 = new TaskCompletionSource<WebSocketMessage>();
            reader.MessageReceived += message => tcs2.TrySetResult(message);
            reader.HandleReceive(message2data.ToArray(), message2data.Count);
            var message2 = tcs2.GetResult();
            Encoding.UTF8.GetString(message2.PayloadData).Is(source);
        }

        [Fact]
        public void DecompressErrorTest()
        {
            // .NET Framework 4.7.2 のテストランナーでは再現しないが、.NET Core 2.1 では再現する謎

            //var reader = WebSocketReader.Create(true);
            //var counter = 0;
            //var tcs = new TaskCompletionSource<WebSocketMessage>();
            //reader.MessageReceived += message =>
            //{
            //    counter++;
            //    if(counter == 1)
            //        tcs.TrySetResult(message);
            //};

            //reader.HandleReceive("C1-8E-8C-3F-33-55-BE-3E-B9-33-E8-29-A4-B1-9B-6A-71-51-8C-3F");
            
            //var result = tcs.GetResult();

            var sourceString = "32-01-8A-66-64-16-97-E4-17-55-42-04-00-00";

            using (var dest = new MemoryStream())
            using (var source = new MemoryStream(sourceString.HexToBytes()))
            {
                using (var deflate = new DeflateStream(source, CompressionMode.Decompress))
                {
                    deflate.CopyTo(dest);
                }
                var bytes = dest.ToArray();
            }
        }

        [Fact]
        public void TooLongMessageTest()
        {
            var reader1 = WebSocketReader.Create(true, 7);
            var bigFrame1 = new byte[]
            {
                0b10000010, // FIN, Binary
                0b00001000, // noMask, 8bytes
            }
            .Concat(Enumerable.Repeat((byte)0, 8))
            .ToArray();
            var tcs1 = new TaskCompletionSource<WebSocketMessage>();
            reader1.MessageReceived += m => tcs1.TrySetResult(m);
            reader1.HandleReceive(bigFrame1, bigFrame1.Length);
            var message1 = tcs1.GetResult();
            message1.PayloadData.Length.Is(0);

            var reader2 = WebSocketReader.Create(true, 125);
            var bigFrame2 = new byte[]
            {
                0b10000010,  // FIN, Binary
                0b01111110,  // noMask, 2byteExPart
                0b00000000,  // BigEndian
                0b01111110,  // 126bytes
            }
            .Concat(Enumerable.Repeat((byte)0, 126))
            .ToArray();
            var tcs2 = new TaskCompletionSource<WebSocketMessage>();
            reader2.MessageReceived += m => tcs2.TrySetResult(m);
            reader2.HandleReceive(bigFrame2, bigFrame2.Length);
            var message2 = tcs2.GetResult();
            message2.PayloadData.Length.Is(0);

            var partialFrame1 = new byte[]
            {
                0b00000010, // notFIN, Binary
                0b00000001, // noMask, 1bytes
                0b11111111, // Payload
            };
            var partialFrame2 = new byte[]
            {
                0b10000000, // FIN, Continuation
                0b00000001, // noMask, 1bytes
                0b01010101, // Payload
            };
            var reader3 = WebSocketReader.Create(true, 1);
            var tcs3 = new TaskCompletionSource<WebSocketMessage>();
            reader3.MessageReceived += m => tcs3.TrySetResult(m);
            reader3.HandleReceive(partialFrame1, partialFrame1.Length);
            reader3.HandleReceive(partialFrame2, partialFrame2.Length);
            var message3 = tcs3.GetResult();
            message3.PayloadData.Length.Is(0);
        }

        [Fact]
        public void BinaryFramesTest()
        {
            var reader = WebSocketReader.Create(false, 1024 * 1024 * 1024);
            var messages = new List<WebSocketMessage>();
            reader.MessageReceived += message => messages.Add(message);

            reader.HandleBase64("ggEB");
            reader.HandleBase64("graea1gljF9SOfMKKk77HysftFErUPMGOVfnRjRK6UYuSvIeNUCUfz1d/QM5S/kOKx+vWmJW6wY1ROwS");
            reader.HandleBase64("gp5GOLA5VCS6IytZwlIjTMMDdAmFAyRX31J8S95YNkvYVjJL");
            reader.HandleBase64("gptsAhKNfhsYmgFjYOYJdmG3XjMntw5tfeZWZnfhGGNh");
            reader.HandleBase64("gv4CwETPd0FmcnJL0s19DwmgDSgooxZuceFHYWyYHi8goAAyZIEjYXX/WXF/7yAoKvlDemS3QXV/7wU3fvlEb3TmVwYhrBwua/1HcHT/RnF17zEoNqoRLjzgQXJq/2VpfPkVIHH7QiIg/k4gJ/xAInyuQyB3/RJxcqxBJCKuTnQg/BNwJfgVdl7LGiAtoVVELq5aCxTlZywlvRwkMLxNc3X6TS4soxRrVqIWMy+qAzJ+/UZ0frsFICCqBGtYohYzL6oDMn7lTTIxohogNrZaLSu4WjcrowIsIeVjJDysHyAqqBIyfv5Geze6GiwlvQ5rXqIWMy+qAzJ+/UZ0fq0YLi/1BC8lvwQpK7sEa1OiFjMvqgMyfv1GdH6tGC4v9RMkKLsWMlZPdnVyq0ZyIf9EJ3D4TnZx/0Z3dKlEdiCtQXh0+UFxdvsSI3GrQ3B9rkZwJvdGeHH8Qyd9rREjJvtFcSKpRnB2+08nIaxEdif9FCAh+Ud3Ia5Od3f+QHd3/EdydfcWdCCsT3lw+EIkJvZOJ3z8QXJz9kJyfawRIHP3T3lx+xN5Jv5BW1f+QnV39k5wffhDeXP+Rnly/EZwZt4aIDakEjU39V17N7oaLCW9DmNYohYzL6oDMn7lTTIxohogNrZaLSu4WjcrowIsIe1mLCW9HCQwvE0yMaIaIDamEjJm2xI5J6cWLyOqBHtu5U0yMaIaIDa2VVMprgUqIbsEe27lTTIxohogNrZVUCW8BCQwvE1rbvUENCmiFjM97WsyMaIaIDamEjJ+qBsuJq4beyymEClpuRgtMaISY1+8AiwprgUoIbxNJiigFSAo9RsuM+IBLii6GiRm2gQ0KaIWMy2qBHshtxQpJaEQJH7+RmNXvAIsKa4FKCG8TSwlvRwkMPVda2bdBDQpohYzLaoEeyW8BCQw9V1rZsEaIDakEjU39UVwcfVdaw==");
            reader.HandleBase64("ggQKAggB");
            reader.HandleBase64("gl0SW0oIGD8gpwcopQwyTxWAW8E4HR5LxDglgFvBOC0AAMhDTUmiFz1SCTAuMDAwMDkyMloJMC4wMDAwOTM2YgkwLjAwMDA5MjJqAzQwMHIHMC4wMzcwMnoBMIIBATA=");
            reader.HandleBase64("glgSVkoIGD8grgcorgwyShUV43w8HRXjfDwlFeN8PC0AAEhDTWiRRUBSCDAuMDE1NDM1WggwLjAxNTQzNWIIMC4wMTU0MzVqAzIwMHIFMy4wODd6ATCCAQEw");
            reader.HandleBase64("gn4AfxJ9SggYPyC5Byi7DDJxFfvqqjkdxXO2OSUFM6Y5LQDq7Ec1oIy4tz2ieIG9TQO4J0JSCDAuMDAwMzI2WggwLjAwMDM0OGIIMC4wMDAzMTdqBjEyMTMwMHIHNDEuOTI5N3oPLTAuMDAwMDIxOTk5OTkzggELLTAuMDYzMjE4Mzc=");
            reader.HandleBase64("gmsSaUoIGD8gwAcowgwyXRXBqKQ8HcGopDwllkOLPC0AACBDNeDzQzs9FvIyPk3dJEJAUgYwLjAyMDFaBjAuMDIwMWIFMC4wMTdqAzE2MHIGMy4wMzM1egcwLjAwMjk5ggEKMC4xNzQ3NTE2MYJ+AIgShQFKCBg/IOMJKOsQMnkV+R3cNR35Hdw1JTSryjUtALB+RjVg2QAzPWalmDxNx4HXPFIKMC4wMDAwMDE2NFoKMC4wMDAwMDE2NGIKMC4wMDAwMDE1MWoFMTYzMDByCDAuMDI2MzA3ehEwLjAwMDAwMDAzMDAwMDAyNIIBCzAuMDE4NjMzNTU2");
            reader.HandleBase64("gnsSeUoIGD8gjwoorREybRUbAUs3HQP7eTclrMUnNy0A8KpFNbAPIbU9BoNBvU2m75U9UgkwLjAwMDAxMjFaCTAuMDAwMDE0OWIHMC4wMDAwMWoENTQ3MHIIMC4wNzMyMTF6Ci0wLjAwMDAwMDaCAQwtMC4wNDcyNDQwOTQ=");
            reader.HandleBase64("gn4AlBKRAUoIGBIgzgoo/iQyhAEVt9GgPx1mZqY/JeF6nD8t/W8pSjUAGeK7PU75srtNEa5VSlIGMS4yNTY0WgMxLjNiBjEuMjIyNWoTMjc3NjA2My4xOTU2MjQ4MDMwOXIXMzUwMDkzMi4yMDE1ODY0MDE3NTg3NTl6DC0wLjAwNjg5OTk1M4IBDS0wLjAwNTQ2MTg0ODM=");
            reader.HandleBase64("gnsSeUoIGD8g2AooiiUybRVeEbw+HY51wT4lXhG8Pi0AALhANQDhtLo99Up1u01q7AdAUgcwLjM2NzMyWgcwLjM3Nzg1YgcwLjM2NzMyagQ1Ljc1cgkyLjEyMzgwNDZ6DS0wLjAwMTM3OTk5NjWCAQwtMC4wMDM3NDI4NzE=");
            reader.HandleBase64("gn4AnBKZAUoIGBIggwIo/SQyjAEVGCazPh0sZbk+JQXFrz4tEghfSzVAEoO7PWcuObxNZMKdSlIGMC4zNDk5WgYwLjM2MjFiBjAuMzQzM2oWMTQ2MTY1OTQuMjM0OTQ1MjA1NzA2NHIZNTE2OTQ1OC4yMTg3NDE0MDE5MDcyOTg5NnoNLTAuMDAzOTk5OTc4M4IBDC0wLjAxMTMwMjU2N4JfEl1KCBg/IIkFKJULMlEV/LuIOB07QIs4JY5UhDgtAOIBSE335ApBUgkwLjAwMDA2NTJaCTAuMDAwMDY2NGIJMC4wMDAwNjMxagYxMzMwMDByBjguNjgwOXoBMIIBATCCfgCIEoUBSggYPyDCBij7CjJ5FdKNsDodobq5OiWd2a46LTMnwkY1AFTJtj1RUJG7TYK1BkJSCDAuMDAxMzQ3WggwLjAwMTQxN2IIMC4wMDEzMzRqBzI0ODUxLjZyCzMzLjY3NzI1NDg3ehAtMC4wMDAwMDYwMDAwNDU3ggENLTAuMDA0NDM0NjIzMw==");
            reader.HandleBase64("gn4AfxJ9SgcYPyA8KOsKMnIVmDUIQB2zJRBAJZg1CEAtZmaEQTXAp/W9PXuIWr1NiWcSQlIIMi4xMjgyNzFaCDIuMjUyMzAxYggyLjEyODI3MWoFMTYuNTVyCzM2LjYwMTEwNzIyegwtMC4xMTk5NDg4NjSCAQwtMC4wNTMzNTI4MTQ=");

            messages.Count.Is(18);
            foreach (var message in messages)
            {
                message.Opcode.Is(Spi.Entities.WebSocket.WebSocketOpcode.Binary);
            }

            //reader.HandleReceive("82-7E-00-88-12-85-01-4A-08-18-0B-20-88-02-28-99-03-32-79-15-40-C6-D2-48-1D-40-8C-DD-48-25-00-02-D0-48-2D-95-D5-A5-48-35-00-04-42-C6-3D-72-0E-E5-BC-4D-F6-54-0B-52-52-06-34-33-31-36-36-36-5A-06-34-35-33-37-33-30-62-06-34-32-36-30-30-30-6A-0F-33-33-39-36-32-38-2E-36-37-31-32-38-31-34-38-72-15-31-34-39-36-30-36-34-35-39-30-31-34-2E-32-30-35-33-37-30-37-36-7A-06-2D-31-32-34-31-37-82-01-0B-2D-30-2E-30-32-37-39-36-30-39-39-82-7E-00-81-12-7F-4A-08-18-3F-20-B7-01-28-D9-0A-32-73-15-19-1D-10-3B-1D-9F-C8-13-3B-25-19-1D-10-3B-2D-85-EB-B1-41-35-80-E1-6A-B8-3D-04-70-CB-BC-4D-EE-B9-48-3D-52-08-30-2E-30-30-32-31-39-39-5A-08-30-2E-30-30-32-32-35-35-62-08-30-2E-30-30-32-31-39-39-6A-05-32-32-2E-32-34-72-0A-30-2E-30-34-39-30-30-35-34-34-7A-0E-2D-30-2E-30-30-30-30-35-35-39-39-39-39-36-82-01-0C-2D-30-2E-30-32-34-38-33-33-36-38-37-82-56-12-54-4A-08-18-3F-20-CE-06-28-8A-0B-32-48-15-28-2C-71-39-1D-28-2C-71-39-25-28-2C-71-39-2D-00-00-F0-42-4D-65-19-E2-3C-52-07-30-2E-30-30-30-32-33-5A-07-30-2E-30-30-30-32-33-62-07-30-2E-30-30-30-32-33-6A-03-31-32-30-72-06-30-2E-30-32-37-36-7A-01-30-82-01-01-30-82-73-12-71-4A-08-18-3F-20-9B-07-28-91-0C-32-65-15-3F-A9-76-3D-1D-9A-99-99-3D-25-3F-A9-76-3D-2D-00-CA-14-47-35-14-3F-46-BC-3D-D3-53-2B-BE-4D-90-71-20-45-52-07-30-2E-30-36-30-32-32-5A-05-30-2E-30-37-35-62-07-30-2E-30-36-30-32-32-6A-05-33-38-30-39-30-72-09-32-35-36-37-2E-30-39-37-36-7A-07-2D-30-2E-30-31-32-31-82-01-0B-2D-30-2E-31-36-37-33-31-31-39-35-82-71-12-6F-4A-08-18-3F-20-C0-06-28-F9-0A-32-63-15-CD-CC-94-41-1D-CD-CC-94-41-25-88-4B-89-41-2D-66-66-B6-41-35-E0-D6-4D-3F-3D-FC-10-39-3D-4D-1B-CF-C6-43-52-04-31-38-2E-36-5A-04-31-38-2E-36-62-08-31-37-2E-31-36-31-38-38-6A-04-32-32-2E-38-72-0B-33-39-37-2E-36-31-38-30-31-35-31-7A-07-30-2E-38-30-34-30-36-82-01-0B-30-2E-30-34-35-31-38-32-32-31-33-82-7C-12-7A-4A-08-18-3F-20-AC-05-28-8B-0C-32-6E-15-E8-69-C0-39-1D-36-8F-C3-39-25-E8-69-C0-39-2D-00-00-20-42-35-80-53-C9-B6-3D-47-C6-83-BC-4D-B3-7B-72-3C-52-08-30-2E-30-30-30-33-36-37-5A-08-30-2E-30-30-30-33-37-33-62-08-30-2E-30-30-30-33-36-37-6A-02-34-30-72-06-30-2E-30-31-34-38-7A-10-2D-30-2E-30-30-30-30-30-35-39-39-39-39-38-37-35-82-01-0C-2D-30-2E-30-31-36-30-38-35-37-35-37-82-71-12-6F-4A-08-18-3F-20-DB-06-28-AA-0B-32-63-15-58-39-34-3D-1D-CD-CC-4C-3D-25-58-39-34-3D-2D-00-90-D2-45-35-A8-9B-C4-BB-3D-92-C2-F5-BD-4D-3D-44-96-43-52-05-30-2E-30-34-34-5A-04-30-2E-30-35-62-05-30-2E-30-34-34-6A-04-36-37-33-38-72-08-33-30-30-2E-35-33-33-31-7A-0C-2D-30-2E-30-30-36-30-30-30-30-30-31-82-01-0B-2D-30-2E-31-32-30-30-30-30-30-32-82-7B-12-79-4A-08-18-3F-20-D9-06-28-A6-0B-32-6D-15-82-A8-7B-38-1D-F7-CC-92-38-25-82-A8-7B-38-2D-9A-99-20-43-35-B0-C5-27-B7-3D-28-49-12-BE-4D-2C-2C-38-3C-52-07-30-2E-30-30-30-30-36-5A-07-30-2E-30-30-30-30-37-62-07-30-2E-30-30-30-30-36-6A-05-31-36-30-2E-36-72-08-30-2E-30-31-31-32-34-31-7A-0F-2D-30-2E-30-30-30-30-31-30-30-30-30-30-30-33-82-01-0A-2D-30-2E-31-34-32-38-35-37-32-82-79-12-77-4A-08-18-3F-20-CB-07-28-CE-0C-32-6B-15-3B-AA-1A-3B-1D-3B-AA-1A-3B-25-F7-CC-12-3B-2D-00-B0-33-46-35-80-A8-FB-38-3D-B4-6D-5B-3D-4D-D9-BD-D6-41-52-07-30-2E-30-30-32-33-36-5A-07-30-2E-30-30-32-33-36-62-07-30-2E-30-30-32-32-34-6A-05-31-31-35-30-30-72-07-32-36-2E-38-34-32-37-7A-0D-30-2E-30-30-30-31-31-39-39-39-39-39-38-82-01-0B-30-2E-30-35-33-35-37-31-34-31-38-82-7E-00-87-12-84-01-4A-08-18-3F-20-BF-08-28-DF-0D-32-78-15-6C-EF-8D-36-1D-CE-A8-96-36-25-A5-32-83-36-2D-00-70-78-46-35-00-F6-76-B4-3D-9D-3A-53-BD-4D-CA-16-89-3D-52-0A-30-2E-30-30-30-30-30-34-32-33-5A-0A-30-2E-30-30-30-30-30-34-34-39-62-0A-30-2E-30-30-30-30-30-33-39-31-6A-05-31-35-39-30-30-72-08-30-2E-30-36-36-39-33-38-7A-10-2D-30-2E-30-30-30-30-30-30-32-33-30-30-30-30-33-82-01-0B-2D-30-2E-30-35-31-35-36-39-35-37-82-7E-00-7E-12-7C-4A-08-18-3F-20-BE-06-28-F6-0A-32-70-15-AE-12-34-3F-1D-B3-CD-3D-3F-25-33-33-33-3F-2D-D3-0D-88-47-35-40-EF-7E-BC-3D-A1-4A-B1-BC-4D-9C-23-42-47-52-07-30-2E-37-30-33-34-31-5A-07-30-2E-37-34-31-34-32-62-03-30-2E-37-6A-08-36-39-36-35-39-2E-36-35-72-0D-34-39-36-39-39-2E-36-30-39-33-38-34-33-7A-0C-2D-30-2E-30-31-35-35-35-39-39-37-31-82-01-0C-2D-30-2E-30-32-31-36-34-32-30-33-31-82-7E-00-80-12-7E-4A-08-18-3F-20-93-08-28-B3-0D-32-72-15-14-6D-58-37-1D-87-97-71-37-25-9C-53-49-37-2D-00-E0-DD-45-35-98-53-C9-B5-3D-52-55-D5-BD-4D-AA-0E-B9-3D-52-09-30-2E-30-30-30-30-31-32-39-5A-09-30-2E-30-30-30-30-31-34-34-62-08-30-2E-30-30-30-30-31-32-6A-04-37-31-30-30-72-07-30-2E-30-39-30-33-36-7A-10-2D-30-2E-30-30-30-30-30-31-34-39-39-39-39-39-36-82-01-0B-2D-30-2E-31-30-34-31-36-36-36-34-82-74-12-72-4A-08-18-3F-20-BA-06-28-EF-0A-32-66-15-8F-C2-75-3C-1D-DA-AC-7A-3C-25-8F-C2-75-3C-2D-A4-70-BD-3E-35-60-49-9D-B9-3D-AF-A0-A0-BC-4D-17-D4-B7-3B-52-05-30-2E-30-31-35-5A-06-30-2E-30-31-35-33-62-05-30-2E-30-31-35-6A-04-30-2E-33-37-72-07-30-2E-30-30-35-36-31-7A-0E-2D-30-2E-30-30-30-33-30-30-30-30-30-34-32-82-01-0B-2D-30-2E-30-31-39-36-30-37-38-37-82-24-12-22-4A-08-18-3F-20-EB-0A-28-A0-25-32-16-52-01-30-5A-01-30-62-01-30-6A-01-30-72-01-30-7A-01-30-82-01-01-30-82-76-12-74-4A-08-18-3F-20-DF-09-28-E5-10-32-68-15-8F-8D-C0-3C-1D-C3-2B-C9-3C-25-8F-8D-C0-3C-2D-00-00-16-44-35-F0-19-89-BA-3D-C8-82-2E-BD-4D-74-24-69-41-52-08-30-2E-30-32-33-35-30-35-5A-08-30-2E-30-32-34-35-35-37-62-08-30-2E-30-32-33-35-30-35-6A-03-36-30-30-72-07-31-34-2E-35-37-31-34-7A-09-2D-30-2E-30-30-31-30-34-36-82-01-0B-2D-30-2E-30-34-32-36-30-35-31-39-82-24-12-22-4A-08-18-01-20-93-05-28-F1-08-32-16-52-01-30-5A-01-30-62-01-30-6A-01-30-72-01-30-7A-01-30-82-01-01-30-82-7E-00-92-12-8F-01-4A-08-18-01-20-E7-04-28-AE-08-32-82-01-15-9F-9D-42-37-1D-43-D2-61-37-25-E9-A4-2D-37-2D-AF-9C-A4-47-35-B0-C5-A7-35-3D-D5-57-F7-3D-4D-65-E6-7D-3F-52-09-30-2E-30-30-30-30-31-31-36-5A-0A-30-2E-30-30-30-30-31-33-34-36-62-0A-30-2E-30-30-30-30-31-30-33-35-6A-0B-38-34-32-38-31-2E-33-36-37-38-34-72-0E-30-2E-39-39-31-37-39-36-37-37-35-32-32-35-7A-0F-30-2E-30-30-30-30-30-31-32-35-30-30-30-30-34-82-01-0B-30-2E-31-32-30-37-37-32-39-39-35-82-7E-00-96-12-93-01-4A-07-18-15-20-3F-28-AB-02-32-87-01-15-23-5C-0E-3F-1D-E9-5E-0F-3F-25-D2-11-0B-3F-2D-FE-DC-12-42-35-00-EC-71-BB-3D-AF-15-D8-BB-4D-15-98-A1-41-52-09-30-2E-35-35-36-30-39-33-34-5A-0A-30-2E-35-36-30-30-34-31-39-36-62-0A-30-2E-35-34-33-32-34-30-36-37-6A-0B-33-36-2E-37-31-35-38-31-32-33-36-72-13-32-30-2E-31-39-39-32-35-38-39-31-38-39-37-32-35-34-30-34-7A-0D-2D-30-2E-30-30-33-36-39-31-34-33-34-39-82-01-0D-2D-30-2E-30-30-36-35-39-34-33-38-31-38-82-62-12-60-4A-08-18-01-20-D2-0B-28-92-28-32-54-15-1E-E1-34-3B-1D-1E-E1-34-3B-25-1E-E1-34-3B-2D-22-AA-2B-43-4D-29-95-F2-3E-52-07-30-2E-30-30-32-37-36-5A-07-30-2E-30-30-32-37-36-62-07-30-2E-30-30-32-37-36-6A-09-31-37-31-2E-36-36-34-35-38-72-0C-30-2E-34-37-33-37-39-34-32-34-30-38-7A-01-30-82-01-01-30-82-7A-12-78-4A-07-18-0B-20-32-28-DA-01-32-6D-15-C0-8A-D2-48-1D-20-09-DD-48-25-00-18-D0-48-2D-08-BC-2E-42-35-00-40-2B-C5-3D-E1-E8-CE-BB-4D-3D-FF-91-4B-52-06-34-33-31-31-39-30-5A-06-34-35-32-36-38-31-62-06-34-32-36-31-37-36-6A-09-34-33-2E-36-38-33-36-32-36-72-0E-31-39-31-33-36-31-32-31-2E-34-30-30-34-36-7A-05-2D-32-37-34-30-82-01-0D-2D-30-2E-30-30-36-33-31-34-33-38-32-34-82-7E-00-95-12-92-01-4A-08-18-01-20-A0-05-28-8A-09-32-85-01-15-E3-44-89-39-1D-E3-44-89-39-25-B8-56-7F-39-2D-97-2A-8C-46-35-70-98-99-37-3D-44-FE-99-3D-4D-F8-95-91-40-52-0A-30-2E-30-30-30-32-36-31-38-32-5A-0A-30-2E-30-30-30-32-36-31-38-32-62-0A-30-2E-30-30-30-32-34-33-35-31-6A-0C-31-37-39-34-31-2E-32-39-35-35-31-32-72-10-34-2E-35-34-39-35-35-36-36-32-36-32-34-38-32-31-7A-0E-30-2E-30-30-30-30-31-38-33-31-30-30-30-36-82-01-0B-30-2E-30-37-35-31-39-32-30-30-34");
        }
    }

    static partial class Ex
    {
        public static void HandleReceive(this WebSocketReader reader, string value)
        {
            var bytes = value.HexToBytes();
            reader.HandleReceive(bytes, bytes.Length);
        }

        public static void HandleBase64(this WebSocketReader reader, string base64)
        {
            var bytes = Convert.FromBase64String(base64);
            reader.HandleReceive(bytes, bytes.Length);
        }
    }
}
