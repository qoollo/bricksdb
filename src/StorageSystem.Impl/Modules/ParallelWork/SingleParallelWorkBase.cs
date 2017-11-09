using Ninject;

namespace Qoollo.Impl.Modules.ParallelWork
{
    internal abstract class SingleParallelWorkBase<T>:ControlModule
    {
        protected SingleParallelWorkBase(StandardKernel kernel) : base(kernel)
        {
        }

        public abstract void Process(T data);
    }
}
