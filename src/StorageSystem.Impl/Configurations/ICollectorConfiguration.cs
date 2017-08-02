﻿namespace Qoollo.Impl.Configurations
{
    public interface ICollectorConfiguration
    {
         Collector.TimeoutsConfiguration Timeouts { get; }
        int ServerPageSize { get; }
        bool UseHashFile { get; }
    }

    public class CollectorConfiguration : ICollectorConfiguration
    {
        public Collector.TimeoutsConfiguration Timeouts { get; protected set; }
        public int ServerPageSize { get; protected set; }
        public bool UseHashFile { get; protected set; }
    }

    namespace Collector
    {
        public class TimeoutsConfiguration
        { 
            public TimeoutConfiguration ServersPingMls { get; protected set; }
            public TimeoutConfiguration DistributorUpdateHashMls { get; protected set; }
        }
    }
}