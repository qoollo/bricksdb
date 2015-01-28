using System.Threading;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.NetInterfaces.Distributor;

namespace Qoollo.Tests.TestProxy
{
    internal class TestNetDistributorForProxy : ICommonNetReceiverForProxy, ICommonNetReceiverForDb
    {
        public int Value = 0;
        public int SendValue = 0;

        public void Process(InnerData ev)
        {
            Interlocked.Increment(ref Value);
        }

        public UserTransaction GetTransaction(UserTransaction transaction)
        {
            Interlocked.Increment(ref Value);
            return new Transaction("", "").UserTransaction;
        }

        public RemoteResult SendSync(NetCommand command)
        {
            Interlocked.Increment(ref SendValue);
            return new SuccessResult();
        }

        public void SendASync(NetCommand command)
        {
            Interlocked.Increment(ref SendValue);
        }

        public RemoteResult Ping()
        {
            return new SuccessResult();
        }

        public void TransactionAnswer(Transaction transaction)
        {
            Interlocked.Increment(ref SendValue);
        }
    }
}
