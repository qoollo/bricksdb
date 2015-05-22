namespace Qoollo.Benchmark.Send
{
    abstract class DataSender
    {
        public abstract void Start();
        public abstract bool Send(long key, string data);
    }
}
