using Qoollo.Client.Support;

namespace BricksDb.RedisInterface.BriksCommunication
{
    class RedisDataProvider: CommonDataProvider<string,string>
    {
        public override string CalculateHashFromKey(string key)
        {
            return key.GetHashCode().ToString();
        }
    }
}
