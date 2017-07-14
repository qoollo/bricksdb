namespace Qoollo.Impl.Configurations.Queue
{
    public interface ICommonConfiguration
    {
        int CountReplics { get; }
        string HashFilename { get; }
    }

    public class CommonConfiguration : ICommonConfiguration
    {
        public int CountReplics { get; protected set; }
        public string HashFilename { get; protected set; }
    }
}