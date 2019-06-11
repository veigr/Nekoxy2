using Nekoxy2.ApplicationLayer.Entities.Http;
using Nekoxy2.ApplicationLayer.MessageBodyParsers;
using System;
using System.IO;
using System.Text;

namespace Nekoxy2.ApplicationLayer.ProtocolReaders.Http
{
    /// <summary>
    /// 通信データを入力し HTTP/1.1 プロトコルとして読み取る抽象クラス
    /// </summary>
    internal abstract class AbstractHttpReader : IProtocolReader
    {
        /// <summary>
        /// ボディをキャプチャするかどうか
        /// </summary>
        private readonly bool isCaptureBody;

        /// <summary>
        /// キャプチャするボディやペイロード長の最大サイズ
        /// </summary>
        private readonly int maxCaptureSize;

        /// <summary>
        /// スタートライン
        /// </summary>
        private string startLine;

        /// <summary>
        /// スタートラインバッファー
        /// </summary>
        private MemoryStream startLineStream = new MemoryStream();

        /// <summary>
        /// ヘッダー
        /// </summary>
        protected internal HttpHeaders Headers { get; protected set; }

        /// <summary>
        /// ヘッダーバッファー
        /// </summary>
        private MemoryStream headersStream = new MemoryStream();

        /// <summary>
        /// ボディー
        /// </summary>
        protected byte[] Body { get; private set; }

        /// <summary>
        /// トレイラー
        /// </summary>
        protected HttpHeaders Trailers { get; private set; }

        /// <summary>
        /// メッセージボディー解析器
        /// </summary>
        private IMessageBodyParser messageBody = new EmptyBodyParser();

        /// <summary>
        /// 解析中のパート
        /// </summary>
        private ParsePart part = ParsePart.StartLine;

        /// <summary>
        /// 読み取りロック
        /// </summary>
        private readonly object readLock = new object();

        /// <summary>
        /// LF 文字
        /// </summary>
        private readonly byte LF = Convert.ToByte('\n');

        protected AbstractHttpReader(bool isCaptureBody, int maxCaptureSize)
        {
            this.isCaptureBody = isCaptureBody;
            this.maxCaptureSize = maxCaptureSize;
        }

        #region Receive

        /// <summary>
        /// データ受信
        /// </summary>
        /// <param name="buffer">受信バッファー</param>
        /// <param name="readSize">読み取りサイズ</param>
        public void HandleReceive(byte[] buffer, int readSize)
        {
            lock (this.readLock)
            {
                var readed = 0;
                var position = -1;
                while (readed < readSize)
                {
                    position = Array.IndexOf(buffer, this.LF, position + 1);
                    var length = -1 < position ? position - readed + 1 : readSize - readed;
                    var bytes = new byte[length];
                    Buffer.BlockCopy(buffer, readed, bytes, 0, length);
                    // LF まで切り取って処理
                    switch (this.part)
                    {
                        case ParsePart.StartLine:
                            this.InputStartLine(bytes);
                            break;
                        case ParsePart.Headers:
                            this.InputHeaders(bytes);
                            break;
                        case ParsePart.Body:
                            var readsize = this.InputBody(bytes);
                            readed -= length - readsize;
                            break;
                        default:
                            break;
                    }
                    readed += length;
                }
            }
        }

        /// <summary>
        /// TCP 切断
        /// </summary>
        public void CloseTcp()
        {
            lock (this.readLock)
            {
                this.TerminateBody();
            }
        }

        #endregion

        #region StartLine

        /// <summary>
        /// スタートラインデータを入力
        /// </summary>
        /// <param name="buffer">入力データ</param>
        private void InputStartLine(byte[] buffer)
        {
            if (buffer.Length < 1)
                return;

            foreach (var b in buffer)
            {
                this.startLineStream.WriteByte(b);
            }

            // StartLine, Headers の行終端は LF だけで識別して良い RFC7230 3.5
            if (buffer[buffer.Length - 1] != this.LF)
                return;

            this.startLine = Encoding.ASCII.GetString(this.startLineStream.ToArray());

            // StartLine に先行する空行は無視すべき RFC7230 3.5
            if (this.startLine.Trim().Length == 0)
            {
                this.startLineStream.Dispose();
                this.startLineStream = new MemoryStream();
                return;
            }

            this.startLineStream.Dispose();
            this.ParseStartLine(this.startLine);
            this.startLineStream = new MemoryStream();
            this.part = ParsePart.Headers;
        }

        /// <summary>
        /// スタートラインを解釈
        /// </summary>
        protected abstract void ParseStartLine(string startLine);

        #endregion

        #region Headers

        /// <summary>
        /// ヘッダーデータを入力
        /// </summary>
        /// <param name="buffer">入力データ</param>
        private void InputHeaders(byte[] buffer)
        {
            if (buffer.Length < 1)
                return;

            foreach (var b in buffer)
            {
                this.headersStream.WriteByte(b);
            }

            if (buffer[buffer.Length - 1] != this.LF)
                return;

            if (this.headersStream.Length == 2   // Header がない
            || this.headersStream.CurrentWithEmptyLine())
            {
                this.TerminateHeaders();
            }
        }

        /// <summary>
        /// ヘッダーを解釈
        /// </summary>
        /// <param name="headerString"></param>
        protected abstract void ParseHeaders(string headerString);

        /// <summary>
        /// ヘッダー終端処理
        /// </summary>
        private void TerminateHeaders()
        {
            this.ParseHeaders(Encoding.ASCII.GetString(this.headersStream.ToArray()));
            this.headersStream.Dispose();
            this.headersStream = new MemoryStream();

            this.InvokeReceivedHeaders();

            if (this.Headers.TransferEncoding.Exists
            || this.Headers.ContentLength.Exists && 0 < this.Headers.ContentLength.Value)
            {
                // TODO 対応している Transfer Coding に応じた TE, 501 Error の実装

                // RFC7230 3.3.3
                // Transfer-Encoding, Content-Length 両方ある場合は TransferEncoding を使用する
                if (this.Headers.IsChunked)
                    this.messageBody = new ChunkedBodyParser(this.isCaptureBody, this.maxCaptureSize);
                else if (this.Headers.TransferEncoding.Exists)
                    // RFC7230 3.3.3
                    // TransferEncoding があり chunked でないレスポンスの場合は Close により終端が決定される
                    this.messageBody = new TerminateWithCloseBodyParser(this.isCaptureBody, this.maxCaptureSize);
                else if (this.Headers.ContentLength.Exists)
                    this.messageBody = new ContentLengthBodyParser(this.isCaptureBody, this.maxCaptureSize, this.Headers.ContentLength.Value);
                this.part = ParsePart.Body;
            }
            else
            {
                // 空の Body として扱う
                this.messageBody = new EmptyBodyParser();
                this.InvokeReceivedBody();
                this.part = ParsePart.StartLine;
            }
        }

        /// <summary>
        /// ヘッダー受信完了イベントを発生させます
        /// </summary>
        protected abstract void InvokeReceivedHeaders();

        #endregion

        #region Body

        /// <summary>
        /// ボディーデータを入力
        /// </summary>
        /// <param name="buffer">入力するデータ</param>
        /// <returns>入力されたサイズ</returns>
        private int InputBody(byte[] buffer)
        {
            if (buffer.Length < 1)
                return 0;

            var readSize = this.messageBody.Write(buffer);
            this.ReadBody?.Invoke((buffer, readSize));

            if (this.messageBody.IsTerminated)
                this.TerminateBody();

            return readSize;
        }

        /// <summary>
        /// ボディー終端処理
        /// </summary>
        private void TerminateBody()
        {
            if (this.part != ParsePart.Body)
                return;

            if ((this.Headers.IsChunked || this.Headers.ContentLength.Exists)
            && this.messageBody?.IsTerminated != true)
            {
                // RFC7230 3.4
                // Content-Length、Chunked に満たない場合は不完全なメッセージとして TCP Close する(その後 Resume されたりする)
                throw new IncompleteBodyException();
            }

            this.Body = this.messageBody.Body;
            HttpHeaders.TryParse(this.messageBody.Trailers, out var trailers);
            this.Trailers = trailers;

            this.messageBody.Dispose();
            this.messageBody = null;

            this.part = ParsePart.StartLine;
            this.InvokeReceivedBody();
        }

        /// <summary>
        /// ボディー受信完了イベントを発生させます
        /// </summary>
        protected abstract void InvokeReceivedBody();

        #endregion

        /// <summary>
        /// ボディーデータ読み取り時に発生
        /// </summary>
        public event Action<(byte[] buffer, int readSize)> ReadBody;

        #region IDisposable Support
        private bool disposedValue = false; // 重複する呼び出しを検出するには

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    // マネージ状態を破棄します (マネージ オブジェクト)。
                    this.startLineStream?.Close();
                    this.headersStream?.Close();

                    this.startLineStream?.Dispose();
                    this.headersStream?.Dispose();
                    this.messageBody?.Dispose();
                }

                // アンマネージ リソース (アンマネージ オブジェクト) を解放し、下のファイナライザーをオーバーライドします。
                // 大きなフィールドを null に設定します。
                this.startLineStream = null;
                this.headersStream = null;
                this.messageBody = null;

                this.disposedValue = true;
            }
        }

        // 上の Dispose(bool disposing) にアンマネージ リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします。
        // ~HttpConnection() {
        //   // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
        //   Dispose(false);
        // }

        // このコードは、破棄可能なパターンを正しく実装できるように追加されました。
        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
            this.Dispose(true);
            // 上のファイナライザーがオーバーライドされる場合は、次の行のコメントを解除してください。
            // GC.SuppressFinalize(this);
        }
        #endregion

        /// <summary>
        /// 解析パート
        /// </summary>
        private enum ParsePart
        {
            /// <summary>
            /// スタートライン
            /// </summary>
            StartLine,
            /// <summary>
            /// ヘッダー
            /// </summary>
            Headers,
            /// <summary>
            /// ボディー
            /// </summary>
            Body,
        }
    }
}
