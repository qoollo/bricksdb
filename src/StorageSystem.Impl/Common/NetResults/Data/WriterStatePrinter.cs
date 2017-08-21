using System;
using System.Linq;
using Qoollo.Impl.Common.Support;

namespace Qoollo.Impl.Common.NetResults.Data
{
    internal class WriterStatePrinter
    {
        private GetRestoreStateResult _state;

        public WriterStatePrinter(GetRestoreStateResult state)
        {
            _state = state;
        }


        public string GetAllState()
        {
            var result = string.Empty;

            result += GetInitiatorString();
            result = Concat(result, GetTransferString());
            result = Concat(result, GetBroadcastString());

            return result;
        }

        private string GetInitiatorString()
        {
            var result = string.Empty;
            var initiator = _state.InitiatorState;
            if (initiator == null)
                return result;

            result += "restore is running in initiator mode \n";
            result += $"current server: {initiator.CurrentServer}\n";
            result += $"servers: \n{GetServersList()}";

            return result;
        }

        private string GetTransferString()
        {
            var result = string.Empty;
            var transfer = _state.TransferState;
            if (transfer == null)
                return result;

            result += "restore is running in transfer mode \n";
            result += $"transfert server: {transfer.TransferServer}\n";
            result += $"start time server: {transfer.StartedTime}";

            return result;
        }

        private string GetBroadcastString()
        {
            var result = string.Empty;
            var transfer = _state.BroadcastState;
            if (transfer == null)
                return result;

            result += "restore is running in broadcast mode \n";
            result += $"start time server: {transfer.StartedTime}";

            return result;
        }

        private string GetServersList(string start = "\n")
        {
            return _state.RestoreServers.Aggregate(start, (current, server) => current + $"\t{server}\n");
        }

        private string Concat(string result, string str)
        {
            if (string.IsNullOrEmpty(str))
                return result;

            if (string.IsNullOrEmpty(result))
                return str;

            return result + "\n\n" + str;
        }
    }
}