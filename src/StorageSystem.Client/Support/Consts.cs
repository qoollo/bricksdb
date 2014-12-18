using System;

namespace Qoollo.Client.Support
{
    public static class Consts
    {
        public static int CountThreads = 2;
        public static int QuerySize = 10000;
        public static int CountReplics = 2;
        public static int PageSize = 100;

        public static TimeSpan DataAliveTime = TimeSpan.FromSeconds(30);
        public static TimeSpan DataAliveAfterUpdate = TimeSpan.FromMinutes(1);        
        public static TimeSpan CheckPeriod = TimeSpan.FromMinutes(1);
        public static TimeSpan PingPeriod = TimeSpan.FromSeconds(30);
        public static string FileWithHashName = "ServersHashFile";

        public static int CountConnectionsToSingleServer = 10;
        public static string WcfServiceName = "StorageWcfName";

        public static TimeSpan AsyncPingTimeout = TimeSpan.FromSeconds(60);
        public static TimeSpan AsyncUpdateTimeout = TimeSpan.FromMinutes(10);
        public static TimeSpan SyncOperationsTimeoutSec = TimeSpan.FromSeconds(30);
        public static TimeSpan ChangeDistributorTimeoutSec = TimeSpan.FromSeconds(60);

        public static bool IsForceDelete = false;
        public static TimeSpan PeriodStartDelete = TimeSpan.FromHours(12);
        public static TimeSpan PeriodDeleteAfterRestore = TimeSpan.FromDays(2);
        public static TimeSpan TimeoutSendAnswerInRestore = TimeSpan.FromMinutes(1);
        public static TimeSpan TimeoutWaitAnswerInRestore = TimeSpan.FromMinutes(2);
        public static int CountRetryWaitAnswerInRestore = 5;        

        public static TimeSpan OpenTimeout = TimeSpan.FromMilliseconds(500);
        public static TimeSpan SendTimeout = TimeSpan.FromMilliseconds(2000);
    }
}
