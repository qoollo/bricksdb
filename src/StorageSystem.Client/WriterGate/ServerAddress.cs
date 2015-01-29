namespace Qoollo.Client.WriterGate
{
    public class ServerAddress
    {
        public ServerAddress(string host, int port)
        {
            Port = port;
            Host = host;
        }

        public int Port { get; private set; }
        public string Host { get; private set; }

        public override string ToString()
        {
            return string.Format("{0}:{1}", Host, Port);
        }
    }
}
