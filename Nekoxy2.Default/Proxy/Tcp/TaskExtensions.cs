using System;
using System.Threading.Tasks;

namespace Nekoxy2.Default.Proxy.Tcp
{
    internal static class TaskExtensions
    {
        /// <summary>
        /// true が返される限り再試行
        /// </summary>
        /// <param name="isContinueFunc">再試行する関数</param>
        /// <returns></returns>
        public static Task RunAutoRestartTask(this Func<bool> isContinueFunc)
        {
            return Task.Run(isContinueFunc)
                .ContinueWith(t =>
                {
                    if (t.Result)
                        RunAutoRestartTask(isContinueFunc);
                });
        }
    }
}
