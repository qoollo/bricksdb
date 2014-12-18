using System;

namespace Qoollo.Impl.Modules.Db.Exceptions
{
    public class ReaderIsFailException:Exception
    {
        public ReaderIsFailException(string message) : base(message)
        {
        }
    }
}
