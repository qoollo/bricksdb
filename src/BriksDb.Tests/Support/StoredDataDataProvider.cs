using Qoollo.Client.Support;

namespace Qoollo.Tests.Support
{
    public class StoredDataDataProvider:CommonDataProvider<int, StoredData>
    {
        public override string CalculateHashFromKey(int key)
        {
            return key.ToString();
        }
    }
}