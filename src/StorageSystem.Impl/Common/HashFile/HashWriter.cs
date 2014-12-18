using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;

namespace Qoollo.Impl.Common.HashFile
{
    internal class HashWriter:HashMap
    {
        public HashWriter(HashMapConfiguration configuration) : base(configuration)
        {
        }

        public void SetServer(int index, string host, int portForDistributor, int portForCollector)
        {
            Map[index].Save = new SavedServerId( host, portForDistributor, portForCollector);
        }

        public void Save()
        {
            CreateNewFile();
        }
    }
}
