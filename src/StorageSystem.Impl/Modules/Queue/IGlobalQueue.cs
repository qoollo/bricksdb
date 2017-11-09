using System.Collections.Generic;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.TransactionTypes;

namespace Qoollo.Impl.Modules.Queue
{
    internal interface IGlobalQueue
    {
        QueueWithParam<NetCommand> DbDistributorInnerQueue { get; }
        QueueWithParam<InnerData> DbInputProcessQueue { get; }
        QueueWithParam<InnerData> DbInputRollbackQueue { get; }
        QueueWithParam<List<InnerData>> DbRestorePackageQueue { get; }
        QueueWithParam<InnerData> DbRestoreQueue { get; }
        QueueWithParam<InnerData> DbTimeoutQueue { get; }
        QueueWithParam<NetCommand> DistributorDistributorQueue { get; }
        QueueWithParam<Transaction> DistributorTransactionCallbackQueue { get; }
        QueueWithParam<NetCommand> ProxyDistributorQueue { get; }
        QueueWithParam<InnerData> ProxyInputOtherQueue { get; }
        QueueWithParam<InnerData> ProxyInputWriteAndUpdateQueue { get; }
        QueueWithParam<Transaction> TransactionAnswerQueue { get; }
        QueueWithParam<Transaction> TransactionQueue { get; }
    }
}