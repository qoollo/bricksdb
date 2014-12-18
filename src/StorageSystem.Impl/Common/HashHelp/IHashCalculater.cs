namespace Qoollo.Impl.Common.HashHelp
{
    public interface IHashCalculater
    {
        string CalculateHashFromKey(object key);

        string CalculateHashFromValue(object value);

        byte[] SerializeValue(object value);

        byte[] SerializeOther(object value);

        byte[] SerializeKey(object key);

        object DeserializeValue(byte[] data);

        object DeserializeKey(byte[] key);
    }
}
