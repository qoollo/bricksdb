using System.Diagnostics.Contracts;
using Qoollo.Client.Support;

namespace Qoollo.Client.Configuration
{
    public class CommonConfiguration
    {
        /// <summary>
        /// System thread pool size
        /// </summary>
        public int CountThreads { get; private set; }
        /// <summary>
        /// Queue sizes
        /// </summary>
        public int QueueSize { get; private set; }

        public CommonConfiguration(int countThreads, int queueSize)
        {
            Contract.Requires(countThreads>0);
            Contract.Requires(queueSize>0);

            QueueSize = queueSize;
            CountThreads = countThreads;
        }

        public CommonConfiguration(int countThreads)
            :this(countThreads, Consts.QuerySize)
        {
        }

        public CommonConfiguration()
            : this(Consts.CountThreads, Consts.QuerySize)
        {
        }
    }
}
