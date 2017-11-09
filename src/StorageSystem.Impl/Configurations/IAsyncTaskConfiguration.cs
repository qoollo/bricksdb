namespace Qoollo.Impl.Configurations
{
    public interface IAsyncTaskConfiguration
    {
        int CountThreads { get;  }
    }

    public class AsyncTaskConfiguration : IAsyncTaskConfiguration
    {
        public int CountThreads { get; protected set; }
    }
}