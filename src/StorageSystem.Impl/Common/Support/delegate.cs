using System.Threading;

namespace Qoollo.Impl.Common.Support
{
    internal delegate bool CreateElementDelegate<T>(out T elem, int timeout, CancellationToken token);
}
