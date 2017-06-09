using System.Runtime.Serialization;
using Qoollo.Impl.Configurations;

namespace Qoollo.Impl.Modules.Net.ReceiveBehavior
{
    internal abstract class ReceiveBehaviorBase<TReceive> : IReceiveBehavior<TReceive>
    {
        protected ReceiveBehaviorBase(NetReceiverConfiguration configuration)
        {
        }

        public abstract void Start();

        protected abstract void Dispose(bool isUserCall);

        public void Dispose()
        {
            Dispose(true);
        }
    }
}