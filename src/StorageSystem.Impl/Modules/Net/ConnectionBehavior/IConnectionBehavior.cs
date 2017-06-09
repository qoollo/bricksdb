using System;
using System.Threading.Tasks;
using Qoollo.Impl.Common.Server;

namespace Qoollo.Impl.Modules.Net.ConnectionBehavior
{
    internal interface IConnectionBehavior<TConnection>:IDisposable
    {
        ServerId Server { get; }

        bool Connect();
        void Dispose();

        Task<TResult> SendAsyncFunc<TResult, TApi>(Func<TApi, Task<TResult>> func, Func<Exception, TResult> errorRet,
            string errorLogFromData) where TApi : class;

        TResult SendFunc<TResult, TApi>(Func<TApi, TResult> func, Func<Exception, TResult> errorRet,
            string errorLogFromData) where TApi : class;
    }
}