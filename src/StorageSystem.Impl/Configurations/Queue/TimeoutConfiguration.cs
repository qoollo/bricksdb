using System;

namespace Qoollo.Impl.Configurations.Queue
{
    public class TimeoutConfiguration
    {
        public int PeriodMls { get; protected set; }
        internal TimeSpan PeriodTimeSpan { get; set; }
    }
}