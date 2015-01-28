using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Client.Support;
using Qoollo.Impl.Common.HashHelp;

namespace Qoollo.Tests.Support
{
    public class IntDataProvider : IDataProvider<int, int>
    {
        public string CalculateHashFromKey(int key)
        {
            return HashConvertor.GetString(key.ToString(CultureInfo.InvariantCulture));
        }

        public string CalculateHashFromValue(int value)
        {
            return HashConvertor.GetString(value.ToString(CultureInfo.InvariantCulture));
        }

        public byte[] SerializeValue(int value)
        {
            return CommonDataSerializer.Serialize(value);
        }

        public byte[] SerializeKey(int key)
        {
            return CommonDataSerializer.Serialize(key);
        }

        public int DeserializeValue(byte[] data)
        {
            return CommonDataSerializer.Deserialize<int>(data);
        }

        public int DeserializeKey(byte[] key)
        {
            return CommonDataSerializer.Deserialize<int>(key);
        }
    }
}
