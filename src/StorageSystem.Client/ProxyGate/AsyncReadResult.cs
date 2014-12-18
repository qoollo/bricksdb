using Qoollo.Client.Request;

namespace Qoollo.Client.ProxyGate
{
    public class AsyncReadResult<TValue>
    {
        public AsyncReadResult(RequestDescription requestDescription, TValue value)
        {
            Value = value;
            RequestDescription = requestDescription;
        }

        public RequestDescription RequestDescription { get; private set; }
        public TValue Value { get; private set; }
    }
}
