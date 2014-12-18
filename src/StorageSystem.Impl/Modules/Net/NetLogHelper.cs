using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.Server;

namespace Qoollo.Impl.Modules.Net
{
    internal static class NetLogHelper
    {
        public static string GetLog(NetCommand command)
        {
            return string.Format("command = {0}", command.GetType());
        }

        public static string GetLog(InnerData data)
        {
            return string.Format("process data = {0}", data.Transaction.EventHash);
        }

        public static string GetLog(Transaction transaction)
        {
            return string.Format("key = {0}", transaction.CacheKey);
        }

        public static string GetLog(UserTransaction transaction)
        {
            return string.Format("key = {0}", transaction.CacheKey);
        }

        public static string GetLog(SelectDescription description)
        {
            return string.Format("Script = {0}", description.Script);
        }

        public static string GetLog(ServerId server, NetCommand command)
        {
            return string.Format("Server = {0}, command = {1}", server, command.GetType());
        }

    }
}
