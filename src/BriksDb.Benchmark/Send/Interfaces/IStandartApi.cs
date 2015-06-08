namespace Qoollo.Benchmark.Send.Interfaces
{
    interface IStandartApi
    {
        bool Send(long key, string data);

        bool Read(long key);
    }
}
