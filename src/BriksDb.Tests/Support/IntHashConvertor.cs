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
    public class IntHashConvertor : IHashCalculater
    {
        public string CalculateHashFromKey(object key)
        {
            return HashConvertor.GetString(((int)key).ToString(CultureInfo.InvariantCulture));
        }

        public string CalculateHashFromValue(object value)
        {
            return HashConvertor.GetString(((int)value).ToString(CultureInfo.InvariantCulture));
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
            return CommonDataSerializer.Deserialize<int>(data);
        }

        public object DeserializeKey(byte[] key)
        {
            return CommonDataSerializer.Deserialize<int>(key);
        }
    }
}
