using Qoollo.Impl.Common.Data.DataTypes;

namespace Qoollo.Impl.DistributorModules.ParallelWork
{
    internal interface IInputModule
    {
        void ProcessAsync(InnerData ev);
    }
}
