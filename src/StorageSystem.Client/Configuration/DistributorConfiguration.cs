using System;
using System.Diagnostics.Contracts;
using Qoollo.Client.Support;

namespace Qoollo.Client.Configuration
{
    public class DistributorConfiguration
    {
        /// <summary>
        /// File with server addresses and hashes
        /// </summary>
        public string FileWithHashName { get; private set; }
        /// <summary>
        /// Replic count
        /// </summary>
        public int CountReplics { get; private set; }
        /// <summary>
        /// The time during which an data in the cache obsolete        
        /// </summary>
        public TimeSpan DataAliveTime { get; private set; }
        /// <summary>
        /// How much data is stored in the cache after the upgrade.
        /// </summary>
        public TimeSpan DataAliveAfterUpdate { get; private set; }
        /// <summary>
        /// Server ping period
        /// </summary>
        public TimeSpan PingPeriod { get; private set; }
        /// <summary>
        /// Server recovery check period        
        /// </summary>
        public TimeSpan CheckPeriod { get; private set; }

        public DistributorConfiguration(int countReplics, string fileWithHashName, TimeSpan dataAliveAfterUpdate,
            TimeSpan pingPeriod, TimeSpan checkPeriod, TimeSpan dataAliveTime)
        {
            Contract.Requires(countReplics>0);
            Contract.Requires(fileWithHashName!="");

            CheckPeriod = checkPeriod;
            PingPeriod = pingPeriod;
            DataAliveAfterUpdate = dataAliveAfterUpdate;
            DataAliveTime = dataAliveTime;
            FileWithHashName = fileWithHashName;
            CountReplics = countReplics;
        }

        public DistributorConfiguration(int countReplics, string fileWithHashName, TimeSpan dataAliveAfterUpdate,
            TimeSpan pingPeriod, TimeSpan checkPeriod)
            : this(countReplics, fileWithHashName, dataAliveAfterUpdate, pingPeriod, checkPeriod, Consts.DataAliveTime)
        {
        }

        public DistributorConfiguration(int countReplics, string fileWithHashName, TimeSpan dataAliveAfterUpdate,
            TimeSpan pingPeriod)
            : this(countReplics, fileWithHashName, dataAliveAfterUpdate, pingPeriod, Consts.CheckPeriod, Consts.DataAliveTime)
        {
        }

        public DistributorConfiguration(int countReplics, string fileWithHashName, TimeSpan dataAliveAfterUpdate)
            : this(
                countReplics, fileWithHashName, dataAliveAfterUpdate, Consts.PingPeriod, Consts.CheckPeriod,
                Consts.DataAliveTime)
        {
        }

        public DistributorConfiguration(int countReplics, string fileWithHashName)
            : this(
                countReplics, fileWithHashName, Consts.DataAliveAfterUpdate, Consts.PingPeriod, Consts.CheckPeriod,
                Consts.DataAliveTime)
        {
        }

        public DistributorConfiguration(int countReplics)
            : this(
                countReplics, Consts.FileWithHashName, Consts.DataAliveAfterUpdate, Consts.PingPeriod,
                Consts.CheckPeriod, Consts.DataAliveTime)
        {
        }

        public DistributorConfiguration()
            : this(
                Consts.CountReplics, Consts.FileWithHashName, Consts.DataAliveAfterUpdate, Consts.PingPeriod,
                Consts.CheckPeriod, Consts.DataAliveTime)
        {
        }
    }
}
