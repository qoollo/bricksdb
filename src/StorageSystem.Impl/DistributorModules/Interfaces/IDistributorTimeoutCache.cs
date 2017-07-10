using System;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Modules.Interfaces;

namespace Qoollo.Impl.DistributorModules.Interfaces
{
    internal interface IDistributorTimeoutCache : ICacheModule<InnerData>
    {
        void AddDataToCache(InnerData data);
        void Update(string key, InnerData obj);
        void UpdateDataToCache(InnerData data);

        Action<InnerData> DataTimeout { get; set; }
    }
}