using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.Writer.Db;

namespace Qoollo.Client.WriterGate
{
    public abstract class DbFactory
    {
        public abstract DbModule Build();

        public abstract ScriptParser GetParser();
    }
}
