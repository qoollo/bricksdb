using Qoollo.Concierge;

namespace Qoollo.Benchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            new AppBuilder()
            .WithDefaultStartupString(DefaultStatupArguments.Interactive)
            .Run(args);
        }
    }
}
