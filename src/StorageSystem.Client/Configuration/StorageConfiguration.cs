using System;
using System.Diagnostics.Contracts;
using Qoollo.Client.Support;

namespace Qoollo.Client.Configuration
{
    public class StorageConfiguration
    {        
        /// <summary>
        /// File with server addresses and hashes
        /// </summary>
        public string FileWithHashName { get; private set; }

        /// <summary>
        /// Replic counts
        /// </summary>
        public int CountReplics { get; private set; }

        /// <summary>
        /// How many times try to receive answer from restored server       
        /// </summary>
        public int CountRetryWaitAnswerInRestore { get; private set; }

        /// <summary>
        /// Period for waiting one answer from restored server         
        /// </summary>
        public TimeSpan TimeoutWaitAnswerInRestore { get; private set; }

        /// <summary>
        /// Period for sending info that info in progress        
        /// </summary>
        public TimeSpan TimeoutSendAnswerInRestore { get; private set; }

        /// <summary>
        /// Period for delete data after restore
        /// </summary>
        public TimeSpan PeriodDeleteAfterRestore { get; private set; }

        /// <summary>
        /// Periof for starting delete process from db        
        /// </summary>
        public TimeSpan PeriodStartDelete { get;private set; }

        /// <summary>
        /// Is start delete process after starting Writer
        /// </summary>
        public bool IsForceDelete { get; private set; }

        public StorageConfiguration(string fileWithHashName, int countReplics, int countRetryWaitAnswerInRestore,
            TimeSpan timeoutWaitAnswerInRestore, TimeSpan timeoutSendAnswerInRestore, TimeSpan periodDeleteAfterRestore,
            TimeSpan periodStartDelete, bool isForceDelete)
        {
            Contract.Requires(fileWithHashName!="");
            Contract.Requires(countReplics>0);
            Contract.Requires(countRetryWaitAnswerInRestore > 0);

            IsForceDelete = isForceDelete;
            PeriodStartDelete = periodStartDelete;
            PeriodDeleteAfterRestore = periodDeleteAfterRestore;
            TimeoutSendAnswerInRestore = timeoutSendAnswerInRestore;
            TimeoutWaitAnswerInRestore = timeoutWaitAnswerInRestore;
            CountRetryWaitAnswerInRestore = countRetryWaitAnswerInRestore;
            CountReplics = countReplics;
            FileWithHashName = fileWithHashName;
        }

        public StorageConfiguration(int countReplics, string fileWithHashName, int countRetryWaitAnswerInRestore,
            TimeSpan timeoutWaitAnswerInRestore, TimeSpan timeoutSendAnswerInRestore, TimeSpan periodDeleteAfterRestore,
            TimeSpan periodStartDelete)
            : this(fileWithHashName, countReplics, countRetryWaitAnswerInRestore, timeoutWaitAnswerInRestore,
                timeoutSendAnswerInRestore, periodDeleteAfterRestore, periodStartDelete, Consts.IsForceDelete)
        {
        }

        public StorageConfiguration(int countReplics, string fileWithHashName, int countRetryWaitAnswerInRestore,
            TimeSpan timeoutWaitAnswerInRestore, TimeSpan timeoutSendAnswerInRestore, TimeSpan periodDeleteAfterRestore)
            : this(fileWithHashName, countReplics, countRetryWaitAnswerInRestore, timeoutWaitAnswerInRestore,
                timeoutSendAnswerInRestore, periodDeleteAfterRestore, Consts.PeriodStartDelete, Consts.IsForceDelete)
        {
        }

        public StorageConfiguration(int countReplics, string fileWithHashName, int countRetryWaitAnswerInRestore,
            TimeSpan timeoutWaitAnswerInRestore, TimeSpan timeoutSendAnswerInRestore)
            : this(fileWithHashName, countReplics, countRetryWaitAnswerInRestore, timeoutWaitAnswerInRestore,
                timeoutSendAnswerInRestore, Consts.PeriodDeleteAfterRestore, Consts.PeriodStartDelete,
                Consts.IsForceDelete)
        {
        }

        public StorageConfiguration(int countReplics, string fileWithHashName, int countRetryWaitAnswerInRestore,
            TimeSpan timeoutWaitAnswerInRestore)
            : this(fileWithHashName, countReplics, countRetryWaitAnswerInRestore, timeoutWaitAnswerInRestore,
                Consts.TimeoutSendAnswerInRestore, Consts.PeriodDeleteAfterRestore, Consts.PeriodStartDelete,
                Consts.IsForceDelete)
        {
        }

        public StorageConfiguration(int countReplics, string fileWithHashName, int countRetryWaitAnswerInRestore)
            : this(fileWithHashName, countReplics, countRetryWaitAnswerInRestore, Consts.TimeoutWaitAnswerInRestore,
                Consts.TimeoutSendAnswerInRestore, Consts.PeriodDeleteAfterRestore, Consts.PeriodStartDelete,
                Consts.IsForceDelete)
        {
        }

        public StorageConfiguration(int countReplics, string fileWithHashName)
            : this(
                fileWithHashName, countReplics, Consts.CountRetryWaitAnswerInRestore, Consts.TimeoutWaitAnswerInRestore,
                Consts.TimeoutSendAnswerInRestore, Consts.PeriodDeleteAfterRestore, Consts.PeriodStartDelete,
                Consts.IsForceDelete)
        {
        }

        public StorageConfiguration(int countReplics)
            : this(
                Consts.FileWithHashName, countReplics, Consts.CountRetryWaitAnswerInRestore,
                Consts.TimeoutWaitAnswerInRestore, Consts.TimeoutSendAnswerInRestore, Consts.PeriodDeleteAfterRestore,
                Consts.PeriodStartDelete, Consts.IsForceDelete)
        {
        }

        public StorageConfiguration()
            : this(
                Consts.FileWithHashName, Consts.CountReplics, Consts.CountRetryWaitAnswerInRestore,
                Consts.TimeoutWaitAnswerInRestore, Consts.TimeoutSendAnswerInRestore, Consts.PeriodDeleteAfterRestore,
                Consts.PeriodStartDelete, Consts.IsForceDelete)
        {
        }
    }
}
