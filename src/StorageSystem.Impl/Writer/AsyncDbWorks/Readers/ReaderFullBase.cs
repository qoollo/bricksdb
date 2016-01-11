using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Impl.Modules;

namespace Qoollo.Impl.Writer.AsyncDbWorks.Readers
{
    internal abstract class ReaderFullBase : ControlModule
    {
        public abstract bool IsComplete { get; }

        public abstract bool IsQueueEmpty { get; }

        public abstract void GetAnotherData();
    }
}
