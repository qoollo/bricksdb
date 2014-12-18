using System;

namespace Qoollo.Impl.Modules
{
    public abstract class ControlModule:IDisposable
    {
        public virtual void Build()
        {
        }

        public virtual void Start()
        {
        }

        protected virtual void Dispose(bool isUserCall)
        {
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
