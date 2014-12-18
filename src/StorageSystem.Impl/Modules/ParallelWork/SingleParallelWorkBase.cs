namespace Qoollo.Impl.Modules.ParallelWork
{
    internal abstract class SingleParallelWorkBase<T>:ControlModule
    {
        public abstract void Process(T data);
    }
}
