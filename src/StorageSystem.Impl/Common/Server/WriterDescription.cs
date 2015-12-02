using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Qoollo.Impl.Common.Support;

namespace Qoollo.Impl.Common.Server
{
    public class WriterDescription:ServerId
    {
        public bool IsAvailable { get; private set; }

        public bool IsServerRestored { get { return RestoreState == RestoreState.Restored; } }

        public RestoreState RestoreState { get; private set; }

        public bool IsRestoreInProcess
        {
            get
            {
                bool result = false;
                string value;
                if (_stateInfo.TryGetValue(ServerState.RestoreInProcess, out value))
                    result = bool.Parse(value);

                if (!result && _stateInfo.TryGetValue(ServerState.RestoreTransferInProcess, out value))
                    result = bool.Parse(value);

                return result;
            }
        }

        public string StateString
        {
            get
            {
                return string.Format("{0}. Restore state: {1}. Available: {2}. {3}", ToString(),
                    Enum.GetName(typeof(RestoreState), RestoreState), IsAvailable, GetInnerState());
            }
        }

        public bool RestoreSendStatus
        {
            get
            {
                string value;
                if (_stateInfo.TryGetValue(ServerState.RestoreSendStatus, out value) && value != string.Empty)
                {
                    return bool.Parse(value);
                }

                return false;
            }

            set { SetInfoMessage(ServerState.RestoreSendStatus, value.ToString()); }
        }

        public WriterDescription(string host,  int port)
            : base(host,  port)
        {
            IsAvailable = true;
            RestoreState = RestoreState.Restored;
        }

        public WriterDescription(ServerId server) : this(server.RemoteHost, server.Port)
        {            
        }

        private readonly ConcurrentDictionary<string, string> _stateInfo = new ConcurrentDictionary<string, string>();

        private string GetInnerState()
        {
            var keys = _stateInfo.Keys;
            string result = string.Empty;
            
            foreach (var key in keys)
            {
                string value;
                if (_stateInfo.TryGetValue(key, out value))
                    result += string.Format(". {0}: {1}", key, value);
            }

            return result;
        }

        public void NotAvailable()
        {
            IsAvailable = false;
            UpdateState(RestoreState.SimpleRestoreNeed);
        }

        public void Available()
        {
            IsAvailable = true;
        }

        public void UpdateModel()
        {
            UpdateState(RestoreState.FullRestoreNeed);
        }

        public void UpdateState(RestoreState state)
        {
            switch (RestoreState)
            {
                case RestoreState.Restored:
                case RestoreState.SimpleRestoreNeed:
                    RestoreState = state;
                    break;
                case RestoreState.FullRestoreNeed:
                    if (state != RestoreState.SimpleRestoreNeed)
                        RestoreState = state;
                    break;
            }
        }

        public void SetInfoMessage(string tag, string message)
        {
            _stateInfo.AddOrUpdate(tag, message, (key, oldValue) => message);
        }

        public void SetInfoMessageList(Dictionary<string, string> info)
        {
            foreach (var record in info)
            {
                SetInfoMessage(record.Key, record.Value);
            }

            ClearInfoMessages(info);
        }

        private void ClearInfoMessages(Dictionary<string, string> info)
        {
            var keys = _stateInfo.Keys.ToList();

            foreach (var key in keys)
            {
                if (!info.ContainsKey(key))
                {
                    string value;
                    _stateInfo.TryRemove(key, out value);
                }
            }
        }
    }
}
