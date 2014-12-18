using System;

namespace Qoollo.Impl.Common.Support
{
    internal static class QueueEnum
    {
        public static string QueueInner = "QueueInner";
        public static string QueueOuter = "QueueOuter";
    }

    internal static class AsyncTasksNames
    {
        public static string GetInfo = "getInfo";
        public static string RestoreRemote = "restoreRemote";
        public static string RestoreLocal = "restoreLocal";
        public static string AsyncPing = "asyncPing";
        public static string CheckRestore = "checkRestore";
        public static string CheckDistributors = "checkDistributors";
        public static string TimeoutDelete = "timeoutDelete";
        public static string GetHashFromDistributor = "getHashFromDistributor";
    }

    internal static class ModuleNames
    {
        public static string ProxyInputModule = "ProxyInputModule";
        public static string DistributorInputModule = "DistributorInputModule";
    }

    internal static class Errors
    {
        public static string NotAvailableServersForWrite = "Need more available servers to store data";
        public static string NotAvailableServersInSystem = "There is no Distributor in systme";
        public static string ServerWithResultNotAvailable = "Servers with operation result is unavailable";
        public static string NoErrorAddDbControllerServer = "No error";
        public static string ServerIsNotAvailable = "Servers is unavailable";
        public static string ServerIsNotAvailableFormat = "Servers {0}:{1} is unavailable";
        public static string TimeoutExpired = "Operation timeout";
        public static string TransactionCountAnswersError = "Transaction answer error";
        public static string NoDataToRead = "No data in db";
        public static string OperationTimeoutException = "Operation timeout";
        public static string InnerServerError = "Inner servers error";
        public static string DbReaderIsFail = "Db reader is fail";
        public static string RestoreAlreadyStarted = "Recover process is already in proggres";
        public static string RestoreFailConnectToDistributor = "Cannot connect to Distributor";
        public static string FailRead = "Fail read";
        public static string RestoreStartedWithoutErrors = "Recover procces started";
        public static string DataAlreadyExists = "Data already exist";
        public static string TableAlreadyExists = "Table already exist";
        public static string TableDoesNotExists = "Table does not exist";
        public static string DatabaseIsEmpty = "Not addeds any table";
        public static string SyncOperationTimeout = "Оperation timeout";

        public static string DbReaderNotStarted = "Reader not started";
        public static string NotEnoughServers = "Need more available servers to store data";
        public static string CannotParseQuery = "Unknown command type";
        public static string ScriptError = "Unknown data in query";
        public static string QueryError = "Query read error";
    }

    internal static class Consts
    {
        public static string StartHashInRing = "00000000000000000000000000000000";

        public static TimeSpan OpenTimeout = TimeSpan.FromMilliseconds(100);
        public static TimeSpan SendTimeout = TimeSpan.FromMilliseconds(1000);
        public static int UserPage = 200;
        public static int ServerPage = 100;
        public static int CompareFailed = -2;
        public static string Page = "pageSize";
        
        public static string AllTables = "AllTablesyNameThatMustntBeUsedAsTableName";
    }
}
