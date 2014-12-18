using System.Diagnostics.Contracts;

namespace Qoollo.Impl.Configurations
{
    internal class QueueConfiguration
    {
        public int ProcessotCount { get; private set; }
        public int MaxSizeQueue { get; private set; }

        public QueueConfiguration(int processorCount, int maxSizeQueue)
        {
            Contract.Requires(processorCount>0);
            Contract.Requires(maxSizeQueue>0);

            ProcessotCount = processorCount;
            MaxSizeQueue = maxSizeQueue;
        }
    }
}
