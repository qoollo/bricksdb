using System;

namespace Qoollo.Client.Support
{
    public abstract class CommonDataProvider<TKey, TValue> : IDataProvider<TKey, TValue>
    {
        public abstract string CalculateHashFromKey(TKey key);

        public virtual string CalculateHashFromValue(TValue value)
        {
            throw new NotImplementedException();
        }

        public virtual byte[] SerializeValue(TValue value)
        {
            return CommonDataSerializer.Serialize(value);
        }

        public virtual byte[] SerializeKey(TKey key)
        {
            return CommonDataSerializer.Serialize(key);
        }

        public virtual TValue DeserializeValue(byte[] data)
        {
            return CommonDataSerializer.Deserialize<TValue>(data);
        }

        public virtual TKey DeserializeKey(byte[] key)
        {
            return CommonDataSerializer.Deserialize<TKey>(key);
        }
    }
}
