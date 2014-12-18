using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.DbController.Db;

namespace Qoollo.Client.StorageGate
{
    public abstract class DbFactory
    {
        public abstract DbModule Build();

        public abstract ScriptParser GetParser();
    }
}
