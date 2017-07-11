namespace Qoollo.Impl.Configurations.Queue
{
    public interface IDistributorConfiguration
    {
        int CountThreads { get; }
    }

    public class DistributorConfiguration : IDistributorConfiguration
    {
        public int CountThreads { get; protected set; }
    }
}