using System.Diagnostics.Contracts;
using Qoollo.Impl.Common.HashHelp;

namespace Qoollo.Client.Support
{
    internal class HashFakeImpl<TKey, TValue> : IHashCalculater
    {
        private readonly IDataProvider<TKey, TValue> _dataProvider;

        public HashFakeImpl(IDataProvider<TKey, TValue> dataProvider)
        {
            Contract.Requires(dataProvider!=null);
            _dataProvider = dataProvider;
        }

        public string CalculateHashFromKey(object key)
        {
            return HashConvertor.GetString(_dataProvider.CalculateHashFromKey((TKey)key));
        }

        public string CalculateHashFromValue(object value)
        {
            return HashConvertor.GetString(_dataProvider.CalculateHashFromValue((TValue)value));
        }

        public byte[] SerializeValue(object value)
        {
            return _dataProvider.SerializeValue((TValue)value);
        }

        public byte[] SerializeOther(object value)
        {
            return CommonDataSerializer.Serialize(value);
        }

        public byte[] SerializeKey(object key)
        {
            return _dataProvider.SerializeKey((TKey)key);
        }

        public object DeserializeValue(byte[] data)
        {
            return _dataProvider.DeserializeValue(data);
        }

        public object DeserializeKey(byte[] key)
        {
            return _dataProvider.DeserializeKey(key);
        }        
    }
}
