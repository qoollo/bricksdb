namespace Qoollo.Impl.Configurations.Queue
{
    public interface ICommonConfiguration
    {
        int CountReplics { get; }
    }

    public class CommonConfiguration : ICommonConfiguration
    {
        public int CountReplics { get; protected set; }
    }
}