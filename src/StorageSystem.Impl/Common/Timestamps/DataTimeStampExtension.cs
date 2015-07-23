using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Impl.Common.Data.TransactionTypes;

namespace Qoollo.Impl.Common.Timestamps
{
    public static class DataTimeStampExtension
    {
        private static bool _isEnableDataTimeStamps = false;

        public static void EnableDataTimeStamps()
        {
            _isEnableDataTimeStamps = true;
        }

        public static void Start(this SystemTransaction transaction, string module)
        {
            transaction.DataTimeStamps = new DataTimeStamps(_isEnableDataTimeStamps);
            transaction.DataTimeStamps.StartMeasure(module);
        }

        public static void Start(this Transaction transaction, string module)
        {
            transaction.SystemTransaction.DataTimeStamps = new DataTimeStamps(_isEnableDataTimeStamps);
            transaction.SystemTransaction.DataTimeStamps.StartMeasure(module);
        }

        public static void MakeStamp(this SystemTransaction transaction, string stampName)
        {
            if (transaction.DataTimeStamps != null)
                transaction.DataTimeStamps.MakeStamp(stampName);
        }        

        public static void MakeStamp(this Transaction transaction, string stampName)
        {
            if (transaction.SystemTransaction.DataTimeStamps != null)
                transaction.SystemTransaction.DataTimeStamps.MakeStamp(stampName);
        }

        public static void MakeStamp(this Transaction transaction,  string stamp, params object[] objects)
        {
            MakeStamp(transaction, string.Format(stamp, objects));
        }

        public static void MakeStampWithTransactionError(this Transaction transaction, string stamp)
        {
            MakeStamp(transaction, string.Format("{0} : {1}", stamp, transaction.ErrorDescription));
        }

        public static void AddStamps(this Transaction transaction, Transaction stamps)
        {
            if (transaction.SystemTransaction.DataTimeStamps != null)
                transaction.SystemTransaction.DataTimeStamps.AddStamps(stamps.SystemTransaction.DataTimeStamps);
        }

        public static void CopyStamps(this Transaction newTransaction, Transaction oldTransaction)
        {            
            newTransaction.UserTransaction.DataTimeStamps = oldTransaction.SystemTransaction.DataTimeStamps;
            newTransaction.UserTransaction.DataTimeStamps.StopMeasure();
        }
    }
}
