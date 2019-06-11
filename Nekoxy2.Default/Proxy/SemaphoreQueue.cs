using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Nekoxy2.Default.Proxy
{
    /// <summary>
    /// 待機した順序通りに実行されるセマフォ
    /// </summary>
    internal sealed class SemaphoreQueue : IDisposable
    {
        /// <summary>
        /// セマフォ
        /// </summary>
        private readonly SemaphoreSlim semaphore;

        /// <summary>
        /// 待機キュー
        /// </summary>
        private readonly ConcurrentQueue<TaskCompletionSource<bool>> queue
            = new ConcurrentQueue<TaskCompletionSource<bool>>();

        /// <summary>
        /// 初期値と最大値を指定してインスタンスを作成
        /// </summary>
        /// <param name="initialCount">初期カウント</param>
        /// <param name="maxCount">最大カウント</param>
        public SemaphoreQueue(int initialCount, int maxCount)
            => this.semaphore = new SemaphoreSlim(initialCount, maxCount);

        /// <summary>
        /// セマフォに入れるようになるまでスレッドをブロック
        /// </summary>
        /// <returns></returns>
        public Task WaitAsync()
        {
            var tcs = new TaskCompletionSource<bool>();
            this.queue.Enqueue(tcs);
            this.semaphore.WaitAsync()
                .ContinueWith(t =>
                {
                    if (this.queue.TryDequeue(out var popped))
                        popped.TrySetResult(true);
                });
            return tcs.Task;
        }

        /// <summary>
        /// セマフォを解放
        /// </summary>
        public void Release()
            => this.semaphore.Release();

        #region IDisposable Support
        private bool disposedValue = false; // 重複する呼び出しを検出するには

        private void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    // マネージド状態を破棄します (マネージド オブジェクト)。
                    this.semaphore.Dispose();
                }

                // アンマネージド リソース (アンマネージド オブジェクト) を解放し、下のファイナライザーをオーバーライドします。
                // 大きなフィールドを null に設定します。

                this.disposedValue = true;
            }
        }

        // 上の Dispose(bool disposing) にアンマネージド リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします。
        // ~SemaphoreQueue() {
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
    }
}
