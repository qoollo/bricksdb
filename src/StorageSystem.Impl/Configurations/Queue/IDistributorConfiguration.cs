namespace Qoollo.Impl.Configurations.Queue
{
    public interface IDistributorConfiguration
    {
        int CountThreads { get; }

        NetConfiguration NetProxy { get; }
        NetConfiguration NetWriter { get; }
        DistributorCacheConfiguration Cache { get; }
    }

    public class DistributorConfiguration : IDistributorConfiguration
    {
        public int CountThreads { get; protected set; }
        public NetConfiguration NetProxy { get; protected set; }
        public NetConfiguration NetWriter { get; protected set; }
        public DistributorCacheConfiguration Cache { get; protected set; }
    }

    public class DistributorCacheConfiguration
    {
        public int TimeAliveBeforeDeleteMls { get; protected set; }
        public int TimeAliveAfterUpdateMls { get; protected set; }

        internal DistributorCacheConfiguration(int timeAliveBeforeDeleteMls, int timeAliveAfterUpdateMls)
        {
            TimeAliveBeforeDeleteMls = timeAliveBeforeDeleteMls;
            TimeAliveAfterUpdateMls = timeAliveAfterUpdateMls;
        }

        public DistributorCacheConfiguration()
        {
        }
    }
}