using Qoollo.Impl.Common.Data.DataTypes;

namespace Qoollo.Impl.DistributorModules.Interfaces
{
    internal interface IInputModule
    {
        void ProcessAsync(InnerData ev);
    }
}
