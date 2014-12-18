using System.Collections.Generic;
using System.Threading;

namespace Qoollo.Impl.TestSupport
{
    internal static class GlobalLock
    {
        private static List<ReaderWriterLockSlim>  _lock = new List<ReaderWriterLockSlim>();

        static GlobalLock()
        {
            _lock.Add(new ReaderWriterLockSlim());
        }

        public static void Lock(int numLock)
        {
            _lock[numLock].EnterWriteLock();
        }

        public static void Free(int numLock)
        {
            _lock[numLock].ExitWriteLock();
        }
    }
}
