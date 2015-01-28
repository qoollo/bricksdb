using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Client.Support;
using Qoollo.Impl.Common.HashHelp;

namespace Qoollo.Tests.Support
{
    class StoredDataHashCalculator : IHashCalculater
    {
        public string CalculateHashFromKey(object key)
        {
            return HashConvertor.GetString(key.ToString());
        }

        public string CalculateHashFromValue(object value)
        {
            return HashConvertor.GetString(value.ToString());
        }

        public byte[] SerializeValue(object value)
        {
            return CommonDataSerializer.Serialize(value);
        }

        public byte[] SerializeOther(object value)
        {
            return CommonDataSerializer.Serialize(value);
        }

        public byte[] SerializeKey(object key)
        {
            return CommonDataSerializer.Serialize(key);
        }

        public object DeserializeValue(byte[] data)
        {
            return CommonDataSerializer.Deserialize<StoredData>(data);
        }

        public object DeserializeKey(byte[] key)
        {
            return CommonDataSerializer.Deserialize<int>(key);
        }
    }
}
