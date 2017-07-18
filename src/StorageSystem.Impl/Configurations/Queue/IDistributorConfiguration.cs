namespace Qoollo.Impl.Configurations.Queue
{
    public interface IDistributorConfiguration
    {
        int CountThreads { get; }

        NetConfiguration NetProxy { get; }
        NetConfiguration NetWriter { get; }
    }

    public class DistributorConfiguration : IDistributorConfiguration
    {
        public int CountThreads { get; protected set; }
        public NetConfiguration NetProxy { get; protected set; }
        public NetConfiguration NetWriter { get; protected set; }
    }
}