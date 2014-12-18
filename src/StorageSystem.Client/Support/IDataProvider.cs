namespace Qoollo.Client.Support
{
    public interface IDataProvider<TKey, TValue>
    {
        string CalculateHashFromKey(TKey key);

        string CalculateHashFromValue(TValue value);

        byte[] SerializeValue(TValue value);

        byte[] SerializeKey(TKey key);

        TValue DeserializeValue(byte[] data);

        TKey DeserializeKey(byte[] key);  
    }
}
